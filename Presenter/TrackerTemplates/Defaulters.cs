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
		readonly RequestStorageHelper<int> recurrDayOfMonth;
		readonly RequestStorageHelper<TimeSpan> recurrNSpan;
		readonly RequestStorageHelper<bool> recurrForever;
		readonly RequestStorageHelper<DateTime> recurrEnd;

		public DefaultEntryRequests()
		{
			name = new RequestStorageHelper<string> ("name",()=>"",Validate);
			when = new RequestStorageHelper<DateTime>("when",()=>DateTime.Now,Validate);
		
			var ogv = new OptionGroupValue (new[] { "None", "Day of month", "Per days" });
			recurranceMode = new RequestStorageHelper<OptionGroupValue>("Repeat", () => ogv, ValidateRecurr);

			recurrDayOfMonth = new RequestStorageHelper<int> ("Day of month", () => 1, ValidateRecurr);
			recurrNSpan= new RequestStorageHelper<TimeSpan>("Repeat this often", () => TimeSpan.FromDays(7), ValidateRecurr);
			recurrForever = new RequestStorageHelper<bool> ("Repeat forever", () => true, ValidateRecurr);
			recurrEnd= new RequestStorageHelper<DateTime>("Stop repeating by", () => DateTime.Now.AddDays(14), ValidateRecurr);
		}
		void Validate()
		{
			if(when.request != null) when.request.valid = true;
			if(name.request != null) name.request.valid = name.request.value.Length > 0;
		}
		void ValidateRecurr()
		{
			recurrEnd.request.valid = when.request.value < recurrEnd.request.value;
			recurrForever.request.valid = true;
			recurranceMode.request.valid = true;
			switch ((RecurranceType)recurranceMode.request.value.SelectedOption) {
			case RecurranceType.EveryNDays:
				recurrNSpan.request.valid = (int)recurrNSpan.request.value.TotalDays > 0;
				break;
			case RecurranceType.DayOfMonth:
				recurrDayOfMonth.request.valid = (int)recurrDayOfMonth > 0 && (int)recurrDayOfMonth <= 27; // 27 days in july...sooooyeah.
				break;
			}
		}
		public void Set(BaseEntry entry)
		{
			entry.entryName = name;
			entry.entryWhen = when;
			entry.repeatType = (RecurranceType) recurranceMode.request.value.SelectedOption;
			if (entry.repeatType == RecurranceType.DayOfMonth) entry.repeatData = (int)recurrDayOfMonth;
			if (entry.repeatType == RecurranceType.EveryNDays) entry.repeatData = (int)recurrNSpan.request.value.TotalDays;
			entry.repeatEnd = recurrEnd;
			entry.repeatForever = recurrForever;
		}
		public void PushInDefaults(BaseEntry editing, IBindingList requestPackage, IValueRequestFactory fac)
		{
			// no reset here...for entries...event registration clearing is automatic though.
			var nr = name.CGet (fac.StringRequestor);
			var wr = when.CGet (fac.DateRequestor);

			// set up recurrance stuff....off by default, so lets add the repeat mode button
			var rMode = recurranceMode.CGet(fac.OptionGroupRequestor);

			// NOTE dont forget, this package will be added to after/before.  Gotta remember starting index and insert there each time etc.
			int firstItem = -1;
			requestPackage.ListChanged += (sender, e) => firstItem = requestPackage.Contains (rMode) ? requestPackage.IndexOf (rMode) + 1 : firstItem;

			// and we need to preload the others for maybe adding later.
			var rRepForever = recurrForever.CGet (fac.BoolRequestor);
			var rEnd = recurrEnd.CGet (fac.DateRequestor);

			var rDaySpan = recurrNSpan.CGet (fac.TimeSpanRequestor);
			var rMonthDay = recurrDayOfMonth.CGet (fac.IntRequestor);

			Action aEnded = () => {
				if (!(bool)recurrForever)
				{
					if(requestPackage.Contains (rRepForever) && !requestPackage.Contains (rEnd))
						requestPackage.Insert (requestPackage.IndexOf (rRepForever) + 1, rEnd);
				}
				else requestPackage.Remove (rEnd);
			};

			// push in reqs
			requestPackage.Add(nr, wr, rMode);

			// ok how are we set up?
			Action rModeChanged = () => {
				// ... so  when one is chosen we should set things up.
				switch((RecurranceType)recurranceMode.request.value.SelectedOption)
				{
				case RecurranceType.None:
					requestPackage.RemoveAll(rRepForever,rEnd,rDaySpan, rMonthDay); // fails nicely.
					break;
				case RecurranceType.EveryNDays:
					requestPackage.RemoveAll(rMonthDay);
					requestPackage.Ensure(firstItem, rDaySpan, rRepForever);
					aEnded();
					break;
				case RecurranceType.DayOfMonth:
					requestPackage.RemoveAll(rDaySpan);
					requestPackage.Ensure(firstItem, rMonthDay, rRepForever);
					aEnded();
					break;
				}
			};

			// this changes too! but it can only itself change while mode != none (or I hope so)
			recurrForever.request.changed += aEnded;
			recurranceMode.request.changed += rModeChanged;

			// set exiting data
			if (editing != null) {
				name.request.value = editing.entryName;
				when.request.value = editing.entryWhen;
				recurrForever.request.value = editing.repeatForever;
				recurrEnd.request.value = editing.repeatEnd;
				recurranceMode.request.value.SelectedOption = (int)editing.repeatType;
				if (editing.repeatType == RecurranceType.DayOfMonth) recurrDayOfMonth.request.value = editing.repeatData;
				if (editing.repeatType == RecurranceType.EveryNDays) recurrNSpan.request.value = TimeSpan.FromDays (editing.repeatData);
			}

			rModeChanged (); //  needed because we could be on a non-initialiation call from CreationFields for example.
		}
		public void ResetRequests()
		{
			when.Reset ();
			name.Reset ();
			recurranceMode.Reset ();
			recurrDayOfMonth.Reset ();
			recurrNSpan.Reset ();
			recurrForever.Reset ();
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
		public void Reset()
		{
			dietName.Reset ();
			dietStart.Reset ();
			dietEnd.Reset ();
			dietEnded.Reset ();
		}
		public void PushInDefaults(TrackerInstance editing, IBindingList requestPackage, IValueRequestFactory fac)
		{
			// switchy state 
			this.editing = editing != null;

			// get them ready
			var cName = dietName.CGet (fac.StringRequestor);
			var cWhen = dietStart.CGet (fac.DateRequestor);
			var cEndWhen = dietEnd.CGet (fac.DateRequestor); 
			var cHasEnded = dietEnded.CGet (fac.BoolRequestor);

			// CGet method will clear listners to changed, so no need for unhooking madness..tis stateful.
			requestPackage.Add (cName);
			requestPackage.Add (cWhen);
			if (this.editing) {
				requestPackage.Add (cHasEnded);
				if(editing.hasEnded) requestPackage.Add(cEndWhen);
				// lets hook this
				dietEnded.request.changed += () => {
					if(dietEnded) 
					{
						if(!requestPackage.Contains(cEndWhen))
							requestPackage.Add(cEndWhen);
					}
					else requestPackage.Remove(cEndWhen);
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

