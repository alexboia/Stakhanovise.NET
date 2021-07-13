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
	public static class NpgsqlDataReaderCommonExtensions
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

		public static async Task<QueuedTask> ReadQueuedTaskAsync ( this NpgsqlDataReader reader )
		{
			string payloadString;
			QueuedTask queuedTask;

			if ( reader == null )
				throw new ArgumentNullException( nameof( reader ) );

			queuedTask = new QueuedTask();

			queuedTask.Id = await reader.GetFieldValueAsync<Guid>( "task_id",
				defaultValue: Guid.Empty );
			queuedTask.LockHandleId = await reader.GetFieldValueAsync<long>( "task_lock_handle_id",
				defaultValue: 0 );

			queuedTask.Priority = await reader.GetFieldValueAsync<int>( "task_priority",
				defaultValue: 0 );

			queuedTask.Type = await reader.GetFieldValueAsync<string>( "task_type",
				defaultValue: string.Empty );
			queuedTask.Source = await reader.GetFieldValueAsync<string>( "task_source",
				defaultValue: string.Empty );

			//Get payoad
			payloadString = await reader.GetFieldValueAsync<string>( "task_payload",
				defaultValue: string.Empty );

			queuedTask.Payload = payloadString
				.AsObjectFromJson();

			queuedTask.PostedAtTs = await reader.GetFieldValueAsync<DateTimeOffset>( "task_posted_at_ts",
				defaultValue: DateTimeOffset.MinValue );
			queuedTask.LockedUntilTs = await reader.GetFieldValueAsync<DateTimeOffset>( "task_locked_until_ts",
				defaultValue: DateTimeOffset.MinValue );

			return queuedTask;
		}
	}
}
