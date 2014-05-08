#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;
using System.Diagnostics.Contracts;
using FlitBit.Core;
using FlitBit.Wireup.Meta;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Wireup record for dependencies.
	/// </summary>
	public class WiredDependency : WireupRecord
	{
		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="where"></param>
		/// <param name="attr"></param>
		public WiredDependency(WireupRecord where, WireupDependencyAttribute attr)
			: base(where.Context)
		{
			Contract.Requires<ArgumentNullException>(where != null);

			this.Phase = attr.Phase;
			this.TargetType = attr.TargetType;
			this.Description = String.Concat("Dependency: ", this.TargetType.GetReadableFullName());
		}

		/// <summary>
		///   Wireup phase in which the dependency is wired.
		/// </summary>
		public WireupPhase Phase { get; protected set; }

		/// <summary>
		///   The target type.
		/// </summary>
		public Type TargetType { get; protected set; }

		/// <summary>
		///   Special handling for the Wireup phase.
		/// </summary>
		/// <param name="coordinator"></param>
		/// <param name="context"></param>
		protected override void OnWireup(IWireupCoordinator coordinator, WireupContext context)
		{
			context.PerformWireupType(coordinator, TargetType);
			base.OnWireup(coordinator, context);
		}
	}
}