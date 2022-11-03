using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public interface IAsyncProcessingRequest<TRequest> : IAsyncProcessingRequest
	{
		void SetCompleted( TRequest result );
	}
}
