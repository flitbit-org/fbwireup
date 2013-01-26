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
		public IEnumerable<AssemblyDependency> WireupDependencies(Assembly asm)
		{
			Contract.Assert(asm != null);

			return PerformWireupDependencies(asm);
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
						var tasks = new List<WireupTaskAttribute>();

						deps.AddRange(asm.GetCustomAttributes(typeof(WireupDependencyAttribute), false).Cast<WireupDependencyAttribute>());
						tasks.AddRange(asm.GetCustomAttributes(typeof(WireupTaskAttribute), false).Cast<WireupTaskAttribute>());

						// Assemblies may have more than one module.
						foreach (var mod in asm.GetModules(false))
						{
							deps.AddRange(mod.GetCustomAttributes(typeof(WireupDependencyAttribute), false).Cast<WireupDependencyAttribute>());
							tasks.AddRange(mod.GetCustomAttributes(typeof(WireupTaskAttribute), false).Cast<WireupTaskAttribute>());
							foreach (var type in mod.GetTypes())
							{
								tasks.AddRange(type.GetCustomAttributes(typeof(WireupTaskAttribute), false).Cast<WireupTaskAttribute>());
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
							foreach (Type t in w.CommandType)
							{
								if (!_types.Contains(t))
									InvokeWireupCommand(t);
							}
						}

						// Phase: AfterWireup
						ProcessPhase(deps, tasks, asm, myDeps, WireupPhase.AfterWireup);

						if (!IsAssemblyWired(asm))
						{
							_asmTracking.Add(asm.GetKeyForAssembly(), myDeps);
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

		private void ProcessPhase(List<WireupDependencyAttribute> deps, List<WireupTaskAttribute> tasks, Assembly asm, List<AssemblyDependency> myDeps, WireupPhase wireupPhase)
		{
			foreach (var d in deps.Where(d => d.Phase == WireupPhase.BeforeDependencies))
			{
				ProcessDependencyTarget(asm, d.TargetType, myDeps);
			}
			foreach (var t in tasks.Where(t => t.Phase == WireupPhase.BeforeDependencies))
			{
				t.ExecuteTask(this);
			}
		}

		private void ProcessDependencyTarget(Assembly asm, Type type, List<AssemblyDependency> myDeps)
		{
			if (type.Assembly != asm && !_types.Contains(type))
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

		public void WireupDependency(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (!typeof(IWireupCommand).IsAssignableFrom(type))
				throw new ArgumentException(Resources.Chk_TypeMustBeAssignableToIWireupCommand, "type");

			if (!_types.Contains(type))
				InvokeWireupCommand(type);
		}

		class CommandTracking
		{
			int _dependencyCount = 1;
			internal CommandTracking(string typeName)
			{
				this.TypeName = typeName;
			}
			public string TypeName { get; private set; }
			public int Increment()
			{
				return Interlocked.Increment(ref _dependencyCount);
			}
		}

		readonly Object _lock = new Object();
		readonly Stack<Assembly> _assemblies = new Stack<Assembly>();
		readonly Stack<Type> _types = new Stack<Type>();
		readonly Dictionary<object, CommandTracking> _wired = new Dictionary<object, CommandTracking>();
		readonly Dictionary<object, IEnumerable<AssemblyDependency>> _asmTracking = new Dictionary<object, IEnumerable<AssemblyDependency>>();

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
						Type r = d.TargetType;
						WireupDependencies(r.Assembly);
					}
					var key = type.GetKeyForType();
					CommandTracking tracking;
					if (_wired.TryGetValue(key, out tracking))
					{
						Contract.Assume(tracking != null);
						tracking.Increment();
					}
					else
					{
						_wired.Add(key, new CommandTracking(type.AssemblyQualifiedName));
						var cmd = (IWireupCommand)Activator.CreateInstance(type);
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

		public IEnumerable<AssemblyDependency> ExposeDependenciesFor(Assembly assem)
		{
			IEnumerable<AssemblyDependency> deps;
			if (_asmTracking.TryGetValue(assem.GetKeyForAssembly(), out deps))
			{
				return deps.ToReadOnly();
			}
			return Enumerable.Empty<AssemblyDependency>();
		}


		ConcurrentDictionary<Guid, IWireupObserver> _observers = new ConcurrentDictionary<Guid, IWireupObserver>();

		public void RegisterObserver(IWireupObserver observer)
		{
			Contract.Requires<ArgumentNullException>(observer != null);
			
			_observers.TryAdd(observer.ObserverKey, observer);
		}

		public void UnregisterObserver(Guid key)
		{
			IWireupObserver observer;
			_observers.TryRemove(key, out observer);
		}
	}

}
