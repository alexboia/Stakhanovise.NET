using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Samples.FileHasher.Configuration;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator
{
	public interface ISourceFileGenerator
	{
		Task<ISourceFileRepository> GenerateSourceFilesAsync( FileHasherAppConfig appConfig );
	}
}
