using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LVD.Stakhanovise.NET.Processor
{
	public class AutoProcessIdProvider : IProcessIdProvider
	{
		private const string ProcessIdFileName = ".sk-process-id";

		private string mProcessIdFilePath;

		private string mProcessId;

		public AutoProcessIdProvider()
		{
			mProcessIdFilePath = DetermineProcessIdFilePath();
		}

		private string DetermineProcessIdFilePath()
		{
			return Path.Combine( Directory.GetCurrentDirectory(),
				ProcessIdFileName );
		}

		public string GetProcessId()
		{
			return mProcessId;
		}

		public Task SetupAsync()
		{
			mProcessId = ReadStoredProcessId();

			if ( string.IsNullOrEmpty( mProcessId ) )
				GenerateAndStoreProcessId();

			return Task.CompletedTask;
		}

		private string ReadStoredProcessId()
		{
			return File.Exists( mProcessIdFilePath )
				? File.ReadAllText( mProcessIdFilePath, Encoding.UTF8 )
					?.Trim()
				: null;
		}

		private void GenerateAndStoreProcessId()
		{
			mProcessId = Guid.NewGuid().ToString();
			File.WriteAllText( mProcessIdFilePath,
				mProcessId,
				Encoding.UTF8 );
		}
	}
}
