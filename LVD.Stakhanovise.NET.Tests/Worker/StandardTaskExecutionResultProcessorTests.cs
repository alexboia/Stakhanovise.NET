using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests.Worker
{
	[TestFixture]
	public class StandardTaskExecutionResultProcessorTests
	{
		[Test]
		public async Task Test_CanProcessResult_WhenHasResult_AndNotCancelled()
		{

		}

		[Test]
		public async Task Test_CanProcessResult_WhenHasResult_Cancelled()
		{

		}
	}
}
