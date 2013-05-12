#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FlitBit.Core;
using FlitBit.Wireup.Recording;

namespace FlitBit.Wireup
{
	internal sealed class DefaultWireupCoordinator : IWireupCoordinator
	{
		readonly ConcurrentDictionary<object, WiredAssembly> _assemblies =
			new ConcurrentDictionary<object, WiredAssembly>();

		readonly ConcurrentDictionary<int, WireupContext> _contexts = new ConcurrentDictionary<int, WireupContext>();

		readonly Object _lock = new Object();

		readonly ConcurrentDictionary<Guid, IWireupObserver> _observers = new ConcurrentDictionary<Guid, IWireupObserver>();

		WiredAssembly PerformWireupDependencies(WireupContext context, Assembly asm)
		{
			var key = asm.GetKeyForAssembly();
			WiredAssembly wired;
			if (!_assemblies.TryGetValue(key, out wired))
			{
				wired = new WiredAssembly(context, asm);
				var concurrent = _assemblies.GetOrAdd(key, wired);
				if (ReferenceEquals(wired, concurrent))
				{
					wired.FirstWireupAssembly(this, context, asm);
				}
				else
				{
					concurrent.SubsequentWireupAssembly(this, context, asm);
				}
				return concurrent;
			}
			wired.SubsequentWireupAssembly(this, context, asm);
			return wired;
		}

		WiredType PerformWireupDependencies(WireupContext context, Type type)
		{
			var asm = type.Assembly;
			var key = asm.GetKeyForAssembly();
			WiredAssembly wired;
			if (!_assemblies.TryGetValue(key, out wired))
			{
				wired = new WiredAssembly(context, asm);
				var concurrent = _assemblies.GetOrAdd(key, wired);
				if (ReferenceEquals(wired, concurrent) && wired.HasDeclarations)
				{
					wired.PerformImmediatePhase(this, context);
					wired.PerformWireup(this, context, asm);
				}
				wired = concurrent;
			}
			return wired.PerformWireupType(this, context, type);
		}

		#region IWireupCoordinator Members

		public WiredType WireupDependency(WireupContext context, Type type)
		{
			if (context != null)
			{
				_contexts.TryAdd(context.ID, context);
				return PerformWireupDependencies(context, type);
			}
			using (var myContext = WireupContext.NewOrShared(this, c => c.InitialType(this, type)))
			{
				_contexts.TryAdd(myContext.ID, myContext);
				return PerformWireupDependencies(myContext, type);
			}
		}

		/// <summary>
		///   Creates a string reporting of the wireup history.
		/// </summary>
		/// <returns></returns>
		public string ReportWireupHistory()
		{
			var buffer = new StringBuilder(4000);
			foreach (var h in this.ContextHistory)
			{
				foreach (var r in h.Sequence.Records)
				{
					buffer.Append(Environment.NewLine)
								.Append("Thread: ")
								.Append(r.ThreadId)
								.Append("; ");
					if (r.Depth > 0)
						buffer.Append(new string('\t', r.Depth));
					buffer.Append(r.Details);
				}
			}
			return buffer.ToString();
		}

		public WiredAssembly WireupDependencies(WireupContext context, Assembly asm)
		{
			if (context != null)
			{
				_contexts.TryAdd(context.ID, context);
				return PerformWireupDependencies(context, asm);
			}
			using (var myContext = WireupContext.NewOrShared(this, c => c.InitialAssembly(this, asm)))
			{
				_contexts.TryAdd(myContext.ID, myContext);
				return PerformWireupDependencies(myContext, asm);
			}
		}

		public IEnumerable<WireupContext> ContextHistory
		{
			get
			{
				return from c in _contexts.Values
							orderby c.ID
							select c;
			}
		}

		public void RegisterObserver(IWireupObserver observer)
		{
			_observers.TryAdd(observer.ObserverKey, observer);
		}

		public void UnregisterObserver(Guid key)
		{
			IWireupObserver observer;
			_observers.TryRemove(key, out observer);
		}

		public void NotifyAssemblyLoaded(Assembly assembly)
		{
			lock (_lock)
			{
				WireupDependencies(WireupContext.Current, assembly);
			}
		}

		#endregion


		public void NotifyTaskObservers(WireupContext context, Meta.WireupTaskAttribute task, Type targetType)
		{
			if (_observers.Any())
			{
				var targetDesc = (targetType != null) ? targetType.GetReadableFullName() : (string) null;
				context.Sequence.Push(String.Concat("Notifying wireup observers that the task:target pair is being executed: ", task.GetType().GetReadableFullName(), ":", targetDesc));
				context.Sequence.BeginScope();
				try
				{
					foreach (var o in _observers.Values)
					{
						context.Sequence.Push(String.Concat("Notifying wireup observer: ", o.GetType().GetReadableFullName()));
						o.NotifyWireupTask(this, task, targetType);
					}
				}
				finally
				{
					context.Sequence.EndScope();
				}
			}
		}
	}
}