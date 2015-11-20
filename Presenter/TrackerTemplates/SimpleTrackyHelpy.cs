using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Consonance
{
	//and some helpers
	// helper for indexing.
	public interface IExtraRelfectedHelpy<A, B, C, D> : IReflectedHelpy<A, B, C, D>
		where  A : BaseInfo, new()
		where  B : BaseInfo, new()
	{
		TrackerDetailsVM TrackerDetails { get;}
		TrackerDialect TrackerDialect {get;}
	}
	public class SimpleTrackerHolder<T,I,Ii, Im, O, Oi, Om> where T : TrackerInstance, new()
		where I : BaseEntry, new()
		where O : BaseEntry, new()
		where Ii : BaseInfo, new()
		where Oi : BaseInfo, new()
	{
		public readonly ITrackModel<T,I,Ii,O,Oi> model;
		public readonly ITrackerPresenter<T,I,Ii,O,Oi> presenter;		
		public SimpleTrackerHolder(IExtraRelfectedHelpy<Ii,Oi,Im,Om> helpy)
		{
			model = new SimpleTrackyHelpy<T, I,Ii, O, Oi, Im,Om> (helpy);
			presenter = new SimpleTrackyHelpyPresenter<T, I,Ii, O, Oi, Im,Om> (helpy.TrackerDetails, helpy.TrackerDialect, helpy);
		}
	}


	// & how to get it from entrymodels = use reflection provide string.
	// Alright, from the top, what would you ideally wanna specify? for a simple situation...
	//  - unit of measure to track (e.g. points) and it's probabbly gotta be a double. 
	//  - unit of amountage for input (e.g. grams) 
	//  - unit of amountage for output (e.g. minutes) 
	//  - list of items needed from respective infos in order to calc (also helps create infos)
	//  - expression involving those units to creeate entries (creating without an info is simple)
	//  - how to determine target (for creation etc)

	/// <summary>
	/// I helpy.  strings are used for reflection 
	/// </summary>
	public interface IReflectedHelpy<InInfo,OutInfo,QuantityIn,QuantityOut>
		where  InInfo : BaseInfo, new()
		where  OutInfo : BaseInfo, new()
	{
		//Textual - not used for reflection or anything.
		String name {get;}
		String typename {get;}
		String trackedname {get;}

		// instance
		InstanceValue [] instanceValueFields { get; } // for create/edit/andmemebernames
		//InstanceValue<bool> [] instanceOptions { get; } // boolean fields

		// This has to represent any number of targets, in any number of patterns, for any period range.
		SimpleTrackingTarget[] Calcluate(Object[] fieldValues); 

		// Entries and infos
		IReflectedHelpyQuants<InInfo,QuantityIn> input { get; }
		IReflectedHelpyQuants<OutInfo,QuantityOut> output { get; }
	}
	public class InstanceValue  // lets try a object-cast based one here - I think it will be better then generics even after the casts
	{ 
		public readonly Object defaultValue; 
		public readonly String name; 
		public readonly String fieldName; 
		public readonly Func<Action, IRequestStorageHelper> CreateHelper;
		public readonly Func<IValueRequestFactory, Func<String, Object>> FindRequestorDelegate;
		public readonly Func<Object,Object> ConvertDataToRequestValue, ConvertRequestValueToData;
		private InstanceValue(Object def, String name, String field,Func<Action, 
			IRequestStorageHelper> CreateHelper, Func<IValueRequestFactory, Func<String, Object>> FindRequestorDelegate,
			Func<Object,Object> ConvertDataToRequestValue, Func<Object,Object> ConvertRequestValueToData)
		{
			defaultValue = def; this.name = name; this.fieldName = field; 
			this.CreateHelper = CreateHelper; this.FindRequestorDelegate=FindRequestorDelegate;
			this.ConvertDataToRequestValue = ConvertDataToRequestValue;
			this.ConvertRequestValueToData = ConvertRequestValueToData;
		}

		public static InstanceValue FromType<T>(T defaultValue, String name, String field, Func<IValueRequestFactory, Func<String, IValueRequest<T>>> directRequest)
		{
			return new InstanceValue (
				defaultValue,
				name,
				field, 
				v => new RequestStorageHelper<T> (name, () => defaultValue, v),
				f => directRequest (f),
				o => (T)o, o => (T)o // they are the same.
			);
		}
	}
	public class SimpleTrackingTarget
	{
		public enum RangeType { DaysFromStart = 1, DaysEitherSide = 2 }
		public readonly int[] DayPattern; 
		public readonly double[] DayTargets; 
		public readonly int[] DayTargetRanges;
		public readonly RangeType[] DayTargetRangeTypes; 
		public SimpleTrackingTarget(int[] targetRanges, RangeType[] rangetypes, int[] targetPattern, double[] patternTarget)
		{
			DayTargetRanges = targetRanges;
			DayTargetRangeTypes = rangetypes;
			DayPattern = targetPattern;
			DayTargets = patternTarget;
		}
	}
	public interface IReflectedHelpyQuants<I,MT>
	{
		// these expect type double
		String trackedMember { get; }
		InstanceValue quantifier { get; } // this is the field that defines the quantity eg grams or minutes
		// these expect double? cause is optional data.
		String[] calculation { get; } // these are the fields we want to take from/store to the info
		double Calcluate (MT amount, double[] values); // obvs
		MT InfoFixedQuantity { get; }
		String Convert (MT quant);
		Expression<Func<I, bool>> InfoComplete { get; }
	}
	class IRSPair 
	{
		public readonly IRequestStorageHelper requestStore; 
		public readonly InstanceValue descriptor;
		public IRSPair(IRequestStorageHelper rs, InstanceValue iv)
		{
			this.requestStore=rs;
			this.descriptor = iv;
		}
	}
	public class SimpleTrackyHelpy<Inst, In, InInfo, Out, OutInfo, MIn, MOut> : ITrackModel<Inst, In, InInfo, Out, OutInfo>
		where    Inst : TrackerInstance, new()
		where      In : BaseEntry, new()
		where  InInfo : BaseInfo, new()
		where     Out : BaseEntry, new()
		where OutInfo : BaseInfo, new()
	{
		readonly HelpedCreation<In,InInfo,MIn> inc;
		readonly HelpedCreation<Out,OutInfo,MOut> ouc;
		readonly IReflectedHelpy<InInfo,OutInfo, MIn,MOut> helpy; 
		readonly String name;

		readonly IReadOnlyList<Flecter<Inst>> trackflectors; 
		readonly IReadOnlyList<IRSPair> flectyRequests;

		readonly DefaultTrackerInstanceRequests defaultTrackerStuff;
		public SimpleTrackyHelpy(IReflectedHelpy<InInfo,OutInfo, MIn,MOut> helpy) 
		{
			this.name = helpy.name;
			defaultTrackerStuff = new DefaultTrackerInstanceRequests (helpy.typename);
			this.helpy = helpy; 
			inc = new HelpedCreation<In, InInfo, MIn> (helpy.input);
			ouc = new HelpedCreation<Out, OutInfo, MOut> (helpy.output);
			this.trackflectors = helpy.instanceValueFields.MakeList(s => new Flecter<Inst> (s.fieldName));
			this.flectyRequests = helpy.instanceValueFields.MakeList (s => new IRSPair(s.CreateHelper (Validate), s));
		}
		void Validate()
		{
			foreach (var r in flectyRequests)
				r.requestStore.requestValid = true;
		}
		#region ITrackModel implementation
		public IEntryCreation<In, InInfo> increator {get{ return inc; }}
		public IEntryCreation<Out, OutInfo> outcreator { get { return ouc; }}
		public IEnumerable<GetValuesPage> CreationPages (IValueRequestFactory factory)
		{
			defaultTrackerStuff.Reset (); // maybe on ITrackModel interface? :/
			var rqs = flectyRequests.MakeBindingList (f => f.requestStore.CGet (factory, f.descriptor.FindRequestorDelegate));
			defaultTrackerStuff.PushInDefaults (null, rqs, factory);
			var gvp = new GetValuesPage (name);
			gvp.SetList (rqs);
			yield return gvp;
		}
		public IEnumerable<GetValuesPage> EditPages (Inst editing, IValueRequestFactory factory)
		{
			defaultTrackerStuff.Reset (); // maybe on ITrackModel interface? :/
			var rqs = flectyRequests.MakeBindingList (f => f.requestStore.CGet (factory, f.descriptor.FindRequestorDelegate));
			for (int i = 0; i < flectyRequests.Count; i++) flectyRequests [i].requestStore.requestValue = flectyRequests [i].descriptor.ConvertDataToRequestValue(trackflectors [i].Get (editing));
			defaultTrackerStuff.PushInDefaults (editing, rqs, factory);
			var gvp = new GetValuesPage (name);
			gvp.SetList (rqs);
			yield return gvp;
		}
		public Inst New ()
		{
			var ti = new Inst ();
			Edit (ti);
			return ti;
		}
		public void Edit (Inst toEdit)
		{
			defaultTrackerStuff.Set (toEdit);
			for (int i = 0; i < flectyRequests.Count; i++) trackflectors [i].Set (toEdit, flectyRequests [i].descriptor.ConvertRequestValueToData(flectyRequests [i].requestStore.requestValue));
		}
		#endregion
	}
	class Flecter<T> where T : class
	{
		readonly PropertyInfo pi;
		public String name { get { return pi.Name; } }
		public Flecter(String name)
		{
			pi = typeof(T).GetProperty (name);
		}
		public Object Get(T offof)
		{
			return pi.GetValue (offof);
		}
		public void Set(T onto, Object value)
		{
			pi.SetValue (onto, value);
		}
	}
	class HelpedCreation<E,I,MT> : IEntryCreation<E,I>
		where E : BaseEntry, new()
		where I : BaseInfo, new()
	{
		// for no-info requests
		readonly RequestStorageHelper<double> trackedQuantity;
		readonly Flecter<E> trackedQuantityFlecter;
		// for info requests
		readonly Flecter<E> measureQuantityFlecter_ent;
		readonly Flecter<I> measureQuantityFlecter_info; // the amount on the info
		readonly IRequestStorageHelper measureQuantity;
		readonly IReadOnlyList<RequestStorageHelper<double>> forMissingInfoQuantities; // pull these as needed
		readonly IReadOnlyList<Flecter<I>> requiredInfoFlecters;
		readonly IReflectedHelpyQuants<I,MT> quant;
		readonly DefaultEntryRequests defaulter = new DefaultEntryRequests ();
		readonly RequestStorageHelper<String> infoNameRequest;
		public HelpedCreation(IReflectedHelpyQuants<I,MT> quant)
		{
			this.IsInfoComplete = quant.InfoComplete;
			this.quant = quant;
			// we need a direct request for no info creation - and posssssibly one of each for the ones on info that might not exist.
			// also need one for info quantity.
			trackedQuantity = new RequestStorageHelper<double>(quant.trackedMember,()=>0.0,() => trackedQuantity.request.valid = true); // it's same on tinfo and entries
			trackedQuantityFlecter = new Flecter<E>(quant.trackedMember);
			measureQuantityFlecter_info = new Flecter<I> (quant.quantifier.fieldName);
			measureQuantityFlecter_ent = new Flecter<E> (quant.quantifier.fieldName);
			measureQuantity = quant.quantifier.CreateHelper (() => measureQuantity.requestValid = true);
			var l = new List<RequestStorageHelper<double>> ();
			var f = new List<Flecter<I>> ();
			foreach (var q in quant.calculation) {
				RequestStorageHelper<double> ns = null;
				ns = new RequestStorageHelper<double> (q + " per " + quant.Convert (quant.InfoFixedQuantity), () => 0.0, () => ns.request.valid = true);
				l.Add (ns);
				f.Add (new Flecter<I> (q));
			}
			forMissingInfoQuantities = l;
			requiredInfoFlecters = f;
			infoNameRequest = new RequestStorageHelper<string> ("Name", () => "", () => infoNameRequest.request.valid = true);

		}
		// deal with displaying realtime calc data for calc creates and edit.
		I calcInfo = null;
		public void CalcChange(I info)
		{
			// Check the ClearListeners - it happens on reset and Cget.
			if (info != null)  // so that mesquant is cgetted
				measureQuantity.requestChanged += MeasureQuantity_request_changed;
			trackedQuantity.request.read_only = (calcInfo = info) != null;
			MeasureQuantity_request_changed ();
		}
		void MeasureQuantity_request_changed ()
		{
			// update the calories field with a calc
			if (calcInfo != null) 
				trackedQuantity.request.value = CalcVal(calcInfo, (MT)measureQuantity.requestValue);
		}
		#region IEntryCreation implementation

		public void ResetRequests ()
		{
			trackedQuantity.Reset ();
			measureQuantity.Reset ();
			foreach (var mi in forMissingInfoQuantities)
				mi.Reset ();
			defaulter.ResetRequests ();
		}

		public BindingList<object> CreationFields (IValueRequestFactory factory)
		{
			// we just need the amount...we can get the name and when etc from a defaulter...
			var rp = new BindingList<object> () { trackedQuantity.CGet(factory.DoubleRequestor) };
			defaulter.PushInDefaults (null, rp, factory);
			CalcChange (null);
			return rp;
		}

		public E Create ()
		{
			var rv = new E ();
			trackedQuantityFlecter.Set (rv, trackedQuantity.requestValue);
			defaulter.Set (rv);
			return rv;
		}

		public BindingList<object> CalculationFields (IValueRequestFactory factory, I info)
		{
			var blo = new BindingList<Object> () { trackedQuantity.CGet(factory.DoubleRequestor), measureQuantity.CGet(factory, quant.quantifier.FindRequestorDelegate) };
			ProcessRequestsForInfo (blo, factory, info);
			defaulter.PushInDefaults (null, blo, factory);
			CalcChange (info);
			return blo;
		}

		// I know these will change when info changes...but...the buisness modelling will re-call us in those cases.
		void ProcessRequestsForInfo(BindingList<Object> requestoutput, IValueRequestFactory factory, I info)
		{
			for (int i = 0; i < forMissingInfoQuantities.Count; i++) {
				var getit = forMissingInfoQuantities [i].CGet (factory.DoubleRequestor); // make sure cgot it.
				var ival = (double?)requiredInfoFlecters [i].Get (info);
				if (ival.HasValue)
					forMissingInfoQuantities [i].request.value = ival.Value; // ok got it, set requestor for easy sames
				else
					requestoutput.Add (getit); // ash need this so adddyyy.
			}
		}

		public E Calculate (I info, bool shouldComplete)
		{
			var rv = new E ();
			var amount = measureQuantity.requestValue;
			var res = CalcVal (info, (MT)amount);
			trackedQuantityFlecter.Set(rv, res);
			measureQuantityFlecter_ent.Set (rv, amount);
			defaulter.Set (rv);
			return rv;
		}
		double CalcVal(I info, MT amount)
		{
			List<double> vals = new List<double> ();
			foreach (var tv in forMissingInfoQuantities)
				vals.Add (tv);
			return quant.Calcluate (amount, vals.ToArray ());
		}

		public BindingList<object> EditFields (E toEdit, IValueRequestFactory factory)
		{
			// request objects
			var rp = new BindingList<object> () { trackedQuantity.CGet(factory.DoubleRequestor) };
			defaulter.PushInDefaults (toEdit, rp, factory);

			// init request objects - defaulter did both
			trackedQuantity.request.value = (double)trackedQuantityFlecter.Get(toEdit);

			// give it back
			CalcChange (null);
			return rp;
		}

		public E Edit (E toEdit)
		{
			// commit edit...simple one
			defaulter.Set(toEdit);
			trackedQuantityFlecter.Set (toEdit, trackedQuantity.requestValue);
			return toEdit;
		}

		public BindingList<object> EditFields (E toEdit, IValueRequestFactory factory, I info)
		{
			// requests for editing a calced one....hmmouch?
			var blo = new BindingList<Object> () { trackedQuantity.CGet(factory.DoubleRequestor),  measureQuantity.CGet(factory, quant.quantifier.FindRequestorDelegate) };
			measureQuantity.requestValue = measureQuantityFlecter_ent.Get (toEdit);
			ProcessRequestsForInfo (blo, factory, info); // initially, no, we'll add none...but maybe subsequently.
			defaulter.PushInDefaults (toEdit, blo, factory);
			CalcChange (info);
			return blo;
		}

		public E Edit (E toEdit, I info, bool shouldComplete)
		{
			defaulter.Set (toEdit);
			var amount = measureQuantity.requestValue;
			double res = CalcVal (info, (MT)amount);
			trackedQuantityFlecter.Set (toEdit, res);
			measureQuantityFlecter_ent.Set (toEdit, amount);
			return toEdit;
		}

		#endregion

		#region IInfoCreation implementation	
		public BindingList<object> InfoFields (IValueRequestFactory factory)
		{
			var rv = new BindingList<Object> () { infoNameRequest.CGet (factory.StringRequestor) };
			foreach (var iv in forMissingInfoQuantities)
				rv.Add (iv.CGet (factory.DoubleRequestor));
			return rv;
		}

		public void FillRequestData (I item)
		{
			// put the data in item into the requests please.
			for (int i = 0; i < forMissingInfoQuantities.Count; i++) 
				forMissingInfoQuantities [i].request.value = ((double?)requiredInfoFlecters [i].Get (item)).Value;
			infoNameRequest.request.value = item.name;
		}

		public I MakeInfo (I toEdit = default(I))
		{
			// yeah...but a default for a reference type is null...
			var ret = toEdit ?? new I();
			ret.name = infoNameRequest;
			measureQuantityFlecter_info.Set (ret, quant.InfoFixedQuantity);
			for (int i = 0; i < forMissingInfoQuantities.Count; i++)
				requiredInfoFlecters [i].Set (ret, forMissingInfoQuantities [i].requestValue);
			return ret;
		}

		public Expression<Func<I, bool>> IsInfoComplete { get; private set; }

		#endregion
	}

	public class SimpleTrackyHelpyPresenter<Inst, In, InInfo, Out, OutInfo, MIn, MOut> : ITrackerPresenter<Inst, In, InInfo, Out, OutInfo>
		where    Inst : TrackerInstance, new()
		where      In : BaseEntry, new()
		where  InInfo : BaseInfo, new()
		where     Out : BaseEntry, new()
		where OutInfo : BaseInfo, new()
	{
		public TrackerDetailsVM details { get; private set; }
		public TrackerDialect dialect { get; private set; }
		readonly IReflectedHelpy<InInfo,OutInfo,MIn,MOut> helpy;

		Flecter<InInfo> InQuant;
		Flecter<OutInfo> OutQuant;
		Flecter<In> InTrack;
		Flecter<Out> OutTrack;
		public SimpleTrackyHelpyPresenter(TrackerDetailsVM details, TrackerDialect dialect, IReflectedHelpy<InInfo,OutInfo,MIn,MOut> helpy)
		{
			this.helpy = helpy;
			this.details = details;
			this.dialect = dialect;

			InQuant = new Flecter<InInfo> (helpy.input.quantifier.fieldName);
			OutQuant = new Flecter<OutInfo> (helpy.output.quantifier.fieldName);
			InTrack = new Flecter<In> (helpy.input.trackedMember);
			OutTrack = new Flecter<Out> (helpy.output.trackedMember);

			fvalues = helpy.instanceValueFields.MakeList (s => new Flecter<Inst> (s.fieldName));
		}
		IReadOnlyList<Flecter<Inst>> fvalues;

		#region IDietPresenter implementation
		String QuantyGet(String amt, String unit, String name)
		{
			return amt + " " + unit + " of " + name;
		}
		public EntryLineVM GetRepresentation (In entry, InInfo info)
		{
			return new EntryLineVM (
				entry.entryWhen,
				TimeSpan.Zero,
				entry.entryName, 
				info == null ? "" : QuantyGet (helpy.input.Convert((MIn)InQuant.Get (info)), helpy.input.quantifier.name, info.name), 
				new KVPList<string, double> { { helpy.trackedname, (double)InTrack.Get (entry) } }
			);
		}
		public EntryLineVM GetRepresentation (Out entry, OutInfo info)
		{
			return new EntryLineVM (
				entry.entryWhen,
				TimeSpan.Zero,
				entry.entryName, 
				info == null ? "" : QuantyGet (helpy.output.Convert((MOut)OutQuant.Get (info)), helpy.output.quantifier.name, info.name), 
				new KVPList<string, double> { { helpy.trackedname, (double)OutTrack.Get (entry) } }
			);
		}

		SimpleTrackingTarget[] GetTargets(Inst entry)
		{
			var vals = fvalues.MakeList(f => f.Get (entry));
			return helpy.Calcluate (vals.ToArray ());
		}
		// Cases:
		//
		//   targets->patterns->values
		//   special 1: 1target,1pattern (say noms per timespans)
		//   special 2: n targerts, 1pattern each (each line say noms per timespan)
		//   special 3: 1 target, n patterns (1line, say noms with pattern: timespan@value, timespan@value then timespan@value)
		//   general: ntargets wit n patterns.  Select each line from special 1 or special 3;
		//
		public TrackerInstanceVM GetRepresentation (Inst entry)
		{			
			var kl = new KVPList<string, double> ();
			foreach (var target in GetTargets(entry)) {
				if (target.DayPattern.Length == 1)
					kl.Add (helpy.trackedname + " per " + TimeSpan.FromDays (target.DayPattern [0]).ToString (), target.DayTargets [0]);
				else
					for (int i = 0; i < target.DayPattern.Length; i++)
						kl.Add (helpy.trackedname + " for " + TimeSpan.FromDays (target.DayPattern [0]).ToString (), target.DayTargets [0]);	
			}
			return new TrackerInstanceVM(
				dialect,
				entry.started, 
				entry.hasEnded,
				entry.ended,
				entry.name,
				"",
				kl
			);
		}
		public InfoLineVM GetRepresentation (InInfo info)
		{
			return new InfoLineVM () { name = info.name };
		}
		public InfoLineVM GetRepresentation (OutInfo info)
		{
			return new InfoLineVM () { name = info.name };
		}

		public IEnumerable<TrackingInfoVM> DetermineInTrackingForDay(Inst di, EntryRetriever<In> eats, EntryRetriever<Out> burns, DateTime dayStart)
		{
			if(dayStart < di.started) yield break;
			var targets = GetTargets (di);
			for (var ti=0; ti < targets.Length; ti++) {
				var trg = targets [ti];
				for (int tr=0; tr < trg.DayTargetRanges.Length; tr++) {
					TimeSpan spantime = TimeSpan.FromDays (tr);
					String rangeString = trg.DayTargetRanges.Length == 1 && trg.DayTargetRanges [0] == 1 ? " balance" : " " + spantime.ToString () + " balance";

					// use method to get target info
					var targetInfo = FindTargetForDay (di, trg, tr, dayStart);

					TrackingInfoVM info = new TrackingInfoVM () {
						valueName = helpy.trackedname + rangeString,
						targetValue = targetInfo.target
					};

					GetValsForRange (eats, burns, targetInfo.begin, targetInfo.end, out info.inValues, out info.outValues);
					yield return info;
				}
			}
		}
			
		struct DayTargetReturn {
			public readonly DateTime begin;
			public readonly DateTime end;
			public readonly double target;
			public DayTargetReturn(DateTime begin, DateTime end, double target)
			{
				this.begin=begin;
				this.end=end;
				this.target=target;
			}
		}
		DayTargetReturn FindTargetForDay(Inst di, SimpleTrackingTarget trg, int ridx, DateTime dayStart)
		{
			var r = trg.DayTargetRanges [ridx];
			var t = trg.DayTargetRangeTypes [ridx];

			// find the total length of the pattern
			int totalPatternLength = 0;
			foreach (var dp in trg.DayPattern)
				totalPatternLength += dp;
			
			// easier to unwrap i think.
			double[] unwrapped = new double[totalPatternLength];
			int c = 0;
			for (int i = 0; i < trg.DayTargets.Length; i++)
				for (int j = 0; j < trg.DayPattern [i]; j++)
					unwrapped [c++] = trg.DayTargets [i];

			//total days since started (big number)
			var daysSinceStart = (dayStart - di.started).TotalDays;

			// get the number of days into a pattern (could concieveably be negative)
			var daysSincePatternStarted = (int)(daysSinceStart % totalPatternLength);
			if (daysSincePatternStarted < 0) daysSincePatternStarted += totalPatternLength;

			// ok which way?
			double targ =0;
			DateTime useStart = DateTime.MinValue, useEnd = DateTime.MinValue;
			switch (t) {
			case SimpleTrackingTarget.RangeType.DaysFromStart:
				useStart = dayStart.AddDays (-daysSincePatternStarted);
				useEnd = dayStart.AddDays (r);
				for (int ds = 0; ds < r; ds++)
					targ += unwrapped [ds % totalPatternLength];
				break;
			case SimpleTrackingTarget.RangeType.DaysEitherSide:
				useStart = dayStart.AddDays (-r);
				useEnd = dayStart.AddDays (r);
				var rs = daysSincePatternStarted + r;
				for (int ds = daysSincePatternStarted - r; ds <= rs; ds++)
					targ += unwrapped [Math.Abs(ds % totalPatternLength)];
				break;
			}
			return new DayTargetReturn (useStart, useEnd, targ);
		}

		void GetValsForRange(EntryRetriever<In> eats, EntryRetriever<Out> burns, DateTime s, DateTime e, out TrackingElementVM[] invals,out TrackingElementVM[] outvals )
		{
			double tot = 0.0;
			List<TrackingElementVM> vin = new List<TrackingElementVM> (), vout = new List<TrackingElementVM> ();
			foreach (var ient in eats(s, e)) {
				var v = (double)InTrack.Get (ient);
				vin.Add (new TrackingElementVM () { value = v , name = ient.entryName });
				tot += v;
			}
			foreach (var oent in burns(s,e)) {
				var v = (double)OutTrack.Get (oent);
				vout.Add (new TrackingElementVM () { value = v, name = oent.entryName });
				tot -= v;
			}
			invals = vin.ToArray ();
			outvals = vout.ToArray ();
		}
			
		public IEnumerable<TrackingInfoVM> DetermineOutTrackingForDay(Inst di, EntryRetriever<In> eats, EntryRetriever<Out> burns, DateTime dayStart)
		{
			return DetermineInTrackingForDay(di, eats, burns, dayStart);
		}
		#endregion
	}

}