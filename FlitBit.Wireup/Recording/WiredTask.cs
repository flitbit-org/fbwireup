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
		/// <param name="type">The task's target type (if exists; otherwise null).</param>
		public WiredTask(WireupRecord where, WireupTaskAttribute task, Type type)
			: base(where.Context)
		{
			Contract.Requires<ArgumentNullException>(where != null);

			this.Phase = task.Phase;
			this.TargetTask = task;
			this.TargetType = type;

			this.Description = task.GetType()
														.GetReadableFullName();
		}

		/// <summary>
		///   The phase in which the task is executed.
		/// </summary>
		public WireupPhase Phase { get; protected set; }

		/// <summary>
		///   Special handling for the Wireup phase.
		/// </summary>
		/// <param name="coordinator"></param>
		/// <param name="context"></param>
		protected override void OnWireup(IWireupCoordinator coordinator, WireupContext context)
		{
			context.Sequence.Push(String.Concat("Executing ", this.Description));
			coordinator.NotifyTaskObservers(context, TargetTask, TargetType);
			TargetTask.ExecuteTask(coordinator, context);
			base.OnWireup(coordinator, context);
		}

		/// <summary>
		/// The task.
		/// </summary>
		public WireupTaskAttribute TargetTask { get; private set; }

		/// <summary>
		/// The type on which the task is declared.
		/// </summary>
		public Type TargetType { get; private set; }
	}
}