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
	public class CalorieDietInstance : DietInstance
	{
		public double callim;
	}

	public class CalorieDiet : IDietModel<CalorieDietInstance, CalorieDietEatEntry, CalorieDietEatInfo, CalorieDietBurnEntry, CalorieDietBurnInfo>
	{
		public String name { get { return "Calorie Diet"; } }
		public String[] DietCreationFields() 
		{
			return new string[] { "Calorie Limit" };
		}
		public DietInstance NewDiet (double[] values)
		{
			return new CalorieDietInstance () { name = "Calorie Diet", callim = values[0] };
		}
		public bool IsDietInstance(DietInstance di)
		{
			return di is CalorieDietInstance;
		}

		CalorieDietEatCreation cde = new CalorieDietEatCreation();
		CalorieDietBurnCreation cdb = new CalorieDietBurnCreation();

		public IEntryCreation<BaseEatEntry, FoodInfo> foodcreator { get { return cde; } }
		public IEntryCreation<BaseBurnEntry, FireInfo> firecreator { get { return cdb; } }

		public IEnumerable<TrackingInfo> DetermineEatTrackingForRange (IEnumerable<BaseEatEntry> eats, IEnumerable<BaseBurnEntry> burns, DateTime startBound, DateTime endBound)
		{
			TrackingInfo ti = new TrackingInfo () {
				valueName = "Calories Balance"
			};

			double kctot = 0.0;
			List<double> kcin = new List<double> (), kcout = new List<double> ();
			foreach (var eat in eats) {
				var ke = ((CalorieDietEatEntry)eat);
				kcin.Add (ke.kcals);
				kctot += ke.kcals;
			}
			foreach (var burn in burns) {
				var ke = ((CalorieDietBurnEntry)burn);
				kcout.Add (ke.kcals);
				kctot -= ke.kcals;
			}

			ti.eatValues = kcin.ToArray ();
			ti.eatSources = new List<BaseEatEntry> (eats).ToArray ();
			ti.burnValues = kcout.ToArray ();
			ti.burnSources = new List<BaseBurnEntry> (burns).ToArray ();

			yield return ti;
		}

		public IEnumerable<TrackingInfo> DetermineBurnTrackingForRange (IEnumerable<BaseEatEntry> eats, IEnumerable<BaseBurnEntry> burns, DateTime startBound, DateTime endBound)
		{
			return DetermineEatTrackingForRange (eats, burns, startBound, endBound);
		}
	}
	public class CalorieDietEatCreation :IEntryCreation<BaseEatEntry, FoodInfo>
	{
		#region IEntryCreation implementation
		public string[] CreationFields ()
		{
			return new string[] { "Calories" };
		}
		public bool Create (IList<double> values, out BaseEatEntry entry)
		{
			entry = new CalorieDietEatEntry () { kcals = values [0] };
			return true;
		}
		public string[] CalculationFields (FoodInfo info)
		{
			List<String> needed = new List<string> ();
			needed.Add ("Grams");
			if (!info.calories.HasValue)
				needed.Add ("Calories");
			return needed.ToArray();
		}
		public bool Calculate (FoodInfo info, IList<double> values, out BaseEatEntry result)
		{
			result = null;
			if (values.Count != CalculationFields (info).Length)
				return false;
			result = new CalorieDietEatEntry () { 
				kcals = (info.calories ?? values [1]) * ((values [0] / 100.0) / info.per_hundred_grams)
			};
			return true;
		}
		public void CompleteInfo (ref FoodInfo toComplete, IList<double> values)
		{
			toComplete.calories = values [1];
		}
		public string[] InfoCreationFields ()
		{
			return new string[] { "Calories" };
		}
		public FoodInfo CreateInfo (IList<double> values)
		{
			return new CalorieDietEatInfo () { calories = values [0] };
		}
		public bool IsInfoComplete (FoodInfo info)
		{
			return info.calories.HasValue;
		}
		#endregion
	}
	public class CalorieDietBurnCreation :IEntryCreation<BaseBurnEntry, FireInfo>
	{
		#region IEntryCreation implementation
		public string[] CreationFields ()
		{
			return new string[] { "Cal Burned" };
		}
		public bool Create (IList<double> values, out BaseBurnEntry entry)
		{
			entry = null;
			if (values.Count != 1)
				return false;
			entry = new CalorieDietBurnEntry () { kcals = values [0] };
			return true;
		}
		public string[] CalculationFields (FireInfo info)
		{
			List<String> needs = new List<string> ();
			needs.Add ("Duration (h)");
			if (info.calories.HasValue)
				needs.Add ("Calories Burned");
			return needs.ToArray();
		}
		public bool Calculate (FireInfo info, IList<double> values, out BaseBurnEntry result)
		{
			result = null;
			if (values.Count != CalculationFields (info).Length)
				return false;
			result = new CalorieDietBurnEntry () {
				kcals = (info.calories ?? values [1]) * (values [0] / info.per_hour)
			};
			return true;
		}
		public void CompleteInfo (ref FireInfo toComplete, IList<double> values)
		{
			toComplete.calories = values [0];
		}
		public string[] InfoCreationFields ()
		{
			return new String[] { "Calories Burned", "Duration" };
		}
		public FireInfo CreateInfo (IList<double> values)
		{
			return new CalorieDietBurnInfo () { per_hour = 1.0 / values [1], calories = values [0] };
		}
		public bool IsInfoComplete (FireInfo info)
		{
			return info.calories.HasValue;
		}
		#endregion
	}

	// hmmmm calling into presenter is a nasty....abstract class?
	public class CalorieDietPresenter : DietPresenter<CalorieDietInstance, CalorieDietEatEntry, CalorieDietEatInfo, CalorieDietBurnEntry, CalorieDietBurnInfo>
	{
		#region IDietPresenter implementation
		public override EntryLineVM GetRepresentation (BaseEatEntry entry)
		{
			var ent = (entry as CalorieDietEatEntry);
			var fi = FindInfo<FoodInfo> (ent.infoinstanceid);
			return new EntryLineVM (
				ent.entryWhen,
				ent.entryDur,
				ent.myname, 
				fi == null ? "" : fi.name, 
				new KVPList<string, double> { { "kcal", ent.kcals } }
			);
		}
		public override EntryLineVM GetRepresentation (BaseBurnEntry entry)
		{
			var ent = (entry as CalorieDietBurnEntry);
			var fi = FindInfo<FireInfo> (ent.infoinstanceid);
			return new EntryLineVM (
				ent.entryWhen, 
				ent.entryDur,
				ent.myname, 
				fi == null ? "" : fi.name, 
				new KVPList<string, double> { { "kcal", ent.kcals } }
			);
		}
		public override DietInstanceVM GetRepresentation (DietInstance entry)
		{
			var ent = (entry as CalorieDietInstance);
			return new DietInstanceVM(
				ent.started, 
				ent.ended,
				ent.name, 
				"",
				new KVPList<string, double> { { "kcal", ent.callim } }
			);
		}
		public override SelectableItemVM GetRepresentation (FoodInfo info)
		{
			return new SelectableItemVM () { name = info.name };
		}
		public override SelectableItemVM GetRepresentation (FireInfo info)
		{
			return new SelectableItemVM () { name = info.name };
		}
		#endregion
	}
}

