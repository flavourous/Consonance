using System;
using System.ComponentModel;

namespace Consonance
{
	class DefaultEntryRequests
	{
		readonly RequestStorageHelper<string> name;
		readonly RequestStorageHelper<DateTime> when;
		public DefaultEntryRequests()
		{
			name = new RequestStorageHelper<string> ("name",()=>"",Validate);
			when = new RequestStorageHelper<DateTime>("when",()=>DateTime.Now,Validate);
		}
		void Validate()
		{
			when.request.valid = true;
			name.request.valid = name.request.value.Length > 0;
		}
		public void Set(BaseEntry entry)
		{
			entry.entryName = name;
			entry.entryWhen = when;
		}
		public void PushInDefaults(BaseEntry editing, IBindingList requestPackage, IValueRequestFactory fac)
		{
			// no reset here...for entries...
			requestPackage.Add (name.CGet (fac.StringRequestor));
			requestPackage.Add (when.CGet (fac.DateRequestor));
			if (editing != null) {
				name.request.value = editing.entryName;
				when.request.value = editing.entryWhen;
			}
		}
		public void ResetRequests()
		{
			when.Reset ();
			name.Reset ();
		}
	}
	class DefaultTrackerInstanceRequests 
	{
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

