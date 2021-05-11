using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Setup;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	[SingleThreaded]
	public class DequeueFunctionDbAssetSetupTests : BaseSetupDbTests
	{
		[Test]
		[NonParallelizable]
		[Repeat( 5 )]
		public async Task Test_CanCreateDequeueFunction_WithDefaultMapping ()
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping );
		}

		[Test]
		[NonParallelizable]
		[Repeat( 5 )]
		public async Task Test_CanCreateDequeueFunction_WithNonDefaultMapping ()
		{
			QueuedTaskMapping mapping = GenerateNonDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping );
		}

		private async Task RunDbAssetSetupTestsAsync ( QueuedTaskMapping mapping )
		{
			DequeueFunctionDbAssetSetup setup =
				new DequeueFunctionDbAssetSetup();

			await setup.SetupDbAssetAsync( GetSetupTestDbConnectionOptions(),
				mapping );

			bool functionExists = await PgFunctionExists( mapping.DequeueFunctionName, 
				GetDequeueFunctionExpectedParametersInfo() );

			Assert.IsTrue( functionExists,
				"Function {0} does not exist or does not have expected arguments",
				mapping.DequeueFunctionName );
		}

		private Dictionary<string, char> GetDequeueFunctionExpectedParametersInfo ()
		{
			return new Dictionary<string, char>()
			{
				{ DequeueFunctionDbAssetSetup.SelectTypesParamName, 'i' },
				{ DequeueFunctionDbAssetSetup.ExcludeIdsParamName, 'i' },
				{ DequeueFunctionDbAssetSetup.RefNowParamName, 'i' },

				{ DequeueFunctionDbAssetSetup.TaskIdTableParamName, 't' },
				{ DequeueFunctionDbAssetSetup.TaskLockHandleIdTableParamName, 't' },
				{ DequeueFunctionDbAssetSetup.TaskTypeTableParamName, 't' },
				{ DequeueFunctionDbAssetSetup.TaskSourceTableParamName, 't' },
				{ DequeueFunctionDbAssetSetup.TaskPayloadTableParamName, 't' },
				{ DequeueFunctionDbAssetSetup.TaskPriorityTableParamName, 't' },
				{ DequeueFunctionDbAssetSetup.TaskPostedAtTableParamName, 't' },
				{ DequeueFunctionDbAssetSetup.TaskLockedUntilTableParamName, 't' }
			};
		}

		private QueuedTaskMapping GetDefaultMapping ()
		{
			return new QueuedTaskMapping();
		}

		private QueuedTaskMapping GenerateNonDefaultMapping ()
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();
			mapping.DequeueFunctionName = RandomizeDbAssetName( mapping.DequeueFunctionName );
			return mapping;
		}
	}
}
