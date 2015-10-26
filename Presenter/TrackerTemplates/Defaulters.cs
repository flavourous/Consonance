using System;
using System.ComponentModel;

namespace Consonance
{
	class DefaultEntryRequests
	{
		readonly RequestStorageHelper<string> name;
		readonly RequestStorageHelper<DateTime> when;

		// recurrance
		readonly RequestStorageHelper<OptionGroupValue> recurranceMode;
		readonly RequestStorageHelper<DateTime> recurrStart;
		readonly RequestStorageHelper<int> recurrDayOfMonth;
		readonly RequestStorageHelper<TimeSpan> recurrNSpan;
		readonly RequestStorageHelper<bool> recurrEnded;
		readonly RequestStorageHelper<DateTime> recurrEnd;

		public DefaultEntryRequests()
		{
			name = new RequestStorageHelper<string> ("name",()=>"",Validate);
			when = new RequestStorageHelper<DateTime>("when",()=>DateTime.Now,Validate);
			var ogv = new OptionGroupValue (new[] { "None", "Day of month", "Per days" });

			recurranceMode = new RequestStorageHelper<OptionGroupValue>("Repeat", () => ogv, ValidateRecurr);
			recurrStart= new RequestStorageHelper<DateTime> ("Starting", () => DateTime.Now, ValidateRecurr);
			recurrDayOfMonth = new RequestStorageHelper<int> ("Day of month", () => 1, ValidateRecurr);
			recurrNSpan= new RequestStorageHelper<TimeSpan>("Repeat this often", () => TimeSpan.FromDays(7), ValidateRecurr);
			recurrEnded = new RequestStorageHelper<bool> ("Repeat forever", () => true, ValidateRecurr);
			recurrEnd= new RequestStorageHelper<DateTime>("Stop repeating by", () => DateTime.Now.AddDays(14), validate recurr);
		}
		void Validate()
		{
			if(when.request != null) when.request.valid = true;
			if(name.request != null) name.request.valid = name.request.value.Length > 0;
		}
		void ValidateRecurr()
		{
			recurrStart.request.valid = !recurrEnded || recurrStart < recurrEnd;
			recurrEnd.request.valid = recurrStart < recurrEnd;
			recurrEnded.request.valid = true;
			recurranceMode.request.valid = true;
			switch (recurranceMode) {
			case RecurranceType.EveryNDays:
				recurrNSpan.request.valid = (int)recurrNSpan.request.value.TotalDays > 0;
				break;
			case RecurranceType.DayOfMonth:
				recurrDayOfMonth > 0 && recurrDayOfMonth <= 27; // 27 days in july...sooooyeah.
				break;
			}
		}
		public void Set(BaseEntry entry)
		{
			entry.entryName = name;
			entry.entryWhen = when;
			entry.repeatType = recurranceMode;
			entry.repeatStart = recurrStart;
			if (recurranceMode == RecurranceType.DayOfMonth) entry.repeatData = recurrDayOfMonth;
			if (recurranceMode == RecurranceType.EveryNDays) entry.repeatData = recurrNSpan.request.value.TotalDays;
			entry.repeatEnd = recurrEnded ? recurrEnd : null;
		}
		public void PushInDefaults(BaseEntry editing, IBindingList requestPackage, IValueRequestFactory fac)
		{
			// no reset here...for entries...event registration clearing is automatic though.
			requestPackage.Add (name.CGet (fac.StringRequestor));
			requestPackage.Add (when.CGet (fac.DateRequestor));

			// set up recurrance stuff....off by default, so lets add the repeat mode button
			var rMode = recurranceMode.CGet(fac.OptionGroupRequestor);
			requestPackage.Add(rMode);

			// NOTE dont forget, this package will be added to after/before.  Gotta remember starting index and insert there each time etc.
			int firstItem = requestPackage.Count;
			requestPackage.ListChanged += (sender, e) => firstItem = requestPackage.Contains (rMode) ? requestPackage.IndexOf (rMode) + 1 : firstItem;

			// and we need to preload the others for maybe adding later.
			var rStart = recurrStart.CGet(fac.DateRequestor);

			var rEnded = recurrEnded.CGet (fac.BoolRequestor);
			var rEnd = recurrEnd.CGet (fac.DateRequestor);

			var rDaySpan = recurrNSpan.CGet (fac.IntRequestor);
			var rMonthDay = recurrDayOfMonth.CGet (fac.IntRequestor);

			Action aEnded = () => {
				if (rEnded && requestPackage.Contains (rEnd))
					requestPackage.Insert (requestPackage.IndexOf (rEnd) + 1, rEnded);
				else
					requestPackage.Remove (rEnded);
			};

			// ok how are we set up?
			recurranceMode.request.changed += () => {
				// ... so  when one is chosen we should set things up.
				switch(recurranceMode)
				{
				case RecurranceType.None:
					requestPackage.RemoveAll(rStart,rEnded,rEnd,rDaySpan, rMonthDay); // fails nicely.
					break;
				case RecurranceType.EveryNDays:
					requestPackage.RemoveAll(rMonthDay);
					requestPackage.Ensure(firstItem, rStart, rDaySpan, rEnded);
					aEnded();
					break;
				case RecurranceType.DayOfMonth:
					requestPackage.RemoveAll(rDaySpan);
					requestPackage.Ensure(firstItem, rStart, rMonthDay, rEnded);
					aEnded();
					break;
				}
			};
			// this changes too! but it can only itself change while mode != none (or I hope so)
			recurrEnded.request.changed += aEnded;

			// set exiting data
			if (editing != null) {
				name.request.value = editing.entryName;
				when.request.value = editing.entryWhen;
				recurrEnded = editing.repeatEnd.HasValue;
				if (editing.repeatEnd.HasValue) recurrEnd = editing.repeatEnd.Value;
				recurrStart = editing.repeatStart;
				recurranceMode = editing.repeatType;
				if (recurranceMode == RecurranceType.DayOfMonth) recurrDayOfMonth = editing.repeatData;
				if (recurranceMode == RecurranceType.EveryNDays) recurrNSpan = TimeSpan.FromDays (editing.repeatData);
			}
		}
		public void ResetRequests()
		{
			when.Reset ();
			name.Reset ();
			recurranceMode.Reset ();
			recurrStart.Reset ();
			recurrDayOfMonth.Reset ();
			recurrNSpan.Reset ();
			recurrEnded.Reset ();
			recurrEnd.Reset ();
		}
	}


	class DefaultTrackerInstanceRequests 
	{
		// info 
		readonly RequestStorageHelper<String> dietName;
		readonly RequestStorageHelper<DateTime> dietStart;
		readonly RequestStorageHelper<bool> dietEnded;
		readonly RequestStorageHelper<DateTime> dietEnd;


		public DefaultTrackerInstanceRequests(String typeName)
		{
			dietName = new RequestStorageHelper<string> (typeName + " Name", () => "", Validate);
			dietStart = new RequestStorageHelper<DateTime> ("Start Date", () => DateTime.Now, Validate);
			dietEnded = new RequestStorageHelper<bool> ("Ended", () => false, Validate);
			dietEnd = new RequestStorageHelper<DateTime> ("End Date", () => DateTime.Now, Validate);
		}
		public bool editing = false;
		void Validate()
		{
			if (!editing) {
				dietName.request.valid = !string.IsNullOrWhiteSpace (dietName);
				dietStart.request.valid = true;
			} else {
				dietName.request.valid = !string.IsNullOrWhiteSpace (dietName);
				dietStart.request.valid = !dietEnded || dietStart.request.value < dietEnd;
				dietEnded.request.valid = true;
				if(dietEnded)
					dietEnd.request.valid = !dietEnded || dietEnd > dietStart.request.value;
			}
		}
		public void Set(TrackerInstance thisone)
		{
			if (!editing) {
				thisone.name = dietName; 
				thisone.started = dietStart;
			} else {
				thisone.name = dietName;
				thisone.started = dietStart;
				thisone.hasEnded = dietEnded;
				if (dietEnded) thisone.ended = dietEnd;
			}
		}
		public void PushInDefaults(TrackerInstance editing, IBindingList requestPackage, IValueRequestFactory fac)
		{
			// switchy state 
			this.editing = editing != null;

			// CGet method will clear listners to changed, so no need for unhooking madness..tis stateful.
			dietName.Reset();
			dietStart.Reset ();
			requestPackage.Add (dietName.CGet (fac.StringRequestor));
			requestPackage.Add (dietStart.CGet (fac.DateRequestor));
			if (this.editing) {
				dietEnd.Reset ();
				dietEnded.Reset ();
				var ded = dietEnd.CGet (fac.DateRequestor); 
				if(editing.hasEnded) requestPackage.Add(ded);
				requestPackage.Add (dietEnded.CGet (fac.BoolRequestor));
				// lets hook this
				dietEnded.request.changed += () => {
					if(dietEnded) requestPackage.Add(ded);
					else requestPackage.Remove(ded);
				};

				// we're editing, lets setty
				dietName.request.value = editing.name;
				dietStart.request.value = editing.started;
				dietEnded.request.value = editing.hasEnded;
				dietEnd.request.value = editing.ended;
			}
		}
	}
}

