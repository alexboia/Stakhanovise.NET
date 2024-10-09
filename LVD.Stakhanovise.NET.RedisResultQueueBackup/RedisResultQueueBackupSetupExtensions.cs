using LVD.Stakhanovise.NET.RedisResultQueueBackup.Options;
using LVD.Stakhanovise.NET.RedisResultQueueBackup.Setup;
using LVD.Stakhanovise.NET.Setup;
using System;

namespace LVD.Stakhanovise.NET.RedisResultQueueBackup
{
    public static class RedisResultQueueBackupSetupExtensions
	{
		public static IStakhanoviseSetup WithRedisResultQueueBackup( this IStakhanoviseSetup setup, RedisResultQueueBackupOptions options )
		{
			if (setup == null)
				throw new ArgumentNullException( nameof( setup ) );

			if (options == null)
				throw new ArgumentNullException( nameof( options ) );

			return setup.WithResultQueueBackup( new RedisTaskResultQueueBackup( options ) );
		}

		public static IStakhanoviseSetup WithRedisResultQueueBackup( this IStakhanoviseSetup setup, string connectionString )
		{
			if (setup == null)
				throw new ArgumentNullException( nameof( setup ) );

			if (string.IsNullOrWhiteSpace( connectionString ))
				throw new ArgumentNullException( nameof( connectionString ) );

			RedisResultQueueBackupOptions options = 
				new RedisResultQueueBackupOptions( connectionString );

			return setup.WithResultQueueBackup( new RedisTaskResultQueueBackup( options ) );
		}

		public static IStakhanoviseSetup WithRedisResultQueueBackup( this IStakhanoviseSetup setup, Action<IRedisResultQueueBackupSetup> setupFn )
		{
			if (setup == null)
				throw new ArgumentNullException( nameof( setup ) );

			if (setupFn == null)
				throw new ArgumentNullException( nameof( setupFn ) );

			StandardRedisResultQueueBackupSetup redisSetup = 
				new StandardRedisResultQueueBackupSetup();

			setupFn.Invoke( redisSetup );

			RedisResultQueueBackupOptions options = redisSetup
				.GetOptions();

			return setup.WithResultQueueBackup( new RedisTaskResultQueueBackup( options ) );
		}
	}
}
