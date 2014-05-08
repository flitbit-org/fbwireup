#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Diagnostics.Contracts;

namespace FlitBit.Wireup.Meta
{
	/// <summary>
	///   Attribute declaring a wireup dependance on another type (a "reliant" type).
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true)]
	public sealed class WireupDependencyAttribute : Attribute
	{
		/// <summary>
		///   Createas a new WireupDependency and initializes it with a type upon which the
		///   current element is dependent.
		/// </summary>
		/// <param name="target">The type upon which the attribute target is dependent</param>
		public WireupDependencyAttribute(Type target)
			: this(WireupPhase.Dependencies, target)
		{
			Contract.Requires<ArgumentNullException>(target != null);
		}

		/// <summary>
		///   Createas a new WireupDependency and initializes it with a type upon which the
		///   current element is dependent.
		/// </summary>
		/// <param name="phase">the wireup phase in which the dependency is resolved</param>
		/// <param name="target">The type upon which the attribute target is dependent</param>
		public WireupDependencyAttribute(WireupPhase phase, Type target)
		{
			Contract.Requires<ArgumentNullException>(target != null);

			this.Phase = phase;
			this.TargetType = target;
		}

		/// <summary>
		///   Indicates the wireup phase.
		/// </summary>
		public WireupPhase Phase { get; private set; }

		/// <summary>
		///   The target of the dependency.
		/// </summary>
		public Type TargetType { get; private set; }
	}
}