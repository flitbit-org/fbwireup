#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   A recorded exception during wireup.
	/// </summary>
	[Serializable]
	public class WireupRecordingException : Exception
	{
		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="record"></param>
		public WireupRecordingException(WireupErrorRecord record)
			: base(record.Describe(false), record.Cause)
		{
			this.ErrorRecord = record;
		}

		/// <summary>
		///   An error record describing the source of the exception.
		/// </summary>
		public WireupErrorRecord ErrorRecord { get; private set; }
	}
}