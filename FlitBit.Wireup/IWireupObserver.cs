#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;
using FlitBit.Wireup.Meta;

namespace FlitBit.Wireup
{
	/// <summary>
	/// Interface for wireup observers. Wireup coordinators will notify observers of 
	/// tasks and dependencies having a matching observer key.
	/// </summary>
	public interface IWireupObserver
	{
		/// <summary>
		/// Gets the observer's key.
		/// </summary>
		Guid ObserverKey { get; }
		/// <summary>
		/// Called by coordinators to notify observers of wireup tasks.
		/// </summary>
		/// <param name="coordinator"></param>
		/// <param name="task"></param>
		/// <param name="target"></param>
		void NotifyWireupTask(IWireupCoordinator coordinator, WireupTaskAttribute task, Type target);
	}
}
