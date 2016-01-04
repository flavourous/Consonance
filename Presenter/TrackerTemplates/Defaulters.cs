using System;
using System.ComponentModel;
using LibRTP;
using LibSharpHelp;

namespace Consonance
{
	class DefaultEntryRequests
	{
		// normal
		readonly RequestStorageHelper<string> name;
		readonly RequestStorageHelper<DateTime> when;

		// recurrance type & common
		readonly RequestStorageHelper<OptionGroupValue> recurranceMode;
		readonly RequestStorageHelper<bool> recurrEnded, recurrStarted;
		readonly RequestStorageHelper<DateTime> recurrEnd, recurrStart;

		// Varaints of repeating
		readonly RequestStorageHelper<RecurrsEveryPatternValue> recurrEvery;
		readonly RequestStorageHelper<RecurrsOnPatternValue> recurrOn;

		public DefaultEntryRequests()
		{
			// name etc
			name = new RequestStorageHelper<string> ("name",()=>"",Validate);
			when = new RequestStorageHelper<DateTime>("when",()=>DateTime.Now.StartOfDay(),Validate);
		
			// recurrance
			var ogv = new OptionGroupValue (new[] { "None", "Repeat On...", "Repeat Every..." });
			recurranceMode = new RequestStorageHelper<OptionGroupValue>("Repeat", () => ogv, ValidateRecurr);

			// common recurrance
			recurrEnded = new RequestStorageHelper<bool> ("Has End", () => false, ValidateRecurr);
			recurrStarted=new RequestStorageHelper<bool> ("Has Start", () => false, ValidateRecurr);
			recurrEnd=new RequestStorageHelper<DateTime> ("Repeat Until", () => DateTime.Now.StartOfDay(), ValidateRecurr);
			recurrStart=new RequestStorageHelper<DateTime> ("Repeat Since", () => DateTime.Now.StartOfDay(), ValidateRecurr);

			// ones
			recurrEvery=new RequestStorageHelper<RecurrsEveryPatternValue> ("Every Pattern", () => new RecurrsEveryPatternValue(), ValidateRecurr);
			recurrOn=new RequestStorageHelper<RecurrsOnPatternValue> ("On Pattern", () => new RecurrsOnPatternValue(), ValidateRecurr);
		}
		void Validate()
		{
			if(when.request != null) when.request.valid = true;
			if(name.request != null) name.request.valid = name.request.value.Length > 0;
		}
		void ValidateRecurr()
		{
			// these switches are always ok
			recurrEnded.request.valid = recurrStarted.request.valid = recurranceMode.request.valid = true;

			// the start/ends just muse be contigious.
			recurrEnd.request.valid = !recurrStarted || recurrEnd.request.value >= recurrStart.request.value;
			recurrStart.request.valid = !recurrEnded || recurrEnd.request.value >= recurrStart.request.value;

			// just validate both every time.
			recurrEvery.request.valid = recurrEvery.request.value.IsValid;
			recurrOn.request.valid = recurrOn.request.value.IsValid;
		}
		// consumers be like "ok done - please set the data you got on this guy please"
		public void Set(BaseEntry entry)
		{
			entry.entryName = name;
			entry.entryWhen = when;

			// these are 'auxiallry', used in queries but actually stored in the above blob for actual api purposes.
			var s = entry.repeatEnd = recurrEnded ? recurrEnd : null;
			var e = entry.repeatStart = recurrStarted ? recurrStart : null;

			// recurring stuff
			entry.repeatType = (RecurranceType) recurranceMode.request.value.SelectedOption;
			if (entry.repeatType == RecurranceType.RecurrsEveryPattern) entry.repeatData= recurrEvery.request.value.Create(s,e).ToBinary();
			if (entry.repeatType == RecurranceType.RecurrsOnPattern) entry.repeatData = recurrOn.request.value.Create(s,e).ToBinary();
		}
		// entry point for consumers asking "hey i need the default request objects for stuff please"
		public void PushInDefaults(BaseEntry editing, IBindingList requestPackage, IValueRequestFactory fac)
		{
			// no reset here...for entries...event registration clearing is automatic though.
			var nr = name.CGet (fac.StringRequestor);
			var wr = when.CGet (fac.DateRequestor);

			// set up recurrance stuff....off by default, so lets add the repeat mode button
			var rMode = recurranceMode.CGet(fac.OptionGroupRequestor);

			// NOTE dont forget, this package will be added to after/before.  Gotta remember starting index and insert there each time etc.
			int firstItem = -1;
			requestPackage.ListChanged += (sender, e) => firstItem = requestPackage.IndexOf (nr);

			// and we need to preload the others for maybe adding later.
			var rRepStarted = recurrStarted.CGet (fac.BoolRequestor);
			var rRepEnded = recurrEnded.CGet (fac.BoolRequestor);
			var rRepEnd = recurrEnd.CGet (fac.DateRequestor);
			var rRepStart = recurrStart.CGet (fac.DateRequestor);
			var rRepOn = recurrOn.CGet (fac.RecurrOnRequestor);
			var rRepEvery = recurrEvery.CGet (fac.RecurrEveryRequestor);

			Action CheckRepStartedEnded = () => {
				if ((bool)recurrEnded && requestPackage.Contains(rRepEnded))
					requestPackage.Ensure(requestPackage.IndexOf(rRepEnded)+1,rRepEnd);
				else requestPackage.Remove (rRepEnd);
				if ((bool)recurrStarted && requestPackage.Contains(rRepStarted))
					requestPackage.Ensure (requestPackage.IndexOf(rRepStarted) +1, rRepStart);
				else requestPackage.Remove (rRepStart);
			};

			// push in reqs
			requestPackage.Add(nr, wr, rMode);

			// ok how are we set up?
			Action rModeChanged = () => {
				// ... so  when one is chosen we should set things up.
				switch((RecurranceType)recurranceMode.request.value.SelectedOption)
				{
				case RecurranceType.None:
					requestPackage.RemoveAll(rRepStarted, rRepStart, rRepEnded,rRepEnd, rRepOn, rRepEvery); // fails nicely.
					requestPackage.Ensure(firstItem+1, wr);
					break;
				case RecurranceType.RecurrsEveryPattern:
					requestPackage.RemoveAll(rRepOn, wr);
					requestPackage.Ensure(requestPackage.IndexOf(rMode) +1, rRepEvery, rRepStarted, rRepEnded);
					CheckRepStartedEnded();
					break;
				case RecurranceType.RecurrsOnPattern:
					requestPackage.RemoveAll(rRepEvery, wr);
					requestPackage.Ensure(requestPackage.IndexOf(rMode) +1, rRepOn, rRepStarted, rRepEnded);
					CheckRepStartedEnded();
					break;
				}
			};

			// this changes too! but it can only itself change while mode != none (or I hope so)
			recurrStarted.request.changed += CheckRepStartedEnded;
			recurrEnded.request.changed += CheckRepStartedEnded;
			recurranceMode.request.changed += rModeChanged;

			// set editing data if we are
			if (editing != null) {
				//nameanddate
				name.request.value = editing.entryName;
				when.request.value = editing.entryWhen;

				// repeat bounds
				recurrEnded.request.value = editing.repeatEnd.HasValue;
				if (editing.repeatEnd.HasValue)
					recurrEnd.request.value = editing.repeatEnd.Value;
				recurrStarted.request.value = editing.repeatStart.HasValue;
				if (editing.repeatStart.HasValue)
					recurrStart.request.value = editing.repeatStart.Value;

				// which pattern
				recurranceMode.request.value.SelectedOption = (int)editing.repeatType;
				// pattern specific
				if (editing.repeatType == RecurranceType.RecurrsEveryPattern) {
					var rd = RecurrsEveryPattern.FromBinary (editing.repeatData);
					recurrEvery.request.value = new RecurrsEveryPatternValue(rd.FixedPoint, rd.units, rd.frequency);
				}
				if (editing.repeatType == RecurranceType.RecurrsOnPattern) {
					var rd = RecurrsOnPattern.FromBinary (editing.repeatData);
					RecurrSpan use = rd.units [0];
					foreach (var d in rd.units)
						use |= d;
					recurrOn.request.value = new RecurrsOnPatternValue(use, rd.onIndexes);
				}
			}

			rModeChanged (); //  needed because we could be on a non-initialiation call from CreationFields for example.
		}
		// consumers are like "hey stuff needs resetting whatever that stuff u got is"
		public void ResetRequests()
		{
			when.Reset ();
			name.Reset ();
			recurranceMode.Reset ();
			recurrOn.Reset ();
			recurrEvery.Reset ();
			recurrEnded.Reset ();
			recurrStarted.Reset ();
			recurrEnd.Reset ();
			recurrStart.Reset ();
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
			dietStart = new RequestStorageHelper<DateTime> ("Start Date", () => DateTime.Now.StartOfDay(), Validate);
			dietEnded = new RequestStorageHelper<bool> ("Ended", () => false, Validate);
			dietEnd = new RequestStorageHelper<DateTime> ("End Date", () => DateTime.Now.StartOfDay(), Validate);
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

