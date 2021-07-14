using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public interface ISourceFileProcessor
	{
		Task StartProcesingFilesAsync( ISourceFileRepository sourceFileRepository,
			IFileHashRepository fileHashRepository,
			IProcessingWatcher processingWatcher );

		Task StopProcessingFilesAsync();
	}
}
