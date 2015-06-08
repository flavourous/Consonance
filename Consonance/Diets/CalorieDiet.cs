﻿using System;
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
		RequestStorageHelper<String> dietName = new RequestStorageHelper<string> ("Diet Name");
		RequestStorageHelper<double> dietCalLim = new RequestStorageHelper<double> ("Calorie Limit");
		public IEnumerable<DietWizardPage<T>> DietCreationPages<T>(IValueRequestFactory<T> factory) 
		{
			yield return GetIt<T> ("Create a simple calorie diet", factory);
		}
		public IEnumerable<DietWizardPage<T>> DietEditPages<T> (CalorieDietInstance editing, IValueRequestFactory<T> factory)
		{
			var eros = GetIt<T> ("Edit simple diet", factory);
			dietName.request.value = editing.name;
			dietCalLim.request.value = editing.callim;
			yield return eros;
		}
		DietWizardPage<T> GetIt<T>(String name, IValueRequestFactory<T> factory)
		{
			return new DietWizardPage<T> (name,
				new T[] {
					dietName.CGet(factory.StringRequestor),
					dietCalLim.CGet(factory.DoubleRequestor)
				});
		}
		public CalorieDietInstance NewDiet ()
		{
			return new CalorieDietInstance () { name = dietName, callim = dietCalLim };
		}
		public void EditDiet (CalorieDietInstance toEdit)
		{
			toEdit.callim = dietCalLim;
			toEdit.name = dietName;
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
		public IList<T> CreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new List<T> 
			{
				calories.CGet(factory.DoubleRequestor)
			};
		}
		public CalorieDietEatEntry  Create ()
		{
			return new CalorieDietEatEntry () { kcals = calories };
		}
		public IList<T> CalculationFields <T>(IValueRequestFactory<T> factory, FoodInfo info)
		{
			List<T> needed = new List<T> ();
			needed.Add (grams.CGet(factory.DoubleRequestor));
			if (!info.calories.HasValue)
				needed.Add (calories.CGet (factory.DoubleRequestor));
			return needed;
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
		public IList<T> InfoCreationFields<T> (IValueRequestFactory<T> rf)
		{
			return new List<T> { 
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
		public IList<T> CreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new List<T> { 
				caloriesBurned.CGet (factory.DoubleRequestor)
			};
		}
		public CalorieDietBurnEntry Create ()
		{
			return new CalorieDietBurnEntry () { kcals = caloriesBurned };
		}
		public IList<T> CalculationFields <T>(IValueRequestFactory<T> factory, FireInfo info)
		{
			List<T> needs = new List<T> ();
			needs.Add (burnTime.CGet (factory.TimeSpanRequestor));
			if (info.calories.HasValue)
				needs.Add (caloriesBurned.CGet(factory.DoubleRequestor));
			return needs;
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
		public IList<T> InfoCreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new List<T> { 
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

