using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Producer.Tests.Support
{
	public class NpgsqlNotificationMonitor : IDisposable
	{
		private readonly TaskQueueOptions mOptions;

		private NpgsqlConnection mConnection;

		private CountdownEvent mWaitHandle;

		private Task mWaitTask = null;


		public NpgsqlNotificationMonitor( TaskQueueOptions options, int expectedCount )
		{
			mOptions = options;
			mWaitHandle = new CountdownEvent( expectedCount );
		}

		public async Task BeginWatchNotificationsAsync()
		{
			mWaitHandle.Reset();
			mConnection = await TryOpenConnectionAsync();

			await mConnection.ListenAsync( NewTaskNotificationChannelName, HandleNotificationReceived );
			mWaitTask = Task.Run( WaitForNotifications );
		}

		private async Task<NpgsqlConnection> TryOpenConnectionAsync()
		{
			return await mOptions
				.ConnectionOptions
				.TryOpenConnectionAsync();
		}

		private void WaitForNotifications()
		{
			while ( !mWaitHandle.IsSet && mConnection != null )
				mConnection.Wait();
		}

		private void HandleNotificationReceived( object sender, NpgsqlNotificationEventArgs e )
		{
			mWaitHandle.Signal();
		}

		public void WaitForNotificationsToBeReceived()
		{
			mWaitHandle.Wait();
		}

		public async Task EndWatchNotificationsAsync()
		{
			await mWaitTask;
			await mConnection.UnlistenAsync( NewTaskNotificationChannelName, HandleNotificationReceived );
			await mConnection.CloseAsync();

			mWaitHandle.Reset();
			mWaitTask = null;
			mConnection = null;
		}

		public void Dispose()
		{
			Dispose( true );
		}

		protected void Dispose( bool disposing )
		{
			if ( disposing )
			{
				mConnection?.Dispose();
				mWaitHandle?.Dispose();
				mConnection = null;
				mWaitHandle = null;
			}
		}

		private string NewTaskNotificationChannelName
			=> mOptions.Mapping.NewTaskNotificationChannelName;
	}
}
