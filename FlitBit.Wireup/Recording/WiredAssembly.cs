using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using FlitBit.Core;
using FlitBit.Wireup.Meta;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Records assembly wireup dependencies and processing results.
	/// </summary>
	[Serializable]
	public class WiredAssembly : WireupRecord
	{
		static readonly AssemblyName WireupAssemblyName = Assembly.GetExecutingAssembly()
																															.GetName();

		readonly ConcurrentDictionary<object, Tuple<int, WiredAssembly>> _assemblies =
			new ConcurrentDictionary<object, Tuple<int, WiredAssembly>>();

		readonly ConcurrentDictionary<string, WiredModule> _modules = new ConcurrentDictionary<string, WiredModule>();

		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="asm"></param>
		public WiredAssembly(WireupContext context, Assembly asm)
			: base(context)
		{
			Contract.Requires<ArgumentNullException>(context != null);
			Contract.Requires<ArgumentNullException>(asm != null);

			this.FullName = asm.FullName;
			this.AssemblyName = asm.GetName();
			this.Description = String.Concat("Assembly: ", FullName);

			this.HasDeclarations = asm == typeof(WiredAssembly).Assembly
				|| asm.GetReferencedAssemblies()
							.Any(an => String.Equals(an.Name, WireupAssemblyName.Name, StringComparison.InvariantCulture));
			if (this.HasDeclarations)
			{
				this.WireupDeclarations = asm.GetCustomAttributes(typeof(WireupAttribute), false)
																		.Cast<WireupAttribute>()
																		.ToArray();
				this.DeclarationsOnly = this.WireupDeclarations.Any(w => w.Behaviors == WireupBehaviors.DeclarationsOnly);
				this.Dependencies = asm.GetCustomAttributes(typeof(WireupDependencyAttribute), false)
															.Cast<WireupDependencyAttribute>()
															.Select(dep => new WiredDependency(this, dep))
															.ToArray();
				this.Tasks = asm.GetCustomAttributes(typeof(WireupTaskAttribute), false)
												.Cast<WireupTaskAttribute>()
												.Select(task => new WiredTask(this, task, null))
												.ToArray();
			}
		}

		/// <summary>
		///   Gets assembly dependencies.
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
		///   The assembly's name.
		/// </summary>
		public AssemblyName AssemblyName { get; private set; }

		/// <summary>
		///   The assembly's full name.
		/// </summary>
		public string FullName { get; private set; }

		/// <summary>
		///   Indicates whether the assembly has declarations.
		/// </summary>
		public bool HasDeclarations { get; private set; }

		/// <summary>
		///   Gets module dependencies.
		/// </summary>
		public IEnumerable<WiredModule> Modules { get { return _modules.Values.ToReadOnly(); } }

		/// <summary>
		///   Event dispatcher for wireup phase events. Specialized to process modules.
		/// </summary>
		/// <param name="coordinator">The coordinator.</param>
		/// <param name="context">The context.</param>
		/// <param name="phase">The phase.</param>
		protected override void OnWireupPhaseEvent(IWireupCoordinator coordinator, WireupContext context, WireupPhase phase)
		{
			foreach (var m in Modules)
			{
				m.PerformWireupPhase(coordinator, context, phase);
			}
			base.OnWireupPhaseEvent(coordinator, context, phase);
		}

		internal void PerformWireup(IWireupCoordinator coordinator, WireupContext context, Assembly asm)
		{
			Contract.Requires<ArgumentNullException>(coordinator != null);
			Contract.Requires<ArgumentNullException>(context != null);
			Contract.Requires<ArgumentNullException>(asm != null);
			Contract.Requires<ArgumentException>(String.Equals(asm.FullName, this.FullName, StringComparison.InvariantCulture));

			// Discovery...
			if (!DeclarationsOnly)
			{
				// Assemblies may have more than one module.
				foreach (var module in asm.GetModules(false))
				{
					var mod = new WiredModule(context, this, module);
					if (_modules.TryAdd(mod.FullyQualifiedName, mod))
					{
						mod.PerformImmediatePhase(coordinator, context);
						mod.WireupTypes(coordinator, context, module);

					}
				}
			}
			this.PerformWireupPhases(coordinator, context);
		}

		internal WiredType PerformWireupType(IWireupCoordinator coordinator, WireupContext context, Type type)
		{
			Contract.Requires<ArgumentNullException>(coordinator != null);
			Contract.Requires<ArgumentNullException>(context != null);
			Contract.Requires<ArgumentNullException>(type != null);
			Contract.Requires<ArgumentException>(String.Equals(type.Assembly.FullName, this.FullName,
																												StringComparison.InvariantCulture));

			Contract.Ensures(Contract.Result<WiredType>() != null);

			var module =
				_modules.Values.First(
														 m =>
															String.Equals(type.Module.FullyQualifiedName, m.FullyQualifiedName, StringComparison.InvariantCulture));
			return module.PerformWireupType(coordinator, context, type);
		}

		internal WiredAssembly WireupDependency(IWireupCoordinator coordinator, Assembly asm)
		{
			var key = asm.GetKeyForAssembly();
			Tuple<int, WiredAssembly> wired;
			if (!_assemblies.TryGetValue(key, out wired))
			{
				wired = Tuple.Create(_assemblies.Count, Context.WireupDependency(coordinator, asm));
				return _assemblies.GetOrAdd(key, wired)
													.Item2;
			}
			return wired.Item2;
		}
	}
}