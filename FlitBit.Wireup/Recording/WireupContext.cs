#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using FlitBit.Core;
using FlitBit.Core.Log;
using FlitBit.Core.Parallel;
using FlitBit.Wireup.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Collects and carries context information related to an operation against wireup.
	/// </summary>
	public class WireupContext : Disposable
	{
    static readonly ILogSink LogSink = typeof(WireupContext).GetLogSink();
		static int __idSeed;

    internal class WireupContextFlowProvider : IContextFlowProvider
    {
      static readonly Lazy<WireupContextFlowProvider> Provider =
        new Lazy<WireupContextFlowProvider>(CreateAndRegisterContextFlowProvider, LazyThreadSafetyMode.ExecutionAndPublication);

      static WireupContextFlowProvider CreateAndRegisterContextFlowProvider()
      {
        var res = new WireupContextFlowProvider();
        ContextFlow.RegisterProvider(res);
        return res;
      }

      [ThreadStatic]
      static Stack<WireupContext> __scopes;

      public WireupContextFlowProvider()
      {
        this.ContextKey = Guid.NewGuid();
      }

      public Guid ContextKey
      {
        get;
        private set;
      }

      public object Capture()
      {
        var top = Peek();
        if (top != null)
        {
          return top.ParallelShare();
        }
        return null;
      }

      public void Attach(ContextFlow context, object captureKey)
      {
        var scope = (captureKey as WireupContext);
        if (scope != null)
        {
          if (__scopes == null)
          {
            __scopes = new Stack<WireupContext>();
          }
          if (__scopes.Count > 0)
          {
            ReportAndClearOrphanedScopes(__scopes);
          }
          __scopes.Push(scope);
        }
      }

      private void ReportAndClearOrphanedScopes(Stack<WireupContext> scopes)
      {
        scopes.Clear();
      }

      public void Detach(ContextFlow context, object captureKey)
      {
        var scope = (captureKey as WireupContext);
        if (scope != null)
        {
          scope.Dispose();
        }
      }

      internal static void Push(WireupContext scope)
      {
        var dummy = Provider.Value;
        if (__scopes == null)
        {
          __scopes = new Stack<WireupContext>();
        }
        __scopes.Push(scope);
      }

      internal static bool TryPop(WireupContext scope)
      {
        if (__scopes != null && __scopes.Count > 0)
        {
          if (ReferenceEquals(__scopes.Peek(), scope))
          {
            __scopes.Pop();
            return true;
          }
        }
        return false;
      }

      internal static WireupContext Pop()
      {
        if (__scopes != null && __scopes.Count > 0)
        {
          return __scopes.Pop();
        }
        return default(WireupContext);
      }


      internal static WireupContext Peek()
      {
        if (__scopes != null && __scopes.Count > 0)
        {
          return __scopes.Peek();
        }
        return default(WireupContext);
      }
    }


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
			WireupContextFlowProvider.Push(this);
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
			if (disposing && !WireupContextFlowProvider.TryPop(this))
			{
				// Notify the caller that they are calling dispose out of order.
				// This never happens if the caller uses a 'using' 
				const string message =
					"WireupContext disposed out of order. To eliminate this possibility always wrap its use in a `using` clause.";
				try
				{
          LogSink.Warning(message);
				}
				catch
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

			var asm = WireupDependency(coordinator, type.Assembly);
			return asm.PerformWireupType(coordinator, this, type);
		}

		internal WiredAssembly WireupDependency(IWireupCoordinator coordinator, Assembly asm)
		{
			Contract.Requires<InvalidOperationException>(IsInitialized);

			var key = asm.GetKeyForAssembly();
			Tuple<int, WiredAssembly> wired;
			if (!_assemblies.TryGetValue(key, out wired))
			{
				if (asm.FullName != _initiator.FullName)
				{
					Sequence.Push(String.Concat("Wiring dependency: ", asm.FullName));
				}
				wired = Tuple.Create(_assemblies.Count, coordinator.WireupDependencies(this, asm));
				var final = _assemblies.GetOrAdd(key, wired);
				if (!ReferenceEquals(final, wired) && asm.FullName != _initiator.FullName)
				{
					Sequence.Push(String.Concat("Dependency already wired: ", asm.FullName));
				}
				return final.Item2;
			}
			wired.Item2.SubsequentWireupAssembly(coordinator, this, asm);
			return wired.Item2;
		}

		WireupContext ParallelShare()
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

		/// <summary>
		///   Gets the current "ambient" wireup context.
		/// </summary>
		public static WireupContext Current
		{
			get
			{
			  return WireupContextFlowProvider.Peek();
			}
		}

		/// <summary>
		///   Shares the ambient context if it exists; otherwise, creates a new one.
		/// </summary>
		/// <returns>a context</returns>
		public static WireupContext NewOrShared(IWireupCoordinator coordinator, Action<WireupContext> init)
		{
		  var ambient = WireupContextFlowProvider.Peek();
      if (ambient != null)
			{
				return ambient.ParallelShare();
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