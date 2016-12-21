using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;
using LibSharpHelp;
using LibRTP;
using System.Diagnostics;
using System.Linq;

namespace Consonance
{
    // basemodels
    public abstract class HBaseInfo : BaseInfo
    {
        public int quantifierID { get; set; }
        public double quantity { get; set; }
    }
    public abstract class HBaseEntry : BaseEntry
    {
        public double quantity { get; set; }
    }

	//and some helpers
	// helper for indexing.
	public interface IExtraRelfectedHelpy<T,A, B> : IReflectedHelpy<T,A, B>
		where  A : HBaseInfo, new()
		where  B : HBaseInfo, new()
        where T : TrackerInstance
	{
		TrackerDetailsVM TrackerDetails { get;}
		TrackerDialect TrackerDialect {get;}
	}
	public class SimpleTrackerHolder<T,I,Ii, O, Oi> where T : TrackerInstance, new()
		where I : HBaseEntry, new()
		where O : HBaseEntry, new()
		where Ii : HBaseInfo, new()
		where Oi : HBaseInfo, new()
	{
		public readonly ITrackModel<T,I,Ii,O,Oi> model;
		public readonly ITrackerPresenter<T,I,Ii,O,Oi> presenter;		
		public SimpleTrackerHolder(IExtraRelfectedHelpy<T,Ii,Oi> helpy)
		{
			model = new SimpleTrackyHelpy<T, I,Ii, O, Oi> (helpy);
			presenter = new SimpleTrackyHelpyPresenter<T, I,Ii, O, Oi> (helpy);
		}
	}

    // Derriving from the LibRTP here...
    public class SimpleTrackyTarget : RecurringAggregatePattern
    {
        public readonly bool Tracked, Shown;
        public readonly String TargetName;
        public SimpleTrackyTarget(String targetName, bool tracked, bool shown, int targetRange, AggregateRangeType rangetype, int[] targetPattern, double[] patternTarget)
            : base(targetRange, rangetype, targetPattern, patternTarget)
        {
            this.TargetName = targetName;
            this.Tracked = tracked;
            this.Shown = shown;
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
	/// I helpy.  strings are used for reflection. Provides info for SimpleTrackyHelpy to wrap the main state-observer-machine API thing,
    /// which is most suited to a powerful OO/Procedural implimentation.  This interface suits a declarative approach (and is much more specific and simple).
	/// </summary>
	public interface IReflectedHelpy<T,InInfo,OutInfo>
		where  InInfo : HBaseInfo, new()
		where  OutInfo : HBaseInfo, new()
        where T : TrackerInstance
	{
		// instance
		VRVConnectedValue[] instanceValueFields { get; } // for create/edit/andmemebernames

		// This has to represent any number of targets, in any number of patterns, for any period range.
		SimpleTrackyTarget[] Calcluate(Object[] fieldValues); 

		// Entries and infos
        InstanceValue<double> tracked_on_entries { get; } // global now its not a property name
		IReflectedHelpyQuants<InInfo> input { get; }
		IReflectedHelpyQuants<OutInfo> output { get; }
	}

    public class InstanceValue<T>  // lets try a object-cast based one here - I think it will be better then generics even after the casts
    {
        public readonly T defaultValue;
        public readonly String name;
        public readonly Func<Object, T> valueGetter;
        public readonly Action<Object, T> valueSetter;
        public InstanceValue(String name, Func<Object, T> valueGetter, Action<Object, T> valueSetter, T defaultValue)
        {
            this.name = name;
            this.valueGetter = valueGetter;
            this.valueSetter = valueSetter;
            this.defaultValue = defaultValue;
        }
    }
    public class VRVConnectedValue : InstanceValue<Object>
    { 
		public readonly Predicate<IRequestStorageHelper[]> ValidateHelper;
		public readonly Func<Action, IRequestStorageHelper> CreateHelper;
		public readonly Func<IValueRequestFactory, Func<String, Object>> FindRequestorDelegate;
		public readonly Func<Object,Object> ConvertDataToRequestValue, ConvertRequestValueToData;
		private VRVConnectedValue(Object def, String name, Func<Object, Object> valueGetter, Action<Object, Object> valueSetter, Func<Action, IRequestStorageHelper> CreateHelper, 
			Func<IValueRequestFactory, Func<String, Object>> FindRequestorDelegate, Predicate<IRequestStorageHelper[]> ValidateHelper,
			Func<Object,Object> ConvertDataToRequestValue, Func<Object,Object> ConvertRequestValueToData)
            : base(name, valueGetter, valueSetter, def)
		{
			this.CreateHelper = CreateHelper; 
			this.FindRequestorDelegate=FindRequestorDelegate;
			this.ValidateHelper = ValidateHelper;
			this.ConvertDataToRequestValue = ConvertDataToRequestValue;
			this.ConvertRequestValueToData = ConvertRequestValueToData;
		}
		public static VRVConnectedValue FromType<T>(T defaultValue, Predicate<object[]> validateit, String name, Func<Object, Object> valueGetter, Action<Object, Object> valueSetter, Func<IValueRequestFactory, Func<String, IValueRequest<T>>> directRequest)
		{
            return FromType<T, Object>(defaultValue, validateit, name, valueGetter,valueSetter, directRequest, o => (T)o, o => (T)o);
		}

        static VRVConnectedValue FromType<T,D>(T defaultValue, Predicate<object[]> validateit, String name, Func<Object, Object> valueGetter, Action<Object, Object> valueSetter, Func<IValueRequestFactory, Func<String, IValueRequest<T>>> directRequest, Func<T,D> RequestToData, Func<D,T> DataToRequest )
        {
            Func<Action, IRequestStorageHelper> creator = v => new RequestStorageHelper<T> (name, () => defaultValue, v);
			return new VRVConnectedValue (
				defaultValue,
				name,
                valueGetter,
                valueSetter,
                creator,
				f => directRequest (f),
				irhs => (validateit ?? ( _=> true)) (irhs.MakeList(rsh => rsh.requestValue).ToArray()),
				o => DataToRequest((D)o), o => RequestToData((T)o)
			);
        }

        public static VRVConnectedValue FromTypec<T, D>(D defaultValue, Predicate<T> validateit, String name, Func<Object, Object> valueGetter, Action<Object, Object> valueSetter, Func<IValueRequestFactory, Func<String, IValueRequest<T>>> directRequest, Func<T, D> RequestToData, Func<D, T> DataToRequest)
        {
            return FromType<T, D>(DataToRequest(defaultValue), d => validateit((T)d[0]), name, valueGetter, valueSetter, directRequest, RequestToData, DataToRequest);
        }
        public static VRVConnectedValue FromType<T, D>(T defaultValue, Predicate<T> validateit, String name, Func<Object, Object> valueGetter, Action<Object, Object> valueSetter, Func<IValueRequestFactory, Func<String, IValueRequest<T>>> directRequest, Func<T, D> RequestToData, Func<D, T> DataToRequest)
        {
            return FromType<T, D>(defaultValue, d => validateit((T)d[0]), name, valueGetter, valueSetter, directRequest, RequestToData, DataToRequest);
        }

        public static VRVConnectedValue FromType<T>(T defaultValue, String name, Func<Object, Object> valueGetter, Action<Object, Object> valueSetter, Func<IValueRequestFactory, Func<String, IValueRequest<T>>> directRequest)
		{
			return FromType (defaultValue, null, name, valueGetter, valueSetter, directRequest);
		}
	}
    public static class HelpyInfoQuantifier
    {
        public static InfoQuantifier FromType(InfoQuantifier.InfoQuantifierTypes t, String name, int uid, double dv)
        {
            return InfoQuantifier.FromType(t, name, uid, dv, o => ((HBaseInfo)o).quantity, (o, v) => ((HBaseInfo)o).quantity = (double)v);
        }
    }
    public sealed class InfoQuantifier
    {
        public enum InfoQuantifierTypes {  Double, Integer, Duration };
        public readonly int QuantifierID;
        public readonly VRVConnectedValue ConnectedValue;
        public readonly Func<double, String> DisplayConversion;
        private InfoQuantifier(int id, VRVConnectedValue vcv, Func<double, String> dc)
        {
            QuantifierID = id;
            ConnectedValue = vcv;
            DisplayConversion = dc;
        }
        // a double column injected into the info, and an id for which infoquantifier
        // a double column injected into the entry for amount - already foreign keys info
        // this gives textual description, and gets a valuerequest, and converts vrv into doubles for storage.

        public static InfoQuantifier FromType(InfoQuantifierTypes t, String name, int uid, double dv, Func<Object, Object> getQuantity, Action<Object, Object> setQuantity)
        {
            switch (t)
            {
                default:
                case InfoQuantifierTypes.Double:
                    return new InfoQuantifier(uid, VRVConnectedValue.FromTypec(dv, d => d > 0.0, name, getQuantity, setQuantity, f => f.DoubleRequestor, d => d, d => d), d => d.ToString("F2"));
                case InfoQuantifierTypes.Integer:
                    return new InfoQuantifier(uid, VRVConnectedValue.FromTypec(dv, d => d > 0, name, getQuantity, setQuantity, f => f.IntRequestor, d => (double)d, d => (int)d), d => d.ToString());
                case InfoQuantifierTypes.Duration:
                    return new InfoQuantifier(uid, VRVConnectedValue.FromTypec(dv, d => d.TotalHours > 0.0, name, getQuantity, setQuantity, f => f.TimeSpanRequestor, d => d.TotalHours, d => TimeSpan.FromHours(d)), d => TimeSpan.FromHours(d).WithSuffix());
            }
        }
    }
    public interface IReflectedHelpyQuants<I>
    {
        // these expect type double
        InstanceValue<double>[] calculation { get; } // these are the fields we want to take from/store to the info
        InfoQuantifier[] quantifier_choices { get; }
        double Calcluate(double[] values); // obvs
        Expression<Func<I, bool>> InfoComplete { get; }
    }
	class IRSPair<I>
	{
		public readonly IRequestStorageHelper requestStore; 
		public readonly VRVConnectedValue descriptor;
		public IRSPair(IRequestStorageHelper rs, VRVConnectedValue iv)
		{
			this.requestStore=rs;
			this.descriptor = iv;
		}
	}
	public class SimpleTrackyHelpy<Inst, In, InInfo, Out, OutInfo> : ITrackModel<Inst, In, InInfo, Out, OutInfo>
		where    Inst : TrackerInstance, new()
		where      In : HBaseEntry, new()
		where  InInfo : HBaseInfo, new()
		where     Out : HBaseEntry, new()
		where OutInfo : HBaseInfo, new()
	{
		readonly HelpedCreation<In,InInfo> inc;
		readonly HelpedCreation<Out,OutInfo> ouc;
		readonly IExtraRelfectedHelpy<Inst,InInfo,OutInfo> helpy; 

		readonly IReadOnlyList<IRSPair<Inst>> flectyRequests;

		readonly DefaultTrackerInstanceRequests defaultTrackerStuff;
		public SimpleTrackyHelpy(IExtraRelfectedHelpy<Inst,InInfo, OutInfo> helpy) 
		{
			this.helpy = helpy; 
			defaultTrackerStuff = new DefaultTrackerInstanceRequests (helpy.TrackerDetails.category);
			inc = new HelpedCreation<In, InInfo> (helpy.input, helpy.tracked_on_entries);
			ouc = new HelpedCreation<Out, OutInfo> (helpy.output, helpy.tracked_on_entries);
			this.flectyRequests = helpy.instanceValueFields.MakeList (s => new IRSPair<Inst>(s.CreateHelper (Validate), s));
		}
		void Validate()
		{
			var rqs = flectyRequests.MakeList (ip => ip.requestStore).ToArray ();
			System.Diagnostics.Debug.WriteLine ("Validating: " + string.Join (" ", rqs.MakeList (rh => rh.requestValue.ToString ())));
			foreach (var r in flectyRequests)
				r.requestStore.requestValid = r.descriptor.ValidateHelper (rqs);
		}
		#region ITrackModel implementation
		public IEntryCreation<In, InInfo> increator {get{ return inc; }}
		public IEntryCreation<Out, OutInfo> outcreator { get { return ouc; }}
		public IEnumerable<GetValuesPage> CreationPages (IValueRequestFactory factory)
		{
			defaultTrackerStuff.Reset (); // maybe on ITrackModel interface? :/
			var rqs = flectyRequests.MakeBindingList (f => f.requestStore.CGet (factory, f.descriptor.FindRequestorDelegate));
			defaultTrackerStuff.PushInDefaults (null, rqs, factory);
			var gvp = new GetValuesPage (helpy.TrackerDetails.name);
			gvp.SetList (rqs);
			yield return gvp;
		}
		public IEnumerable<GetValuesPage> EditPages (Inst editing, IValueRequestFactory factory)
		{
			defaultTrackerStuff.Reset (); // maybe on ITrackModel interface? :/
			var rqs = flectyRequests.MakeBindingList (f => f.requestStore.CGet (factory, f.descriptor.FindRequestorDelegate));
            foreach (var fr in flectyRequests) fr.requestStore.requestValue = fr.descriptor.ConvertDataToRequestValue(fr.descriptor.valueGetter(editing));
			defaultTrackerStuff.PushInDefaults (editing, rqs, factory);
			var gvp = new GetValuesPage (helpy.TrackerDetails.name);
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
            foreach (var fr in flectyRequests) fr.descriptor.valueSetter(toEdit, fr.descriptor.ConvertRequestValueToData(fr.requestStore.requestValue));
		}
		#endregion
	}
	
	class HelpedCreation<E,I> : IEntryCreation<E,I>
		where E : HBaseEntry, new()
		where I : HBaseInfo, new()
	{
		// for no-info requests
		readonly RequestStorageHelper<double> trackedQuantity;

		// for info requests
        class MeasureOptionsContainer
        {
            public InfoQuantifier quant;
            public IRequestStorageHelper requestStorage;
        }
        readonly List<MeasureOptionsContainer> measureOptionContainers;
        MeasureOptionsContainer currentMeasureOption = null;
        readonly RequestStorageHelper<MultiRequestOptionValue> measureAndOptionsRequest;
        readonly Dictionary<int, int> IndexToQID = new Dictionary<int, int>();

        class InfoDataContainer
        {
            public InstanceValue<double> quant;
            public RequestStorageHelper<double> requestStore;
        }
        readonly IReadOnlyList<InfoDataContainer> forMissingInfoQuantities; // pull these as needed
		readonly IReflectedHelpyQuants<I> quant;
        readonly InstanceValue<double> tracked;
		readonly DefaultEntryRequests defaulter = new DefaultEntryRequests ();
		readonly RequestStorageHelper<String> infoNameRequest;
		public HelpedCreation(IReflectedHelpyQuants<I> quant, InstanceValue<double> tracked)
		{
			this.IsInfoComplete = quant.InfoComplete;
			this.quant = quant;
            this.tracked = tracked;

            // ready this
            Action ValidateCurrentAmount = () =>
            {
                var rv = measureAndOptionsRequest.request.value;
                var irv = measureOptionContainers[rv.SelectedRequest];
                measureAndOptionsRequest.requestValid = irv.requestStorage.requestValid;
            };

            // Get these ready!
            measureOptionContainers = new List<MeasureOptionsContainer>();
            foreach (var q in quant.quantifier_choices)
            {
                IRequestStorageHelper rs = null;
                rs = q.ConnectedValue.CreateHelper(() => rs.requestValid = q.ConnectedValue.ValidateHelper(new[] { rs }));
                measureOptionContainers.Add(new MeasureOptionsContainer
                {
                    quant = q, // 'cause ordering?
                    requestStorage = rs,
                });
                rs.requestChanged += ValidateCurrentAmount;
            }

            // and this dudes
            Func<MultiRequestOptionValue, MultiRequestOptionValue> deffer = d =>
            {
                if (d == null) return null;
                foreach (var dd in measureOptionContainers)
                    dd.requestStorage.Reset(); // wont get resetted by other dudes, hidden by the composition.
                d.SelectedRequest = 0;
                return d;
            };
            measureAndOptionsRequest = new RequestStorageHelper<MultiRequestOptionValue>("Amount", deffer, ValidateCurrentAmount);

            // we need a direct request for no info creation - and posssssibly one of each for the ones on info that might not exist.
            // also need one for info quantity.
            trackedQuantity = new RequestStorageHelper<double>(tracked.name, () => tracked.defaultValue, () => trackedQuantity.request.valid = true); // it's same on tinfo and entries

            var l = new List<InfoDataContainer> ();
			foreach (var q in quant.calculation) {
				RequestStorageHelper<double> ns = null;
                ns = new RequestStorageHelper<double>(q.name, () => q.defaultValue, () => ns.request.valid = true);
                l.Add(new InfoDataContainer { requestStore = ns, quant = q });
			}
			forMissingInfoQuantities = l;
			infoNameRequest = new RequestStorageHelper<string> ("Name", () => "", () => infoNameRequest.request.valid = true);
		}
        // deal with displaying realtime calc data for calc creates and edit.
        I currInfo;
		void SetupInfoObservers(I info)
		{
            //get this in early
            currInfo = info;

            // Check the ClearListeners - it happens on reset and Cget.
            if (info != null)  // so that mesquant is cgetted
            {

                //but this might cause dangling stuff
                if (currentMeasureOption != null)
                    currentMeasureOption.requestStorage.requestChanged -= MeasureQuantity_request_changed;

                // Determine quantifier used.
                var usedquant = (from q in measureOptionContainers where q.quant.QuantifierID == info.quantifierID select q);
                if (usedquant.Count() != 1)
                    throw new ArgumentException("Got " + usedquant.Count() + " possibilities for quantifiers!");
                currentMeasureOption = usedquant.First();
            }
            else currentMeasureOption = null;
			trackedQuantity.request.read_only = info != null;
		}
        void BeginObserving()
        {
            // watch for changes on it
            currentMeasureOption.requestStorage.requestChanged += MeasureQuantity_request_changed;
            MeasureQuantity_request_changed();
        }
		void MeasureQuantity_request_changed ()
		{
            // update the calories field with a calc
            var amt = currentMeasureOption.quant.ConnectedValue.ConvertRequestValueToData(currentMeasureOption.requestStorage.requestValue);
		    trackedQuantity.request.value = CalcVal((double)amt, currInfo);
		}
		#region IEntryCreation implementation

		public void ResetRequests ()
		{
			trackedQuantity.Reset ();
			if(currentMeasureOption != null)
                currentMeasureOption.requestStorage.Reset ();
			foreach (var mi in forMissingInfoQuantities)
				mi.requestStore.Reset ();
			defaulter.ResetRequests ();
		}

		public BindingList<object> CreationFields (IValueRequestFactory factory)
		{
			// we just need the amount...we can get the name and when etc from a defaulter...
			var rp = new BindingList<object> () { trackedQuantity.CGet(factory.DoubleRequestor) };
			defaulter.PushInDefaults (null, rp, factory);
			SetupInfoObservers (null);
			return rp;
		}

		public E Create ()
		{
			var rv = new E ();
            tracked.valueSetter (rv, trackedQuantity.request.value);
			defaulter.Set (rv);
			return rv;
		}

		public BindingList<object> CalculationFields (IValueRequestFactory factory, I info)
		{
            // do requests
			SetupInfoObservers (info);
            var blo = new BindingList<Object>() { trackedQuantity.CGet(factory.DoubleRequestor), currentMeasureOption.requestStorage.CGet(factory, currentMeasureOption.quant.ConnectedValue.FindRequestorDelegate) };
			ProcessRequestsForInfo (blo, factory, info);
			defaulter.PushInDefaults(null, blo, factory);
            BeginObserving();
			return blo;
		}

		// I know these will change when info changes...but...the buisness modelling will re-call us in those cases.
		void ProcessRequestsForInfo(BindingList<Object> requestoutput, IValueRequestFactory factory, I info)
		{
            foreach(var q in forMissingInfoQuantities)
            { 
				var getit = q.requestStore.CGet (factory.DoubleRequestor); // make sure cgot it.
				var ival = (double?)q.quant.valueGetter (info);
				if (ival.HasValue)
					q.requestStore.request.value = ival.Value; // ok got it, set requestor for easy sames
				else
					requestoutput.Add (getit); // ash need this so adddyyy. FIXME this like never occurrsss...but you know...
			}
		}

		public E Calculate (I info, bool shouldComplete)
		{
			var rv = new E ();
			var amount = currentMeasureOption.quant.ConnectedValue.ConvertRequestValueToData(currentMeasureOption.requestStorage.requestValue);
			var res = CalcVal ((double)amount, info);

            // techniacllly only need to set amount field, cause, info will be set too. and the val is calculated!
            // but i think that calc only happens here during creation and editing
			currentMeasureOption.quant.ConnectedValue.valueSetter (rv, amount);
            tracked.valueSetter(rv, res);
			defaulter.Set (rv);
			return rv;
		}

		double CalcVal(double entryAmount, I info)
		{
            // we need the amount on the entry, vs, the amount on the info to factor what goes into the calculation linearly.
            double fac = entryAmount / info.quantity;

            // formissinginfoquantities here actually contains the info values, even prior to a info being edited or anything.
            // thats cause these pop up if any are "missing" on selected info.
			List<double> vals = new List<double> ();
			foreach (var tv in forMissingInfoQuantities)
				vals.Add (tv.requestStore.request.value*fac);
			return quant.Calcluate (vals.ToArray ());
		}

		public BindingList<object> EditFields (E toEdit, IValueRequestFactory factory)
		{
			// request objects
			var rp = new BindingList<object> () { trackedQuantity.CGet(factory.DoubleRequestor) };
			defaulter.PushInDefaults (toEdit, rp, factory);
           
			// init request objects - defaulter did both
			trackedQuantity.request.value = tracked.valueGetter(toEdit);

			// give it back
			SetupInfoObservers (null); // doesnt involve quantities
			return rp;
		}

		public E Edit (E toEdit)
		{
			// commit edit...simple one
			defaulter.Set(toEdit);
			tracked.valueSetter(toEdit, trackedQuantity.request.value);
			return toEdit;
		}

		public BindingList<object> EditFields (E toEdit, IValueRequestFactory factory, I info)
		{
			// requests for editing a calced one....hmmouch?
			SetupInfoObservers (info);
			var blo = new BindingList<Object> () { trackedQuantity.CGet(factory.DoubleRequestor),  currentMeasureOption.requestStorage.CGet(factory, currentMeasureOption.quant.ConnectedValue.FindRequestorDelegate) };
			currentMeasureOption.requestStorage.requestValue = currentMeasureOption.quant.ConnectedValue.valueGetter(toEdit);
			ProcessRequestsForInfo (blo, factory, info); // initially, no, we'll add none...but maybe subsequently.
			defaulter.PushInDefaults (toEdit, blo, factory);
            BeginObserving();
            return blo;
		}

		public E Edit (E toEdit, I info, bool shouldComplete)
		{
			defaulter.Set (toEdit);
            var amount = currentMeasureOption.quant.ConnectedValue.ConvertRequestValueToData(currentMeasureOption.requestStorage.requestValue);
            double res = CalcVal ((double)amount, info);

            currentMeasureOption.quant.ConnectedValue.valueSetter(toEdit, amount);
            tracked.valueSetter(toEdit, res);
			return toEdit;
		}

        #endregion

        #region IInfoCreation implementation	
		public BindingList<object> InfoFields (IValueRequestFactory factory)
		{
            // get request options
            measureAndOptionsRequest.request.value = new MultiRequestOptionValue((from q in measureOptionContainers select q.requestStorage.CGet(factory, q.quant.ConnectedValue.FindRequestorDelegate)).ToArray(), 0);
            var rv = new BindingList<Object>() { infoNameRequest.CGet(factory.StringRequestor), measureAndOptionsRequest.CGet(factory.IValueRequestOptionGroupRequestor) };
			foreach (var iv in forMissingInfoQuantities)
				rv.Add (iv.requestStore.CGet (factory.DoubleRequestor));
			return rv;
		}

		public void FillRequestData (I item)
		{
            // going to do edit
            var usedquant = measureOptionContainers.FindAll(p => p.quant.QuantifierID == item.quantifierID); 
            if(usedquant.Count != 1) throw new ArgumentException("Got " + usedquant.Count() + " possibilities for quantifiers!");

            var qi = measureOptionContainers.IndexOf(usedquant[0]);
            measureAndOptionsRequest.request.value.SelectedRequest = qi;
            var amt = usedquant[0].quant.ConnectedValue.valueGetter(item);
            usedquant[0].requestStorage.requestValue = usedquant[0].quant.ConnectedValue.ConvertDataToRequestValue(amt);

			// put the data in item into the requests please.
            foreach(var q in forMissingInfoQuantities)
				q.requestStore.request.value = ((double?)q.quant.valueGetter(item)).Value;
			infoNameRequest.request.value = item.name;
		}

		public I MakeInfo (I toEdit = default(I))
		{
			// yeah...but a default for a reference type is null...
			var ret = toEdit ?? new I();
			ret.name = infoNameRequest;

            // get the info need to store
            int sr = measureAndOptionsRequest.request.value.SelectedRequest;
            var mv = measureOptionContainers[sr];
            var sv = mv.requestStorage.requestValue;
            var dv = mv.quant.ConnectedValue.ConvertRequestValueToData(sv);
            var qid = mv.quant.QuantifierID;

            // set them
            mv.quant.ConnectedValue.valueSetter(ret, dv);
            ret.quantifierID = qid;
            foreach(var q in forMissingInfoQuantities)
				q.quant.valueSetter(ret, q.requestStore.request.value);
			return ret;
		}

		public Expression<Func<I, bool>> IsInfoComplete { get; private set; }

		#endregion
	}

	public class SimpleTrackyHelpyPresenter<Inst, In, InInfo, Out, OutInfo> : ITrackerPresenter<Inst, In, InInfo, Out, OutInfo>
		where    Inst : TrackerInstance, new()
		where      In : HBaseEntry, new()
		where  InInfo : HBaseInfo, new()
		where     Out : HBaseEntry, new()
		where OutInfo : HBaseInfo, new()
	{
		public TrackerDetailsVM details { get { return helpy.TrackerDetails; } }
		public TrackerDialect dialect { get { return helpy.TrackerDialect; } }
		readonly IExtraRelfectedHelpy<Inst,InInfo, OutInfo> helpy;

        Dictionary<int, VRVConnectedValue> qcv_in = new Dictionary<int, VRVConnectedValue>();
        Dictionary<int, VRVConnectedValue> qcv_out = new Dictionary<int, VRVConnectedValue>();
		public SimpleTrackyHelpyPresenter(IExtraRelfectedHelpy<Inst,InInfo, OutInfo> helpy)
		{
			this.helpy = helpy;
            qcv_in = helpy.input.quantifier_choices.ToDictionary(p => p.QuantifierID, p => p.ConnectedValue);
            qcv_out = helpy.output.quantifier_choices.ToDictionary(p => p.QuantifierID, p => p.ConnectedValue);
		}

		#region IDietPresenter implementation
        InfoQuantifier GetQuant(int id, IEnumerable<InfoQuantifier> q)
        {
            var usedquant = q.Where(p => p.QuantifierID == id);
            if (usedquant.Count() != 1) throw new ArgumentException("Got " + usedquant.Count() + " possibilities for quantifiers!");
            return usedquant.First();
        }

		String QuantyGet<A>(IReflectedHelpyQuants<A> q, HBaseEntry e, HBaseInfo i)
		{
            var uq = GetQuant(i.quantifierID, q.quantifier_choices);
			return uq.DisplayConversion(e.quantity) + " of " + i.name;
		}
        const String noninfo = "Quick Entry";
		public EntryLineVM GetRepresentation (In entry, InInfo info)
		{
            return new EntryLineVM(
                entry.entryWhen,
                TimeSpan.Zero,
                entry.entryName,
                info == null ? noninfo : QuantyGet(helpy.input, entry, info),
                new KVPList<string, double> { { helpy.tracked_on_entries.name, helpy.tracked_on_entries.valueGetter(entry) } }
            );
		}
		public EntryLineVM GetRepresentation (Out entry, OutInfo info)
		{
            return new EntryLineVM(
                entry.entryWhen,
                TimeSpan.Zero,
                entry.entryName,
                info == null ? noninfo : QuantyGet(helpy.output,entry, info),
                new KVPList<string, double> { { helpy.tracked_on_entries.name, helpy.tracked_on_entries.valueGetter(entry) } }
            );
		}

		SimpleTrackyTarget[] GetTargets(Inst entry)
		{
            var vals = from cv in helpy.instanceValueFields select cv.valueGetter(entry);
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
                if (!target.Shown) continue;
				if (target.DayPattern.Length == 1)
					kl.Add (helpy.tracked_on_entries.name + " per " + TimeSpan.FromDays (target.DayPattern [0]).WithSuffix (), target.DayTargets [0]);
				else
					for (int i = 0; i < target.DayPattern.Length; i++)
						kl.Add (helpy.tracked_on_entries.name + " for " + TimeSpan.FromDays (target.DayPattern [i]).WithSuffix (true), target.DayTargets [i]);	
			}
			return new TrackerInstanceVM(
				dialect,
				entry.tracked,
				entry.startpoint, 
				entry.name,
				"",
				kl
			);
		}
        KVPList<String, double> GetIK<A>(IReflectedHelpyQuants<A> q, A info) where A : HBaseInfo
        {
            var uq = GetQuant(info.quantifierID, helpy.input.quantifier_choices);
            return new KVPList<string, double>
                    {
                        { helpy.tracked_on_entries.name, helpy.tracked_on_entries.valueGetter(info) },
                        { uq.ConnectedValue.name, (double)uq.ConnectedValue.valueGetter(info) }
                    };
        }
		public InfoLineVM GetRepresentation (InInfo info)
		{
            return new InfoLineVM
            {
                name = info.name,
                displayAmounts = GetIK(helpy.input, info)
            };
		}
		public InfoLineVM GetRepresentation (OutInfo info)
		{
            return new InfoLineVM
            {
                name = info.name,
                displayAmounts = GetIK(helpy.output, info)
            };
        }
        
		public IEnumerable<TrackingInfoVM> DetermineInTrackingForDay(Inst di, EntryRetriever<In> eats, EntryRetriever<Out> burns, DateTime dayStart)
		{
			var targets = GetTargets (di);
			for (var ti=0; ti < targets.Length; ti++) {
                if (!targets[ti].Tracked) continue;
				var trg = targets [ti].FindTargetForDay(di.startpoint, dayStart);
				var dtr = targets [ti].DayTargetRange;
                var dtt = targets[ti].DayTargetRangeType;
                var yret = new TrackingInfoVM { targetValue = trg.target };
				GetValsForRange (eats, burns, trg.begin, trg.end, out yret.inValues, out yret.outValues);
                // FIXME I'm not sure about AggregateRangeType.DaysFromStart, if dtr == 0 means range = 0 or 1, ready nope
                if (dtr ==0)
                    yret.targetValueName = targets[ti].TargetName;
                else
                {
                    int fac = dtt == AggregateRangeType.DaysEitherSide ?  2 : 1;
                    yret.targetValueName = targets[ti].TargetName + " per " + TimeSpan.FromDays(dtr*fac + fac - 1).WithSuffix();
                }
                yret.inValuesName = dialect.InputInfoVerbPast;
                yret.outValuesName = dialect.OutputInfoVerbPast;
				yield return yret;
			}
		}
		void GetValsForRange(EntryRetriever<In> eats, EntryRetriever<Out> burns, DateTime s, DateTime e, out TrackingElementVM[] invals,out TrackingElementVM[] outvals )
		{
			List<TrackingElementVM> vin = new List<TrackingElementVM> (), vout = new List<TrackingElementVM> ();
			foreach (var ient in eats(s, e)) {
				var v = helpy.tracked_on_entries.valueGetter(ient);
				vin.Add (new TrackingElementVM () { value = v , name = ient.entryName });
			}
			foreach (var oent in burns(s,e)) {
				var v = helpy.tracked_on_entries.valueGetter(oent);
				vout.Add (new TrackingElementVM () { value = v, name = oent.entryName });
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