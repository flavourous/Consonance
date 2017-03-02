using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;
using LibSharpHelp;
using LibRTP;
using System.Diagnostics;
using System.Linq;
using static Consonance.Presenter;
using Consonance.Protocol;
using System.Collections.ObjectModel;

namespace Consonance
{
    // basemodels
    interface HQ
    {
        double quantity { get; set; }
    }
    public enum InfoQuantifierTypes { Number, Duration };
    [TableIdentifier(1)]
    public class SimpleTrackyInfoQuantifierDescriptor : BaseDB, IPrimaryKey
    {
        public InfoQuantifierTypes quantifier_type { get; set; }
        public double defaultvalue { get; set; }
        public String Name { get; set; }
        public override bool Equals(object obj)
        {
            if(obj is SimpleTrackyInfoQuantifierDescriptor)
            {
                var sti = obj as SimpleTrackyInfoQuantifierDescriptor;
                return sti.quantifier_type == quantifier_type && sti.defaultvalue == defaultvalue && sti.Name == Name;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return HashUtil.CombineHashCodes(quantifier_type, defaultvalue, Name);
        }
    }
    public abstract class HBaseInfo : BaseInfo,HQ
    {
        public int quantifierID { get; set; }
        public double quantity { get; set; }
    }
    public abstract class HBaseEntry : BaseEntry,HQ
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


    class SimpleTrackerHolder<T, I, Ii, O, Oi> : ITracker<T, I, Ii, O, Oi>
        where T : TrackerInstance, new()
        where I : HBaseEntry, new()
        where O : HBaseEntry, new()
        where Ii : HBaseInfo, new()
        where Oi : HBaseInfo, new()
    {
        public bool ShareInfo { get { return true; } }
        public IServices services
        {
            set
            {
                _model.Init(value.dal);
                _presenter.Init(value.dal);
            }
        }
        readonly SimpleTrackyHelpy<T, I, Ii, O, Oi> _model;
        public ITrackModel<T,I,Ii,O,Oi> model { get { return _model; } }
        readonly SimpleTrackyHelpyPresenter<T, I, Ii, O, Oi> _presenter;
        public ITrackerPresenter<T,I,Ii,O,Oi> presenter { get { return _presenter; } }
        public SimpleTrackerHolder(IExtraRelfectedHelpy<T,Ii,Oi> helpy)
		{
			_model = new SimpleTrackyHelpy<T, I,Ii, O, Oi> (helpy);
			_presenter =new SimpleTrackyHelpyPresenter<T, I,Ii, O, Oi> (helpy);
		}
	}

    // Derriving from the LibRTP here...
    public class SimpleTrackyTarget : RecurringAggregatePattern
    {
        public readonly bool Tracked, Shown;
        public readonly String TargetName, TargetID;
        public SimpleTrackyTarget(String targetName,String targetID, bool tracked, bool shown, int targetRange, AggregateRangeType rangetype, int[] targetPattern, double[] patternTarget)
            : base(targetRange, rangetype, targetPattern, patternTarget)
        {
            this.TargetID = targetID;
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
        IReflectedHelpyQuants<InInfo> input { get; }
		IReflectedHelpyQuants<OutInfo> output { get; }
	}
    public interface IReflectedHelpyQuants<I>
    {
        InstanceValue<double>[] calculation { get; } // these are the fields we want to take from/store to the info
        IReflectedHelpyCalc[] calculators { get; }
        SimpleTrackyInfoQuantifierDescriptor[] quantifier_choices { get; }
        Expression<Func<I, bool>> InfoComplete { get; }
    }
    public interface IReflectedHelpyCalc
    {
        String TargetID { get; }
        InstanceValue<double> direct { get; } // this is the no-info storage on the entry (and used as a calculation cache it seems)
        double Calculate(double[] values);
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

	class SimpleTrackyHelpy<Inst, In, InInfo, Out, OutInfo> : ITrackModel<Inst, In, InInfo, Out, OutInfo>
		where    Inst : TrackerInstance, new()
		where      In : HBaseEntry, new()
		where  InInfo : HBaseInfo, new()
		where     Out : HBaseEntry, new()
		where OutInfo : HBaseInfo, new()
	{
        public void Init(IDAL dal)
        {
            inc.Init(dal);
            ouc.Init(dal);
        }
		readonly HelpedCreation<In,InInfo> inc;
		readonly HelpedCreation<Out,OutInfo> ouc;
		readonly IExtraRelfectedHelpy<Inst,InInfo,OutInfo> helpy; 

		readonly IReadOnlyList<IRSPair<Inst>> flectyRequests;
		readonly DefaultTrackerInstanceRequests defaultTrackerStuff;
		public SimpleTrackyHelpy(IExtraRelfectedHelpy<Inst,InInfo, OutInfo> helpy) 
		{
			this.helpy = helpy; 
			defaultTrackerStuff = new DefaultTrackerInstanceRequests (helpy.TrackerDetails.category);
			inc = new HelpedCreation<In, InInfo> (helpy.input);
			ouc = new HelpedCreation<Out, OutInfo> (helpy.output);
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
            var rqs = new ObservableCollection<Object>(
                flectyRequests.Select(f => f.requestStore.CGet(factory, f.descriptor.FindRequestorDelegate))
                );
            defaultTrackerStuff.PushInDefaults (null, rqs, factory);
			var gvp = new GetValuesPage (helpy.TrackerDetails.name);
			gvp.SetList (rqs);
			yield return gvp;
		}
		public IEnumerable<GetValuesPage> EditPages (Inst editing, IValueRequestFactory factory)
		{
			defaultTrackerStuff.Reset (); // maybe on ITrackModel interface? :/
            var rqs = new ObservableCollection<Object>(
                flectyRequests.Select(f => f.requestStore.CGet(factory, f.descriptor.FindRequestorDelegate))
                );
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

    class MeasureOptionsContainer
    {
        public SimpleTrackyInfoQuantifierDescriptor desc;
        public VRVConnectedValue quant;
        public IRequestStorageHelper requestStorage;
        public Func<double, String> displayConversion;
    }
    class InfoFinderHelper
    {
        IDAL dal;
        readonly Action Validator;
        public InfoFinderHelper(IDAL conn, Action Validator)
        {
            this.dal = conn;
            this.Validator = Validator;
        }

        // if you have an actual FK
        public MeasureOptionsContainer FindMO(int qid)
        {
            // From somewhere random? dont create if it's not been created yet (errore)
            var w = dal.Get<SimpleTrackyInfoQuantifierDescriptor>(d => d.id == qid);
            return Geto(w.First()); // otherwise, how did you ask?? :/
        }

        public SimpleTrackyInfoQuantifierDescriptor Find(SimpleTrackyInfoQuantifierDescriptor q)
        {
            dal.CreateTable<SimpleTrackyInfoQuantifierDescriptor>(); // ensure

            // Ensure has in DB - we wont have an id to start with from defs
            var hit =dal.Get<SimpleTrackyInfoQuantifierDescriptor>(d => d.Name == q.Name && d.quantifier_type == q.quantifier_type && d.defaultvalue == q.defaultvalue);
            if (hit.Count() != 1) return null;
            else return hit.First();
        }

        // For no id (maybe create)
        public MeasureOptionsContainer CreateMO(SimpleTrackyInfoQuantifierDescriptor q)
        {
            var exist = Find(q);
            if (exist == null) dal.Commit(q); // updates pk on this instance too
            else q = exist;

            // Give it back (with proper k)
            return  Geto(q);
        }

        MeasureOptionsContainer Geto(SimpleTrackyInfoQuantifierDescriptor q )
        {
            var moc = new MeasureOptionsContainer { desc = q };
            moc.quant = FromEntry(q, out moc.displayConversion);
            moc.requestStorage = moc.quant.CreateHelper(
                () => moc.requestStorage.requestValid =
                moc.quant.ValidateHelper(new[] { moc.requestStorage }));
            moc.requestStorage.requestChanged += Validator;
            return moc;
        }

        VRVConnectedValue FromEntry(SimpleTrackyInfoQuantifierDescriptor dsc, out Func<double,string> cv)
        {
            Func<object, object> getQuantity = o => ((HQ)o).quantity;
            Action<object, object> setQuantity = (o, v) => ((HQ)o).quantity = (double)v;
            switch (dsc.quantifier_type)
            {
                default:
                case InfoQuantifierTypes.Number:
                    cv = d => d.ToString("F2");
                    return VRVConnectedValue.FromTypec(dsc.defaultvalue, d => d > 0.0, dsc.Name, getQuantity, setQuantity, f => f.DoubleRequestor, d => d, d => d);
                case InfoQuantifierTypes.Duration:
                    cv = d => TimeSpan.FromSeconds(d).WithSuffix();
                    return VRVConnectedValue.FromTypec(dsc.defaultvalue, d => d.TotalHours > 0.0, dsc.Name, getQuantity, setQuantity, f => f.TimeSpanRequestor, d => d.TotalHours, d => TimeSpan.FromHours(d));
            }
        }
    }

	class HelpedCreation<E,I> : IEntryCreation<E,I>
		where E : HBaseEntry, new()
		where I : HBaseInfo, new()
	{
		// for info requests
        InfoFinderHelper ihelper;
        List<MeasureOptionsContainer> measureOptionContainers;
        MeasureOptionsContainer currentMeasureOption = null;
        readonly RequestStorageHelper<MultiRequestOptionValue> measureAndOptionsRequest;
        readonly Dictionary<int, int> IndexToQID = new Dictionary<int, int>();
        class TargetedRequestStorage
        {
            public InstanceValue<double> quant;
            public RequestStorageHelper<double> requestStore;
        }
        readonly IReadOnlyList<TargetedRequestStorage> forMissingInfoQuantities; // pull these as needed
        readonly IReadOnlyDictionary<String, TargetedRequestStorage> directRequests;
		readonly IReflectedHelpyQuants<I> quant;
		readonly DefaultEntryRequests defaulter = new DefaultEntryRequests ();
		readonly RequestStorageHelper<String> infoNameRequest;

        public void Init(IDAL dal)
        {
            this.ihelper = new InfoFinderHelper(dal, ValidateCurrentAmount);

            // Get these ready!
            measureOptionContainers = quant.quantifier_choices.Select(q => ihelper.CreateMO(q)).ToList();
        }

		public HelpedCreation(IReflectedHelpyQuants<I> quant)
		{
			this.IsInfoComplete = quant.InfoComplete;
			this.quant = quant;


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

            // we need direct requests for no info creation 
            var dr = new Dictionary<String, TargetedRequestStorage>();
            foreach (var d in quant.calculators) dr.Add(d.TargetID, Gen(d.direct));
            directRequests = dr;

            // posssssibly one request of each for the ones on info that might not exist.
            forMissingInfoQuantities = new List<TargetedRequestStorage>(from q in quant.calculation select Gen(q));

            // also need one for info quantity.
            infoNameRequest = new RequestStorageHelper<string> ("Name", () => "", () => infoNameRequest.request.valid = true);
		}
        TargetedRequestStorage Gen(InstanceValue<double> q)
        {
            RequestStorageHelper<double> ns = null;
            ns = new RequestStorageHelper<double>(q.name, () => q.defaultValue, () => ns.request.valid = true);
            return new TargetedRequestStorage { requestStore = ns, quant = q };
        }
        // ready this
        void ValidateCurrentAmount()
        {
            var rv = measureAndOptionsRequest.request.value;
            if (rv != null)
            {
                var irv = rv.SelectedRequest == -1 ? hiddenQuant : measureOptionContainers[rv.SelectedRequest];
                measureAndOptionsRequest.requestValid = irv.requestStorage.requestValid;
            }
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
                currentMeasureOption = ihelper.FindMO(info.quantifierID);
            }
            else currentMeasureOption = null;
            directRequests.Act(d => d.Value.requestStore.request.read_only = info != null);
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
            var amt = currentMeasureOption.quant.ConvertRequestValueToData(currentMeasureOption.requestStorage.requestValue);
            var cv=CalcVals((double)amt, currInfo);
            directRequests.Act(d => d.Value.requestStore.request.value = cv[d.Key]);
		}

		#region IEntryCreation implementation

		public void ResetRequests ()
		{
            directRequests.Act(d => d.Value.requestStore.Reset());
			if(currentMeasureOption != null)
                currentMeasureOption.requestStorage.Reset ();
			foreach (var mi in forMissingInfoQuantities)
				mi.requestStore.Reset ();
			defaulter.ResetRequests ();
		}

		public IList<object> CreationFields (IValueRequestFactory factory)
		{
            // we just need the amount...we can get the name and when etc from a defaulter...
            var rp = new ObservableCollection<Object>(
                directRequests.Select(d => d.Value.requestStore.CGet(factory.DoubleRequestor))
            );
			defaulter.PushInDefaults (null, rp, factory);
			SetupInfoObservers (null);
			return rp;
		}

		public E Create ()
		{
			var rv = new E ();
            directRequests.Act(d => d.Value.quant.valueSetter(rv, d.Value.requestStore.request.value));
			defaulter.Set (rv);
			return rv;
		}

		public IList<object> CalculationFields (IValueRequestFactory factory, I info)
		{
            // do requests
			SetupInfoObservers (info);
            var blo = new ObservableCollection<Object>();
            blo.Add(currentMeasureOption.requestStorage.CGet(factory, currentMeasureOption.quant.FindRequestorDelegate));
            blo.AddAll(directRequests.Select(d => d.Value.requestStore.CGet(factory.DoubleRequestor)));//readonly
			ProcessRequestsForInfo (blo, factory, info);
			defaulter.PushInDefaults(null, blo, factory);
            BeginObserving();
			return blo;
		}

		// I know these will change when info changes...but...the buisness modelling will re-call us in those cases.
		void ProcessRequestsForInfo(ObservableCollection<Object> requestoutput, IValueRequestFactory factory, I info)
		{
            foreach(var q in forMissingInfoQuantities)
            { 
				var getit = q.requestStore.CGet (factory.DoubleRequestor); // make sure cgot it.
				var ival = (double?)q.quant.valueGetter (info);
				if (ival.HasValue) q.requestStore.request.value = ival.Value; // ok got it, set requestor for easy sames
				else requestoutput.Add (getit); // ash need this so adddyyy. FIXME this like never occurrsss...but you know...
			}
		}

		public E Calculate (I info, bool shouldComplete)
		{
			var rv = new E ();
			var amount = currentMeasureOption.quant.ConvertRequestValueToData(currentMeasureOption.requestStorage.requestValue);
			var res = CalcVals ((double)amount, info);

            // techniacllly only need to set amount field, cause, info will be set too. and the val is calculated!
            // but i think that calc only happens here during creation and editing
			currentMeasureOption.quant.valueSetter (rv, amount);
            directRequests.Act(d => d.Value.quant.valueSetter(rv, res[d.Key]));
			defaulter.Set (rv);
			return rv;
		}

		IReadOnlyDictionary<String,double> CalcVals(double entryAmount, I info)
		{
            // we need the amount on the entry, vs, the amount on the info to factor what goes into the calculation linearly.
            double fac = entryAmount / info.quantity;

            // formissinginfoquantities here actually contains the info values, even prior to a info being edited or anything.
            // thats cause these pop up if any are "missing" on selected info.
			List<double> vals = new List<double> ();
			foreach (var tv in forMissingInfoQuantities)
				vals.Add (tv.requestStore.request.value*fac);
            return quant.calculators.ToDictionary(c => c.TargetID, c => c.Calculate(vals.ToArray()));
		}

		public IList<object> EditFields (E toEdit, IValueRequestFactory factory)
		{
            // request objects
            var rp = new ObservableCollection<Object>(
                directRequests.Select(d => d.Value.requestStore.CGet(factory.DoubleRequestor))
            );
			defaulter.PushInDefaults (toEdit, rp, factory);

            // init request objects - defaulter did both
            directRequests.Act(d => d.Value.requestStore.request.value = d.Value.quant.valueGetter(toEdit));

			// give it back
			SetupInfoObservers (null); // doesnt involve quantities
			return rp;
		}

		public E Edit (E toEdit)
		{
			// commit edit...simple one
			defaulter.Set(toEdit);
            directRequests.Act(d => d.Value.quant.valueSetter(toEdit, d.Value.requestStore.request.value));
			return toEdit;
		}

		public IList<object> EditFields (E toEdit, IValueRequestFactory factory, I info)
		{
			// requests for editing a calced one....hmmouch?
			SetupInfoObservers (info);
            var blo = new ObservableCollection<Object>();
            blo.Add(currentMeasureOption.requestStorage.CGet(factory, currentMeasureOption.quant.FindRequestorDelegate));
            blo.AddAll(directRequests.Select(d => d.Value.requestStore.CGet(factory.DoubleRequestor)));//readonly
            currentMeasureOption.requestStorage.requestValue = currentMeasureOption.quant.valueGetter(toEdit);
			ProcessRequestsForInfo (blo, factory, info); // initially, no, we'll add none...but maybe subsequently.
			defaulter.PushInDefaults (toEdit, blo, factory);
            BeginObserving();
            return blo;
		}

		public E Edit (E toEdit, I info, bool shouldComplete)
		{
			defaulter.Set (toEdit);
            var amount = currentMeasureOption.quant.ConvertRequestValueToData(currentMeasureOption.requestStorage.requestValue);
            var res = CalcVals ((double)amount, info);

            currentMeasureOption.quant.valueSetter(toEdit, amount);
            directRequests.Act(d => d.Value.quant.valueSetter(toEdit, res[d.Key]));
            return toEdit;
		}

        #endregion

        #region IInfoCreation implementation	
		public IList<object> InfoFields (IValueRequestFactory factory)
		{
            var rv = new ObservableCollection<Object>();
            rv.Add(infoNameRequest.CGet(factory.StringRequestor));
            rv.Add(measureAndOptionsRequest.CGet(factory.IValueRequestOptionGroupRequestor));

            // get request options
            measureAndOptionsRequest.request.value = new MultiRequestOptionValue(
                (from q in measureOptionContainers
                 select q.requestStorage.CGet(factory, q.quant.FindRequestorDelegate))
                 .ToArray(), 0, null);

            foreach (var iv in forMissingInfoQuantities)
				rv.Add (iv.requestStore.CGet (factory.DoubleRequestor));
			return rv;
		}

        MeasureOptionsContainer hiddenQuant;
		public void FillRequestData (I item, IValueRequestFactory factory)
		{
            // going to do edit
            var usedquant = ihelper.FindMO(item.quantifierID);

            // this is -1 if not option on this traker
            var qi = measureOptionContainers.FindIndex(u => u.desc.id == item.quantifierID);

            // need hdden request? -1?
            Object hr = null;
            if (qi == -1)
            {
                hiddenQuant = usedquant;
                hr = usedquant.requestStorage.CGet(factory, usedquant.quant.FindRequestorDelegate);
            }
            measureAndOptionsRequest.request.value.HiddenRequest = hr;
            measureAndOptionsRequest.request.value.SelectedRequest = qi;

            var amt = usedquant.quant.valueGetter(item);
            usedquant.requestStorage.requestValue = usedquant.quant.ConvertDataToRequestValue(amt);

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
            var mv = sr == -1 ? hiddenQuant : measureOptionContainers[sr];
            var sv = mv.requestStorage.requestValue;
            var dv = mv.quant.ConvertRequestValueToData(sv);
            var qid = mv.desc.id;

            // set them
            mv.quant.valueSetter(ret, dv);
            ret.quantifierID = qid;
            foreach(var q in forMissingInfoQuantities)
				q.quant.valueSetter(ret, q.requestStore.request.value);
			return ret;
		}

		public Expression<Func<I, bool>> IsInfoComplete { get; private set; }

		#endregion
	}

	class SimpleTrackyHelpyPresenter<Inst, In, InInfo, Out, OutInfo> : ITrackerPresenter<Inst, In, InInfo, Out, OutInfo>
		where    Inst : TrackerInstance, new()
		where      In : HBaseEntry, new()
		where  InInfo : HBaseInfo, new()
		where     Out : HBaseEntry, new()
		where OutInfo : HBaseInfo, new()
	{
		public TrackerDetailsVM details { get { return helpy.TrackerDetails; } }
		public TrackerDialect dialect { get { return helpy.TrackerDialect; } }
		readonly IExtraRelfectedHelpy<Inst,InInfo, OutInfo> helpy;
        InfoFinderHelper ihelper;
        public void Init(IDAL dal)
        {
            this.ihelper = new InfoFinderHelper(dal, delegate { });
            helpy.input.quantifier_choices.Concat(helpy.output.quantifier_choices)
                .Select(c => ihelper.CreateMO(c)); // not used but starts things up
        }
        public SimpleTrackyHelpyPresenter(IExtraRelfectedHelpy<Inst,InInfo, OutInfo> helpy)
		{
			this.helpy = helpy;
		}

		#region IDietPresenter implementation
		String QuantyGet<A>(IReflectedHelpyQuants<A> q, HBaseEntry e, HBaseInfo i)
		{
            var uq = ihelper.FindMO(i.quantifierID);
			return uq.displayConversion(e.quantity) + " of " + i.name;
		}
        const String noninfo = "Quick Entry";
		public EntryLineVM GetRepresentation (In entry, InInfo info)
		{
            return new EntryLineVM(
                entry.entryWhen,
                TimeSpan.Zero,
                entry.entryName,
                info == null ? noninfo : QuantyGet(helpy.input, entry, info),
                new KVPList<string, double>(helpy.input.calculators.ToKeyValue(d => d.TargetID, d => d.direct.valueGetter(entry)))
            );
		}
		public EntryLineVM GetRepresentation (Out entry, OutInfo info)
		{
            return new EntryLineVM(
                entry.entryWhen,
                TimeSpan.Zero,
                entry.entryName,
                info == null ? noninfo : QuantyGet(helpy.output,entry, info),
                new KVPList<string, double>(helpy.output.calculators.ToKeyValue(d => d.TargetID, d => d.direct.valueGetter(entry)))
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
					kl.Add (target.TargetID + " per " + TimeSpan.FromDays (target.DayPattern [0]).WithSuffix (), target.DayTargets [0]);
				else
					for (int i = 0; i < target.DayPattern.Length; i++)
						kl.Add (target.TargetID + " for " + TimeSpan.FromDays (target.DayPattern [i]).WithSuffix (true), target.DayTargets [i]);	
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
            var uq = ihelper.FindMO(info.quantifierID);
            var args = q.calculation.Select(c => c.valueGetter(info)).ToArray();
            var res = q.calculators.ToKeyValue(c => c.TargetID, c => c.Calculate(args));
            var kl = new KVPList<string, double>(res);
            kl.Add(uq.quant.name, (double)uq.quant.valueGetter(info));
            return kl;
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
				var trg = targets[ti].FindTargetForDay(di.startpoint, dayStart);
				var dtr = targets[ti].DayTargetRange;
                var dtt = targets[ti].DayTargetRangeType;
                var yret = new TrackingInfoVM { targetValue = trg.target };
                var tgin = helpy.input.calculators.Where(c => c.TargetID == targets[ti].TargetID).First().direct.valueGetter;
                var tgout = helpy.output.calculators.Where(c => c.TargetID == targets[ti].TargetID).First().direct.valueGetter;
                GetValsForRange (eats, burns, tgin,tgout,  trg.begin, trg.end, out yret.inValues, out yret.outValues);
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
		void GetValsForRange(EntryRetriever<In> eats, EntryRetriever<Out> burns, Func<In,double> tgin, Func<Out,double> tgout,  DateTime s, DateTime e, out TrackingElementVM[] invals,out TrackingElementVM[] outvals )
		{
			List<TrackingElementVM> vin = new List<TrackingElementVM> (), vout = new List<TrackingElementVM> ();
			foreach (var ient in eats(s, e)) {
				var v = tgin(ient);
				vin.Add (new TrackingElementVM () { value = v , name = ient.entryName });
			}
			foreach (var oent in burns(s,e)) {
				var v = tgout(oent);
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