﻿using System;
using LibRTP;
using LibSharpHelp;
using System.Diagnostics;
using Consonance.Protocol;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Consonance
{
    static class RTPExt
    {
        public static bool IsValid(this RecurrsOnPatternValue @this)
        {
            var PTR = (uint)@this.PatternType;
            int pc = 0;
            foreach (var pt in (PTR.SplitAsFlags()))
                pc++;
            bool s1 = @this.PatternValues.Length > 1 && pc == @this.PatternValues.Length;
            if (s1)
            {
                try { new RecurrsOnPattern(@this.PatternValues,(LibRTP.RecurrSpan)PTR, null, null); }
                catch { s1 = false; }
            }
            return s1;
        }
        public static bool IsValid(this RecurrsEveryPatternValue @this)
        {
                return @this.PatternType == Protocol.RecurrSpan.Day ||
                       @this.PatternType == Protocol.RecurrSpan.Month ||
                       @this.PatternType == Protocol.RecurrSpan.Year ||
                       @this.PatternType == Protocol.RecurrSpan.Week;
        }
    }
    static class RequestStorageHelperExtensions
    {
        /// <summary>
        /// deals with boring reentranct etc
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="from"></param>
        /// <param name="assignment"></param>
        public static void Observe<A>(this RequestStorageHelper<A> from, Action<A> assignment)
        {
            bool block_reentrancy = false;
            from.requestChanged += () =>
            {
                if (block_reentrancy) return;
                block_reentrancy = true;
                assignment(from.request.value);
                block_reentrancy = false;
            };
        }
    }

    class DefaultEntryRequests
	{
		// normal
		readonly RequestStorageHelper<string> name;
		readonly RequestStorageHelper<DateTime> when;

		// recurrance type & common
		readonly RequestStorageHelper<OptionGroupValue> recurranceMode;
		readonly RequestStorageHelper<DateTime?> recurrEnd, recurrStart;

		// Varaints of repeating
		readonly RequestStorageHelper<RecurrsEveryPatternValue> recurrEvery;
		readonly RequestStorageHelper<Protocol.RecurrsOnPatternValue> recurrOn;

		public DefaultEntryRequests()
		{
			// name etc
			name = new RequestStorageHelper<string> ("Name",()=>"",Validate);
            when = new RequestStorageHelper<DateTime>("When", () =>
             {

                 var vdy = Presenter.singleton?.view?.day ?? DateTime.Now.StartOfDay();
                 var now = DateTime.Now;
                 var tdy = now.StartOfDay();
                 if (tdy == vdy) return now;
                 else return vdy.AddHours((now - tdy).TotalHours);
             }, Validate);
		
			// recurrance
			Func<OptionGroupValue> ogv = () => new OptionGroupValue (new[] { "None", "Repeat On...", "Repeat Every..." });
			recurranceMode = new RequestStorageHelper<OptionGroupValue> ("Repeat", ogv, ValidateRecurr);

			// common recurrance
			recurrStart=new RequestStorageHelper<DateTime?> ("Repeat Since", () => new Nullable<DateTime>(), ValidateRecurr);
			recurrEnd=new RequestStorageHelper<DateTime?> ("Repeat Until", () => new Nullable<DateTime>(), ValidateRecurr);

			// ones
			recurrEvery=new RequestStorageHelper<RecurrsEveryPatternValue> ("", () => new RecurrsEveryPatternValue(), ValidateRecurr);
            recurrOn = new RequestStorageHelper<Protocol.RecurrsOnPatternValue>("", () => new RecurrsOnPatternValue(), this.ValidateRecurr);
		}
		void Validate()
		{
			if(when.request != null) when.request.valid = true;
			if(name.request != null) name.request.valid = name.request.value.Length > 0;
		}
		void ValidateRecurr()
		{
			// these switches are always ok
			recurranceMode.request.valid = true;

			// the start/ends just muse be contigious.
			recurrEnd.request.valid = !recurrEnd.request.value.HasValue || recurrEnd.request.value >= recurrStart.request.value;
			recurrStart.request.valid = !recurrStart.request.value.HasValue || recurrEnd.request.value >= recurrStart.request.value;

			// just validate both every time.
			recurrEvery.request.valid = recurrEvery.request.value.IsValid();
			recurrOn.request.valid = recurrOn.request.value.IsValid();
		}
		// consumers be like "ok done - please set the data you got on this guy please"
		public void Set(BaseEntry entry)
		{
			entry.entryName = name;
			entry.entryWhen = when;

			var s = entry.repeatStart = recurrStart;
            var e = entry.repeatEnd = recurrEnd;

			// recurring stuff
			entry.repeatType = (RecurranceType) recurranceMode.request.value.SelectedOption;
			if (entry.repeatType == RecurranceType.RecurrsEveryPattern) entry.repeatData= recurrEvery.request.value.Create(s,e).ToBinary();
			if (entry.repeatType == RecurranceType.RecurrsOnPattern) entry.repeatData = recurrOn.request.value.Create(s,e).ToBinary();
		}
		// entry point for consumers asking "hey i need the default request objects for stuff please"
		public void PushInDefaults<T>(BaseEntry editing, T requestPackage, IValueRequestFactory fac) where T : IList<Object>, INotifyCollectionChanged
        {
			// no reset here...for entries...event registration clearing is automatic though.
			var nr = name.CGet (fac.StringRequestor);
			var wr = when.CGet (fac.DateTimeRequestor);
                
			// set up recurrance stuff....off by default, so lets add the repeat mode button
			var rMode = recurranceMode.CGet(fac.OptionGroupRequestor);

			// NOTE dont forget, this package will be added to after/before.  Gotta remember starting index and insert there each time etc.
			int firstItem = -1;
			requestPackage.CollectionChanged += (sender, e) => firstItem = requestPackage.IndexOf (nr);

			// and we need to preload the others for maybe adding later.
			var rRepEnd = recurrEnd.CGet (fac.nDateRequestor);
			var rRepStart = recurrStart.CGet (fac.nDateRequestor);
			var rRepOn = recurrOn.CGet (fac.RecurrOnRequestor);
			var rRepEvery = recurrEvery.CGet (fac.RecurrEveryRequestor);

            // connections
            when.Observe(d => recurrEvery.request.value.PatternFixed = d);
            recurrEvery.Observe(re => when.request.value = re.PatternFixed);

			// push in reqs
			requestPackage.Add(nr, wr, rMode);

			// ok how are we set up?
			Action rModeChanged = () => {
                // ... so  when one is chosen we should set things up.
                var ttype = (RecurranceType)recurranceMode.request.value.SelectedOption;
                switch (ttype)
				{
				case RecurranceType.None:
					requestPackage.RemoveAll(rRepStart,rRepEnd, rRepOn, rRepEvery); // fails nicely.
					requestPackage.Ensure(firstItem+1, wr);
					break;
				case RecurranceType.RecurrsEveryPattern:
					requestPackage.RemoveAll(rRepOn);
                    requestPackage.Ensure(firstItem + 1, wr);
                    requestPackage.Ensure(requestPackage.IndexOf(rMode) +1, rRepEvery, rRepStart, rRepEnd);
					break;
				case RecurranceType.RecurrsOnPattern:
					requestPackage.RemoveAll(rRepEvery, wr);
					requestPackage.Ensure(requestPackage.IndexOf(rMode) +1, rRepOn, rRepStart, rRepEnd);
                    break;
				}
			};

			// this changes too! but it can only itself change while mode != none (or I hope so)
			recurranceMode.request.ValueChanged += rModeChanged;

			// set editing data if we are
			if (editing != null) {
				//nameanddate
				name.request.value = editing.entryName;
				when.request.value = editing.entryWhen;

				// repeat bounds
				recurrEnd.request.value = editing.repeatEnd;
				recurrStart.request.value = editing.repeatStart;

				// which pattern
				recurranceMode.request.value.SelectedOption = (int)editing.repeatType;

				// pattern specific
				IRecurr pat;
				if (editing.repeatType == RecurranceType.RecurrsEveryPattern &&
					RecurrsEveryPattern.TryFromBinary (editing.repeatData, out pat)) {
					var rd = (RecurrsEveryPattern)pat;
					recurrEvery.request.value = new RecurrsEveryPatternValue (rd.FixedPoint, (Protocol.RecurrSpan)rd.units, rd.frequency);
				}
				if (editing.repeatType == RecurranceType.RecurrsOnPattern &&
					RecurrsOnPattern.TryFromBinary (editing.repeatData, out pat)) {
					var rd = (RecurrsOnPattern)pat;
                    LibRTP.RecurrSpan use = rd.units [0];
					foreach (var d in rd.units)
						use |= d;
                    recurrOn.request.value = new RecurrsOnPatternValue((Protocol.RecurrSpan)use, rd.onIndexes);
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
			recurrEnd.Reset ();
			recurrStart.Reset ();
		}
	}


	class DefaultTrackerInstanceRequests 
	{
		// info 
		readonly RequestStorageHelper<String> dietName;
		readonly RequestStorageHelper<DateTime> dietStart;
		readonly RequestStorageHelper<bool> tracked;

		public DefaultTrackerInstanceRequests(String typeName)
		{
			dietName = new RequestStorageHelper<string> (typeName + " Name", () => "", Validate);
			dietStart = new RequestStorageHelper<DateTime> ("Start Date", () => DateTime.Now.StartOfDay(), Validate);
			tracked = new RequestStorageHelper<bool> ("Tracked", () => true, Validate);
		}
		public bool editing = false;
		void Validate()
		{
			tracked.requestValid = true;
			if (!editing) {
				dietName.request.valid = !string.IsNullOrWhiteSpace (dietName);
				dietStart.request.valid = true;
			} else {
				dietName.request.valid = !string.IsNullOrWhiteSpace (dietName);
				dietStart.request.valid = true;
			}
		}
		public void Set(TrackerInstance thisone)
		{
			thisone.tracked = tracked;
			thisone.name = dietName; 
			thisone.startpoint = dietStart;
		}
		public void Reset()
		{
			tracked.Reset ();
			dietName.Reset ();
			dietStart.Reset ();
		}
		public void PushInDefaults(TrackerInstance editing, IList<Object> requestPackage, IValueRequestFactory fac)
		{
			// switchy state 
			this.editing = editing != null;

			// get them ready
			var cName = dietName.CGet (fac.StringRequestor);
			var cWhen = dietStart.CGet (fac.DateRequestor);
			var cTr = tracked.CGet (fac.BoolRequestor);

			// CGet method will clear listners to changed, so no need for unhooking madness..tis stateful.
			requestPackage.Add (cName);
			requestPackage.Add (cWhen);
			requestPackage.Add (cTr);
			if (this.editing) {
				// we're editing, lets setty
				dietName.request.value = editing.name;
				dietStart.request.value = editing.startpoint;
				tracked.request.value = editing.tracked;
			}
            Validate();
		}
	}
}

