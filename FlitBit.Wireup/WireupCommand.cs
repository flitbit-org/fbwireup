#region COPYRIGHT© 2009-2012 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion


using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace FlitBit.Wireup
{
	/// <summary>
	/// Abstract wireup command.
	/// </summary>
	public abstract class WireupCommand : IWireupCommand
	{
		/// <summary>
		/// Executes the wireup command.
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1033")]
		void IWireupCommand.Execute(IWireupCoordinator coordinator)
		{
			PerformWireup(coordinator);
		}

		/// <summary>
		/// Called by the base class upon execute. Derived classes should 
		/// provide an implementation that performs the wireup logic.
		/// </summary>
		protected abstract void PerformWireup(IWireupCoordinator coordinator);
	}
}