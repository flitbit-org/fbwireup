#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Event arguments for wireup phase events.
	/// </summary>
	public class WireupRecordPhaseEventArgs : EventArgs
	{
		/// <summary>
		///   The wireup context.
		/// </summary>
		public WireupContext Context { get; internal set; }

		/// <summary>
		///   The coordinator.
		/// </summary>
		public IWireupCoordinator Coordinator { get; internal set; }

		/// <summary>
		///   The wireup phase.
		/// </summary>
		public WireupPhase Phase { get; internal set; }
	}
}