using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Executors.IoC
{
	public class TinyIoCThreadSingletonLifetimeManager : TinyIoCContainer.ITinyIoCObjectLifetimeProvider
	{
		[ThreadStatic]
		private static object mInstance;

		public object GetObject ()
		{
			return mInstance;
		}

		public void ReleaseObject ()
		{
			IDisposable disposableInstance = mInstance as IDisposable;

			if ( disposableInstance != null )
				disposableInstance.Dispose();

			SetObject( null );
		}

		public void SetObject ( object value )
		{
			mInstance = value;
		}
	}

	public static class TinyIoCThreadSingletonExtensions
	{
		public static TinyIoCContainer.RegisterOptions AsPerRequestSingleton ( this TinyIoCContainer.RegisterOptions registerOptions )
		{
			return TinyIoCContainer.RegisterOptions.ToCustomLifetimeManager( registerOptions, 
				new TinyIoCThreadSingletonLifetimeManager(), 
				"per thread singleton" );
		}

		public static TinyIoCContainer.MultiRegisterOptions AsPerRequestSingleton ( this TinyIoCContainer.MultiRegisterOptions registerOptions )
		{
			return TinyIoCContainer.MultiRegisterOptions.ToCustomLifetimeManager( registerOptions, 
				new TinyIoCThreadSingletonLifetimeManager(), 
				"per thread singleton" );
		}
	}
}
