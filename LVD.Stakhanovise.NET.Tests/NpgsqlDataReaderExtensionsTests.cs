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
using NUnit.Framework;
using Npgsql;
using LVD.Stakhanovise.NET.Helpers;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Tests.Support;
using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System.Dynamic;
using NpgsqlTypes;
using LVD.Stakhanovise.NET.Tests.Helpers;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class NpgsqlDataReaderExtensionsTests : BaseDbTests
	{
		private QueuedTaskMapping mMapping;

		private PostgreSqlTaskQueueDbOperations mOperations;

		public NpgsqlDataReaderExtensionsTests ()
		{
			mMapping = TestOptions.DefaultMapping;
			mOperations = new PostgreSqlTaskQueueDbOperations( ConnectionString, mMapping );
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanReadQueuedTask ()
		{
			Faker<QueuedTask> qFaker =
				GetQueuedTaskFaker();

			QueuedTask expectedTask = qFaker
				.Generate();

			await mOperations
				.AddQueuedTaskAsync( expectedTask );

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( $"SELECT * FROM {mMapping.QueueTableName} WHERE task_id = @t_id", conn ) )
			{
				cmd.Parameters.AddWithValue( "t_id",
					NpgsqlDbType.Uuid,
					expectedTask.Id );

				await cmd.PrepareAsync();
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					if ( await rdr.ReadAsync() )
					{
						QueuedTask actualTask = await rdr.ReadQueuedTaskAsync();
						Assert.NotNull( actualTask );
						Assert.NotNull( actualTask.Payload );
						Assert.IsInstanceOf<SampleTaskPayload>( actualTask.Payload );

						Assert.AreEqual( ( ( SampleTaskPayload )expectedTask.Payload ).Counter,
							( ( SampleTaskPayload )actualTask.Payload ).Counter );

						Assert.AreEqual( expectedTask.Id, actualTask.Id );
						Assert.IsTrue( actualTask.LockedUntilTs.EqualsAproximately( expectedTask.LockedUntilTs ) );
						Assert.AreEqual( expectedTask.Type, actualTask.Type );
						Assert.AreEqual( expectedTask.Source, actualTask.Source );
						Assert.AreEqual( expectedTask.Priority, actualTask.Priority );

						Assert.IsTrue( actualTask.PostedAtTs.EqualsAproximately( expectedTask.PostedAtTs ) );

						Assert.Greater( actualTask.LockHandleId, 0 );
					}
				}

				await conn.CloseAsync();
			}
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanReadQueuedTaskResult ()
		{
			Faker<QueuedTaskResult> qFaker =
				GetQueuedTaskResultFaker();

			QueuedTaskResult expectedTaskResult = qFaker
				.Generate();

			await mOperations
				.AddQueuedTaskResultAsync( expectedTaskResult );

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( $"SELECT * FROM {mMapping.ResultsQueueTableName} WHERE task_id = @t_id", conn ) )
			{
				cmd.Parameters.AddWithValue( "t_id",
					NpgsqlDbType.Uuid,
					expectedTaskResult.Id );

				await cmd.PrepareAsync();
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					if ( await rdr.ReadAsync() )
					{
						QueuedTaskResult actualTaskResult = await rdr.ReadQueuedTaskResultAsync();
						Assert.NotNull( actualTaskResult );
						Assert.NotNull( actualTaskResult.Payload );
						Assert.IsInstanceOf<SampleTaskPayload>( actualTaskResult.Payload );

						Assert.AreEqual( ( ( SampleTaskPayload )expectedTaskResult.Payload ).Counter,
							( ( SampleTaskPayload )actualTaskResult.Payload ).Counter );

						Assert.AreEqual( expectedTaskResult.Id, actualTaskResult.Id );
						Assert.AreEqual( expectedTaskResult.Type, actualTaskResult.Type );
						Assert.AreEqual( expectedTaskResult.Source, actualTaskResult.Source );
						Assert.AreEqual( expectedTaskResult.Priority, actualTaskResult.Priority );
						Assert.AreEqual( expectedTaskResult.ErrorCount, actualTaskResult.ErrorCount );
						Assert.AreEqual( expectedTaskResult.LastErrorIsRecoverable, actualTaskResult.LastErrorIsRecoverable );
						Assert.AreEqual( expectedTaskResult.LastError, actualTaskResult.LastError );

						Assert.AreEqual( expectedTaskResult.Status, actualTaskResult.Status );

						Assert.IsTrue( expectedTaskResult.PostedAtTs
							.EqualsAproximately( actualTaskResult.PostedAtTs ) );

						Assert.IsTrue( expectedTaskResult.ProcessingFinalizedAtTs
							.EqualsAproximately( actualTaskResult.ProcessingFinalizedAtTs ) );

						Assert.IsTrue( expectedTaskResult.FirstProcessingAttemptedAtTs
							.EqualsAproximately( actualTaskResult.FirstProcessingAttemptedAtTs ) );
						
						Assert.IsTrue( expectedTaskResult.LastProcessingAttemptedAtTs
							.EqualsAproximately( actualTaskResult.LastProcessingAttemptedAtTs ) );
					}
				}

				await conn.CloseAsync();
			}
		}

		private Faker<QueuedTask> GetQueuedTaskFaker ()
		{
			Faker<QueuedTask> qFaker = new Faker<QueuedTask>();

			qFaker.RuleFor( q => q.Id, f => Guid.NewGuid() );
			qFaker.RuleFor( q => q.LockedUntilTs, f => f.Date.FutureOffset() );
			qFaker.RuleFor( q => q.Payload, f => new SampleTaskPayload( f.Random.Int() ) );
			qFaker.RuleFor( q => q.Type, f => typeof( SampleTaskPayload ).FullName );
			qFaker.RuleFor( q => q.Source, f => nameof( GetQueuedTaskFaker ) );
			qFaker.RuleFor( q => q.PostedAtTs, f => f.Date.SoonOffset() );
			qFaker.RuleFor( q => q.Priority, f => f.Random.Int( 1, 1000 ) );

			return qFaker;
		}

		private Faker<QueuedTaskResult> GetQueuedTaskResultFaker ()
		{
			Faker<QueuedTaskResult> qrFaker = new Faker<QueuedTaskResult>();

			qrFaker.RuleFor( q => q.Id, f => Guid.NewGuid() );
			qrFaker.RuleFor( q => q.Payload, f => new SampleTaskPayload( f.Random.Int() ) );
			qrFaker.RuleFor( q => q.Type, f => typeof( SampleTaskPayload ).FullName );
			qrFaker.RuleFor( q => q.Source, f => nameof( GetQueuedTaskFaker ) );
			qrFaker.RuleFor( q => q.PostedAtTs, f => f.Date.SoonOffset() );
			qrFaker.RuleFor( q => q.FirstProcessingAttemptedAtTs, f => f.Date.SoonOffset() );
			qrFaker.RuleFor( q => q.LastProcessingAttemptedAtTs, f => f.Date.SoonOffset() );
			qrFaker.RuleFor( q => q.ProcessingFinalizedAtTs, f => f.Date.SoonOffset() );
			qrFaker.RuleFor( q => q.ProcessingTimeMilliseconds, f => f.Random.Long( 1, 100000 ) );
			qrFaker.RuleFor( q => q.Status, f => f.Random.Enum<QueuedTaskStatus>() );
			qrFaker.RuleFor( q => q.LastError, f => new QueuedTaskError( f.System.Exception() ) );
			qrFaker.RuleFor( q => q.LastErrorIsRecoverable, f => f.Random.Bool() );
			qrFaker.RuleFor( q => q.ErrorCount, f => f.Random.Int( 1, 100 ) );
			qrFaker.RuleFor( q => q.Priority, f => f.Random.Int( 1, 1000 ) );

			return qrFaker;
		}

		private string ConnectionString
			=> GetConnectionString( "testDbConnectionString" );
	}
}
