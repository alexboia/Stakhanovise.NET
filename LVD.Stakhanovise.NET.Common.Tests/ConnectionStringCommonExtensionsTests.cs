using Bogus;
using LVD.Stakhanovise.NET.Helpers;
using Npgsql;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Common.Tests
{
	[TestFixture]
	public class ConnectionStringCommonExtensionsTests
	{
		[Test]
		public void Test_CanCopyConnectionStringBuilder_NotEmpty()
		{
			NpgsqlConnectionStringBuilder builder =
				CreateNonEmptyBuilder();

			NpgsqlConnectionStringBuilder builderCopy =
				builder.Copy();

			Assert.NotNull( builderCopy );
			
			Assert_BuildersMatch( builder, 
				builderCopy );
		}

		private NpgsqlConnectionStringBuilder CreateNonEmptyBuilder()
		{
			Faker faker = 
				new Faker();

			NpgsqlConnectionStringBuilder builder =
				new NpgsqlConnectionStringBuilder();

			builder.Host = faker.Internet.Ip();
			builder.Port = faker.Internet.Port();
			builder.Pooling = true;
			builder.MinPoolSize = 1;
			builder.MaxPoolSize = 10;
			builder.Username = "super";
			builder.Password = "valid";

			return builder;
		}

		private void Assert_BuildersMatch( NpgsqlConnectionStringBuilder builder,
			NpgsqlConnectionStringBuilder copy )
		{
			Assert.AreEqual( builder.Host, copy.Host );
			Assert.AreEqual( builder.Port, copy.Port );
			Assert.AreEqual( builder.Pooling, copy.Pooling );
			Assert.AreEqual( builder.MinPoolSize, copy.MinPoolSize );
			Assert.AreEqual( builder.MaxPoolSize, copy.MaxPoolSize );
			Assert.AreEqual( builder.Username, copy.Username );

			foreach ( string key in builder.Keys )
			{
				Assert.IsTrue( copy.ContainsKey( key ) );
				Assert.AreEqual( builder [ key ], copy [ key ] );
			}
		}

		[Test]
		public void Test_CanCopyConnectionStringBuilder_Empty()
		{
			NpgsqlConnectionStringBuilder builder =
				CreateEmptyBuilder();

			NpgsqlConnectionStringBuilder builderCopy =
				builder.Copy();

			Assert.NotNull( builderCopy );

			Assert_BuildersMatch( builder,
				builderCopy );
		}

		private NpgsqlConnectionStringBuilder CreateEmptyBuilder()
		{
			return new NpgsqlConnectionStringBuilder();
		}
	}
}
