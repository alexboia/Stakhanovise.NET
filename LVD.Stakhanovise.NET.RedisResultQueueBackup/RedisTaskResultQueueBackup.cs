using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.RedisResultQueueBackup.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.RedisResultQueueBackup
{
	public class RedisTaskResultQueueBackup : ITaskResultQueueBackup, IDisposable
	{
		private IConnectionMultiplexer mRedis;

		private readonly RedisResultQueueBackupOptions mOptions;

		private bool mIsDisposed;

		public RedisTaskResultQueueBackup( RedisResultQueueBackupOptions options )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
		}

		private void CheckInitializedOrThrow()
		{
			if (mRedis == null)
			{
				throw new InvalidOperationException(
					$"Cannot use an uninitialized {nameof( RedisTaskResultQueueBackup )} instance."
				);
			}
		}

		private void CheckNotDisposedOrThrow()
		{
			if (mIsDisposed)
			{
				throw new ObjectDisposedException(
					nameof( RedisTaskResultQueueBackup ),
					"Cannot reuse a disposed REDIS task queue result backup"
				);
			}
		}

		public async Task PutAsync( IQueuedTaskResult result )
		{
			CheckInitializedOrThrow();
			CheckNotDisposedOrThrow();

			if (result == null)
				throw new ArgumentNullException( nameof( result ) );

			string objectKey = ObjectKey( result );
			string objRegKey = ObjectRegistryKey();
			string objSerialized = Serialize( result );

			IDatabase db = GetRedisDb();
			bool objAdded = await db.StringSetAsync( objectKey, objSerialized );

			if (objAdded)
				await db.SetAddAsync( objRegKey, objectKey );
		}

		private string ObjectKey( IQueuedTaskResult result )
		{
			return $"{mOptions.ObjectKeysPrefix}-obj-{result.Id}";
		}

		private string ObjectRegistryKey()
		{
			return $"{mOptions.ObjectKeysPrefix}-obj-reg";
		}

		private IDatabase GetRedisDb()
		{
			return mRedis.GetDatabase( mOptions.DatabaseNumber );
		}

		public async Task RemoveAsync( IQueuedTaskResult result )
		{
			CheckInitializedOrThrow();
			CheckNotDisposedOrThrow();

			if (result == null)
				throw new ArgumentNullException( nameof( result ) );

			string objectKey = ObjectKey( result );
			string objRegKey = ObjectRegistryKey();

			IDatabase db = GetRedisDb();
			await db.KeyDeleteAsync( objectKey );
			await db.SetRemoveAsync( objRegKey, objectKey );
		}

		public async Task<IEnumerable<IQueuedTaskResult>> RetrieveBackedUpItemsAsync()
		{
			CheckInitializedOrThrow();
			CheckNotDisposedOrThrow();

			string objRegKey = ObjectRegistryKey();
			IDatabase db = mRedis.GetDatabase( mOptions.DatabaseNumber );

			List<string> foundObjectKeys =
				new List<string>();

			await foreach (RedisValue vKey in db.SetScanAsync( objRegKey, pageOffset: 0, pageSize: 250 ))
				foundObjectKeys.Add( vKey.ToString() );

			List<IQueuedTaskResult> queuedTaskResults =
				new List<IQueuedTaskResult>();

			foreach (string key in foundObjectKeys)
			{
				string objSerialized = await db.StringGetAsync( key );
				IQueuedTaskResult result = Deserialize( objSerialized );
				if (result == null)
					continue;

				queuedTaskResults.Add( result );
			}

			return queuedTaskResults.OrderBy( r => r.LastProcessingAttemptedAtTs );
		}

		private string Serialize( IQueuedTaskResult result )
		{
			QueuedTaskResultBackup backupInfo = new QueuedTaskResultBackup()
			{
				Id = result.Id,
				Type = result.Type,
				Source = result.Source,
				Payload = result.Payload,
				Status = result.Status,
				ErrorCount = result.ErrorCount,
				LastError = result.LastError,
				FirstProcessingAttemptedAtTs = result.FirstProcessingAttemptedAtTs,
				LastErrorIsRecoverable = result.LastErrorIsRecoverable,
				LastProcessingAttemptedAtTs = result.LastProcessingAttemptedAtTs,
				PostedAtTs = result.PostedAtTs,
				Priority = result.Priority,
				ProcessingFinalizedAtTs = result.ProcessingFinalizedAtTs,
				ProcessingTimeMilliseconds = result.ProcessingTimeMilliseconds
			};

			return JsonConvert.SerializeObject( backupInfo,
				GetSerializerSettings() );
		}

		private IQueuedTaskResult Deserialize( string cachedString )
		{
			if (string.IsNullOrWhiteSpace( cachedString ))
				return null;

			QueuedTaskResultBackup backupInfo = JsonConvert
				.DeserializeObject<QueuedTaskResultBackup>( cachedString,
					GetSerializerSettings() );

			return new QueuedTaskResult()
			{
				Id = backupInfo.Id,
				Type = backupInfo.Type,
				Source = backupInfo.Source,
				Payload = backupInfo.Payload,
				Status = backupInfo.Status,
				ErrorCount = backupInfo.ErrorCount,
				LastError = backupInfo.LastError,
				FirstProcessingAttemptedAtTs = backupInfo.FirstProcessingAttemptedAtTs,
				LastErrorIsRecoverable = backupInfo.LastErrorIsRecoverable,
				LastProcessingAttemptedAtTs = backupInfo.LastProcessingAttemptedAtTs,
				PostedAtTs = backupInfo.PostedAtTs,
				Priority = backupInfo.Priority,
				ProcessingFinalizedAtTs = backupInfo.ProcessingFinalizedAtTs,
				ProcessingTimeMilliseconds = backupInfo.ProcessingTimeMilliseconds
			};
		}

		private JsonSerializerSettings GetSerializerSettings()
		{
			JsonSerializerSettings settings =
				new JsonSerializerSettings();

			settings.ConstructorHandling = ConstructorHandling
				.AllowNonPublicDefaultConstructor;
			settings.TypeNameHandling = TypeNameHandling
				.Auto;
			settings.DateFormatHandling = DateFormatHandling
				.IsoDateFormat;

			return settings;
		}

		public async Task InitAsync()
		{
			CheckNotDisposedOrThrow();
			if (mRedis == null)
			{
				ConfigurationOptions redisOpts = ConfigurationOptions
					.Parse( mOptions.ConnectionString );

				redisOpts.HighIntegrity = mOptions.HighIntegrity;

				mRedis = await ConnectionMultiplexer
					.ConnectAsync( redisOpts );
			}
		}

		protected virtual void Dispose( bool disposing )
		{
			if (!mIsDisposed)
			{
				if (disposing)
				{
					if (mRedis != null)
					{
						mRedis.Dispose();
						mRedis = null;
					}
				}

				mIsDisposed = true;
			}
		}

		public void Dispose()
		{
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
	}
}
