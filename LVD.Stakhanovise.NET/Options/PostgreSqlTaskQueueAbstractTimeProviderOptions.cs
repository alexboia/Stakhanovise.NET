using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class PostgreSqlTaskQueueAbstractTimeProviderOptions
	{
		public PostgreSqlTaskQueueAbstractTimeProviderOptions ( Guid timeId, ConnectionOptions connectionOptions )
		{
			TimeId = timeId;
			ConnectionOptions = connectionOptions
				?? throw new ArgumentNullException( nameof( connectionOptions ) );
		}

		public Guid TimeId { get; private set; }

		public ConnectionOptions ConnectionOptions { get; private set; }
	}
}
