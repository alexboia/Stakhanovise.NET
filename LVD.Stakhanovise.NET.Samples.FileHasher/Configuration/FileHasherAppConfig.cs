using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.Configuration
{
	public class FileHasherAppConfig
	{
		public string WorkingDirectory { get; set; }

		public IntervalConfig FileCount { get; set; }

		public IntervalConfig FileSizeBytes { get; set; }
	}
}
