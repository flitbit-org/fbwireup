using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace FlitBit.Wireup.Recording
{
	/// <summary>
	///   Records the processing sequence for a wireup context.
	/// </summary>
	public sealed class WireupProcessingSequence
	{
		readonly ConcurrentBag<Record> _items = new ConcurrentBag<Record>();
		readonly ConcurrentDictionary<int, int> _threads = new ConcurrentDictionary<int, int>();
		int _sequencing;

		/// <summary>
		///   Returns the processing record.
		/// </summary>
		public IEnumerable<Record> Records
		{
			get
			{
				return from r in _items
							orderby r.Sequence
							select r;
			}
		}

		/// <summary>
		///   Increments the current thread's scope depth in the processing sequence.
		/// </summary>
		public void BeginScope()
		{
			var id = Thread.CurrentThread.ManagedThreadId;
			int depth;
			if (_threads.TryGetValue(id, out depth))
			{
				_threads.TryUpdate(id, depth + 1, depth);
			}
			else
			{
				_threads.TryAdd(id, 0);
			}
		}

		/// <summary>
		///   Decrements the current thread's scope depth in the processing sequence.
		/// </summary>
		public void EndScope()
		{
			var id = Thread.CurrentThread.ManagedThreadId;
			int depth;
			if (_threads.TryGetValue(id, out depth))
			{
				_threads.TryUpdate(id, depth - 1, depth);
			}
		}

		internal void Push(string description)
		{
			Contract.Requires<ArgumentNullException>(description != null);
			var id = Thread.CurrentThread.ManagedThreadId;
			int depth;
			if (!_threads.TryGetValue(id, out depth))
			{
				throw new InvalidOperationException("Thread not involved in the WireupContext.");
			}

			_items.Add(new Record
			{
				ThreadId = id,
				Depth = depth,
				Sequence = Interlocked.Increment(ref _sequencing),
				Details = description
			});
		}

		/// <summary>
		///   A processing record.
		/// </summary>
		public class Record
		{
			/// <summary>
			///   The thread's processing depth.
			/// </summary>
			public int Depth { get; internal set; }

			/// <summary>
			///   Details about the processing.
			/// </summary>
			public string Details { get; internal set; }

			/// <summary>
			///   The sequence of the record within the processing.
			/// </summary>
			public int Sequence { get; internal set; }

			/// <summary>
			///   The managed thread Id.
			/// </summary>
			public int ThreadId { get; internal set; }
		}
	}
}