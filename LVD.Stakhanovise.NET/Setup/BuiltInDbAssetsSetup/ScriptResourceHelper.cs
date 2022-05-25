using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public static class ScriptResourceHelper
	{
		public static string DeriveSetupScriptResourceId( this string scriptName )
		{
			string currentNamespace = typeof( ScriptResourceHelper )
				.Namespace;

			return $"{currentNamespace}.BuiltInDbAssetsSetup.Scripts.{scriptName}";
		}
	}
}
