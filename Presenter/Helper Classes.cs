using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel;

namespace Consonance
{
	public static class EnumerableExtenstions
	{
		public static List<Out> MakeList<In,Out>(this IEnumerable<In> myself, Func<In, Out> creator)
		{
			return new List<Out>(from s in myself select creator(s));
		}
		public static BindingList<Out> MakeBindingList<In,Out>(this IEnumerable<In> myself, Func<In, Out> creator)
		{
			return new BindingList<Out>(new List<Out>(from s in myself select creator(s)));
		}
		public static TList MakeSomething<In,Out,TList>(this IEnumerable<In> myself, Func<In, Out> creator, Func<IEnumerable<Out>,TList> listCreator)
		{
			return listCreator(from s in myself select creator(s));
		}
		public static void RemoveAll<T>(this IList<T> list, params T[] items)
		{
			foreach (var t in items)
				list.Remove (t);
		}
		public static void AddAll<T>(this IList<T> list, bool allowDuplicates, params T[] items)
		{
			foreach (var t in items)
				if(allowDuplicates || !list.Contains(t))
					list.Add (t);
		}
		public static void Ensure<T>(this IList<T> list, int index, params T[] items)
		{
			foreach (var t in items)
				if (list [index] != t) {
					list.Remove (t);
					list.Insert (index, t);
					index++;
				}
		}
	}
	public static class SerialiserFactory
	{
		static BinaryFormatter bf = new BinaryFormatter ();
		public static byte[] Serialize(Object graph)
		{
			MemoryStream ms = new MemoryStream ();
			bf.Serialize (ms, graph);
			return ms.ToArray ();
		}
		public static T Deserialize<T>(byte[] data)
		{
			return (T)bf.Deserialize (new MemoryStream (data));
		}
	}
	class HookedInfoLines : IDisposable
	{
		readonly InfoManageType imt;
		readonly IAbstractedTracker cdh;
		public readonly ObservableCollection<InfoLineVM> lines;
		public HookedInfoLines(IAbstractedTracker cdh, InfoManageType imt)
		{
			this.imt = imt;
			this.cdh = cdh;
			this.lines = new ObservableCollection<InfoLineVM> ();
			cdh.ViewModelsChanged += Cdh_ViewModelsChanged;;
			PushInLinesAndFire ();
		}

		void Cdh_ViewModelsChanged (IAbstractedTracker sender, DietVMChangeEventArgs args)
		{
			PushInLinesAndFire ();
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			cdh.ViewModelsChanged -= Cdh_ViewModelsChanged;;
		}
		#endregion

		void PushInLinesAndFire()
		{
			lines.Clear ();
			switch (imt) {
			case InfoManageType.In:
				foreach (var ii in cdh.InInfos (false))
					lines.Add (ii);
				break;
			case InfoManageType.Out:
				foreach (var oi in cdh.OutInfos (false))
					lines.Add (oi);
				break;
			}
		}
	}

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

