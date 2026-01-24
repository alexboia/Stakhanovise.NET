using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public enum FanOutErrorPolicy
	{
		/// <summary>
		/// Strict mode. Waits for all producers to complete. 
		/// If ANY producer fails, an AggregateException is thrown. 
		/// Note: The task may still have been successfully written to some queues.
		/// </summary>
		ThrowOnAnyError = 0,

		/// <summary>
		/// Best-effort mode. Waits for all producers to complete.
		/// If AT LEAST ONE producer succeeds, the operation is considered successful.
		/// If ALL producers fail, an AggregateException is thrown.
		/// </summary>
		SucceedIfAny = 1
	}

	public class FanOutTaskQueueProducerOptions
	{
		/// <summary>
		/// If true, throws an InvalidOperationException when a task does not match any FanOutTarget.
		/// If false, returns a virtual/completed task representation without enqueuing.
		/// </summary>
		public bool ThrowIfNoTargetsMatched { get; set; } = false;

		/// <summary>
		/// If true, stops evaluating subsequent targets after the first target (which may contain multiple producers) matches the task.
		/// </summary>
		public bool StopOnFirstMatch { get; set; } = false;

		/// <summary>
		/// If true, ensures the same Task ID is used for all destinations.
		/// </summary>
		public bool CorrelateIds { get; set; } = true;

		/// <summary>
		/// Determines how to handle exceptions when one or more producers fail.
		/// </summary>
		public FanOutErrorPolicy ErrorPolicy { get; set; } = FanOutErrorPolicy.ThrowOnAnyError;
	}
}
