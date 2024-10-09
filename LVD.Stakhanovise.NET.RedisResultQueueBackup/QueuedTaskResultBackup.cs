using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.RedisResultQueueBackup
{
	public class QueuedTaskResultBackup
	{
		public Guid Id
		{
			get; set;
		}

		public string Type
		{
			get; set;
		}

		public string Source
		{
			get; set;
		}

		public object Payload
		{
			get; set;
		}

		public QueuedTaskStatus Status
		{
			get; set;
		}

		public int Priority
		{
			get; set;
		}

		public long ProcessingTimeMilliseconds
		{
			get; set;
		}

		public QueuedTaskError LastError
		{
			get; set;
		}

		public bool LastErrorIsRecoverable
		{
			get; set;
		}

		public int ErrorCount
		{
			get; set;
		}

		public DateTimeOffset PostedAtTs
		{
			get; set;
		}

		public DateTimeOffset? FirstProcessingAttemptedAtTs
		{
			get; set;
		}

		public DateTimeOffset? LastProcessingAttemptedAtTs
		{
			get; set;
		}

		public DateTimeOffset? ProcessingFinalizedAtTs
		{
			get; set;
		}

		public bool CanBeUpdated
		{
			get; set;
		}
	}
}
