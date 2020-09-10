using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Model;
using Npgsql;

namespace LVD.Stakhanovise.NET.Queue
{
	public class QueuedTaskLock : IDisposable
	{
		private bool mIsDisposed;

		public QueuedTaskLock ( QueuedTask queuedTask, NpgsqlConnection connection )
		{
			QueuedTask = queuedTask
				?? throw new ArgumentNullException( nameof( queuedTask ) );
			Connection = connection
				?? throw new ArgumentNullException( nameof( connection ) );
		}

		protected void Dispose ( bool disposing )
		{

			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					Connection.Close();
					Connection.Dispose();
					Connection = null;
					QueuedTask = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public QueuedTask QueuedTask { get; private set; }

		public NpgsqlConnection Connection { get; private set; }
	}
}
