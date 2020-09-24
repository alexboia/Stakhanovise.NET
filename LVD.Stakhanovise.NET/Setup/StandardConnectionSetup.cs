using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardConnectionSetup : IConnectionSetup
	{
		private string mConnectionString;

		private int mConnectionKeepAlive;

		private int mConnectionRetryCount = 3;

		private int mConnectionRetryDelayMilliseconds = 100;
		
		public IConnectionSetup WithConnectionKeepAlive ( int connectionKeepAlive )
		{
			mConnectionKeepAlive = connectionKeepAlive;
			return this;
		}

		public IConnectionSetup WithConnectionRetryCount ( int connectionRetryCount )
		{
			mConnectionRetryCount = connectionRetryCount;
			return this;
		}

		public IConnectionSetup WithConnectionRetryDelayMilliseconds ( int connectionRetryDelayMilliseconds )
		{
			mConnectionRetryDelayMilliseconds = connectionRetryDelayMilliseconds;
			return this;
		}

		public IConnectionSetup WithConnectionString ( string connectionString )
		{
			if ( string.IsNullOrEmpty( connectionString ) )
				throw new ArgumentNullException( nameof( connectionString ) );

			mConnectionString = connectionString;
			return this;
		}

		public ConnectionOptions BuildOptions()
		{
			return new ConnectionOptions( mConnectionString, 
				mConnectionKeepAlive, 
				mConnectionRetryCount, 
				mConnectionRetryDelayMilliseconds );
		}
	}
}
