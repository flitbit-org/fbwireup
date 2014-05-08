#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Captures a wireup error.
	/// </summary>
	public struct WireupErrorRecord
	{
		/// <summary>
		///   The exception.
		/// </summary>
		public Exception Cause { get; set; }

		/// <summary>
		///   The wireup context at the time of the error.
		/// </summary>
		public WireupContext Context { get; set; }

		/// <summary>
		///   Which wireup phase was executing when the error was encountered.
		/// </summary>
		public WireupPhase Phase { get; set; }

		/// <summary>
		///   Where the error was encountered.
		/// </summary>
		public WireupRecord Where { get; set; }

		/// <summary>
		///   Produces a description of the error.
		/// </summary>
		/// <param name="detailed">indicates whether the description should be detailed</param>
		/// <returns>
		///   A detailed description if <paramref name="detailed" /> is provied; otherwise a summary of the error.
		/// </returns>
		public string Describe(bool detailed)
		{
			return String.Empty;
		}
	}
}