#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using FlitBit.Wireup.Meta;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Wireup record for modules.
	/// </summary>
	public class WiredModule : WireupRecord
	{
		readonly ConcurrentDictionary<string, WiredType> _types = new ConcurrentDictionary<string, WiredType>();

		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="assembly">The module's assembly</param>
		/// <param name="module">The module</param>
		public WiredModule(WireupContext context, WiredAssembly assembly, Module module)
			: base(context)
		{
			Contract.Requires<ArgumentNullException>(context != null);
			Contract.Requires<ArgumentNullException>(assembly != null);
			Contract.Requires<ArgumentNullException>(module != null);

			this.Assembly = assembly;
			this.FullyQualifiedName = module.FullyQualifiedName;
			this.Description = String.Concat("Module: ", module.Name, " (from ", assembly.AssemblyName.Name, ")");

			this.WireupDeclarations = module.GetCustomAttributes(typeof(WireupAttribute), false)
																			.Cast<WireupAttribute>()
																			.ToArray();
			this.DeclarationsOnly = this.WireupDeclarations.Any(w => w.Behaviors == WireupBehaviors.DeclarationsOnly);
			this.Dependencies = module.GetCustomAttributes(typeof(WireupDependencyAttribute), false)
																.Cast<WireupDependencyAttribute>()
																.Select(dep => new WiredDependency(this, dep))
																.ToArray();
			this.Tasks = module.GetCustomAttributes(typeof(WireupTaskAttribute), false)
												.Cast<WireupTaskAttribute>()
												.Select(task => new WiredTask(this, task, null))
												.ToArray();
		}

		/// <summary>
		///   The module's assembly.
		/// </summary>
		public WiredAssembly Assembly { get; private set; }

		/// <summary>
		///   The module's fully qualified name.
		/// </summary>
		public string FullyQualifiedName { get; private set; }

		/// <summary>
		///   Special handling for the Wireup phase.
		/// </summary>
		/// <param name="coordinator"></param>
		/// <param name="context"></param>
		protected override void OnWireup(IWireupCoordinator coordinator, WireupContext context)
		{
			foreach (var typ in _types.Values)
			{
				typ.PerformWireupPhases(coordinator, context);
			}
			base.OnWireup(coordinator, context);
		}

		internal WiredType PerformWireupType(IWireupCoordinator coordinator, WireupContext context, Type type)
		{
			Contract.Requires<ArgumentNullException>(coordinator != null);
			Contract.Requires<ArgumentNullException>(context != null);
			Contract.Requires<ArgumentNullException>(type != null);
			Contract.Requires<ArgumentException>(String.Equals(type.Module.FullyQualifiedName, this.FullyQualifiedName,
																												StringComparison.InvariantCulture));

			Contract.Ensures(Contract.Result<WiredType>() != null);

			WiredType wired;
			if (!_types.TryGetValue(type.FullName, out wired))
			{
				wired = new WiredType(context, this, type);
				if (_types.TryAdd(type.FullName, wired))
				{
					wired.PerformWireupPhases(coordinator, context);
				}
			}
			return wired;
		}

		internal void WireupTypes(IWireupCoordinator coordinator, WireupContext context, Module mod)
		{
			Contract.Requires<ArgumentNullException>(mod != null);
			Contract.Requires<ArgumentNullException>(context != null);
			Contract.Requires<ArgumentException>(String.Equals(mod.FullyQualifiedName, this.FullyQualifiedName,
																												StringComparison.InvariantCulture));

			if (!DeclarationsOnly)
			{
				foreach (var type in mod.GetTypes())
				{
					if (type.IsDefined(typeof(WireupDependencyAttribute), true)
						|| type.IsDefined(typeof(WireupTaskAttribute), true))
					{
						var typ = new WiredType(context, this, type);
						_types.TryAdd(type.FullName, typ);
					}
				}
			}
		}
	}
}