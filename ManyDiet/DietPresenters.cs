using System;
using System.Collections.Generic;

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
	public class EatEntryLineVM
	{
		public readonly DateTime when;
		public readonly String name;
		public readonly String desc;
		public readonly KVPList<string,double> trackedAmounts;

		public EatEntryLineVM(DateTime w, String n, String d, KVPList<string,double>  t)
		{
			when=w; name=n; desc=d;
			trackedAmounts = t;
		}
	}
	public interface IDietPresenter<T> : IDietPresenter where T : BaseDietEntry { }
	public interface IDietPresenter
	{
		// other presenters probabbbly call into here to get representations of the model domain...
		EatEntryLineVM GetLineRepresentation(BaseDietEntry entry);
		// EntryAggregateVM GetAggregationRepresentation(DateTime start, DateTime end);
	}
}

