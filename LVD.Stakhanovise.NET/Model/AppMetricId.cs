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
using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace LVD.Stakhanovise.NET.Model
{
	public class AppMetricId : IEquatable<AppMetricId>
	{
		public static readonly AppMetricId ListenerTaskNotificationCount =
			new AppMetricId( "listener@task-notification-count",
				valueCategory: "task-queue-notification-listener" );

		public static readonly AppMetricId ListenerReconnectCount =
			new AppMetricId( "listener@reconnect-count",
				valueCategory: "task-queue-notification-listener" );

		public static readonly AppMetricId ListenerNotificationWaitTimeoutCount =
			new AppMetricId( "listener@notification-wait-timeout-count",
				valueCategory: "task-queue-notification-listener" );

		public static readonly AppMetricId PollerDequeueCount =
			new AppMetricId( "poller@dequeue-count", 
				valueCategory: "task-poller" );

		public static readonly AppMetricId PollerWaitForDequeueCount =
			new AppMetricId( "poller@wait-dequeue-count", 
				valueCategory: "task-poller" );

		public static readonly AppMetricId PollerWaitForBufferSpaceCount =
			new AppMetricId( "poller@wait-buffer-space-count", 
				valueCategory: "task-poller" );

		public static readonly AppMetricId WorkerProcessedPayloadCount =
			new AppMetricId( "worker@processed-payload-count", 
				valueCategory: "task-worer" );

		public static readonly AppMetricId WorkerBufferWaitCount =
			new AppMetricId( "worker@buffer-wait-count", 
				valueCategory: "task-worer" );

		public static readonly AppMetricId WorkerTotalProcessingTime =
			new AppMetricId( "worker@total-processing-time", 
				valueCategory: "task-worer" );

		public static readonly AppMetricId WorkerSuccessfulProcessedPayloadCount =
			new AppMetricId( "worker@successful-processed-payload-count", 
				valueCategory: "task-worer" );

		public static readonly AppMetricId WorkerFailedProcessedPayloadCount =
			new AppMetricId( "worker@failed-processed-payload-count", 
				valueCategory: "task-worer" );

		public static readonly AppMetricId WorkerProcessingCancelledPayloadCount =
			new AppMetricId( "worker@processing-cancelled-payload-count", 
				valueCategory: "task-worer" );

		public static readonly AppMetricId QueueConsumerDequeueCount =
			new AppMetricId( "queue-consumer@dequeue-count", 
				valueCategory: "task-queue-consumer" );

		public static readonly AppMetricId QueueConsumerTotalDequeueDuration =
			new AppMetricId( "queue-consumer@total-dequeue-duration", 
				valueCategory:"task-queue-consumer" );

		public static readonly AppMetricId QueueConsumerMinimumDequeueDuration =
			new AppMetricId( "queue-consumer@minimum-dequeue-duration", 
				valueCategory: "task-queue-consumer" );

		public static readonly AppMetricId QueueConsumerMaximumDequeueDuration =
			new AppMetricId( "queue-consumer@maximum-dequeue-duration", 
				valueCategory: "task-queue-consumer" );

		public static readonly AppMetricId ResultQueueResultPostCount =
			new AppMetricId( "result-queue@result-post-count", 
				valueCategory: "task-result-queue" );

		public static readonly AppMetricId ResultQueueResultWriteCount =
			new AppMetricId( "result-queue@result-writes-count", 
				valueCategory: "task-result-queue" );

		public static readonly AppMetricId ResultQueueMaximumResultWriteDuration =
			new AppMetricId( "result-queue@maximum-result-write-duration", 
				valueCategory: "task-result-queue" );

		public static readonly AppMetricId ResultQueueMinimumResultWriteDuration =
			new AppMetricId( "result-queue@minimum-result-write-duration", 
				valueCategory: "task-result-queue" );

		public static readonly AppMetricId ResultQueueTotalResultWriteDuration =
			new AppMetricId( "result-queue@total-result-write-duration", 
				valueCategory: "task-result-queue" );

		public static readonly AppMetricId ResultQueueResultWriteRequestTimeoutCount =
			new AppMetricId( "result-queue@result-write-rq-timeout-count", 
				valueCategory: "task-result-queue" );

		public static readonly AppMetricId BufferMaxCount =
			new AppMetricId( "task-buffer@max-count", 
				valueCategory: "task-buffer" );

		public static readonly AppMetricId BufferMinCount =
			new AppMetricId( "task-buffer@min-count", 
				valueCategory: "task-buffer" );

		public static readonly AppMetricId BufferTimesFilled =
			new AppMetricId( "task-buffer@times-filled", 
				valueCategory: "task-buffer" );

		public static readonly AppMetricId BufferTimesEmptied =
			new AppMetricId( "task-buffer@times-emptied", 
				valueCategory: "task-buffer" );

		public static readonly AppMetricId PerfMonReportPostCount =
			new AppMetricId( "perf-mon@report-post-count", 
				valueCategory: "execution-perf-mon" );

		public static readonly AppMetricId PerfMonReportWriteCount =
			new AppMetricId( "perf-mon@report-write-count", 
				valueCategory: "execution-perf-mon" );

		public static readonly AppMetricId PerfMonMinimumReportWriteDuration =
			new AppMetricId( "perf-mon@minimum-report-write-duration", 
				valueCategory: "execution-perf-mon" );

		public static readonly AppMetricId PerfMonMaximumReportWriteDuration =
			new AppMetricId( "perf-mon@maximum-report-write-duration", 
				valueCategory: "execution-perf-mon" );

		public static readonly AppMetricId PerfMonReportWriteRequestsTimeoutCount =
			new AppMetricId( "perf-mon@report-requests-timeout-count", 
				valueCategory: "execution-perf-mon" );

		private static readonly SupportedValuesContainer<AppMetricId, string> mBuiltInAppMetricIds
			= new SupportedValuesContainer<AppMetricId, string>( m => m.ValueId );

		public AppMetricId ( string valueId, string valueCategory )
		{
			if ( string.IsNullOrEmpty( valueId ) )
				throw new ArgumentNullException( nameof( valueId ) );
			if ( string.IsNullOrEmpty( valueCategory ) )
				throw new ArgumentNullException( nameof( valueCategory ) );

			ValueId = valueId;
			ValueCategory = valueCategory;
		}

		public static bool IsSupported(string valueId)
		{
			return mBuiltInAppMetricIds.IsSupported( valueId );
		}

		public static AppMetricId TryParse(string valueId)
		{
			return mBuiltInAppMetricIds.TryParse( valueId );
		}

		public bool Equals ( AppMetricId other )
		{
			return other != null
				&& string.Equals( ValueId, other.ValueId )
				&& string.Equals( ValueCategory, other.ValueCategory );
		}

		public override bool Equals ( object obj )
		{
			return Equals( obj as AppMetricId );
		}

		public override int GetHashCode ()
		{
			int result = 1;

			result = result * 13 + ValueId.GetHashCode();
			result = result * 13 + ValueCategory.GetHashCode();

			return result;
		}

		public override string ToString ()
		{
			return $"[{ValueId}@{ValueCategory}]";
		}

		public string ValueId { get; private set; }

		public string ValueCategory { get; private set; }

		public static IEnumerable<AppMetricId> BuiltInAppMetricIds 
			=> mBuiltInAppMetricIds.SupportedValues;
	}
}
