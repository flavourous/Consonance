using System;
using System.ComponentModel;

namespace Consonance
{
	class DefaultTrackerInstanceRequests 
	{
		RequestStorageHelper<String> dietName;
		RequestStorageHelper<DateTime> dietStart;
		RequestStorageHelper<bool> dietEnded;
		RequestStorageHelper<DateTime> dietEnd;
		public DefaultTrackerInstanceRequests(String typeName)
		{
			dietName = new RequestStorageHelper<string> (typeName + " Name", () => "", Validate);
			dietStart = new RequestStorageHelper<DateTime> ("Start Date", () => DateTime.Now, Validate);
			dietEnded = new RequestStorageHelper<bool> ("Ended", () => false, Validate);
			dietEnd = new RequestStorageHelper<DateTime> ("End Date", () => DateTime.Now, Validate);
		}
		public bool editing = false;
		bool Validate()
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
			if (editing) {
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
				dietName = editing.name;
				dietStart = editing.started;
				dietEnded = editing.hasEnded;
				dietEnd = editing.ended;
			}
		}
	}
}

