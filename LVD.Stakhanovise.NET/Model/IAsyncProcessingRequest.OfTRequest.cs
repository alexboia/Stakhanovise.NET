using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public interface IAsyncProcessingRequest<TResult> : IAsyncProcessingRequest
	{
		void SetCompleted( TResult result );

		TResult Result
		{
			get;
		}
	}
}
