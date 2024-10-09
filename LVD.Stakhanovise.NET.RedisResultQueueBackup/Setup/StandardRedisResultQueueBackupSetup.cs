using LVD.Stakhanovise.NET.RedisResultQueueBackup.Options;

namespace LVD.Stakhanovise.NET.RedisResultQueueBackup.Setup
{
	public class StandardRedisResultQueueBackupSetup : IRedisResultQueueBackupSetup
	{
		private string mConnectionString;

		private int mDatabaseNumber = -1;

		private string mObjectKeyPrefix;

		private bool mWithHighIntegrity;

		public IRedisResultQueueBackupSetup WithConnectionString( string connectionString )
		{
			mConnectionString = connectionString;
			return this;
		}

		public IRedisResultQueueBackupSetup WithDatabaseNumber( int databaseNumber )
		{
			mDatabaseNumber = databaseNumber;
			return this;
		}

		public IRedisResultQueueBackupSetup WithObjectKeyPrefix( string objectKeyPrefix )
		{
			mObjectKeyPrefix = objectKeyPrefix;
			return this;
		}

		public IRedisResultQueueBackupSetup WithHighIntegrity()
		{
			mWithHighIntegrity = true;
			return this;
		}

		public RedisResultQueueBackupOptions GetOptions()
		{
			return new RedisResultQueueBackupOptions( mConnectionString,
				mObjectKeyPrefix,
				mDatabaseNumber,
				mWithHighIntegrity );
		}
	}
}
