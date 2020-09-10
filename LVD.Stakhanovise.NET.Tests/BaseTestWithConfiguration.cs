using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class BaseTestWithConfiguration
	{
		private IConfiguration mConfiguration;

		public BaseTestWithConfiguration ()
		{
			mConfiguration = GetConfig();
		}

		protected string GetConnectionString ( string connectionStringName )
		{
			if ( string.IsNullOrEmpty( connectionStringName ) )
				throw new ArgumentNullException( nameof( connectionStringName ) );

			return mConfiguration.GetConnectionString( connectionStringName );
		}

		private static IConfiguration GetConfig ()
		{
			return new ConfigurationBuilder()
				.SetBasePath( Directory.GetCurrentDirectory() )
				.AddJsonFile( "appsettings.json", false, true )
				.Build();
		}
	}
}
