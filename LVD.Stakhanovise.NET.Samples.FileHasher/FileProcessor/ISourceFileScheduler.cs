using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public interface ISourceFileScheduler
	{
		Task ScheduleFilesAsync( ISourceFileRepository sourceFileRepository, IProcessingWatcher processingWatcher );
	}
}
