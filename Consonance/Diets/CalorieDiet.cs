using System;
using System.ComponentModel;
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
		RequestStorageHelper<DateTime> dietStart = new RequestStorageHelper<DateTime> ("Start Date");
		RequestStorageHelper<bool> dietEnded = new RequestStorageHelper<bool> ("Ended");
		RequestStorageHelper<DateTime> dietEnd = new RequestStorageHelper<DateTime> ("End Date");
		public IEnumerable<DietWizardPage<T>> DietCreationPages<T>(IValueRequestFactory<T> factory) 
		{
			yield return GetIt<T> ("Create a simple calorie diet", factory);
		}
		public IEnumerable<DietWizardPage<T>> DietEditPages<T> (CalorieDietInstance editing, IValueRequestFactory<T> factory)
		{
			var eros = GetIt<T> ("Edit simple diet", factory, true);
			dietName.request.value = editing.name;
			dietCalLim.request.value = editing.callim;
			dietEnded.request.value = editing.ended.HasValue;
			if (dietEnded) dietEnd.request.value = editing.ended.Value;
			dietStart.request.value = editing.started;
			yield return eros;
		}
		DietWizardPage<T> GetIt<T>(String name, IValueRequestFactory<T> factory, bool edit = false)
		{
			var ad = new BindingList<T> {
				dietName.CGet (factory.StringRequestor),
				dietStart.CGet (factory.DateRequestor),
				dietCalLim.CGet (factory.DoubleRequestor),
			};
			if (edit) {
				ad.Add (dietEnded.CGet(factory.BoolRequestor));
				Action changeAction = () => {
					if(dietEnded) ad.Add(dietEnd.CGet(factory.DateRequestor));
					else ad.Remove(dietEnd.CGet(factory.DateRequestor));
				};
				Action unhook = null;
				unhook = () => {
					dietEnd.request.changed -= changeAction;
					dietEnd.request.ended -= unhook;
				};
				dietEnd.request.changed += changeAction;
				dietEnd.request.ended += unhook;
			}
			return new DietWizardPage<T> (name, ad);
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
		public BindingList<T> CreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new BindingList<T> 
			{
				calories.CGet(factory.DoubleRequestor)
			};
		}
		public CalorieDietEatEntry  Create ()
		{
			return Edit (new CalorieDietEatEntry());
		}
		public BindingList<T> CalculationFields <T>(IValueRequestFactory<T> factory, FoodInfo info)
		{
			var needed = new BindingList<T> ();
			needed.Add (grams.CGet(factory.DoubleRequestor));
			if (!info.calories.HasValue)
				needed.Add (calories.CGet (factory.DoubleRequestor));
			return needed;
		}
		public CalorieDietEatEntry Calculate (FoodInfo info, bool shouldComplete)
		{
			return Edit (new CalorieDietEatEntry (), info, shouldComplete);
		}
		public BindingList<T> InfoFields<T> (IValueRequestFactory<T> rf, FoodInfo willedit=null)
		{
			var ret = new BindingList<T> { 
				calories.CGet(rf.DoubleRequestor),
				grams.CGet(rf.DoubleRequestor)
			};
			if (willedit != null) {
				calories.request.value = willedit.calories ?? 0.0;
				grams.request.value = willedit.per_hundred_grams * 100.0;
			}
			return ret;
		}
		public FoodInfo MakeInfo (FoodInfo edit=null)
		{
			var use = edit ?? new FoodInfo ();
			use.calories = calories;
			use.per_hundred_grams = grams / 100.0;
			return use;
		}
		public Expression<Func<FoodInfo,bool>> IsInfoComplete {get{return info=>info.calories != null;}}

		public BindingList<T> EditFields<T> (CalorieDietEatEntry toEdit, IValueRequestFactory<T> factory)
		{
			var ret = CreationFields (factory);
			calories.request.value = toEdit.kcals;
			return ret;
		}

		public CalorieDietEatEntry Edit (CalorieDietEatEntry toEdit)
		{
			toEdit.kcals = calories;
			return toEdit;
		}

		public BindingList<T> EditFields<T> (CalorieDietEatEntry toEdit, IValueRequestFactory<T> factory, FoodInfo info)
		{
			var ret = CalculationFields (factory, info);
			calories.request.value = toEdit.kcals;
			return ret;
		}

		public CalorieDietEatEntry Edit (CalorieDietEatEntry toEdit, FoodInfo info, bool shouldComplete)
		{
			if (info.calories == null && shouldComplete)
				info.calories = calories;
			toEdit.kcals = (info.calories ?? calories) * ((grams / 100.0) / info.per_hundred_grams);
			toEdit.info_grams = grams;
			return toEdit;
		}
		#endregion
	}
	public class CalorieDietBurnCreation : IEntryCreation<CalorieDietBurnEntry, FireInfo>
	{
		#region IEntryCreation implementation
		RequestStorageHelper<double> caloriesBurned = new RequestStorageHelper<double>("Calories Burned");
		RequestStorageHelper<TimeSpan> burnTime = new RequestStorageHelper<TimeSpan>("Burn Duration");
		public BindingList<T> CreationFields<T> (IValueRequestFactory<T> factory)
		{
			return new BindingList<T> { 
				caloriesBurned.CGet (factory.DoubleRequestor),
			};
		}
		public CalorieDietBurnEntry Create ()
		{
			var ret = new CalorieDietBurnEntry ();
			Edit (ret);
			return ret;
		}
		public BindingList<T> CalculationFields <T>(IValueRequestFactory<T> factory, FireInfo info)
		{
			var needs = new BindingList<T> ();
			needs.Add (burnTime.CGet (factory.TimeSpanRequestor));
			if (info.calories.HasValue)
				needs.Add (caloriesBurned.CGet(factory.DoubleRequestor));
			return needs;
		}
		public CalorieDietBurnEntry Calculate (FireInfo info, bool shouldComplete)
		{
			var ret = new CalorieDietBurnEntry ();
			Edit (ret, info, shouldComplete);
			return ret;
		}
		public BindingList<T> InfoFields<T> (IValueRequestFactory<T> factory, FireInfo willedit = null)
		{
			var ret = new BindingList<T> { 
				caloriesBurned.CGet(factory.DoubleRequestor),  
				burnTime.CGet(factory.TimeSpanRequestor)
			};

			return ret;
		}
		public FireInfo MakeInfo (FireInfo edit = null)
		{
			var use = edit ?? new FireInfo ();
			use.per_hour = 1.0 / burnTime.request.value.TotalHours;
			use.calories = caloriesBurned;
			return use;
		}
		public Expression<Func<FireInfo,bool>> IsInfoComplete { get { return f => f.calories != null;  } }

		public BindingList<T> EditFields<T> (CalorieDietBurnEntry toEdit, IValueRequestFactory<T> factory)
		{
			var ret = CreationFields<T> (factory);
			caloriesBurned.request.value = toEdit.kcals;
			return ret;
		}

		public CalorieDietBurnEntry Edit (CalorieDietBurnEntry toEdit)
		{
			toEdit.kcals = caloriesBurned;
			return toEdit;
		}

		public BindingList<T> EditFields<T> (CalorieDietBurnEntry toEdit, IValueRequestFactory<T> factory, FireInfo info)
		{
			var ret = CalculationFields<T> (factory, info);
			caloriesBurned.request.value = toEdit.kcals;
			return ret;
		}

		public CalorieDietBurnEntry Edit (CalorieDietBurnEntry toEdit, FireInfo info, bool shouldComplete)
		{
			if (info.calories == null && shouldComplete)
				info.calories = caloriesBurned;
			toEdit.kcals = (info.calories ?? caloriesBurned) * (burnTime.request.value.TotalHours / info.per_hour);
			toEdit.entryDur = TimeSpan.FromHours(burnTime.request.value.TotalHours);
			return toEdit;
		}
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

