using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Management.Model.Interface
{
	public interface IModelComplexType<T> : IEquatable<T>
	{
		T Copy();
	}
}
