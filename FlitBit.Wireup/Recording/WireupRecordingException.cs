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