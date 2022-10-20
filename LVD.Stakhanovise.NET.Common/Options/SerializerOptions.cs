using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class SerializerOptions
	{
		public SerializerOptions( Action<JsonSerializerSettings> onConfigureSerializerSettings )
		{
			OnConfigureSerializerSettings = onConfigureSerializerSettings
				?? throw new ArgumentNullException( nameof( onConfigureSerializerSettings ) );
		}

		public static SerializerOptions Default
		{
			get
			{
				return new SerializerOptions( ConfigureSerializerSettings );
			}
		}

		private static void ConfigureSerializerSettings( JsonSerializerSettings jsonSerializerSettings )
		{
			jsonSerializerSettings.DateFormatHandling = 
				DateFormatHandling.IsoDateFormat;
		}

		public Action<JsonSerializerSettings> OnConfigureSerializerSettings
		{
			get;
			private set;
		}
	}
}
