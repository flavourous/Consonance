using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Consonance
{
	public class CalorieDietEatEntry : BaseEatEntry
	{
		public double kcals { get; set; }
	}
	public class CalorieDietBurnEntry : BaseBurnEntry
	{
		public double kcals { get; set; }
	}
	public class CalorieDietInstance : DietInstance
	{
		public double callim {get;set;}
	}

	public class CalorieDiet : IDietModel<CalorieDietInstance, CalorieDietEatEntry, FoodInfo, CalorieDietBurnEntry, FireInfo>
	{
		public String name { get { return "Calorie Diet"; } }
		IRequest<double> dietCalLim; 
		IRequest<String> dietName;
		public T[] DietCreationFields<T>(IValueRequestFactory<T> factory) 
		{
			return new T[] 
			{
				(dietName as IValueRequest<T,String> ?? factory.StringRequestor ("Diet Name")).request,
				(dietCalLim as IValueRequest<T,double> ?? factory.DoubleRequestor ("Calorie Limit")).request
			};
		}
		public CalorieDietInstance NewDiet ()
		{
			return new CalorieDietInstance () { name = dietName.value, callim = dietCalLim.value };
		}

		CalorieDietEatCreation cde = new CalorieDietEatCreation();
		CalorieDietBurnCreation cdb = new CalorieDietBurnCreation();

		public IEntryCreation<CalorieDietEatEntry, FoodInfo> foodcreator { get { return cde; } }
		public IEntryCreation<CalorieDietBurnEntry, FireInfo> firecreator { get { return cdb; } }

	}
	public class CalorieDietEatCreation : IEntryCreation<CalorieDietEatEntry, FoodInfo>
	{
		#region IEntryCreation implementation
		IRequest<double> calories, grams;
		public T[] CreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new T[] 
			{
				(calories as IValueRequest<T,double> ?? factory.DoubleRequestor ("Calories")).request 
			};
		}
		public bool Create (out CalorieDietEatEntry entry)
		{
			entry = new CalorieDietEatEntry () { kcals = calories };
			return true;
		}
		public T[] CalculationFields <T>(IValueRequestFactory<T> factory, FoodInfo info)
		{
			List<T> needed = new List<T> ();
			needed.Add ("Grams");
			if (!info.calories.HasValue)
				needed.Add ("Calories");
			return needed.ToArray();
		}
		public bool Calculate (FoodInfo info, IList<double> values, out CalorieDietEatEntry result)
		{
			result = null;
			if (values.Count != CalculationFields (info).Length)
				return false;
			result = new CalorieDietEatEntry () { 
				kcals = (info.calories ?? values [1]) * ((values [0] / 100.0) / info.per_hundred_grams),
				info_grams = values[0]
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
			return new FoodInfo () { calories = values [0] };
		}
		public Expression<Func<FoodInfo,bool>> IsInfoComplete {get{return info=>info.calories != null;}}
		#endregion
	}
	public class CalorieDietBurnCreation : IEntryCreation<CalorieDietBurnEntry, FireInfo>
	{
		#region IEntryCreation implementation
		IRequest<String> caloriesBurned;
		public T[] CreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new T[] { (caloriesBurned as IValueRequest<T,String> ?? factory.StringRequestor ("Calories Burned")).request };
		}
		public bool Create (IList<double> values, out CalorieDietBurnEntry entry)
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
		public bool Calculate (FireInfo info, IList<double> values, out CalorieDietBurnEntry result)
		{
			result = null;
			if (values.Count != CalculationFields (info).Length)
				return false;
			result = new CalorieDietBurnEntry () {
				kcals = (info.calories ?? values [1]) * (values [0] / info.per_hour),
				info_hours = values[0]
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
			return new FireInfo () { per_hour = 1.0 / values [1], calories = values [0] };
		}
		public Expression<Func<FireInfo,bool>> IsInfoComplete { get { return f => f.calories != null;  } }
		#endregion
	}

	// hmmmm calling into presenter is a nasty....abstract class?
	public class CalorieDietPresenter : IDietPresenter<CalorieDietInstance, CalorieDietEatEntry, FoodInfo, CalorieDietBurnEntry, FireInfo>
	{
		#region IDietPresenter implementation
		public EntryLineVM GetRepresentation (CalorieDietEatEntry entry, FoodInfo entryInfo)
		{
			return new EntryLineVM (
				entry.entryWhen,
				entry.entryDur,
				entry.entryName, 
				entryInfo == null ? "" : entryInfo.name, 
				new KVPList<string, double> { { "kcal", entry.kcals } }
			);
		}
		public EntryLineVM GetRepresentation (CalorieDietBurnEntry entry, FireInfo entryInfo)
		{
			return new EntryLineVM (
				entry.entryWhen, 
				entry.entryDur,
				entry.entryName, 
				entryInfo == null ? "" : entryInfo.name, 
				new KVPList<string, double> { { "kcal", entry.kcals } }
			);
		}
		public DietInstanceVM GetRepresentation (CalorieDietInstance entry)
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
		public InfoLineVM GetRepresentation (FoodInfo info)
		{
			return new InfoLineVM () { name = info.name };
		}
		public InfoLineVM GetRepresentation (FireInfo info)
		{
			return new InfoLineVM () { name = info.name };
		}


		public IEnumerable<TrackingInfoVM> DetermineEatTrackingForRange(CalorieDietInstance di, IEnumerable<CalorieDietEatEntry> eats, IEnumerable<CalorieDietBurnEntry> burns, DateTime startBound,  DateTime endBound)
		{
			TrackingInfoVM ti = new TrackingInfoVM () {
				valueName = "Calories Balance",
				targetValue= di.callim
			};

			double kctot = 0.0;
			List<TrackingElementVM> kcin = new List<TrackingElementVM> (), kcout = new List<TrackingElementVM> ();
			foreach (var eat in eats) {
				kcin.Add (new TrackingElementVM () { value = eat.kcals, name = eat.entryName });
				kctot += eat.kcals;
			}
			foreach (var burn in burns) {
						kcout.Add (new TrackingElementVM () { value = burn.kcals, name = burn.entryName });
				kctot -= burn.kcals;
			}

			ti.eatValues = kcin.ToArray ();
			ti.burnValues = kcout.ToArray ();
			yield return ti;
		}

		public IEnumerable<TrackingInfoVM> DetermineBurnTrackingForRange(CalorieDietInstance di, IEnumerable<CalorieDietEatEntry> eats, IEnumerable<CalorieDietBurnEntry> burns, DateTime startBound,  DateTime endBound)
		{
			return DetermineEatTrackingForRange (di, eats, burns, startBound, endBound);
		}
		#endregion
	}
}

