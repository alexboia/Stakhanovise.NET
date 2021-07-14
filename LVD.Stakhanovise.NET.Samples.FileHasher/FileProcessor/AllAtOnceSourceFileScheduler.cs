using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor.SeviceModel;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public class AllAtOnceSourceFileScheduler : ISourceFileScheduler
	{
		private ITaskQueueProducer mTaskQueueProducer;

		public AllAtOnceSourceFileScheduler( ITaskQueueProducer taskQueueProducer )
		{
			mTaskQueueProducer = taskQueueProducer
				?? throw new ArgumentNullException( nameof( taskQueueProducer ) );
		}

		public async Task ScheduleFilesAsync( ISourceFileRepository sourceFileRepository, IProcessingWatcher processingWatcher )
		{
			if ( sourceFileRepository == null )
				throw new ArgumentNullException( nameof( sourceFileRepository ) );

			foreach ( Guid fileHandleId in sourceFileRepository.AllFileHandleIds )
			{
				await mTaskQueueProducer.EnqueueAsync<HashFileByHandle>( new HashFileByHandle( fileHandleId ),
					source: nameof( ScheduleFilesAsync ),
					priority: 0 );
			}
		}
	}
}
