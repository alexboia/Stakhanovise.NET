﻿// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
using LVD.Stakhanovise.NET.Model;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class NpgsqlDataReaderExtensions
	{
		public static async Task<QueuedTaskResult> ReadQueuedTaskResultAsync( this NpgsqlDataReader reader )
		{
			Action<JsonSerializerSettings> noOpConfig = DelegateHelpers
				.CreateNoOpAction<JsonSerializerSettings>();
			return await reader
				.ReadQueuedTaskResultAsync( noOpConfig );
		}

		public static async Task<QueuedTaskResult> ReadQueuedTaskResultAsync( this NpgsqlDataReader reader,
			Action<JsonSerializerSettings> configureSerializer )
		{
			if ( reader == null )
				throw new ArgumentNullException( nameof( reader ) );

			if ( configureSerializer == null )
				throw new ArgumentNullException( nameof( configureSerializer ) );

			string payloadString,
				taskErrorString;

			QueuedTaskResult result =
				new QueuedTaskResult();

			result.Id = await reader.GetFieldValueAsync<Guid>( "task_id",
				defaultValue: Guid.Empty );
			result.Type = await reader.GetFieldValueAsync<string>( "task_type",
				defaultValue: string.Empty );
			result.Source = await reader.GetFieldValueAsync<string>( "task_source",
				  defaultValue: string.Empty );
			result.Status = ( QueuedTaskStatus ) ( await reader.GetFieldValueAsync<int>( "task_status",
				defaultValue: 0 ) );
			result.Priority = await reader.GetFieldValueAsync<int>( "task_priority",
				  defaultValue: 0 );

			result.LastErrorIsRecoverable = await reader.GetFieldValueAsync<bool>( "task_last_error_is_recoverable",
				defaultValue: false );
			result.ErrorCount = await reader.GetFieldValueAsync<int>( "task_error_count",
				defaultValue: 0 );

			//Get payoad
			payloadString = await reader.GetFieldValueAsync<string>( "task_payload",
				defaultValue: string.Empty );

			result.Payload = payloadString
				.AsObjectFromJson();

			//Get last task error
			taskErrorString = await reader.GetFieldValueAsync<string>( "task_last_error",
				defaultValue: string.Empty );

			result.LastError = taskErrorString
				.AsObjectFromJson<QueuedTaskError>();

			result.PostedAtTs = await reader.GetFieldValueAsync<DateTimeOffset>( "task_posted_at_ts",
				defaultValue: DateTimeOffset.MinValue );

			result.PostedAtTs = new DateTimeOffset( result.PostedAtTs.UtcDateTime,
				TimeSpan.Zero );

			result.ProcessingTimeMilliseconds = await reader.GetFieldValueAsync<long>( "task_processing_time_milliseconds",
				defaultValue: 0 );

			result.FirstProcessingAttemptedAtTs = await reader.GetNullableFieldValueAsync<DateTimeOffset>( "task_first_processing_attempted_at_ts",
				defaultValue: null );

			if ( result.FirstProcessingAttemptedAtTs.HasValue )
				result.FirstProcessingAttemptedAtTs = 
					new DateTimeOffset( result.FirstProcessingAttemptedAtTs.Value.UtcDateTime, 
						TimeSpan.Zero );

			result.LastProcessingAttemptedAtTs = await reader.GetNullableFieldValueAsync<DateTimeOffset>( "task_last_processing_attempted_at_ts",
				defaultValue: null );

			if ( result.LastProcessingAttemptedAtTs.HasValue )
				result.LastProcessingAttemptedAtTs =
					new DateTimeOffset( result.LastProcessingAttemptedAtTs.Value.UtcDateTime,
						TimeSpan.Zero );

			result.ProcessingFinalizedAtTs = await reader.GetNullableFieldValueAsync<DateTimeOffset>( "task_processing_finalized_at_ts",
				defaultValue: null );

			if ( result.ProcessingFinalizedAtTs.HasValue )
				result.ProcessingFinalizedAtTs =
					new DateTimeOffset( result.ProcessingFinalizedAtTs.Value.UtcDateTime,
						TimeSpan.Zero );

			return result;
		}
	}
}
