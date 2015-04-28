using System;
using System.Collections.Generic;

namespace ManyDiet
{
	public class CalorieDietEatEntry : BaseEatEntry
	{
		public String myname {get;set;}
		public double kcals { get; set; }
	}
	public class CalorieDietEatInfo : FoodInfo
	{
	}
	public class CalorieDietBurnEntry : BaseBurnEntry
	{
		public String myname {get;set;}
		public double kcals { get; set; }
	}
	public class CalorieDietBurnInfo : FireInfo
	{
	}

	public class CalorieDiet : IDietModel<CalorieDietEatEntry, CalorieDietEatInfo, CalorieDietBurnEntry, CalorieDietBurnInfo>
	{
		public DietInstance NewDiet ()
		{
			throw new NotImplementedException ();
		}

		public IEntryCreation<BaseEatEntry, FoodInfo> foodcreator {
			get {
				throw new NotImplementedException ();
			}
		}
		public IEntryCreation<BaseBurnEntry, FireInfo> firecreator {
			get {
				throw new NotImplementedException ();
			}
		}
	}

	// hmmmm calling into presenter is a nasty....abstract class?
	public class CalorieDietPresenter : DietPresenter<CalorieDietEatEntry, CalorieDietEatInfo, CalorieDietBurnEntry, CalorieDietBurnInfo>
	{
		#region IDietPresenter implementation
		public override EntryLineVM GetLineRepresentation (BaseEatEntry entry)
		{
			var ent = (entry as CalorieDietEatEntry);
			var fi = FindInfo<FoodInfo> (ent.infoinstanceid);
			return new EntryLineVM (
				ent.entryWhen, 
				ent.myname, 
				fi == null ? "" : fi.name, 
				new KVPList<string, double> { { "kcal", ent.kcals } }
			);
		}
		public override EntryLineVM GetLineRepresentation (BaseBurnEntry entry)
		{
			var ent = (entry as CalorieDietBurnEntry);
			var fi = FindInfo<FireInfo> (ent.infoinstanceid);
			return new EntryLineVM (
				ent.entryWhen, 
				ent.myname, 
				fi == null ? "" : fi.name, 
				new KVPList<string, double> { { "kcal", ent.kcals } }
			);
		}
		#endregion
	}
}

