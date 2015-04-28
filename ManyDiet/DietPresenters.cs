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
	public abstract class DietPresenter<EatType, EatInfoType, BurnType, BurnInfoType> : IDietPresenter
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
	}
	public interface IDietPresenter
	{
		// Representing eat and burn items
		EntryLineVM GetLineRepresentation(BaseEatEntry entry);
		EntryLineVM GetLineRepresentation(BaseBurnEntry entry);

		// Representing FoodInfo and FireInfo items
		// ...
	}
}

