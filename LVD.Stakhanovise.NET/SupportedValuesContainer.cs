using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LVD.Stakhanovise.NET
{
	public class SupportedValuesContainer<T, TKey>
	{
		private Func<T, TKey> mKeySelector;

		private List<T> mSupportedValues;

		public SupportedValuesContainer ( Func<T, TKey> keySelector )
		{
			mKeySelector = keySelector ?? throw new ArgumentNullException( nameof( keySelector ) );

			//Pre-fetch all the supported values. The convention is that
			//  each supported value of type T is defined as a public, static and readonly field
			mSupportedValues = typeof( T )
				.GetFields( BindingFlags.Public | BindingFlags.Static )
				.Where( f => f.FieldType == typeof( T ) && f.IsInitOnly )
				.Select( f => ( T )f.GetValue( null ) )
				.ToList() ?? new List<T>();
		}

		public bool IsSupported ( TKey key )
		{
			return mSupportedValues
				.Count( t => EqualityComparer<TKey>.Default.Equals( mKeySelector.Invoke( t ), key ) ) == 1;
		}

		public T TryParse ( TKey key )
		{
			return mSupportedValues
				.FirstOrDefault( t => EqualityComparer<TKey>.Default.Equals( mKeySelector.Invoke( t ), key ) );
		}

		public IEnumerable<T> SupportedValues => mSupportedValues;
	}
}
