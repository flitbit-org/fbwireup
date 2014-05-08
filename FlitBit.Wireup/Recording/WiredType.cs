#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using FlitBit.Core;
using FlitBit.Wireup.Meta;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Wireup recored for wired types.
	/// </summary>
	public class WiredType : WireupRecord
	{
		Type _workingType;

		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="context">context in which the type is wired.</param>
		/// <param name="module">The wireup record for the module where the type resides.</param>
		/// <param name="type">the type</param>
		public WiredType(WireupContext context, WiredModule module, Type type)
			: base(context)
		{
			Contract.Requires<ArgumentNullException>(type != null);

			this.Module = module;
			this._workingType = type;
			this.AssemblyQualifiedName = type.AssemblyQualifiedName;
			this.Description = String.Concat("Type: ", type.GetReadableFullName());

			this.Dependencies = type.GetCustomAttributes(typeof(WireupDependencyAttribute), false)
															.Cast<WireupDependencyAttribute>()
															.Select(dep => new WiredDependency(this, dep))
															.ToArray();
			this.Tasks = type.GetCustomAttributes(typeof(WireupTaskAttribute), false)
											.Cast<WireupTaskAttribute>()
											.Select(task => new WiredTask(this, task, type))
											.ToArray();
		}

		/// <summary>
		///   The type's assembly qualified name.
		/// </summary>
		public string AssemblyQualifiedName { get; set; }

		/// <summary>
		///   The type's module.
		/// </summary>
		public WiredModule Module { get; private set; }

		/// <summary>
		///   Special handling for the Wireup phase.
		/// </summary>
		/// <param name="coordinator"></param>
		/// <param name="context"></param>
		protected override void OnWireup(IWireupCoordinator coordinator, WireupContext context)
		{
			if (_workingType != null)
			{
				if (typeof(IWireupCommand).IsAssignableFrom(_workingType))
				{
					context.Sequence.Push(String.Concat("Executing IWireupCommand: ", this.Description));
					var cmd = (IWireupCommand) Activator.CreateInstance(_workingType);
					cmd.Execute(coordinator);
				}

				// Ensure it wires only once.
				_workingType = null;
			}
		}
	}
}