using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public interface ISourceFileScheduler
	{
		void ScheduleFiles( ISourceFileRepository sourceFileRepository );
	}
}
