// 
// BSD 3-Clause License
// 
// Copyright (c) 2020 - 2023, Boia Alexandru
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
		public QueuedTaskMapping()
		{
			QueueTableName = "sk_tasks_queue_t";
			ResultsQueueTableName = "sk_task_results_t";
			ExecutionTimeStatsTableName = "sk_task_execution_time_stats_t";
			MetricsTableName = "sk_metrics_t";
			NewTaskNotificationChannelName = "sk_task_queue_item_added";
			DequeueFunctionName = "sk_try_dequeue_task";
		}

		public QueuedTaskMapping AddTablePrefix( string tablePrefix )
		{
			QueueTableName = $"{tablePrefix}{QueueTableName}";
			ResultsQueueTableName = $"{tablePrefix}{ResultsQueueTableName}";
			ExecutionTimeStatsTableName = $"{tablePrefix}{ExecutionTimeStatsTableName}";
			MetricsTableName = $"{tablePrefix}{MetricsTableName}";
			NewTaskNotificationChannelName = $"{tablePrefix}{NewTaskNotificationChannelName}";
			DequeueFunctionName = $"{tablePrefix}{DequeueFunctionName}";
			return this;
		}

		public static QueuedTaskMapping DefaultWithPrefix( string prefix )
		{
			return Default
				.AddTablePrefix( prefix );
		}

		public static QueuedTaskMapping DefaultWithPrefix( string prefix, Action<QueuedTaskMapping> modifier )
		{
			if ( modifier == null )
				throw new ArgumentNullException(nameof(modifier));
			
			QueuedTaskMapping mapping = DefaultWithPrefix( prefix );
			modifier.Invoke( mapping );
			return mapping;
		}

		public static QueuedTaskMapping Default
			=> new QueuedTaskMapping();

		public bool IsValid
		{
			get
			{
				return !string.IsNullOrWhiteSpace( QueueTableName )
					&& !string.IsNullOrWhiteSpace( ResultsQueueTableName )
					&& !string.IsNullOrWhiteSpace( NewTaskNotificationChannelName )
					&& !string.IsNullOrWhiteSpace( ExecutionTimeStatsTableName )
					&& !string.IsNullOrWhiteSpace( MetricsTableName )
					&& !string.IsNullOrWhiteSpace( DequeueFunctionName );
			}
		}

		public string QueueTableName
		{
			get; set;
		}

		public string ResultsQueueTableName
		{
			get; set;
		}

		public string NewTaskNotificationChannelName
		{
			get; set;
		}

		public string ExecutionTimeStatsTableName
		{
			get; set;
		}

		public string MetricsTableName
		{
			get; set;
		}

		public string DequeueFunctionName
		{
			get; set;
		}
	}
}

