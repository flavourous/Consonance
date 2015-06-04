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
		RequestStorageHelper<double> calories = new RequestStorageHelper<double> ("calories");
		RequestStorageHelper<double> grams = new RequestStorageHelper<double> ("grams");
		public T[] CreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new T[] 
			{
				calories.CGet(factory.DoubleRequestor)
			};
		}
		public CalorieDietEatEntry  Create ()
		{
			return new CalorieDietEatEntry () { kcals = calories };
		}
		public T[] CalculationFields <T>(IValueRequestFactory<T> factory, FoodInfo info)
		{
			List<T> needed = new List<T> ();
			needed.Add (grams.CGet(factory.DoubleRequestor));
			if (!info.calories.HasValue)
				needed.Add (calories.CGet (factory.DoubleRequestor));
			return needed.ToArray();
		}
		public CalorieDietEatEntry Calculate (FoodInfo info, Predicate shouldComplete)
		{
			if (info.calories == null && shouldComplete ())
				info.calories = calories;
			return new CalorieDietEatEntry () { 
				kcals = (info.calories ?? calories) * ((grams / 100.0) / info.per_hundred_grams),
				info_grams = grams
			};
		}
		public T[] InfoCreationFields<T> (IValueRequestFactory<T> rf)
		{
			return new T[] { 
				calories.CGet(rf.DoubleRequestor)
			};
		}
		public FoodInfo CreateInfo ()
		{
			return new FoodInfo () { calories = calories };
		}
		public Expression<Func<FoodInfo,bool>> IsInfoComplete {get{return info=>info.calories != null;}}
		#endregion
	}
	public class CalorieDietBurnCreation : IEntryCreation<CalorieDietBurnEntry, FireInfo>
	{
		#region IEntryCreation implementation
		RequestStorageHelper<double> caloriesBurned = new RequestStorageHelper<double>("Calories Burned");
		RequestStorageHelper<TimeSpan> burnTime = new RequestStorageHelper<TimeSpan>("Burn Duration");
		public T[] CreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new T[] { 
				caloriesBurned.CGet (factory.DoubleRequestor)
			};
		}
		public CalorieDietBurnEntry Create ()
		{
			return new CalorieDietBurnEntry () { kcals = caloriesBurned };
		}
		public T[] CalculationFields <T>(IValueRequestFactory<T> factory, FireInfo info)
		{
			List<T> needs = new List<T> ();
			needs.Add (burnTime.CGet (factory.TimeSpanRequestor));
			if (info.calories.HasValue)
				needs.Add (caloriesBurned.CGet(factory.DoubleRequestor));
			return needs.ToArray();
		}
		public CalorieDietBurnEntry Calculate (FireInfo info, Predicate shouldComplete)
		{
			if (info.calories == null && shouldComplete ())
				info.calories = caloriesBurned;
			return new CalorieDietBurnEntry () {
				kcals = (info.calories ?? caloriesBurned) * (burnTime.request.value.TotalHours / info.per_hour),
				info_hours = burnTime.request.value.TotalHours
			};
		}
		public T[] InfoCreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new T[] { 
				caloriesBurned.CGet(factory.DoubleRequestor),  
				burnTime.CGet(factory.TimeSpanRequestor)
			};
		}
		public FireInfo CreateInfo ()
		{
			return new FireInfo () { per_hour = 1.0 / burnTime.request.value.TotalHours, calories = caloriesBurned };
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

