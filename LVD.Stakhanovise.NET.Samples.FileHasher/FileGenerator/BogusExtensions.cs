using System;
using System.Collections.Generic;
using System.Text;
using Bogus;
using LVD.Stakhanovise.NET.Samples.FileHasher.Configuration;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator
{
	public static class BogusExtensions
	{
		public static int GenerateFileCount( this Faker faker, FileHasherAppConfig appConfig )
		{
			if ( faker == null )
				throw new ArgumentNullException( nameof( faker ) );

			if ( appConfig == null )
				throw new ArgumentNullException( nameof( appConfig ) );

			return faker.Random.Int(
				appConfig.FileCount.Min,
				appConfig.FileCount.Max
			);
		}

		public static int GenerateFileSizeBytes( this Faker faker, FileHasherAppConfig appConfig )
		{
			if ( faker == null )
				throw new ArgumentNullException( nameof( faker ) );

			if ( appConfig == null )
				throw new ArgumentNullException( nameof( appConfig ) );

			return faker.Random.Int(
				appConfig.FileSizeBytes.Min,
				appConfig.FileSizeBytes.Max
			);
		}

		public static byte[] GenerateFileContents( this Faker faker, FileHasherAppConfig appConfig )
		{
			if ( faker == null )
				throw new ArgumentNullException( nameof( faker ) );

			if ( appConfig == null )
				throw new ArgumentNullException( nameof( appConfig ) );

			int fileSizeBytes = faker
				.GenerateFileSizeBytes( appConfig );

			return faker.Random
				.Bytes( fileSizeBytes );
		}
	}
}
