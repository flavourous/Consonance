﻿using System;
using System.Collections.Generic;
using System.Collections;

namespace Consonance
{
	class ReadOnlyListConversionAdapter<In,Out> : IReadOnlyList<Out>
	{
		readonly IReadOnlyList<In> input;
		readonly Func<In,Out> convertDelegate;
		public ReadOnlyListConversionAdapter(IReadOnlyList<In> input, Func<In,Out> convertDelegate)
		{
			this.input = input;
			this.convertDelegate = convertDelegate;
		}

		#region IEnumerable implementation
		public IEnumerator<Out> GetEnumerator ()
		{
			return new ListConverterEnumerator<In,Out> (input, convertDelegate);
		}
		IEnumerator IEnumerable.GetEnumerator () { return GetEnumerator (); }
		#endregion

		#region IReadOnlyList implementation
		public Out this [int index] { get { return convertDelegate (input [index]); } }
		#endregion
		#region IReadOnlyCollection implementation
		public int Count { get { return input.Count; } }
		#endregion
	}


	public class ListEnumerator<T> : IEnumerator<T>
	{
		readonly IReadOnlyList<T> dd;
		int st = -1;
		public ListEnumerator(IReadOnlyList<T> dd)
		{
			this.dd = dd;
		}
		#region IEnumerator implementation
		public bool MoveNext ()
		{
			st++;
			return st < dd.Count;
		}
		public void Reset ()
		{
			st = 0; 
		}
		T current { get { return dd [st]; } }
		object IEnumerator.Current { get { return current; } }
		public virtual T Current { get { return current; } }
		#endregion
		public void Dispose () { }
	}
	public class ListConverterEnumerator<TIn, TOut> : IEnumerator<TOut>
	{
		readonly IReadOnlyList<TIn> dd;
		readonly Func<TIn,TOut> cdel;
		int st = -1;
		public ListConverterEnumerator(IReadOnlyList<TIn> dd, Func<TIn,TOut> cdel)
		{
			this.dd = dd;
			this.cdel = cdel;
		}
		#region IEnumerator implementation
		public bool MoveNext ()
		{
			st++;
			return st < dd.Count;
		}
		public void Reset ()
		{
			st = 0; 
		}
		TOut current { get { return cdel(dd [st]); } }
		object IEnumerator.Current { get { return current; } }
		public virtual TOut Current { get { return current; } }
		#endregion
		public void Dispose () { }
	}
}
