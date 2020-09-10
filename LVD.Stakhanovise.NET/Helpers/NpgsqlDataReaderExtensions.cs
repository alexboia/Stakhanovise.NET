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
using System.Threading.Tasks;
using Npgsql;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class NpgsqlDataReaderExtensions
	{
		public static async Task<T> GetFieldValueAsync<T> ( this NpgsqlDataReader reader, string columnName, T defaultValue )
		{
			if ( reader == null )
				throw new ArgumentNullException( nameof( reader ) );

			if ( string.IsNullOrEmpty( columnName ) )
				throw new ArgumentNullException( nameof( columnName ) );

			int index = reader.GetOrdinal( columnName );

			if ( await reader.IsDBNullAsync( index ) )
				return defaultValue;

			return await reader.GetFieldValueAsync<T>( index );
		}

		public static async Task<T?> GetNullableFieldValueAsync<T> ( this NpgsqlDataReader reader, string columnName, T? defaultValue )
			where T : struct
		{
			if ( reader == null )
				throw new ArgumentNullException( nameof( reader ) );

			if ( string.IsNullOrEmpty( columnName ) )
				throw new ArgumentNullException( nameof( columnName ) );

			int index = reader.GetOrdinal( columnName );

			if ( await reader.IsDBNullAsync( index ) )
				return defaultValue;

			T fieldValue = await reader.GetFieldValueAsync<T>( index );
			return new T?( fieldValue );
		}

		public static async Task<QueuedTask> ReadQueuedTaskAsync ( this NpgsqlDataReader reader, QueuedTaskMapping modelMapping )
		{
			string payloadString,
				taskErrorString;

			QueuedTask queuedTask;

			if ( reader == null )
				throw new ArgumentNullException( nameof( reader ) );

			if ( modelMapping == null )
				throw new ArgumentNullException( nameof( modelMapping ) );

			queuedTask = new QueuedTask();

			queuedTask.Id =
			   await reader.GetFieldValueAsync<Guid>( modelMapping.IdColumnName,
				  defaultValue: Guid.Empty );
			queuedTask.LockHandleId =
			   await reader.GetFieldValueAsync<long>( modelMapping.LockHandleIdColumnName,
				  defaultValue: 0 );

			queuedTask.Status =
			   ( QueuedTaskStatus )( await reader.GetFieldValueAsync<int>( modelMapping.StatusColumnName,
				  defaultValue: 0 ) );
			queuedTask.Priority =
			   await reader.GetFieldValueAsync<int>( modelMapping.PriorityColumnName,
				  defaultValue: 0 );

			queuedTask.Type =
			   await reader.GetFieldValueAsync<string>( modelMapping.TypeColumnName,
				  defaultValue: string.Empty );
			queuedTask.Source =
			   await reader.GetFieldValueAsync<string>( modelMapping.SourceColumnName,
				  defaultValue: string.Empty );

			queuedTask.LastErrorIsRecoverable =
			   await reader.GetFieldValueAsync<bool>( modelMapping.LastErrorIsRecoverableColumnName,
				  defaultValue: false );
			queuedTask.ErrorCount =
			   await reader.GetFieldValueAsync<int>( modelMapping.ErrorCountColumnName,
				  defaultValue: 0 );

			//Get payoad
			payloadString = await reader.GetFieldValueAsync<string>( modelMapping.PayloadColumnName,
				defaultValue: string.Empty );

			queuedTask.Payload = payloadString
				.AsObjectFromJson();

			//Get last task error
			taskErrorString = await reader.GetFieldValueAsync<string>( modelMapping.LastErrorColumnName,
				defaultValue: string.Empty );

			queuedTask.LastError = taskErrorString
				.AsObjectFromJson<QueuedTaskError>();

			queuedTask.PostedAt =
			   await reader.GetFieldValueAsync<DateTimeOffset>( modelMapping.PostedAtColumnName,
				  defaultValue: DateTimeOffset.MinValue );
			queuedTask.RepostedAt =
			   await reader.GetFieldValueAsync<DateTimeOffset>( modelMapping.RepostedAtColumnName,
				  defaultValue: DateTimeOffset.MinValue );

			queuedTask.FirstProcessingAttemptedAt =
			   await reader.GetNullableFieldValueAsync<DateTimeOffset>( modelMapping.FirstProcessingAttemptedAtColumnName,
				  defaultValue: null );
			queuedTask.LastProcessingAttemptedAt =
			   await reader.GetNullableFieldValueAsync<DateTimeOffset>( modelMapping.LastProcessingAttemptedAtColumnName,
				  defaultValue: null );
			queuedTask.ProcessingFinalizedAt =
			   await reader.GetNullableFieldValueAsync<DateTimeOffset>( modelMapping.ProcessingFinalizedAtColumnName,
				  defaultValue: null );

			return queuedTask;
		}
	}
}
