using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.RedisResultQueueBackup.Setup
{
	public interface IRedisResultQueueBackupSetup
	{
		IRedisResultQueueBackupSetup WithConnectionString( string connectionString );

		IRedisResultQueueBackupSetup WithObjectKeyPrefix( string objectKeyPrefix );

		IRedisResultQueueBackupSetup WithDatabaseNumber( int databaseNumber );

		IRedisResultQueueBackupSetup WithHighIntegrity();
	}
}
