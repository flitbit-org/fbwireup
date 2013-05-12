using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using FlitBit.Core;
using FlitBit.Wireup.Meta;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Abstract wireup record.
	/// </summary>
	[Serializable]
	public abstract class WireupRecord
	{
		readonly List<WireupErrorRecord> _errors = new List<WireupErrorRecord>();
		WireupPhase _processingPhase;

		/// <summary>
		///   Event fired when a wireup phase is executing.
		/// </summary>
		public EventHandler<WireupRecordPhaseEventArgs> WireupPhaseExecuting;

		/// <summary>
		///   Creates a new instance on the provided context.
		/// </summary>
		/// <param name="context"></param>
		protected WireupRecord(WireupContext context)
		{
			Contract.Requires<ArgumentNullException>(context != null);

			this.Context = context;
		}

		/// <summary>
		///   Indicates the completed wireup phases.
		/// </summary>
		public WireupPhase CompletedWireupPhase { get; private set; }

		/// <summary>
		///   Gets the context upon which the wireup was recorded.
		/// </summary>
		public WireupContext Context { get; private set; }

		/// <summary>
		///   Indicates whether wireup declarations indicate using declarations only (no discovery).
		/// </summary>
		public bool DeclarationsOnly { get; protected set; }

		/// <summary>
		///   Gets the item's dependencies.
		/// </summary>
		public IEnumerable<WiredDependency> Dependencies { get; protected set; }

		/// <summary>
		///   Description.
		/// </summary>
		public string Description { get; protected set; }

		/// <summary>
		///   Indicates whether wireup has completed.
		/// </summary>
		public bool IsWireupComplete { get { return CompletedWireupPhase == WireupPhase.AfterWireup; } }

		/// <summary>
		///   Gets the item's dependent tasks.
		/// </summary>
		public IEnumerable<WiredTask> Tasks { get; protected set; }

		/// <summary>
		///   Gets wireup declarations.
		/// </summary>
		public IEnumerable<WireupAttribute> WireupDeclarations { get; protected set; }

		/// <summary>
		///   Adds an error record to the wireup.
		/// </summary>
		/// <param name="error"></param>
		protected virtual void AddError(WireupErrorRecord error)
		{
			_errors.Add(error);
		}

		/// <summary>
		///   Special handling for the Wireup phase.
		/// </summary>
		/// <param name="coordinator"></param>
		/// <param name="context"></param>
		protected virtual void OnWireup(IWireupCoordinator coordinator, WireupContext context)
		{
			var decls = this.WireupDeclarations;
			if (decls != null)
			{
				foreach (var decl in decls)
				{
					foreach (var typ in decl.CommandType)
					{
						context.PerformWireupType(coordinator, typ);
					}
				}
			}
		}

		/// <summary>
		///   Event dispatcher for wireup phase events.
		/// </summary>
		/// <param name="coordinator">The coordinator.</param>
		/// <param name="context">The context.</param>
		/// <param name="phase">The phase.</param>
		protected virtual void OnWireupPhaseEvent(IWireupCoordinator coordinator, WireupContext context, WireupPhase phase)
		{
			if (WireupPhaseExecuting != null)
			{
				WireupPhaseExecuting(this, new WireupRecordPhaseEventArgs
				{
					Coordinator = coordinator,
					Context = context,
					Phase = phase
				});
			}
		}

		/// <summary>
		///   Performs wireup. Specialized by subclasses.
		/// </summary>
		/// <param name="coordinator"></param>
		/// <param name="context"></param>
		protected virtual void PerformWireup(IWireupCoordinator coordinator, WireupContext context)
		{
			// Purposely empty.
		}

		/// <summary>
		///   Determines if the wireup phase should be performed.
		/// </summary>
		/// <param name="phase"></param>
		/// <returns></returns>
		protected bool PerformWireupPhase(WireupPhase phase)
		{
			// perform the wireup phase if we have not previously attempted that phase...
			return (phase > CompletedWireupPhase && CompletedWireupPhase == _processingPhase) 
				&& _errors.All(r => r.Phase != phase);
		}

		internal void PerformImmediatePhase(IWireupCoordinator coordinator, WireupContext context)
		{
			PerformWireupPhase(coordinator, context, WireupPhase.Immediate);
		}

		/// <summary>
		///   Performs a wireup phase.
		/// </summary>
		/// <param name="coordinator"></param>
		/// <param name="context"></param>
		/// <param name="phase"></param>
		protected internal virtual void PerformWireupPhase(IWireupCoordinator coordinator, WireupContext context,
			WireupPhase phase)
		{
			if (PerformWireupPhase(phase))
			{
				_processingPhase = phase;
				try
				{
					context.Sequence.Push(String.Concat(phase, " phase for ", Description));
					context.Sequence.BeginScope();
					var deps = this.Dependencies;
					if (deps != null)
					{
						foreach (var dep in deps.Where(d => d.Phase == phase))
						{
							dep.PerformWireupPhases(coordinator, context);
						}
					}
					var tasks = this.Tasks;
					if (tasks != null)
					{
						foreach (var task in tasks.Where(t => t.Phase == phase))
						{
							task.PerformWireupPhases(coordinator, context);
						}
					}

					if (phase == WireupPhase.Wireup)
					{
						OnWireup(coordinator, context);
					}
					OnWireupPhaseEvent(coordinator, context, phase);

					this.CompletedWireupPhase = phase;
					context.Sequence.EndScope();
				}
				catch (WireupRecordingException wre)
				{
					AddError(wre.ErrorRecord);
					context.Sequence.EndScope();
					context.Sequence.Push(String.Concat("ERROR: ", wre.ErrorRecord.Cause.FormatForLogging()));
				}
				catch (Exception e)
				{
					var err = new WireupErrorRecord
					{
						Context = context,
						Phase = phase,
						Where = this,
						Cause = e
					};
					AddError(err);
					context.Sequence.EndScope();
					context.Sequence.Push(String.Concat("ERROR: ", e.FormatForLogging()));
				}
			}
		}
	}

	/// <summary>
	///   Extensions for wireup records.
	/// </summary>
	public static class WireupRecordExtensions
	{
		/// <summary>
		///   Performs wireup phases for the record.
		/// </summary>
		/// <param name="rec">The record.</param>
		/// <param name="coordinator">The coordinator.</param>
		/// <param name="context">The context.</param>
		public static void PerformWireupPhases(this WireupRecord rec, IWireupCoordinator coordinator, WireupContext context)
		{
			rec.PerformWireupPhase(coordinator, context, WireupPhase.BeforeDependencies);
			rec.PerformWireupPhase(coordinator, context, WireupPhase.Dependencies);
			rec.PerformWireupPhase(coordinator, context, WireupPhase.BeforeTasks);
			rec.PerformWireupPhase(coordinator, context, WireupPhase.Tasks);
			rec.PerformWireupPhase(coordinator, context, WireupPhase.BeforeWireup);
			rec.PerformWireupPhase(coordinator, context, WireupPhase.Wireup);
			rec.PerformWireupPhase(coordinator, context, WireupPhase.AfterWireup);
		}
	}
}