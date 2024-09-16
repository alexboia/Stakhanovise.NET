using Bogus;
using LVD.Stakhanovise.NET.Helpers;
using Npgsql;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace LVD.Stakhanovise.NET.Common.Tests.HelpersTests
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

			ClassicAssert.NotNull( builderCopy );

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
			ClassicAssert.AreEqual( builder.Host, copy.Host );
			ClassicAssert.AreEqual( builder.Port, copy.Port );
			ClassicAssert.AreEqual( builder.Pooling, copy.Pooling );
			ClassicAssert.AreEqual( builder.MinPoolSize, copy.MinPoolSize );
			ClassicAssert.AreEqual( builder.MaxPoolSize, copy.MaxPoolSize );
			ClassicAssert.AreEqual( builder.Username, copy.Username );

			foreach ( string key in builder.Keys )
			{
				ClassicAssert.IsTrue( copy.ContainsKey( key ) );
				ClassicAssert.AreEqual( builder [ key ], copy [ key ] );
			}
		}

		[Test]
		public void Test_CanCopyConnectionStringBuilder_Empty()
		{
			NpgsqlConnectionStringBuilder builder =
				CreateEmptyBuilder();

			NpgsqlConnectionStringBuilder builderCopy =
				builder.Copy();

			ClassicAssert.NotNull( builderCopy );

			Assert_BuildersMatch( builder,
				builderCopy );
		}

		private NpgsqlConnectionStringBuilder CreateEmptyBuilder()
		{
			return new NpgsqlConnectionStringBuilder();
		}
	}
}
