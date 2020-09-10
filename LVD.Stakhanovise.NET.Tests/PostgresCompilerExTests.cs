using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Infrastructure;
using NUnit.Framework;
using SqlKata.Execution;
using SqlKata;
using SqlKata.Compilers;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgresCompilerExTests
	{
		[Test]
		public void Test_CanGenerateWithRecursive ()
		{
			Query withRecursive = new Query( "tasks_queue_v" )
				.With( "RECURSIVE tasks_queue_v",
					new Query( "[l0]" )
						.FromRaw( "(SELECT pg_try_advisory_lock(100) AS is_lock_acquired) AS [l0]" )
						.Select( "l0.*" ) )
				.Select( "tasks_queue_v.*" );

			PostgresCompilerEx compiler = new PostgresCompilerEx();
			SqlResult withRecursiveResult = compiler.Compile( withRecursive );

			Assert.NotNull( withRecursiveResult );
			Assert.NotNull( withRecursiveResult.Sql );

			Assert.IsTrue( withRecursiveResult.Sql.StartsWith( "WITH RECURSIVE",
				StringComparison.InvariantCultureIgnoreCase ) );
		}
	}
}
