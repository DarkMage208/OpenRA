﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using System.Collections;

namespace OpenRa.Collections
{
	public class Set<T> : IEnumerable<T>
	{
		Dictionary<T, bool> data = new Dictionary<T, bool>();

		public void Add( T obj )
		{
			data.Add( obj, false );
			if( OnAdd != null )
				OnAdd( obj );
		}

		public void Remove( T obj )
		{
			data.Remove( obj );
			if( OnRemove != null )
				OnRemove( obj );
		}

		public event Action<T> OnAdd;
		public event Action<T> OnRemove;

		public IEnumerator<T> GetEnumerator()
		{
			return data.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public class CachedView<T,U> : Set<U>
	{
		public CachedView( Set<T> set, Func<T,bool> include, Func<T,U> store )
		{
			foreach( var t in set )
				if( include( t ) )
					Add( store( t ) );

			set.OnAdd += obj => { if( include( obj ) ) Add( store( obj ) ); };
			set.OnRemove += obj => { if( include( obj ) ) Remove( store( obj ) ); };
		}
	}
}
