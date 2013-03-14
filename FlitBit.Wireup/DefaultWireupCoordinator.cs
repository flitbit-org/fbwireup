#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using FlitBit.Core;
using FlitBit.Wireup.Meta;
using FlitBit.Wireup.Properties;

namespace FlitBit.Wireup
{
	internal sealed class DefaultWireupCoordinator : IWireupCoordinator
	{
		readonly ConcurrentDictionary<object, IEnumerable<AssemblyDependency>> _asmTracking =
			new ConcurrentDictionary<object, IEnumerable<AssemblyDependency>>();

		readonly Stack<Assembly> _assemblies = new Stack<Assembly>();
		readonly Object _lock = new Object();
		readonly Stack<Type> _types = new Stack<Type>();
		readonly ConcurrentDictionary<object, CommandTracking> _wired = new ConcurrentDictionary<object, CommandTracking>();
		readonly ConcurrentDictionary<Guid, IWireupObserver> _observers = new ConcurrentDictionary<Guid, IWireupObserver>();

		void AddDependencies(Assembly asm, List<AssemblyDependency> deps, List<WireupDependencyAttribute> attrs,
			IEnumerable<object> dependencies)
		{
			foreach (var dep in dependencies.Cast<WireupDependencyAttribute>())
			{
				if (dep.Phase == WireupPhase.Immediate)
				{
					ProcessDependencyTarget(asm, dep.TargetType, deps);
				}
				else
				{
					attrs.Add(dep);
				}
			}
		}

		void AddTasks(Type target, List<WireupTask> attrs, IEnumerable<object> dependencies)
		{
			foreach (var task in dependencies.Cast<WireupTaskAttribute>())
			{
				if (task.Phase == WireupPhase.Immediate)
				{
					ProcessTask(new WireupTask(target, task));
				}
				else
				{
					attrs.Add(new WireupTask(target, task));
				}
			}
		}

		void InvokeWireupCommand(Type type)
		{
			Contract.Requires<ArgumentNullException>(type != null);
			Contract.Assume(_types != null);
			Contract.Assume(_asmTracking != null);

			if (!_types.Contains(type))
			{
				_types.Push(type);
				try
				{
					// Assemblies may have dependencies declared; wire them first.
					foreach (WireupDependencyAttribute d in type.GetCustomAttributes(typeof(WireupDependencyAttribute), false))
					{
						var r = d.TargetType;
						WireupDependencies(r.Assembly);
					}
					var key = type.GetKeyForType();
					CommandTracking ours = null;
					var tracking = _wired.GetOrAdd(key, k =>
					{
						ours = new CommandTracking();
						return ours;
					});
					tracking.Increment();
					if (ReferenceEquals(ours, tracking))
					{
						var cmd = (IWireupCommand) Activator.CreateInstance(type);
						cmd.Execute(this);
					}
				}
				finally
				{
					_types.Pop();
				}
			}
		}

		bool IsAssemblyWired(Assembly asm)
		{
			Contract.Requires<ArgumentNullException>(asm != null);
			Contract.Assume(_asmTracking != null);
			return _asmTracking.ContainsKey(asm.GetKeyForAssembly());
		}

		IEnumerable<AssemblyDependency> PerformWireupDependencies(Assembly asm)
		{
			Contract.Assert(asm != null);
			var myDeps = new List<AssemblyDependency>();
			lock (_lock)
			{
				// The stacks are used to avoid cycles among the dependency declarations.
				if (!IsAssemblyWired(asm) && !_assemblies.Contains(asm))
				{
					_assemblies.Push(asm);
					try
					{
						var deps = new List<WireupDependencyAttribute>();
						var tasks = new List<WireupTask>();

						AddDependencies(asm, myDeps, deps, asm.GetCustomAttributes(typeof(WireupDependencyAttribute), false));
						AddTasks(null, tasks, asm.GetCustomAttributes(typeof(WireupTaskAttribute), false));

						// Assemblies may have more than one module.
						foreach (var mod in asm.GetModules(false))
						{
							AddDependencies(asm, myDeps, deps, mod.GetCustomAttributes(typeof(WireupDependencyAttribute), false));
							AddTasks(null, tasks, mod.GetCustomAttributes(typeof(WireupTaskAttribute), false));

							foreach (var type in mod.GetTypes())
							{
								AddTasks(type, tasks, type.GetCustomAttributes(typeof(WireupTaskAttribute), false));
							}
						}

						// Phase: BeforeDependencies
						ProcessPhase(deps, tasks, asm, myDeps, WireupPhase.BeforeDependencies);

						// Phase: Dependencies
						ProcessPhase(deps, tasks, asm, myDeps, WireupPhase.Dependencies);

						// Phase: BeforeTasks
						ProcessPhase(deps, tasks, asm, myDeps, WireupPhase.BeforeTasks);

						// Phase: Tasks
						ProcessPhase(deps, tasks, asm, myDeps, WireupPhase.Tasks);

						// Phase: BeforeWireup
						ProcessPhase(deps, tasks, asm, myDeps, WireupPhase.BeforeWireup);

						// Execute the wireup commands declared for the assembly...
						foreach (WireupAttribute w in asm.GetCustomAttributes(typeof(WireupAttribute), false))
						{
							foreach (var t in w.CommandType)
							{
								if (!_types.Contains(t))
								{
									InvokeWireupCommand(t);
								}
							}
						}

						// Phase: AfterWireup
						ProcessPhase(deps, tasks, asm, myDeps, WireupPhase.AfterWireup);

						if (!IsAssemblyWired(asm))
						{
							_asmTracking.TryAdd(asm.GetKeyForAssembly(), myDeps);
						}
					}
					finally
					{
						_assemblies.Pop();
					}
				}
			}
			return myDeps.ToReadOnly();
		}

		void ProcessDependencyTarget(Assembly asm, Type type, List<AssemblyDependency> myDeps)
		{
			if (!_types.Contains(type))
			{
				var asmName = asm.GetName();
				var dep = new AssemblyDependency(asmName.Name, String.Concat("v", asmName.Version.ToString()));
				if (!myDeps.Contains(dep))
				{
					myDeps.Add(dep);
					myDeps.AddRange(PerformWireupDependencies(type.Assembly));
				}
				InvokeWireupCommand(type);
			}
		}

		void ProcessPhase(IEnumerable<WireupDependencyAttribute> deps, IEnumerable<WireupTask> tasks, Assembly asm,
			List<AssemblyDependency> myDeps, WireupPhase wireupPhase)
		{
			foreach (var d in deps.Where(d => d.Phase == wireupPhase))
			{
				ProcessDependencyTarget(asm, d.TargetType, myDeps);
			}
			foreach (var t in tasks.Where(t => t.Task.Phase == wireupPhase))
			{
				ProcessTask(t);
			}
		}

		void ProcessTask(WireupTask task)
		{
			task.Task.ExecuteTask(this);
			foreach (var observer in _observers.Values)
			{
				observer.NotifyWireupTask(this, task.Task, task.Target);
			}
		}

		#region IWireupCoordinator Members

		public IEnumerable<AssemblyDependency> WireupDependencies(Assembly asm)
		{
			Contract.Assert(asm != null);

			return PerformWireupDependencies(asm);
		}

		public void WireupDependency(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (!typeof(IWireupCommand).IsAssignableFrom(type))
			{
				throw new ArgumentException(Resources.Chk_TypeMustBeAssignableToIWireupCommand, "type");
			}

			if (!_types.Contains(type))
			{
				InvokeWireupCommand(type);
			}
		}

		public IEnumerable<AssemblyDependency> ExposeDependenciesFor(Assembly assem)
		{
			IEnumerable<AssemblyDependency> deps;
			if (_asmTracking.TryGetValue(assem.GetKeyForAssembly(), out deps))
			{
				return deps.ToReadOnly();
			}
			return Enumerable.Empty<AssemblyDependency>();
		}

		public void RegisterObserver(IWireupObserver observer) { _observers.TryAdd(observer.ObserverKey, observer); }

		public void UnregisterObserver(Guid key)
		{
			IWireupObserver observer;
			_observers.TryRemove(key, out observer);
		}

		public void NotifyAssemblyLoaded(Assembly assembly)
		{
			lock (_lock)
			{
				WireupDependencies(assembly);
			}
		}

		#endregion

		class CommandTracking
		{
			int _dependencyCount = 1;
			public void Increment() {
				Interlocked.Increment(ref this._dependencyCount);
			}
		}

		class WireupTask
		{
			internal WireupTask(Type target, WireupTaskAttribute attr)
			{
				this.Target = target;
				this.Task = attr;
			}

			public Type Target { get; private set; }
			public WireupTaskAttribute Task { get; private set; }
		}
	}
}