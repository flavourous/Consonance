﻿using System;
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
	public class CalorieDietInstance : TrackerInstance
	{
		public double callim {get;set;}
	}

	public class CalorieDiet : ITrackModel<CalorieDietInstance, CalorieDietEatEntry, FoodInfo, CalorieDietBurnEntry, FireInfo>
	{
		RequestStorageHelper<String> dietName;
		RequestStorageHelper<DateTime> dietStart;
		RequestStorageHelper<bool> dietEnded;
		RequestStorageHelper<DateTime> dietEnd;
		RequestStorageHelper<double> dietCalLim;
		public CalorieDiet()
		{
			dietName = new RequestStorageHelper<string> ("Diet Name",()=>"",Validate);
			dietCalLim = new RequestStorageHelper<double> ("Calorie Limit",()=>0.0,Validate);
			dietStart = new RequestStorageHelper<DateTime> ("Start Date",()=>DateTime.Now,Validate);
			dietEnded = new RequestStorageHelper<bool> ("Ended",()=>false,Validate);
			dietEnd = new RequestStorageHelper<DateTime> ("End Date",()=>DateTime.Now,Validate);
		}

		Action toValidate = delegate { };
		void Validate() { toValidate (); }

		public IEnumerable<TrackerWizardPage> CreationPages(IValueRequestFactory factory) 
		{
			toValidate = ValidateCreate;
			yield return GetIt ("Create a simple calorie diet", factory);
			Validate ();
		}
		public IEnumerable<TrackerWizardPage> EditPages(CalorieDietInstance editing, IValueRequestFactory factory)
		{
			toValidate = ValidateEdit;
			var eros = GetIt ("Edit simple diet", factory, true);
			dietName.request.value = editing.name;
			dietCalLim.request.value = editing.callim;
			dietEnded.request.value = editing.hasEnded;
			if (dietEnded) dietEnd.request.value = editing.ended;
			dietStart.request.value = editing.started;
			yield return eros;
			Validate ();
		}
		void ValidateCreate()
		{
			dietName.request.valid = !string.IsNullOrWhiteSpace (dietName);
			dietCalLim.request.valid = dietCalLim > 0;
			dietStart.request.valid = true;
		}
		TrackerWizardPage GetIt(String name, IValueRequestFactory factory, bool edit = false)
		{
			dietName.Reset ();
			dietStart.Reset ();
			dietCalLim.Reset ();
			var ad = new BindingList<Object> {
				dietName.CGet (factory.StringRequestor),
				dietStart.CGet (factory.DateRequestor),
				dietCalLim.CGet (factory.DoubleRequestor),
			};

			// cause of validation these need alive before callbacks
			var de = dietEnded.CGet (factory.BoolRequestor);
			var ded = dietEnd.CGet (factory.DateRequestor);

			if (edit) {
				dietEnded.Reset ();
				dietEnd.Reset ();
				ad.Add (de);
				dietEnded.request.changed += () => {
					if(dietEnded) ad.Add(ded);
					else ad.Remove(ded);
				};
			}
			return new TrackerWizardPage (name, ad);
		}
		void ValidateEdit()
		{
			dietName.request.valid = !string.IsNullOrWhiteSpace (dietName);
			dietCalLim.request.valid = dietCalLim > 0;
			dietStart.request.valid = !dietEnded || dietStart.request.value < dietEnd;
			dietEnded.request.valid = true;
			if(dietEnded)
				dietEnd.request.valid = !dietEnded || dietEnd > dietStart.request.value;
		}
		public CalorieDietInstance New ()
		{
			return new CalorieDietInstance () { name = dietName, callim = dietCalLim, started = dietStart };
		}
		public void Edit (CalorieDietInstance toEdit)
		{
			toEdit.callim = dietCalLim;
			toEdit.name = dietName;
			toEdit.started = dietStart;
			toEdit.hasEnded = dietEnded;
			if (dietEnded)
				toEdit.ended = dietEnd;
		}

		CalorieDietEatCreation cde = new CalorieDietEatCreation();
		CalorieDietBurnCreation cdb = new CalorieDietBurnCreation();

		public IEntryCreation<CalorieDietEatEntry, FoodInfo> increator { get { return cde; } }
		public IEntryCreation<CalorieDietBurnEntry, FireInfo> outcreator { get { return cdb; } }
	}
	public class CalorieDietEatCreation : IEntryCreation<CalorieDietEatEntry, FoodInfo>
	{
		#region IEntryCreation implementation
		RequestStorageHelper<double> calories;
		RequestStorageHelper<double> grams;
		RequestStorageHelper<String> name;
		RequestStorageHelper<DateTime> when;
		public CalorieDietEatCreation()
		{
			calories = new RequestStorageHelper<double> ("calories",()=>0.0,Validate);
			grams = new RequestStorageHelper<double> ("grams",()=>0.0,Validate);
			name = new RequestStorageHelper<string> ("name",()=>"",Validate);
			when = new RequestStorageHelper<DateTime>("when", () => DateTime.Now,Validate);
		}

		Action toValidate = delegate { };
		void Validate() { toValidate (); }

		public void ResetRequests ()
		{
			calories.Reset ();
			grams.Reset ();
			name.Reset ();
			when.Reset ();
		}

		public BindingList<Object> CreationFields (IValueRequestFactory factory)
		{
			toValidate = CheckCreationValidity;
			return new BindingList<Object> 
			{
				name.CGet(factory.StringRequestor),
				when.CGet(factory.DateRequestor),
				calories.CGet(factory.DoubleRequestor)
			};
		}
		void CheckCreationValidity ()
		{
			name.request.valid = !String.IsNullOrWhiteSpace (name);
			when.request.valid = true;
			calories.request.valid = calories >= 0.0;
		}
		public CalorieDietEatEntry  Create ()
		{
			return Edit (new CalorieDietEatEntry());
		}
		public BindingList<Object> CalculationFields (IValueRequestFactory factory, FoodInfo info)
		{
			// fuck this is missing name!! oh well. it's not implied anywhere...hmm
			toValidate = () => CheckCalculationValidity(info);
			var needed = new BindingList<Object> () {
				when.CGet(factory.DateRequestor),				
				grams.CGet (factory.DoubleRequestor),
			};
			if (!info.calories.HasValue)
				needed.Add (calories.CGet (factory.DoubleRequestor));
			return needed;
		}
		void CheckCalculationValidity (FoodInfo info)
		{
			when.request.valid = true;
			grams.request.valid = grams >= 0.0;
			if (!info.calories.HasValue)
				calories.request.valid = calories >= 0;
		}
		public CalorieDietEatEntry Calculate (FoodInfo info, bool shouldComplete)
		{
			name.request.value = grams.request.value + "g of " + info.name;
			return Edit (new CalorieDietEatEntry (), info, shouldComplete);
		}
		public BindingList<Object> InfoFields (IValueRequestFactory rf)
		{
			toValidate = CheckInfoValidity;
			var ret = new BindingList<Object> { 
				name.CGet(rf.StringRequestor),
				calories.CGet(rf.DoubleRequestor),
				grams.CGet(rf.DoubleRequestor)
			};
			return ret;
		}

		public void FillRequestData (FoodInfo item)
		{
			name.request.value = item.name;
			calories.request.value = item.calories ?? 0.0;
			grams.request.value = item.per_hundred_grams * 100.0;
		}

		void CheckInfoValidity()
		{
			name.request.valid = !String.IsNullOrWhiteSpace (name);
			calories.request.valid = calories >= 0;
			grams.request.valid = grams > 0;
		}
		public FoodInfo MakeInfo (FoodInfo edit=null)
		{
			var use = edit ?? new FoodInfo ();
			use.name = name;
			use.calories = calories;
			use.per_hundred_grams = grams / 100.0;
			return use;
		}
		public Expression<Func<FoodInfo,bool>> IsInfoComplete {get{return info=>info.calories != null;}}

		public BindingList<Object> EditFields (CalorieDietEatEntry toEdit, IValueRequestFactory factory)
		{
			var ret = CreationFields (factory);
			calories.request.value = toEdit.kcals;
			return ret;
		}

		public CalorieDietEatEntry Edit (CalorieDietEatEntry toEdit)
		{
			toEdit.entryName = name;
			toEdit.entryWhen = when;
			toEdit.kcals = calories;
			return toEdit;
		}

		public BindingList<Object> EditFields (CalorieDietEatEntry toEdit, IValueRequestFactory factory, FoodInfo info)
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
			toEdit.entryName = name;
			toEdit.entryWhen = when;
			return toEdit;
		}
		#endregion
	}
	public class CalorieDietBurnCreation : IEntryCreation<CalorieDietBurnEntry, FireInfo>
	{
		#region IEntryCreation implementation
		RequestStorageHelper<double> caloriesBurned;
		RequestStorageHelper<TimeSpan> burnTime;
		RequestStorageHelper<String> name;
		RequestStorageHelper<DateTime> when;
		public CalorieDietBurnCreation()
		{
			caloriesBurned = new RequestStorageHelper<double>("Calories Burned",()=>0.0,Validate);
			burnTime = new RequestStorageHelper<TimeSpan>("Burn Duration",()=>TimeSpan.Zero,Validate);
			name = new RequestStorageHelper<string> ("name",()=>"",Validate);
			when = new RequestStorageHelper<DateTime>("when",()=>DateTime.Now,Validate);
		}

		Action toValidate = delegate { };
		void Validate() { toValidate (); }

		public void ResetRequests ()
		{
			caloriesBurned.Reset ();
			burnTime.Reset ();
			name.Reset ();
			when.Reset ();
		}

		public BindingList<Object> CreationFields (IValueRequestFactory factory)
		{
			toValidate = CheckCreationValidity;
			return new BindingList<Object> { 
				name.CGet(factory.StringRequestor),
				when.CGet(factory.DateRequestor),				
				caloriesBurned.CGet (factory.DoubleRequestor),
			};
		}
		void CheckCreationValidity ()
		{
			name.request.valid = !String.IsNullOrWhiteSpace (name);
			when.request.valid = true;
			caloriesBurned.request.valid = caloriesBurned >= 0.0;
		}
		public CalorieDietBurnEntry Create ()
		{
			var ret = new CalorieDietBurnEntry ();
			Edit (ret);
			return ret;
		}
		public BindingList<Object> CalculationFields (IValueRequestFactory factory, FireInfo info)
		{
			toValidate = () => CheckCalculationValidity (info);
			var needs = new BindingList<Object> {
				when.CGet(factory.DateRequestor),				
				burnTime.CGet (factory.TimeSpanRequestor)
			};
			if (!info.calories.HasValue)
				needs.Add (caloriesBurned.CGet(factory.DoubleRequestor));
			return needs;
		}
		void CheckCalculationValidity (FireInfo info)
		{
			when.request.valid = true;
			burnTime.request.valid = burnTime.request.value.TotalHours >= 0.0;
			if (!info.calories.HasValue)
				caloriesBurned.request.valid = caloriesBurned >= 0;
		}
		public CalorieDietBurnEntry Calculate (FireInfo info, bool shouldComplete)
		{
			var ret = new CalorieDietBurnEntry ();
			Edit (ret, info, shouldComplete);
			return ret;
		}
		public BindingList<Object> InfoFields (IValueRequestFactory factory)
		{
			toValidate = CheckInfoValidity;
			var ret = new BindingList<Object> { 
				name.CGet(factory.StringRequestor),
				caloriesBurned.CGet(factory.DoubleRequestor),  
				burnTime.CGet(factory.TimeSpanRequestor)
			};
			return ret;
		}

		public void FillRequestData (FireInfo item)
		{
			name.request.value = item.name;
			caloriesBurned.request.value = item.calories ?? 0.0;
			burnTime.request.value = TimeSpan.FromHours (item.per_hour);
		}

		void CheckInfoValidity()
		{
			name.request.valid = !String.IsNullOrWhiteSpace (name);
			caloriesBurned.request.valid = caloriesBurned >= 0;
			burnTime.request.valid = burnTime.request.value.TotalHours >= 0;
		}
		public FireInfo MakeInfo (FireInfo edit = null)
		{
			var use = edit ?? new FireInfo ();
			use.name = name;
			use.per_hour = 1.0 / burnTime.request.value.TotalHours;
			use.calories = caloriesBurned;
			return use;
		}
		public Expression<Func<FireInfo,bool>> IsInfoComplete { get { return f => f.calories != null;  } }

		public BindingList<Object> EditFields (CalorieDietBurnEntry toEdit, IValueRequestFactory factory)
		{
			var ret = CreationFields (factory);
			caloriesBurned.request.value = toEdit.kcals;
			return ret;
		}

		public CalorieDietBurnEntry Edit (CalorieDietBurnEntry toEdit)
		{
			toEdit.entryName = name;
			toEdit.entryWhen = when;
			toEdit.kcals = caloriesBurned;
			return toEdit;
		}

		public BindingList<Object> EditFields (CalorieDietBurnEntry toEdit, IValueRequestFactory factory, FireInfo info)
		{
			var ret = CalculationFields (factory, info);
			caloriesBurned.request.value = toEdit.kcals;
			return ret;
		}

		public CalorieDietBurnEntry Edit (CalorieDietBurnEntry toEdit, FireInfo info, bool shouldComplete)
		{
			if (info.calories == null && shouldComplete)
				info.calories = caloriesBurned;
			toEdit.kcals = (info.calories ?? caloriesBurned) * (burnTime.request.value.TotalHours / info.per_hour);
			toEdit.burn_duration = TimeSpan.FromHours(burnTime.request.value.TotalHours);
			toEdit.entryName = name;
			toEdit.entryWhen = when;
			return toEdit;
		}
		#endregion
	}

	// hmmmm calling into presenter is a nasty....abstract class?
	public class CalorieDietPresenter : ITrackerPresenter<CalorieDietInstance, CalorieDietEatEntry, FoodInfo, CalorieDietBurnEntry, FireInfo>
	{
		TrackerDetailsVM tvm = new TrackerDetailsVM (
			                        "Calorie Diet", 
			                        "thing thing and stuff, thing thing and stuff, thing thing and stuff, thing thing and stuff, thing thing and stuff, ",
			                        "Diet"
		                        );
		public TrackerDetailsVM details { get { return tvm; } }

		TrackerDialect dia = new TrackerDialect (
			"Eat",
			"Burn",
			"Foods",
			"Exercises"
		);
		public TrackerDialect dialect { get { return dia; } }


		#region IDietPresenter implementation
		public EntryLineVM GetRepresentation (CalorieDietEatEntry entry, FoodInfo entryInfo)
		{
			return new EntryLineVM (
				entry.entryWhen,
				TimeSpan.Zero,
				entry.entryName, 
				entryInfo == null ? "" : entryInfo.name, 
				new KVPList<string, double> { { "kcal", entry.kcals } }
			);
		}
		public EntryLineVM GetRepresentation (CalorieDietBurnEntry entry, FireInfo entryInfo)
		{
			return new EntryLineVM (
				entry.entryWhen, 
				entry.burn_duration.Value,
				entry.entryName, 
				entryInfo == null ? "" : entryInfo.name, 
				new KVPList<string, double> { { "kcal", entry.kcals } }
			);
		}
		public TrackerInstanceVM GetRepresentation (CalorieDietInstance entry)
		{
			var ent = (entry as CalorieDietInstance);
			return new TrackerInstanceVM(
				dialect,
				ent.started, 
				ent.hasEnded,
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


		public IEnumerable<TrackingInfoVM> DetermineInTrackingForRange(CalorieDietInstance di, IEnumerable<CalorieDietEatEntry> eats, IEnumerable<CalorieDietBurnEntry> burns, DateTime startBound,  DateTime endBound)
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

			ti.inValues = kcin.ToArray ();
			ti.outValues = kcout.ToArray ();
			yield return ti;
		}

		public IEnumerable<TrackingInfoVM> DetermineOutTrackingForRange(CalorieDietInstance di, IEnumerable<CalorieDietEatEntry> eats, IEnumerable<CalorieDietBurnEntry> burns, DateTime startBound,  DateTime endBound)
		{
			return DetermineInTrackingForRange (di, eats, burns, startBound, endBound);
		}
		#endregion
	}
}
