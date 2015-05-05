using System;
using System.Collections.Generic;
using SQLite;

namespace ManyDiet
{
	public class KVPList<T1,T2> : List<KeyValuePair<T1,T2>>
	{
		public void Add(T1 a, T2 b) 
		{
			Add (new KeyValuePair<T1, T2> (a, b));
		}
	}
	// basic workings of presenter and VMs, to be rehomed.
	public class EntryLineVM
	{
		public readonly DateTime when;
		public readonly String name;
		public readonly String desc;
		public readonly KVPList<string,double> displayAmounts;

		public EntryLineVM(DateTime w, String n, String d, KVPList<string,double>  t)
		{
			when=w; name=n; desc=d;
			displayAmounts = t;
		}
	}
	public abstract class DietPresenter<DietInstType, EatType, EatInfoType, BurnType, BurnInfoType> : IDietPresenter
	{
		// injected on registration
		public SQLiteConnection conn { private get; set; }

		// Helper
		protected T FindInfo<T> (int? id) where T : BaseInfo, new()
		{
			if (!id.HasValue)
				return null;
			var res = new List<T> (conn.Table<T> ().Where (fi => fi.id == id));
			if (res.Count == 0)
				return null;
			return res [0];
		}

		public abstract EntryLineVM GetLineRepresentation (BaseEatEntry entry);
		public abstract EntryLineVM GetLineRepresentation (BaseBurnEntry entry);
		public abstract EntryLineVM GetLineRepresentation (DietInstance entry);

		public abstract SelectableItemVM GetRepresentation (FoodInfo info);
		public abstract SelectableItemVM GetRepresentation (FireInfo info);
	}
	public interface IDietPresenter
	{
		// Representing eat and burn items
		EntryLineVM GetLineRepresentation(BaseEatEntry entry);
		EntryLineVM GetLineRepresentation(BaseBurnEntry entry);
		EntryLineVM GetLineRepresentation (DietInstance entry);

		// Representing FoodInfo and FireInfo items
		SelectableItemVM GetRepresentation (FoodInfo info);
		SelectableItemVM GetRepresentation (FireInfo info);
	}

	class IndexEnumerator<T> : IEnumerator<T>
	{
		IReadOnlyList<T> items;
		int curr = 0;
		public IndexEnumerator(IReadOnlyList<T> items)
		{
			this.items = items;
		}

		#region IEnumerator implementation
		public bool MoveNext () { return ++curr < items.Count; }
		public void Reset () { curr = 0; }
		object System.Collections.IEnumerator.Current { get { return items [curr]; } }
		#endregion

		#region IDisposable implementation
		public void Dispose () { }
		#endregion

		#region IEnumerator implementation
		T IEnumerator<T>.Current { get { return items [curr]; } }
		#endregion
	}
	delegate SelectableItemVM CreateSelectableVM<T>(T item);
	class SelectVMListDecorator<T> : IReadOnlyList<SelectableItemVM> where T : BaseInfo
	{
		IList<T> items;
		CreateSelectableVM<T> creator;
		public SelectVMListDecorator(IList<T> items, CreateSelectableVM<T> creator)
		{
			this.items = items;
			this.creator = creator;
		}

		#region IEnumerable implementation
		public IEnumerator<SelectableItemVM> GetEnumerator ()
		{
			return new IndexEnumerator<SelectableItemVM> (this);
		}
		#endregion
		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return new IndexEnumerator<SelectableItemVM> (this);
		}
		#endregion
		#region IReadOnlyList implementation
		public SelectableItemVM this [int index] {
			get {
				return creator (items [index]);
			}
		}
		#endregion
		#region IReadOnlyCollection implementation
		public int Count {
			get {
				return items.Count;
			}
		}
		#endregion
	}
}

