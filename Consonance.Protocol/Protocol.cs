using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Consonance.Protocol
{
    public static class IInputResponseExtensions
    {
        class irp : IInputResponse
        {
            public irp(IInputResponse other)
            {
                Opened = other.Opened;
                mResult = other.Result;
                fClose = other.Close;
            }
            public Task Opened { get; }
            public Task mResult;
            public Task Result { get { return mResult; } }
            public Func<Task> fClose { get; }
            public Task Close() => fClose();
        }
        class irp<T> : irp, IInputResponse<T>
        {
            new public Task<T> mResult;
            Task<T> IInputResponse<T>.Result { get { return mResult; } }
            public irp(IInputResponse<T> other) : base(other)
            {
                mResult = other.Result;
            }
        }
        public static IInputResponse ContinueWith(this IInputResponse @this, Action<Task> t)
        {
            return new irp(@this) { mResult = @this.Result.ContinueWith(t) };
        }
        public static IInputResponse ContinueWith<T>(this IInputResponse<T> @this, Action<Task<T>> t)
        {
            return new irp(@this) { mResult = @this.Result.ContinueWith(t) };
        }
        public static IInputResponse<T> ContinueWith<T>(this IInputResponse<T> @this, Func<Task<T>,T> t)
        {
            return new irp<T>(@this) { mResult = @this.Result.ContinueWith<T>(t) };
        }
    }

    public interface IInputResponse
    {
        Task Opened { get; }
        Task Result { get; }
        Task Close();
    }
    public interface IInputResponse<T> : IInputResponse
    {
        new Task<T> Result { get; }
    }

    // mirrors librtp
    [Flags]
    public enum RecurrSpan : uint { Day = 1, Week = 2, Month = 4, Year = 8 }; 

    public interface ITracker<D, E, Ei, B, Bi>
            where D : TrackerInstance, new()
            where E : BaseEntry, new()
            where Ei : BaseInfo, new()
            where B : BaseEntry, new()
            where Bi : BaseInfo, new()
    {
        // Servies
        IConfiguration config { get; }
        IServices services { set; }
        ITrackModel<D, E, Ei, B, Bi> model { get; }
        ITrackerPresenter<D, E, Ei, B, Bi> presenter { get; }
    }

    public interface IConfiguration
    {
        bool ShareInfo { get; }
    }

    #region DIET_PLAN_INTERFACES
    // creates stuff from pages - shared.
    public interface ICreateable<T> where T : BaseDB
    {
        // creator for dietinstance
        IEnumerable<GetValuesPage> CreationPages(IValueRequestFactory factory);
        IEnumerable<GetValuesPage> EditPages(T editing, IValueRequestFactory factory);
        T New();
        void Edit(T toEdit);
    }

    public class GetValuesPage : INotifyPropertyChanged
    {
        public readonly String title;
        public IList<Object> valuerequests { get; private set; }
        public event NotifyCollectionChangedEventHandler valuerequests_CollectionChanged = delegate { };
        public event PropertyChangedEventHandler valuerequests_PropertyChanged = delegate { };

        // this is us
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        readonly NotifyCollectionChangedEventHandler nc;
        readonly PropertyChangedEventHandler pc;
        public GetValuesPage(String title)
        {
            this.title = title;
            nc = (o, e) => valuerequests_CollectionChanged(o, e);
            pc = (o, e) => valuerequests_PropertyChanged(o, e);
            SetList(new ObservableCollection<Object>());
        }
        public void SetList(IList<Object> newlist)
        {
            // Remove events from old list
            I<INotifyPropertyChanged>(valuerequests, d => d.PropertyChanged -= pc);
            I<INotifyCollectionChanged>(valuerequests, d => d.CollectionChanged -= nc);

            // change list in here
            valuerequests = newlist;
            PropertyChanged(this, new PropertyChangedEventArgs("valuerequests"));

            // Hook events of new list to our events
            I<INotifyPropertyChanged>(valuerequests, d => d.PropertyChanged += pc);
            I<INotifyCollectionChanged>(valuerequests, d => d.CollectionChanged += nc);

            // Pretend that out list has reset
            pc(valuerequests, new PropertyChangedEventArgs("Count"));
            pc(valuerequests, new PropertyChangedEventArgs("Item[]"));
            nc(valuerequests, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        void I<T>(Object t, Action<T> d) where T : class
        {
            if (t is T) d(t as T);
        }
    }

    public interface IModelRouter
    {
        bool GetTableRoute<T>(out String tabl, out String[] columns, out Type[] colTypes);
        /// <summary>
        /// dynamic equivilant of TableIdentifier
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        bool GetTableIdentifier<T>(out int id);
    }
    public interface IDAL
    {
        int Count<T>(Expression<Func<T, bool>> where = null) where T : class;
        IEnumerable<T> Get<T>(Expression<Func<T, bool>> where = null) where T : class;
        int Update<T,X>(Expression<Func<T,X>> selector, X value, Expression<Func<T, bool>> where = null) where T : class;
        void Commit<T>(T item) where T : class;
        void Delete<T>(Expression<Func<T, bool>> where = null) where T : class;
        void CreateTable<T>() where T : class;
        void DropTable<T>() where T : class;
        IKeyTo<T> CreateOneToMany<F,T>(F from_this, int from_property_id)
            where T : class, IPrimaryKey
            where F : class, IPrimaryKey;
        IDAL Routed(IModelRouter router);
    }

    public interface IPrimaryKey
    {
        int id { get; set; }
    }
    public interface IKeyTo
    {
        void Clear();
        void Commit();
    }
    public interface IKeyTo<T> : IKeyTo
    {
        IEnumerable<T> Get();
        void Remove(IEnumerable<T> values);
        void Add(IEnumerable<T> values);
        void Replace(IEnumerable<T> values);
    }


    public interface ITrackModel<D, Te, Tei, Tb, Tbi> : ICreateable<D>
        where D : TrackerInstance
        where Te : BaseEntry
        where Tei : BaseInfo
        where Tb : BaseEntry
        where Tbi : BaseInfo
    {
        // creates items
        IEntryCreation<Te, Tei> increator { get; }
        IEntryCreation<Tb, Tbi> outcreator { get; }
    }
    public interface IServices
    {
        IDAL dal { get; }
    }

    public class TrackerDialect
    {
        public readonly String
            InputEntryVerb, OutputEntryVerb,
            InputInfoPlural, InputInfoSingular,
            OutputInfoPlural, OutputInfoSingular,
            InputInfoVerbPast, OutputInfoVerbPast;
        public TrackerDialect(
            String InputEntryVerb, String OutpuEntryVerb,
            String InputInfoPlural, String InputInfoSingular,
            String OutputInfoPlural, String OutputInfoSingular,
            String InputInfoVerbPast, String OutputInfoVerbPast
            )
        {
            this.InputEntryVerb = InputEntryVerb;
            this.OutputEntryVerb = OutpuEntryVerb;
            this.InputInfoPlural = InputInfoPlural;
            this.InputInfoSingular = InputInfoSingular;
            this.OutputInfoPlural = OutputInfoPlural;
            this.OutputInfoSingular = OutputInfoSingular;
            this.InputInfoVerbPast = InputInfoVerbPast;
            this.OutputInfoVerbPast = OutputInfoVerbPast;
        }
    }


    // All IList<T> for requests should impliment ICollectionChanged, IPropertyChanged like ObservableCollection<T> to
    // facilitate requests changing during a session.
     
    public interface IEntryCreation<EntryType, InfoType> : IInfoCreation<InfoType> where InfoType : class
    {
        // ok you can clear stored data now
        void ResetRequests();

        // What named fields do I need to fully create an entry (eg eating a banana) - "kcal", "fat"
        IList<Object> CreationFields(IValueRequestFactory factory);
        // Here's those values, give me the entry (ww points on eating a bananna) - broker wont attempt to remember a "item / info".
        EntryType Create();

        // Ok, I've got info on this food (bananna, per 100g, only kcal info) - I still need "fat" and "grams"
        IList<Object> CalculationFields(IValueRequestFactory factory, InfoType info);
        // right, heres the fat too, give me entry (broker will update that bananna info also)
        EntryType Calculate(InfoType info, bool shouldComplete);

        // and again for editing
        IList<Object> EditFields(EntryType toEdit, IValueRequestFactory factory);
        EntryType Edit(EntryType toEdit);
        IList<Object> EditFields(EntryType toEdit, IValueRequestFactory factory, InfoType info);
        EntryType Edit(EntryType toEdit, InfoType info, bool shouldComplete);
    }
    public interface IInfoCreation<InfoType> where InfoType : class
    {
        // So what info you need to correctly create an info on an eg food item from scratch? "fat" "kcal" "per grams" please
        IList<Object> InfoFields(IValueRequestFactory factory);
        // ok so those objects, put this data in them. im editing, for exmaple.
        void FillRequestData(InfoType item, IValueRequestFactory factory);
        // ok make me an info please here's that data.
        InfoType MakeInfo(InfoType toEdit = null);
        // ok is this info like complete for your diety? yes. ffs.
        Expression<Func<InfoType, bool>> IsInfoComplete { get; }
    }
    #endregion
    #region dpres
    public delegate IEnumerable<T> EntryRetriever<T>(DateTime start, DateTime end);
    public interface ITrackerPresenter<DietInstType, EatType, EatInfoType, BurnType, BurnInfoType>
        where DietInstType : TrackerInstance, new()
        where EatType : BaseEntry, new()
        where EatInfoType : BaseInfo, new()
        where BurnType : BaseEntry, new()
        where BurnInfoType : BaseInfo, new()
    {
        TrackerDialect dialect { get; }
        TrackerDetailsVM details { get; }

        InfoLineVM GetRepresentation(EatInfoType info);
        InfoLineVM GetRepresentation(BurnInfoType info);

        EntryLineVM GetRepresentation(EatType entry, EatInfoType entryInfo);
        EntryLineVM GetRepresentation(BurnType entry, BurnInfoType entryInfo);

        TrackerInstanceVM GetRepresentation(DietInstType entry);

        // Deals with goal tracking
        IEnumerable<TrackingInfoVM> DetermineInTrackingForDay(DietInstType di, EntryRetriever<EatType> eats, EntryRetriever<BurnType> burns, DateTime dayStart);
        IEnumerable<TrackingInfoVM> DetermineOutTrackingForDay(DietInstType di, EntryRetriever<EatType> eats, EntryRetriever<BurnType> burns, DateTime dayStart);
    }
    public class OriginatorVM
    {
        public static bool OriginatorEquals(OriginatorVM first, OriginatorVM second)
        {
            if (first == null && second == null)
                return true;
            if (first == null || second == null)
                return false;
            Object fo = first.originator;
            Object so = second.originator;
            if (fo is BaseDB && so is BaseDB && (fo.GetType() == so.GetType()))  // also from same table though...
                return (fo as BaseDB).id == (so as BaseDB).id;
            else
                return Object.Equals(fo, so);
        }
        public override bool Equals(object obj)
        {
            return OriginatorEquals(obj as OriginatorVM, this);
        }
        public override int GetHashCode()
        {
            return originator is BaseDB ? (originator as BaseDB).id.GetHashCode() : originator.GetHashCode();
        }
        // Dear views, do not modify this object, or I will kill you.
        public Object sender;
        public Object originator;
    }
    public class KVPList<T1, T2> : List<KeyValuePair<T1, T2>>
    {
        public KVPList() { }
        public KVPList(IEnumerable<KeyValuePair<T1, T2>> itms)
        {
            AddRange(itms);
        }
        public void Add(T1 a, T2 b)
        {
            Add(new KeyValuePair<T1, T2>(a, b));
        }
    }
    public class ItemDescriptionVM
    {
        public String name { get; private set; }
        public String description { get; private set; }
        public String category { get; private set; }
        public ItemDescriptionVM(String name, String description, String category)
        {
            this.name = name;
            this.description = description;
            this.category = category;
        }
        public override string ToString()
        {
            return String.Join(" - ", name, category, description);
        }
    }
    public class TrackerDetailsVM : ItemDescriptionVM
    {
        public Guid uid { get; private set; }
        public TrackerDetailsVM(Guid uid, String name, String description, String category)
            : base(name, description, category)
        {
            this.uid = uid;
        }
    }
    public class TrackerInstanceVM : OriginatorVM
    {
        public bool tracked { get; private set; }
        public DateTime started { get; private set; }
        public String name { get; private set; }
        public String desc { get; private set; }
        public KVPList<string, double> displayAmounts { get; private set; }
        public TrackerDialect dialect { get; private set; }
        public TrackerInstanceVM(TrackerDialect td, bool tracked, DateTime s, String n, String d, KVPList<string, double> t)
        {
            this.tracked = tracked;
            this.dialect = td;
            started = s;
            name = n; desc = d;
            displayAmounts = t;
        }
    }
    public class EntryLineVM : OriginatorVM
    {
        public DateTime start { get; private set; }
        public TimeSpan duration { get; private set; }
        public String name { get; private set; }
        public String desc { get; private set; }
        public KVPList<string, double> displayAmounts { get; private set; }

        public EntryLineVM(DateTime w, TimeSpan l, String n, String d, KVPList<string, double> t)
        {
            duration = l;
            start = w; name = n; desc = d;
            displayAmounts = t;
        }
    }

    // The default behaviour is something like this:
    // balance value if both not null.  if one is null, treat as a simple target. 
    public class TrackingElementVM
    {
        public String name;
        public double value;
    }
    public class TrackingInfoVM
    {
        public String inValuesName;
        public TrackingElementVM[] inValues;
        public String outValuesName;
        public TrackingElementVM[] outValues;
        public String targetValueName;
        public double targetValue;
    }
    public class InfoLineVM : OriginatorVM
    {
        public String name { get; set; }
        public KVPList<string, double> displayAmounts { get; set; }

    }

    #endregion

    #region our attributes 
    [AttributeUsage(AttributeTargets.Class)]
    public class TableIdentifierAttribute : Attribute
    {
        public readonly int id;
        public TableIdentifierAttribute(int id_for_fr)
        {
            id = id_for_fr;
        }
    }
    #endregion

    #region mirrored attributes 
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class AutoIncrementAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAccessorAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class IndexedAttribute : Attribute
    {
        // no options...
    }
    #endregion

    #region BASE_MODELS
    public abstract class BaseDB
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }

        [ColumnAccessor, Ignore] // allow on all for simples.
        public Object this[String key] { get { return AdHoc[key]; } set { AdHoc[key] = value; } }
        readonly IDictionary<String, Object> AdHoc = new Dictionary<String, Object>();
    }
    public abstract class BaseEntry : BaseDB
    {
        // keys
        public int trackerinstanceid { get; set; }
        [Indexed] public int? infoinstanceid { get; set; }
        public bool insyncwithinfo { get; set; }

        // entry data
        public String entryName { get; set; }
        [Indexed] public DateTime entryWhen { get; set; }

        // repetition info (starts at when)
        public RecurranceType repeatType { get; set; }
        public byte[] repeatData { get; set; }
        public DateTime? repeatStart { get; set; } // repeated in data
        public DateTime? repeatEnd { get; set; }// repeated in data

        // Helper for cloning - it's a flyweight also due to memberwuse clone reference copying the byte[].
        public BaseEntry FlyweightCloneWithDate(DateTime dt)
        {
            var ret = MemberwiseClone() as BaseEntry;
            ret.entryWhen = dt;
            return ret;
        }
    }

    [Flags] // this deontes a class used from libRTP.
    public enum RecurranceType { None = 0, RecurrsOnPattern = 1, RecurrsEveryPattern = 2 }

    // when we're doing a diet here, created by diet class
    public class TrackerInstance : BaseDB
    {
        public bool tracked { get; set; }
        public string name { get; set; }
        public DateTime startpoint { get; set; }
    }

    public class BaseInfo : BaseDB
    {
        public String name { get; set; }
    }


    #endregion
    public interface IValueRequestBuilder
    {
        // get generic set of values on a page thing
        IInputResponse<bool> GetValues(IEnumerable<GetValuesPage> requestPages);

        // VRO Factory Method
        IValueRequestFactory requestFactory { get; }
    }
    public class Barcode
    {
        public long value; // I think this works?
    }
    public enum InfoManageType { In, Out };
    // dont forget this is client facing
    public interface IValueRequestFactory
    {
        IValueRequest<String> StringRequestor(String name);
        IValueRequest<InfoLineVM> InfoLineVMRequestor(String name, InfoManageType imt);
        IValueRequest<DateTime> DateTimeRequestor(String name);
        IValueRequest<DateTime> DateRequestor(String name);
        IValueRequest<DateTime?> nDateRequestor(String name);
        IValueRequest<TimeSpan> TimeSpanRequestor(String name);
        IValueRequest<double> DoubleRequestor(String name);
        IValueRequest<int> IntRequestor(String name);
        IValueRequest<bool> BoolRequestor(String name);
        IValueRequest<EventArgs> ActionRequestor(String name);
        IValueRequest<Barcode> BarcodeRequestor(String name);
        IValueRequest<OptionGroupValue> OptionGroupRequestor(String name);
        IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor(String name);
        IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor(String name);
        IValueRequest<MultiRequestOptionValue> IValueRequestOptionGroupRequestor(String name);
        IValueRequest<TabularDataRequestValue> GenerateTableRequest();
    }
    #region value types for valuerequests

    public class MultiRequestOptionValue
    {
        public Object HiddenRequest;
        public readonly IEnumerable IValueRequestOptions;
        public int SelectedRequest { get; set; }
        public MultiRequestOptionValue(IEnumerable IValueRequestOptions, int InitiallySelectedRequest, Object HiddenRequest = null)
        {
            this.IValueRequestOptions = IValueRequestOptions;
            SelectedRequest = InitiallySelectedRequest;
            this.HiddenRequest = HiddenRequest;
        }
    }

    public class TabularDataRequestValue
    {
        public String[] Headers { get; private set; }
        public ObservableCollection<Object[]> Items { get; private set; }
        public TabularDataRequestValue(String[] headers)
        {
            Headers = headers;
            Items = new ObservableCollection<Object[]>();
        }
    }

    public class RecurrsEveryPatternValue : INotifyPropertyChanged
    {
        private DateTime patternFixed;
        public DateTime PatternFixed
        {
            get { return patternFixed; }
            set { ChangeProperty(() => patternFixed = value); }
        }
        public RecurrSpan PatternType;
        public int PatternFrequency;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public RecurrsEveryPatternValue(DateTime date, RecurrSpan pt, int freq)
        {
            PatternFixed = date;
            PatternType = pt;
            PatternFrequency = freq;
        }
        public RecurrsEveryPatternValue() : this(DateTime.Now, RecurrSpan.Day, 1)
        {
        }
        

        void ChangeProperty(Action change, [CallerMemberName]String prop = null)
        {
            change();
            PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        
    }
    public class RecurrsOnPatternValue
    {
        public RecurrSpan PatternType;
        public int[] PatternValues;
        public RecurrsOnPatternValue(RecurrSpan pat, int[] vals)
        {
            PatternType = pat;
            PatternValues = vals;
        }
        public RecurrsOnPatternValue() : this(RecurrSpan.Day | RecurrSpan.Month, new[] { 1 })
        {
        }
    }
    public class OptionGroupValue
    {
        public readonly IReadOnlyList<String> OptionNames;
        int selectedOption;
        public int SelectedOption
        {
            get
            {
                return selectedOption;
            }
            set
            {
                selectedOption = value;
            }
        }
        public OptionGroupValue(IEnumerable<String> options)
        {
            SelectedOption = 0;
            OptionNames = new List<String>(options);
        }        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < OptionNames.Count; i++)
            {
                if (i == SelectedOption) sb.Append("[");
                sb.Append(OptionNames[i]);
                if (i == SelectedOption) sb.Append("]");
                if (i != OptionNames.Count - 1) sb.Append(" | ");
            }
            return sb.ToString();
        }
    }
    public interface IValueRequest<V>
    {
        Object request { get; }  // used by view to encapsulate viewbuilding lookups
        V value { get; set; } // set by view when done, and set by view to indicate an initial value.
        event Action ValueChanged; // so model domain can change the flags
        void ClearListeners();
        bool enabled { get; set; } // so the model domain can communicate what fields should be in action (for combining quick and calculate entries)
        bool valid { get; set; } // if we want to check the value set is ok
        bool read_only { get; set; } // if we want to check the value set is ok
    }
    #endregion
}