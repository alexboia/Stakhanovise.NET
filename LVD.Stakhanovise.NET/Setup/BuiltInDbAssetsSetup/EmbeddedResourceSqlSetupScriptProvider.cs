using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Setup.BuiltInDbAssetsSetup
{
	public class EmbeddedResourceSqlSetupScriptProvider : ISqlSetupScriptProvider
	{
		private Assembly mTargetAssembly;

		private string mResourceId;

		public EmbeddedResourceSqlSetupScriptProvider( Assembly targetAssembly, string resourceId )
		{
			if ( targetAssembly == null )
				throw new ArgumentNullException( nameof( targetAssembly ) );

			if ( string.IsNullOrWhiteSpace( resourceId ) )
				throw new ArgumentNullException( nameof( targetAssembly ) );

			mTargetAssembly = targetAssembly;
			mResourceId = resourceId;
		}

		public async Task<string> GetScriptContentsAsync()
		{
			using ( Stream assemblyResourceStream = mTargetAssembly.GetManifestResourceStream( mResourceId ) )
			using ( StreamReader reader = new StreamReader( assemblyResourceStream, Encoding.UTF8 ) )
			{
				string contents = await reader.ReadToEndAsync();
				return contents;
			}
		}
	}
}
