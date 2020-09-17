// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class QueuedTaskMapping
	{
		public QueuedTaskMapping ()
		{
			IdColumnName = "task_id";
			LockHandleIdColumnName = "task_lock_handle_id";
			TypeColumnName = "task_type";
			SourceColumnName = "task_source";
			PayloadColumnName = "task_payload";
			StatusColumnName = "task_status";
			PriorityColumnName = "task_priority";
			LastErrorColumnName = "task_last_error";
			LastErrorIsRecoverableColumnName = "task_last_error_is_recoverable";
			ErrorCountColumnName = "task_error_count";
			PostedAtTsColumnName = "task_posted_at_ts";
			RepostedAtTsColumnName = "task_reposted_at_ts";
			FirstProcessingAttemptedAtTsColumnName = "task_first_processing_attempted_at_ts";
			LastProcessingAttemptedAtTsColumnName = "task_last_processing_attempted_at_ts";
			ProcessingFinalizedAtTsColumnName = "task_processing_finalized_at_ts";
			LockedUntilColumnName = "task_locked_until";
			ProcessingTimeMillisecondsColumnName = "task_processing_time_milliseconds";

			TableName = "sk_tasks_queue_t";
			NewTaskNotificaionChannelName = "sk_task_queue_item_added";
			DequeueFunctionName = "sk_try_dequeue_task";
		}

		public void AddTablePrefix ( string tablePrefix )
		{
			TableName = $"{tablePrefix}{TableName}";
		}

		public string IdColumnName { get; set; }

		public string LockHandleIdColumnName { get; set; }

		public string TypeColumnName { get; set; }

		public string SourceColumnName { get; set; }

		public string PayloadColumnName { get; set; }

		public string StatusColumnName { get; set; }

		public string PostedAtColumnName { get; set; }

		public string LockedUntilColumnName { get; set; }

		public string ProcessingTimeMillisecondsColumnName { get; set; }

		public string PriorityColumnName { get; set; }

		public string LastErrorColumnName { get; set; }

		public string LastErrorIsRecoverableColumnName { get; set; }

		public string ErrorCountColumnName { get; set; }

		public string PostedAtTsColumnName { get; set; }

		public string RepostedAtTsColumnName { get; set; }

		public string FirstProcessingAttemptedAtTsColumnName { get; set; }

		public string LastProcessingAttemptedAtTsColumnName { get; set; }

		public string ProcessingFinalizedAtTsColumnName { get; set; }

		public string TableName { get; set; }

		public string NewTaskNotificaionChannelName { get; set; }

		public string DequeueFunctionName { get; set; }
	}
}
