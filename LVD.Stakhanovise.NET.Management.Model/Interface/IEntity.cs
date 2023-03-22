using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Management.Model.Interface
{
	public interface IEntity<TId, T> : IEquatable<T> where T : class, IEntity<TId, T>
	{
		bool IsTransient
		{
			get;
		}

		TId Id
		{
			get; set;
		}
	}
}
