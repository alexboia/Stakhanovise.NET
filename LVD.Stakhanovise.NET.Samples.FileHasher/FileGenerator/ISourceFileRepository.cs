using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator
{
	public interface ISourceFileRepository
	{
		FileHandle ResolveFileByHandleId( Guid handleId );

		int TotalFileCount { get; }
	}
}
