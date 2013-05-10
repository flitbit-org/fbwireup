using System;
using System.Diagnostics.Contracts;
using FlitBit.Core;
using FlitBit.Wireup.Meta;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Wireup record for wireup tasks.
	/// </summary>
	public class WiredTask : WireupRecord
	{
		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="where">Where the task was encountered.</param>
		/// <param name="task">The task attribute.</param>
		public WiredTask(WireupRecord where, WireupTaskAttribute task)
			: base(where.Context)
		{
			Contract.Requires<ArgumentNullException>(where != null);

			this.Phase = task.Phase;

			this.Description = task.GetType()
														.GetReadableFullName();
		}

		/// <summary>
		///   The phase in which the task is executed.
		/// </summary>
		public WireupPhase Phase { get; protected set; }
	}
}