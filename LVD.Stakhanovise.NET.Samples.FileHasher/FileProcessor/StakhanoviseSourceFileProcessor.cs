using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using LVD.Stakhanovise.NET.Setup;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public class StakhanoviseSourceFileProcessor : ISourceFileProcessor
	{
		private Stakhanovise mStakhanovise;

		private ISourceFileScheduler mSourceFileScheduler;

		public StakhanoviseSourceFileProcessor( ISourceFileScheduler sourceFileScheduler )
		{
			mSourceFileScheduler = sourceFileScheduler
				?? throw new ArgumentNullException( nameof( sourceFileScheduler ) );
		}

		public async Task StartProcesingFilesAsync( ISourceFileRepository sourceFileRepository,
			IFileHashRepository fileHashRepository,
			IProcessingWatcher processingWatcher )
		{
			mStakhanovise = await Stakhanovise
				.CreateForTheMotherland( new NetCoreConfigurationStakhanoviseDefaultsProvider() )
				.SetupWorkingPeoplesCommittee( setup =>
				{
					setup.SetupBuiltInTaskExecutorRegistryDependencies( depSetup =>
					{
						depSetup.BindToInstance<IFileHashRepository>( fileHashRepository );
						depSetup.BindToInstance<ISourceFileRepository>( sourceFileRepository );
						depSetup.BindToInstance<IProcessingWatcher>( processingWatcher );
					} );
				} )
				.StartFulfillingFiveYearPlanAsync();

			await mSourceFileScheduler.ScheduleFilesAsync( sourceFileRepository,
				processingWatcher );
		}

		public async Task StopProcessingFilesAsync()
		{
			await mStakhanovise.StopFulfillingFiveYearPlanAsync();
		}
	}
}
