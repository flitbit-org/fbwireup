using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using FlitBit.Core;
using FlitBit.Core.Parallel;
using FlitBit.Wireup.Meta;
using FlitBit.Wireup.Properties;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Collects and carries context information related to an operation against wireup.
	/// </summary>
	public class WireupContext : Disposable, IParallelShared
	{
		static int __idSeed;

		readonly ConcurrentDictionary<object, Tuple<int, WiredAssembly>> _assemblies =
			new ConcurrentDictionary<object, Tuple<int, WiredAssembly>>();

		int _disposers;

		Assembly _initiator;
		WiredAssembly _wired;

		/// <summary>
		///   Creates a new instance.
		/// </summary>
		public WireupContext()
		{
			this.ID = Interlocked.Increment(ref __idSeed);
			Interlocked.Increment(ref _disposers);
			ContextFlow.Push(this);
			Sequence = new WireupProcessingSequence();
		}

		/// <summary>
		///   The assemblies wired up in the context.
		/// </summary>
		public IEnumerable<WiredAssembly> Assemblies
		{
			get
			{
				return _assemblies.Values.OrderBy(tuple => tuple.Item1)
													.Select(tuple => tuple.Item2);
			}
		}

		/// <summary>
		///   The context's ID.
		/// </summary>
		public int ID { get; private set; }

		/// <summary>
		///   Indicates whether the context has been initialized.
		/// </summary>
		public bool IsInitialized { get { return _initiator != null; } }

		/// <summary>
		///   Proccessing sequence.
		/// </summary>
		public WireupProcessingSequence Sequence { get; private set; }

		/// <summary>
		///   Performs the dispose logic.
		/// </summary>
		/// <param name="disposing">Whether the object is disposing (IDisposable.Dispose method was called).</param>
		/// <returns>
		///   Implementers should return true if the disposal was successful; otherwise false.
		/// </returns>
		protected override bool PerformDispose(bool disposing)
		{
			if (disposing && Interlocked.Decrement(ref _disposers) > 0)
			{
				return false;
			}
			if (disposing && !ContextFlow.TryPop(this))
			{
				// Notify the caller that they are calling dispose out of order.
				// This never happens if the caller uses a 'using' 
				const string message =
					"WireupContext disposed out of order. To eliminate this possibility always wrap its use in a `using` clause.";
				try
				{
					LogSink.OnTraceEvent(this, TraceEventType.Warning, message);
				}
					// ReSharper disable EmptyGeneralCatchClause
				catch
					// ReSharper restore EmptyGeneralCatchClause
				{
					/* safety net, intentionally eat the since we might be in GC thread */
				}
			}
			return true;
		}

		internal void InitialAssembly(IWireupCoordinator coordinator, Assembly assembly)
		{
			Contract.Requires<ArgumentNullException>(coordinator != null);
			Contract.Requires<ArgumentNullException>(assembly != null);

			this.Sequence.BeginScope();
			this.Sequence.Push(String.Concat("Wireup context initiated to wire assembly: ", assembly.FullName, "."));
			_initiator = assembly;
			_wired = coordinator.WireupDependencies(this, assembly);
			_assemblies.TryAdd(assembly.GetKeyForAssembly(), Tuple.Create(_assemblies.Count, _wired));
		}

		internal void InitialType(IWireupCoordinator coordinator, Type type)
		{
			Contract.Requires<ArgumentNullException>(coordinator != null);
			Contract.Requires<ArgumentNullException>(type != null);

			this.Sequence.BeginScope();
			this.Sequence.Push(String.Concat("Wireup context initiated to wire type: ", type.GetReadableFullName(), "."));
			_initiator = type.Assembly;
			_wired = coordinator.WireupDependencies(this, type.Assembly);
			_assemblies.TryAdd(type.Assembly.GetKeyForAssembly(), Tuple.Create(_assemblies.Count, _wired));
			PerformWireupType(coordinator, type);
		}

		internal WiredType PerformWireupType(IWireupCoordinator coordinator, Type type)
		{
			Contract.Requires<InvalidOperationException>(IsInitialized);
			Contract.Requires<ArgumentNullException>(type != null);

			// Types may have dependencies declared; wire them first.
			foreach (WireupDependencyAttribute d in type.GetCustomAttributes(typeof(WireupDependencyAttribute), false))
			{
				var r = d.TargetType;
				coordinator.WireupDependencies(this, r.Assembly);
			}
			var wiredAsm = WireupDependency(coordinator, type.Assembly);
			return wiredAsm.PerformWireupType(coordinator, this, type);
		}

		internal WiredAssembly WireupDependency(IWireupCoordinator coordinator, Assembly asm)
		{
			Contract.Requires<InvalidOperationException>(IsInitialized);

			var key = asm.GetKeyForAssembly();
			Tuple<int, WiredAssembly> wired;
			if (!_assemblies.TryGetValue(key, out wired))
			{
				Sequence.Push(String.Concat("Wiring dependency: ", asm.FullName));
				wired = Tuple.Create(_assemblies.Count, coordinator.WireupDependencies(this, asm));
				var final = _assemblies.GetOrAdd(key, wired);
				if (!ReferenceEquals(final, wired))
				{
					Sequence.Push(String.Concat("Dependency already wired: ", asm.FullName));
				}
				return final.Item2;
			}
			return wired.Item2;
		}

		#region IParallelShared Members

		/// <summary>
		///   Prepares the instance for sharing across threads.
		///   This call should be wrapped in a 'using clause' to
		///   ensure proper cleanup of both the shared and the original.
		/// </summary>
		/// <returns>
		///   An equivalent instance.
		/// </returns>
		public object ParallelShare()
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(Resources.Err_NotInitialized);
			}
			if (IsDisposed)
			{
				throw new ObjectDisposedException(typeof(WireupContext).FullName);
			}

			Interlocked.Increment(ref _disposers);
			return this;
		}

		#endregion

		/// <summary>
		///   Gets the current "ambient" wireup context.
		/// </summary>
		public static WireupContext Current
		{
			get
			{
				WireupContext ambient;
				return (ContextFlow.TryPeek(out ambient)) ? ambient : default(WireupContext);
			}
		}

		/// <summary>
		///   Shares the ambient context if it exists; otherwise, creates a new one.
		/// </summary>
		/// <returns>a context</returns>
		public static WireupContext NewOrShared(IWireupCoordinator coordinator, Action<WireupContext> init)
		{
			WireupContext ambient;
			if (ContextFlow.TryPeek(out ambient))
			{
				return (WireupContext) ambient.ParallelShare();
			}
			ambient = new WireupContext();
			if (init != null)
			{
				init(ambient);
			}
			return ambient;
		}
	}
}