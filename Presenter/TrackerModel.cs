using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Diagnostics;
using LibRTP;
using SQLite.Net.Attributes;
using SQLite.Net;
using LibSharpHelp;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace Consonance
{
    // \Overview\
    // ``````````
    // We want to support different ways of dieting. This might be a calorie control diet with a daily limit, or a weekly limit,
    // or with different limits on each day of the week etc.  It might also not track calories, and instead track some other index
    // like the fiberousness of the food, and aim for a target on that.
    //
    // Strategy is to have a central repository of food items with full nutrient info.  Other data, specific to other diets would
    // exist on foreign tables.  The prime table would have nullable columns, indicating a lack of data.  Diet models could calculate
    // thier index from the prime table or insist manual assignment.

    #region BASE_MODELS
    public abstract class BaseDB
	{
		[PrimaryKey, AutoIncrement]
		public int id{ get; set; }
	}
    public abstract class BaseEntry : BaseDB
	{
		// keys
		public int trackerinstanceid{ get; set; }
		public int? infoinstanceid{ get; set; }

		// entry data
		public String entryName{ get; set; }
		public DateTime entryWhen { get; set; }

		// repetition info (starts at when)
		public RecurranceType repeatType { get; set; }
		public byte[] repeatData { get;set; }
		public DateTime? repeatStart { get; set; } // repeated in data
		public DateTime? repeatEnd {get;set;}// repeated in data

		// Helper for cloning - it's a flyweight also due to memberwuse clone reference copying the byte[].
		public BaseEntry FlyweightCloneWithDate(DateTime dt)
		{
			var ret = MemberwiseClone () as BaseEntry;
			ret.entryWhen = dt;
			return ret;
		}
	}

	[Flags] // this deontes a class used from libRTP.
	public enum RecurranceType { None = 0, RecurrsOnPattern = 1, RecurrsEveryPattern = 2 }

	// when we're doing a diet here, created by diet class
	public class TrackerInstance : BaseDB
	{
		public bool tracked {get;set;}
		public string name{get;set;}
		public DateTime startpoint{get;set;}
	}

	public class BaseInfo : BaseDB
	{
		public String name{ get; set; }
	}


    #endregion

    // creates stuff from pages - shared.
    public interface ICreateable<T> where T : BaseDB
    {
        // creator for dietinstance
        IEnumerable<GetValuesPage> CreationPages(IValueRequestFactory factory);
        IEnumerable<GetValuesPage> EditPages(T editing, IValueRequestFactory factory);
        T New();
        void Edit(T toEdit);
    }

    #region DIET_PLAN_INTERFACES
    public interface ITrackModel<D,Te,Tei,Tb,Tbi> : ICreateable<D>
		where D : TrackerInstance
		where Te  : BaseEntry
		where Tei : BaseInfo
		where Tb  : BaseEntry
		where Tbi : BaseInfo
	{
		// creates items
		IEntryCreation<Te,Tei> increator { get; }
		IEntryCreation<Tb,Tbi> outcreator { get; }
	}
	public class TrackerDialect
	{
		public readonly String 
            InputEntryVerb, OutputEntryVerb, 
            InputInfoPlural, OutputInfoPlural,
            InputInfoVerbPast, OutputInfoVerbPast;
        public TrackerDialect(
            String InputEntryVerb, String OutpuEntryVerb, 
            String InputInfoPlural, String OutputInfoPlural,
            String InputInfoVerbPast, String OutputInfoVerbPast
            )
		{
			this.InputEntryVerb = InputEntryVerb;
			this.OutputEntryVerb = OutpuEntryVerb;
			this.InputInfoPlural = InputInfoPlural;
			this.OutputInfoPlural = OutputInfoPlural;
            this.InputInfoVerbPast = InputInfoVerbPast;
            this.OutputInfoVerbPast = OutputInfoVerbPast;
		}
	}
	public class GetValuesPage
	{
		public readonly String title;
		BindingList<Object> boundrequests = new BindingList<object>();
		public IList<Object> valuerequests { get { return boundrequests; } }
        public IValueRequest<TabularDataRequestValue> listyRequest { get; private set; }
		public ListChangedEventHandler valuerequestsChanegd = delegate { };
		void Newlist_ListChanged (object sender, ListChangedEventArgs e)
		{
			valuerequestsChanegd (sender, e);
		}
		public GetValuesPage(String title)
		{
			this.title = title;
			boundrequests.ListChanged += Newlist_ListChanged;
		}
		public void SetList(BindingList<Object> newlist)
		{
			boundrequests.ListChanged -= Newlist_ListChanged;
			newlist.ListChanged += Newlist_ListChanged;
			boundrequests = newlist;
			Newlist_ListChanged (boundrequests, new ListChangedEventArgs (ListChangedType.Reset, -1));
		}
        public void SetListyRequest(IValueRequest<TabularDataRequestValue> listyRequest)
        {
            this.listyRequest = listyRequest;
        }
	}
	public interface IEntryCreation<EntryType, InfoType> : IInfoCreation<InfoType> where InfoType : class
	{
		// ok you can clear stored data now
		void ResetRequests();

		// What named fields do I need to fully create an entry (eg eating a banana) - "kcal", "fat"
		BindingList<Object> CreationFields (IValueRequestFactory factory); 
		// Here's those values, give me the entry (ww points on eating a bananna) - broker wont attempt to remember a "item / info".
		EntryType Create ();

		// Ok, I've got info on this food (bananna, per 100g, only kcal info) - I still need "fat" and "grams"
		BindingList<Object> CalculationFields (IValueRequestFactory factory, InfoType info);
		// right, heres the fat too, give me entry (broker will update that bananna info also)
		EntryType Calculate(InfoType info, bool shouldComplete);

		// and again for editing
		BindingList<Object> EditFields (EntryType toEdit, IValueRequestFactory factory); 
		EntryType Edit (EntryType toEdit);
		BindingList<Object> EditFields (EntryType toEdit, IValueRequestFactory factory, InfoType info);
		EntryType Edit(EntryType toEdit, InfoType info, bool shouldComplete);
	}
	public interface IInfoCreation<InfoType> where InfoType : class
	{
		// So what info you need to correctly create an info on an eg food item from scratch? "fat" "kcal" "per grams" please
		BindingList<Object> InfoFields(IValueRequestFactory factory);
		// ok so those objects, put this data in them. im editing, for exmaple.
		void FillRequestData (InfoType item); 
		// ok make me an info please here's that data.
		InfoType MakeInfo (InfoType toEdit=null);
		// ok is this info like complete for your diety? yes. ffs.
		Expression<Func<InfoType,bool>> IsInfoComplete { get; }
	}
	#endregion

	#region DIET_MODELS_PRESENTER_HANDLER
    public interface IAbstractedDAL
    {
        void DeleteAll(Action after);
    }

	// just for generic polymorphis, intermal, not used by clients creating diets. they make idietmodel
	delegate void EntryCallback(BaseEntry entry);

    


    interface ICheckedConn
    {
        void Update<T>(T item) where T : BaseDB;
        IEnumerable<T> All<T>() where T : BaseDB;
        IEnumerable<T> Where<T>(Expression<Func<T,bool>> pred) where T : BaseDB;
        int Count<T>() where T : BaseDB;
        int CountWhere<T>(Expression<Func<T, bool>> pred) where T : BaseDB;
        void CreateTable<T>() where T : BaseDB;
        void Delete<T>(int id) where T : BaseDB;
        void DeleteWhere<T>(Expression<Func<T, bool>> pred) where T : BaseDB;
        void DeleteAll<T>() where T : BaseDB;
        void Insert<T>(T item) where T : BaseDB;
    }

    // The trouble is with converting the predicates to sql, which is difficult to do without
    // direction from the router.  Perhaps the mapping should be just to a dictionary always...
    // If the predicates are on dicts, should be able to generate the queries?
    interface IModelRouter
    {
        bool GetTableRoute<T>(out String tabl, out String[] columns, out Type[] colTypes);
        T MapFromTable<T>(Object[] values);
        Object[] MapToTable<T>(T o);
    }
    class NoModelRouter : IModelRouter
    {
        public bool GetTableRoute<T>(out string tabl, out string[] columns, out Type[] colTypes)
        {
            columns = null; colTypes = null; tabl = null; return false;
        }
        public T MapFromTable<T>(object[] values) { throw new NotImplementedException(); }
        public object[] MapToTable<T>(T o) { throw new NotImplementedException(); }
    }

    class SQliteCheckedConnection : ICheckedConn
    {
        // Check against the custom table mapped type.
        readonly SQLiteConnection conn;
        readonly IModelRouter router;
        public SQliteCheckedConnection(SQLiteConnection conn, IModelRouter router)
        {
            this.conn = conn;
            this.router = router;
        }

        Dictionary<Type, TableMapping> cache = new Dictionary<Type, TableMapping>();
        bool GetTableMap<T>(out TableMapping map)
        {
            var t = typeof(T);
            if (cache.ContainsKey(t))
            {
                map = cache[t];
                return map != null;
            }

            map = null;
            String tn; String[] c; Type[] ct;
            if (!router.GetTableRoute<T>(out tn, out c, out ct))
                return false;

            var holder = t.GetTypeInfo().DeclaredProperties.Where(p => p.GetCustomAttributes().Any(a => a is ColumnAccessorAttribute)).First();

            // make the map
            map = conn.GetMapping<T>().WithMutatedSchema(
                tn,
                Enumerable.Range(0, c.Length).Select(i => new TableMapping.AdHocColumn(c[i], ct[i], holder))
                );

            return true;
        }

        TableQuery<T> GetTQ<T>() where T : class
        {
            TableMapping map;
            return GetTableMap<T>(out map) ? conn.Table<T>(map) : conn.Table<T>();
        }

        public IEnumerable<T> All<T>() where T : BaseDB
        {
            return GetTQ<T>();
        }

        public int Count<T>() where T : BaseDB
        {
            return GetTQ<T>().Count();
        }

        public int CountWhere<T>(Expression<Func<T, bool>> pred) where T : BaseDB
        {
            return GetTQ<T>().Count(pred);
        }

        public void CreateTable<T>() where T : BaseDB
        {
            TableMapping map;
            if (GetTableMap<T>(out map)) conn.CreateTable(map);
            else conn.CreateTable<T>();
        }

        public void Delete<T>(int id) where T : BaseDB
        {
            GetTQ<T>().Delete(d => d.id == id);
        }

        public void DeleteAll<T>() where T : BaseDB
        {
            TableMapping map;
            if (GetTableMap<T>(out map)) conn.DeleteAll(map);
            else conn.DeleteAll<T>();
        }

        public void DeleteWhere<T>(Expression<Func<T, bool>> pred) where T : BaseDB
        {
            GetTQ<T>().Delete(pred);
        }

        public void Insert<T>(T item) where T : BaseDB
        {
            TableMapping map;
            if (GetTableMap<T>(out map)) conn.Insert(item,map);
            else conn.Insert(item);
        }

        public void Update<T>(T item) where T : BaseDB
        {
            TableMapping map;
            if (GetTableMap<T>(out map)) conn.Update(item, map);
            else conn.Update(item);
        }

        public IEnumerable<T> Where<T>(Expression<Func<T, bool>> pred) where T : BaseDB
        {
            return GetTQ<T>().Where(pred);
        }
    }

    enum ItemType { Instance, Entry, Info };
    public enum DBChangeType { Insert, Delete, Edit };
	class TrackerModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> : IAbstractedDAL
		where DietInstType : TrackerInstance, new()
		where EatType : BaseEntry, new()
		where EatInfoType : BaseInfo, new()
		where BurnType : BaseEntry, new()
		where BurnInfoType : BaseInfo, new()
	{
        public event Action<TrackerChangeType, DBChangeType, Func<Action>> ToChange = delegate { };

        readonly ICheckedConn conn;
		public readonly ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model;
		public readonly EntryHandler<DietInstType, EatType, EatInfoType> inhandler;
		public readonly EntryHandler<DietInstType, BurnType, BurnInfoType> outhandler;
		public TrackerModelAccessLayer(SQLiteConnection conn, ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model)
		{
            this.conn = new SQliteCheckedConnection(conn, model as IModelRouter ?? new NoModelRouter());
			this.model = model;
			this.conn.CreateTable<DietInstType> ();
			inhandler = new EntryHandler<DietInstType, EatType, EatInfoType> (this.conn, model.increator);
            inhandler.ToChange+= (c, t, p) => ToChange(Convert(c, true), t, p);
			outhandler = new EntryHandler<DietInstType, BurnType, BurnInfoType> (this.conn, model.outcreator);
            outhandler.ToChange += (c, t, p) => ToChange(Convert(c, false), t, p);
        }
        TrackerChangeType Convert(ItemType itp, bool input)
        {
            switch (itp)
            {
                default:
                case ItemType.Instance: return TrackerChangeType.Instances;
                case ItemType.Entry: return input ? TrackerChangeType.EatEntries : TrackerChangeType.BurnEntries;
                case ItemType.Info: return input ? TrackerChangeType.EatInfos: TrackerChangeType.BurnInfos;
            }
        }
	
		public void StartNewTracker()
		{
            ToChange(TrackerChangeType.Instances, DBChangeType.Insert, () =>
            {
                var di = model.New();
                conn.Insert(di as DietInstType);
                return null;
            });
		}
		public void EditTracker(DietInstType diet)
		{
            ToChange(TrackerChangeType.Instances, DBChangeType.Edit, () =>
            {
                model.Edit(diet);
                conn.Update(diet);
                return null;
            });
		}
		public void RemoveTracker(DietInstType rem)
		{
            ToChange(TrackerChangeType.EatEntries | TrackerChangeType.BurnEntries | TrackerChangeType.Instances, DBChangeType.Delete, () =>
            {
                conn.DeleteWhere<EatType>(et => et.trackerinstanceid == rem.id);
                conn.DeleteWhere<BurnType>(et => et.trackerinstanceid == rem.id);
                conn.Delete<DietInstType>(rem.id);
                return null;
            });
        }
        public void DeleteAll(Action after)
        {
            ToChange(TrackerChangeType.EatEntries | TrackerChangeType.EatInfos | TrackerChangeType.BurnEntries| TrackerChangeType.BurnInfos | TrackerChangeType.Instances, DBChangeType.Delete, () =>
            {
                conn.DeleteAll<EatType>();
                conn.DeleteAll<EatInfoType>();
                conn.DeleteAll<BurnType>();
                conn.DeleteAll<BurnInfoType>();
                conn.DeleteAll<DietInstType>();
                return after;
            });
        }
		public IEnumerable<DietInstType> GetTrackers()
		{
			var tab = conn.All<DietInstType> ();
			return tab;
		}
//		public IEnumerable<DietInstType> GetTrackers(DateTime st, DateTime en)
//		{
//			//  | is i1/i2 \ is st/en		i1>st	i2>en	i1>en	i2>st		WANTED
//			// |    \    \     |		= 	false	true	false	true		true		
//			// \  |    \   |			=	true	true	false	true		true
//			// |    \    |   \ 			=	false	false	false	true		true
//			// \    |  |       \		=	true	false	true	true		true
//			// \  \   |   |				=	true	true	true	true		false
//			// |  |   \   \				=	false	false	false	false		false
//
//			// what works is (i1>st ^ i2 > en) | (i1>en ^ i2>st)
//
//			return conn.Table<DietInstType> ().Where (
//				d => ((!d.hasEnded && (st >= d.started || en >= d.started))
//					|| ((d.started >= st ^ d.ended >= en) || (d.started >= en ^ d.ended >= st))));
//		}
	}


	interface IInfoHandler<EntryInfoType> where EntryInfoType : BaseInfo, new()
	{
		void Add ();
		void Edit(EntryInfoType editing);
		void Remove(EntryInfoType removing);
	}

	class EntryHandler<D, EntryType,EntryInfoType> : IInfoHandler<EntryInfoType>
		where D : TrackerInstance, new()
		where EntryType : BaseEntry, new()
		where EntryInfoType : BaseInfo, new()
	{
        public event Action<ItemType, DBChangeType, Func<Action>> ToChange = delegate { };
		readonly ICheckedConn conn;
		readonly IEntryCreation<EntryType, EntryInfoType> creator;
		public EntryHandler(ICheckedConn conn, IEntryCreation<EntryType, EntryInfoType> creator)
		{
			this.conn = conn;
			this.creator = creator;

			// ensure tables are there.
			conn.CreateTable<EntryType> ();
			conn.CreateTable<EntryInfoType> ();
		}

		#region IHandleDietPlanModels implementation
		// eaties
		bool ShouldComplete()
		{
			return true;
		}
        public void Add(D diet, EntryInfoType info)
        {
            ToChange(ItemType.Entry, DBChangeType.Insert, () =>
            {
                EntryType ent = creator.Calculate(info, ShouldComplete());
                ent.trackerinstanceid = diet.id;
                ent.infoinstanceid = info.id;
                conn.Insert(ent as EntryType);
                return null;
            });
		}
		public void Add (D diet)
		{
            ToChange(ItemType.Entry, DBChangeType.Insert, () =>
            {
                EntryType ent = creator.Create();
                ent.trackerinstanceid = diet.id;
                ent.infoinstanceid = null;
                conn.Insert(ent as EntryType);
                return null;
            });
		}
		public void Edit(EntryType ent, D diet, EntryInfoType info)
		{
            ToChange(ItemType.Entry, DBChangeType.Edit, () =>
            {
                creator.Edit(ent, info, ShouldComplete());
                ent.trackerinstanceid = diet.id;
                ent.infoinstanceid = info.id;
                conn.Update(ent as EntryType);
                return null;
            });
		}
		public void Edit(EntryType ent, D diet)
		{
            ToChange(ItemType.Entry, DBChangeType.Edit, () =>
            {
                creator.Edit(ent);
                ent.trackerinstanceid = diet.id;
                ent.infoinstanceid = null;
                conn.Update(ent as EntryType);
                return null;
            });
		}
		public void Remove (params EntryType[] tets)
		{
            ToChange(ItemType.Entry, DBChangeType.Delete, () =>
            {
                // FIXME drop where?
                foreach (var tet in tets)
                    conn.Delete<EntryType>(tet.id);
                return null;
            });
		}
		delegate bool RecurrGetter(byte[] data, out IRecurr rec);
		Dictionary<RecurranceType,RecurrGetter> patcreators = new Dictionary<RecurranceType, RecurrGetter> {
			{ RecurranceType.RecurrsOnPattern, RecurrsOnPattern.TryFromBinary },
			{ RecurranceType.RecurrsEveryPattern, RecurrsEveryPattern.TryFromBinary },
		};
		public IEnumerable<EntryType> Get (D diet, DateTime start, DateTime end)
		{
			// Get the noraml and repeating ones, then repeat the repeating ones
			var normalQuery = conn.Where<EntryType>
				(d => d.trackerinstanceid == diet.id && d.entryWhen >= start && d.entryWhen < end && d.repeatType == RecurranceType.None);
			var repeatersQuery = conn.Where<EntryType>
				(d => d.trackerinstanceid == diet.id && d.repeatType != RecurranceType.None);
			foreach (var ent in normalQuery) yield return ent;

			// Repeaters .. do second half of the "query" here.
			foreach (EntryType ent in repeatersQuery) {
				DateTime patEnd = ent.repeatEnd ?? DateTime.MaxValue;
				DateTime patStart = ent.repeatStart ?? DateTime.MinValue;
				if ((patStart >= start ^ patEnd >= end) || (patStart >= end ^ patEnd >= start)) {
					IRecurr patcreator = null;
					if(!patcreators [ent.repeatType] (ent.repeatData, out patcreator))
						continue; /* report after loop */
					foreach (var rd in patcreator.GetOccurances(start,end))
						yield return (EntryType)ent.FlyweightCloneWithDate (rd);
				}
			}
		}
		public int Count ()
		{
            return conn.Count<EntryType>();
		}
		public int Count (D diet)
		{
            return conn.CountWhere<EntryType>(e => e.trackerinstanceid == diet.id);
		}

		#endregion

		#region IInfoHandler implementation

		public void Add ()
		{
            ToChange(ItemType.Info, DBChangeType.Insert, () =>
            {
                var mod = creator.MakeInfo();
                conn.Insert<EntryInfoType>(mod);
                return null;
            });
		}

		public void Edit (EntryInfoType editing)
		{
            ToChange(ItemType.Info, DBChangeType.Edit, () =>
            {
                creator.MakeInfo(editing);
                conn.Update<EntryInfoType>(editing);
                return null;
            });
		}

		public void Remove (EntryInfoType removing)
		{
            ToChange(ItemType.Info, DBChangeType.Delete, () =>
            {
                conn.Delete<EntryInfoType>(removing.id);
                return null;
            });
		}

		#endregion
	}

	#endregion
}
