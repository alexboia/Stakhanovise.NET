using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.RedisResultQueueBackup.Options
{
	public class RedisResultQueueBackupOptions
	{
		private const string DEFAULT_OBJECT_KEYS_PREFIX = "sknet-results";

		public RedisResultQueueBackupOptions( string connectionString,
			string objectKeysPrefix = null,
			int databaseNumber = -1,
			bool highIntegrity = false )
		{
			if (string.IsNullOrWhiteSpace( objectKeysPrefix ))
				objectKeysPrefix = DEFAULT_OBJECT_KEYS_PREFIX;

			ConnectionString = connectionString;
			DatabaseNumber = databaseNumber;
			ObjectKeysPrefix = objectKeysPrefix;
			HighIntegrity = highIntegrity;
		}

		public string ConnectionString
		{
			get; private set;
		}

		public string ObjectKeysPrefix
		{
			get; private set;
		} = DEFAULT_OBJECT_KEYS_PREFIX;

		public int DatabaseNumber
		{
			get; private set;
		}

		public bool HighIntegrity
		{
			get; private set;
		}
	}
}
