using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Setup.BuiltInDbAssetsSetup
{
	public class FileSystemSqlSetupScriptProvider : ISqlSetupScriptProvider
	{
		private string mScriptFilePath;

		public FileSystemSqlSetupScriptProvider( string scriptFilePath )
		{
			if ( string.IsNullOrEmpty( scriptFilePath ) )
				throw new ArgumentNullException( nameof( scriptFilePath ) );

			if ( !File.Exists( scriptFilePath ) )
				throw new FileNotFoundException( $"Script file {scriptFilePath} not found." );

			mScriptFilePath = scriptFilePath;
		}

		public async Task<string> GetScriptContentsAsync()
		{
			return await File.ReadAllTextAsync( mScriptFilePath, 
				Encoding.UTF8 );
		}
	}
}
