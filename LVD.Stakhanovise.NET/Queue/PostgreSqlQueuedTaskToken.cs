﻿using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Setup;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using System.Reflection;

namespace LVD.Stakhanovise.NET.Queue
{
	public sealed class PostgreSqlQueuedTaskToken : IQueuedTaskToken
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		public event EventHandler<TokenReleasedEventArgs> TokenReleased;

		private bool mIsDisposed = false;

		private CancellationTokenSource mCancellationTokenSource;

		private CancellationTokenSource mWatchdogCancellationTokenSource;

		private QueuedTask mQueuedTask;

		private NpgsqlConnection mSourceConnection;

		private AbstractTimestamp mDequeuedAt;

		private string mSourceConnectionString;

		private TaskQueueOptions mOptions;

		private BlockingCollection<PostgreSqlQueuedTaskTokenOperation> mWatchdogEventsQueue =
			new BlockingCollection<PostgreSqlQueuedTaskTokenOperation>();

		private bool mIsLocked;

		private ManualResetEvent mReconnectWaitHandle =
			new ManualResetEvent( true );

		public PostgreSqlQueuedTaskToken ( QueuedTask queuedTask,
			NpgsqlConnection sourceConnection,
			AbstractTimestamp dequeuedAt,
			TaskQueueOptions options )
		{
			mQueuedTask = queuedTask
				?? throw new ArgumentNullException( nameof( queuedTask ) );
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
			mSourceConnection = sourceConnection
				?? throw new ArgumentNullException( nameof( sourceConnection ) );
			mDequeuedAt = dequeuedAt
				?? throw new ArgumentNullException( nameof( dequeuedAt ) );

			mCancellationTokenSource = new CancellationTokenSource();
			mSourceConnectionString = mSourceConnection.ConnectionString;
			mIsLocked = true;
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlQueuedTaskToken ),
					"Cannot use a disposed token instance" );
		}

		private void NotifyTokenReleased ()
		{
			EventHandler<TokenReleasedEventArgs> handler = TokenReleased;
			if ( handler != null )
				handler( this, new TokenReleasedEventArgs( mQueuedTask.Id ) );
		}

		private async Task<NpgsqlConnection> TryReopenSourceConnectionAsync ( int retryCount )
		{
			NpgsqlConnection newConnection = null;

			while ( retryCount > 0 )
			{
				try
				{
					newConnection = new NpgsqlConnection( mSourceConnectionString );
					await newConnection.OpenAsync();
				}
				catch ( Exception exc )
				{
					mLogger.Error( "Failed to re-establish source token connection.", exc );
					newConnection = null;
					await Task.Delay( 100 );
				}
				finally
				{
					retryCount--;
				}
			}

			return newConnection;
		}

		private async Task<bool> SetTaskCompletedAsync ( TaskExecutionResult result )
		{
			//Mark the task as processed
			mQueuedTask.Processed( result.ProcessingTimeMilliseconds );

			string updateSql = $@"UPDATE {mOptions.Mapping.TableName} 
				SET {mOptions.Mapping.StatusColumnName} = @t_status,
					{mOptions.Mapping.ProcessingFinalizedAtColumnName} = @t_processing_finalized_at,
					{mOptions.Mapping.FirstProcessingAttemptedAtColumnName} = @t_first_processing_attempted_at,
					{mOptions.Mapping.LastProcessingAttemptedAtColumnName} = @t_last_processing_attempted_at,
					{mOptions.Mapping.ProcessingTimeMillisecondsColumnName} = @t_processing_time_milliseconds
				WHERE {mOptions.Mapping.IdColumnName} = @t_id";

			using ( NpgsqlCommand updateCmd = new NpgsqlCommand( updateSql, mSourceConnection ) )
			{
				updateCmd.Parameters.AddWithValue( "t_status", NpgsqlDbType.Integer,
					( int )mQueuedTask.Status );
				updateCmd.Parameters.AddWithValue( "t_processing_finalized_at", NpgsqlDbType.TimestampTz,
					mQueuedTask.ProcessingFinalizedAt );
				updateCmd.Parameters.AddWithValue( "t_first_processing_attempted_at", NpgsqlDbType.TimestampTz,
					mQueuedTask.FirstProcessingAttemptedAt );
				updateCmd.Parameters.AddWithValue( "t_last_processing_attempted_at", NpgsqlDbType.TimestampTz,
					mQueuedTask.LastProcessingAttemptedAt );
				updateCmd.Parameters.AddWithValue( "t_processing_time_milliseconds", NpgsqlDbType.Bigint,
					mQueuedTask.ProcessingTimeMilliseconds );
				updateCmd.Parameters.AddWithValue( "t_id", NpgsqlDbType.Uuid,
					mQueuedTask.Id );

				return await updateCmd.ExecuteNonQueryAsync() == 1;
			}
		}

		private async Task<bool> SetTaskErroredAsync ( TaskExecutionResult result )
		{
			//Mark the task as processed
			mQueuedTask.HadError( result.Error,
				result.IsRecoverable,
				mOptions.FaultErrorThresholdCount,
				result.RetryAt );

			string updateSql = $@"UPDATE {mOptions.Mapping.TableName} 
				SET {mOptions.Mapping.StatusColumnName} = @t_status,
					{mOptions.Mapping.LockedUntilColumnName} = @t_locked_until,
					{mOptions.Mapping.LastErrorColumnName} = @t_last_error,
					{mOptions.Mapping.LastErrorIsRecoverableColumnName} = @t_last_error_is_recoverable,
					{mOptions.Mapping.ErrorCountColumnName} = @t_error_count,
					{mOptions.Mapping.FirstProcessingAttemptedAtColumnName} = @t_first_processing_attempted_at,
					{mOptions.Mapping.LastProcessingAttemptedAtColumnName} = @t_last_processing_attempted_at,
					{mOptions.Mapping.RepostedAtColumnName} = @t_reposted_at
				WHERE {mOptions.Mapping.IdColumnName} = @t_id";

			using ( NpgsqlCommand updateCmd = new NpgsqlCommand( updateSql, mSourceConnection ) )
			{
				updateCmd.Parameters.AddWithValue( "t_status", NpgsqlDbType.Integer,
					( int )mQueuedTask.Status );
				updateCmd.Parameters.AddWithValue( "t_locked_until", NpgsqlDbType.Bigint,
					mQueuedTask.LockedUntil );
				updateCmd.Parameters.AddWithValue( "t_last_error", NpgsqlDbType.Text,
					mQueuedTask.LastError.ToJson() );
				updateCmd.Parameters.AddWithValue( "t_last_error_is_recoverable", NpgsqlDbType.Boolean,
					mQueuedTask.LastErrorIsRecoverable );
				updateCmd.Parameters.AddWithValue( "t_error_count", NpgsqlDbType.Integer,
					mQueuedTask.ErrorCount );
				updateCmd.Parameters.AddWithValue( "t_first_processing_attempted_at", NpgsqlDbType.TimestampTz,
					mQueuedTask.FirstProcessingAttemptedAt );
				updateCmd.Parameters.AddWithValue( "t_last_processing_attempted_at", NpgsqlDbType.TimestampTz,
					mQueuedTask.LastProcessingAttemptedAt );
				updateCmd.Parameters.AddWithValue( "t_reposted_at", NpgsqlDbType.TimestampTz,
					mQueuedTask.RepostedAt );
				updateCmd.Parameters.AddWithValue( "t_id", NpgsqlDbType.Uuid,
					mQueuedTask.Id );

				return await updateCmd.ExecuteNonQueryAsync() == 1;
			}

		}

		private async Task<bool> TryReconnectAsync ()
		{
			try
			{
				//Token operations observe this handle and wait 
				//	until the reconnect attempt is completed
				mReconnectWaitHandle.Reset();

				//Get rid of the the old connection
				mSourceConnection.StateChange -= HandleSourceConnectionStateChanged;
				mSourceConnection.Dispose();
				mSourceConnection = null;

				//Attempt to open a new one and to reacquire the lock
				mSourceConnection = await TryReopenSourceConnectionAsync( 3 );
				if ( mSourceConnection == null || !await mSourceConnection.LockAsync( mQueuedTask.LockHandleId ) )
				{
					//If we cannot open the connection OR we cannot acquire the lock
					//	cancel the task and cleanup
					if ( mSourceConnection != null )
					{
						await mSourceConnection.CloseAsync();
						mSourceConnection.Dispose();
						mSourceConnection = null;
					}

					mWatchdogEventsQueue.CompleteAdding();
					mCancellationTokenSource.Cancel();
					mWatchdogCancellationTokenSource.Cancel();
					mIsLocked = false;
				}
				else
				{
					mSourceConnection.StateChange += HandleSourceConnectionStateChanged;
					mIsLocked = true;
				}
			}
			finally
			{
				mReconnectWaitHandle.Set();
			}

			return mIsLocked;
		}

		private void StartTokenWatchdog ()
		{
			mSourceConnection.StateChange +=
				HandleSourceConnectionStateChanged;

			mWatchdogCancellationTokenSource =
				new CancellationTokenSource();

			Task.Run( async () =>
			{
				CancellationToken watchdogCancellationToken = mWatchdogCancellationTokenSource
					.Token;

				while ( !mWatchdogEventsQueue.IsCompleted )
				{
					try
					{
						PostgreSqlQueuedTaskTokenOperation tokenOperation = mWatchdogEventsQueue
							.Take( watchdogCancellationToken );

						if ( tokenOperation != PostgreSqlQueuedTaskTokenOperation.Reconnect )
							continue;

						if ( !await TryReconnectAsync() )
						{
							NotifyTokenReleased();
							break;
						}
					}
					catch ( OperationCanceledException )
					{
						break;
					}
				}
			} );
		}

		private void HandleSourceConnectionStateChanged ( object sender, StateChangeEventArgs e )
		{
			//React only if the original state was Open 
			//	and the new state is either Broken or Closed
			if ( e.OriginalState == ConnectionState.Open
				&& ( e.CurrentState == ConnectionState.Broken
					|| e.CurrentState == ConnectionState.Closed ) )
			{
				if ( !mWatchdogCancellationTokenSource.IsCancellationRequested )
					mWatchdogEventsQueue.Add( PostgreSqlQueuedTaskTokenOperation.Reconnect );
			}
		}

		public async Task<bool> TrySetStartedAsync ( long lockForMilliseconds )
		{
			CheckNotDisposedOrThrow();

			//this should also allow for setting started any task 
			//	that has been initially taken over by another worker, 
			//	but then lost by it
			if ( !IsPending )
				return false;

			//Wait if there is a reconnect attempt underway
			await mReconnectWaitHandle.ToTask();

			if ( !IsPending )
				return false;

			//Compute the aproximate moment until we need this to be locked
			AbstractTimestamp lockUntil = mDequeuedAt
				.AddWallclockTimeDuration( lockForMilliseconds );

			//Update internal state
			mQueuedTask.ProcessingStarted( lockUntil );

			//Update the task status and hard-lock it 
			//	for the given period of time
			string updateSql = $@"UPDATE {mOptions.Mapping.TableName} 
				SET {mOptions.Mapping.StatusColumnName} = @t_status,
					{mOptions.Mapping.LockedUntilColumnName} = @t_locked_until
				WHERE {mOptions.Mapping.IdColumnName} = @t_id";

			using ( NpgsqlCommand updateCmd = new NpgsqlCommand( updateSql, mSourceConnection ) )
			{
				updateCmd.Parameters.AddWithValue( "t_status", NpgsqlDbType.Integer,
					( int )mQueuedTask.Status );
				updateCmd.Parameters.AddWithValue( "t_locked_until", NpgsqlDbType.Bigint,
					mQueuedTask.LockedUntil );
				updateCmd.Parameters.AddWithValue( "t_id", NpgsqlDbType.Uuid,
					mQueuedTask.Id );

				int rowCount = await updateCmd.ExecuteNonQueryAsync();
				if ( rowCount != 1 )
				{
					mIsLocked = false;
					await mSourceConnection.UnlockAsync( mQueuedTask.LockHandleId );
					await mSourceConnection.CloseAsync();
					NotifyTokenReleased();
					return false;
				}
				else
				{
					mIsLocked = true;
					StartTokenWatchdog();
					return true;
				}
			}
		}

		public async Task<bool> TrySetResultAsync ( TaskExecutionResult result )
		{
			CheckNotDisposedOrThrow();

			if ( !IsActive )
				return false;

			//Wait if there is a reconnect attempt underway
			await mReconnectWaitHandle.ToTask();

			if ( !IsActive )
				return false;

			try
			{
				if ( result.ExecutedSuccessfully )
					await SetTaskCompletedAsync( result );
				else
					await SetTaskErroredAsync( result );

				mSourceConnection.StateChange -= HandleSourceConnectionStateChanged;
				mWatchdogCancellationTokenSource.Cancel();
				mWatchdogEventsQueue.CompleteAdding();

				await mSourceConnection.UnlockAsync( mQueuedTask.LockHandleId );
				await mSourceConnection.CloseAsync();
				NotifyTokenReleased();
			}
			finally
			{
				mIsLocked = false;
			}

			return true;
		}

		public async Task ReleaseLockAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( !IsLocked )
				return;

			await mReconnectWaitHandle.ToTask();

			if ( !IsLocked )
				return;

			mSourceConnection.StateChange -= HandleSourceConnectionStateChanged;
			mWatchdogCancellationTokenSource.Cancel();
			mWatchdogEventsQueue.CompleteAdding();

			await mSourceConnection.UnlockAsync( mQueuedTask.LockHandleId );
			await mSourceConnection.CloseAsync();

			mCancellationTokenSource.Cancel();
			mIsLocked = false;

			NotifyTokenReleased();
		}

		private void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					TokenReleased = null;

					if ( mSourceConnection != null )
						mSourceConnection.Dispose();

					if ( mCancellationTokenSource != null )
						mCancellationTokenSource.Dispose();

					if ( mReconnectWaitHandle != null )
						mReconnectWaitHandle.Dispose();

					if ( mWatchdogCancellationTokenSource != null )
						mWatchdogCancellationTokenSource.Dispose();

					if ( mWatchdogEventsQueue != null )
						mWatchdogEventsQueue.Dispose();

					mIsLocked = false;
					mCancellationTokenSource = null;
					mWatchdogEventsQueue = null;
					mWatchdogCancellationTokenSource = null;
					mReconnectWaitHandle = null;
					mSourceConnection = null;
					mQueuedTask = null;
					mDequeuedAt = null;
					mOptions = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public IQueuedTask QueuedTask
			=> mQueuedTask;

		public bool IsPending
			=> mQueuedTask.Status == QueuedTaskStatus.Unprocessed
			&& !mCancellationTokenSource.IsCancellationRequested;

		public bool IsActive
			=> mQueuedTask.Status == QueuedTaskStatus.Processing
			&& !mCancellationTokenSource.IsCancellationRequested;

		public bool IsLocked => mIsLocked
			&& !mCancellationTokenSource.IsCancellationRequested;

		public CancellationToken CancellationToken
			=> mCancellationTokenSource.Token;
	}
}
