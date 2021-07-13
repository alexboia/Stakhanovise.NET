using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator
{
	public class FileHandle
	{
		public FileHandle( Guid id, string path )
		{
			Id = id;
			Path = path;
		}

		public Guid Id { get; private set; }

		public string Path { get; private set; }
	}
}
