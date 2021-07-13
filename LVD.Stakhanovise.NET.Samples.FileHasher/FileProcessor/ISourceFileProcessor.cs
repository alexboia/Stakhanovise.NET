using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public interface ISourceFileProcessor
	{
		Task StartProcesingFilesAsync( IFileHashRepository fileHashRepository );

		Task StopProcessingFilesAsync();
	}
}
