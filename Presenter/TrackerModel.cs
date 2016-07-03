using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Diagnostics;
using LibRTP;
using SQLite.Net.Attributes;
using SQLite.Net;
using LibSharpHelp;

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

	#region DIET_PLAN_INTERFACES
	public interface ITrackModel<D,Te,Tei,Tb,Tbi>
		where D : TrackerInstance
		where Te  : BaseEntry
		where Tei : BaseInfo
		where Tb  : BaseEntry
		where Tbi : BaseInfo
	{
		// creates items
		IEntryCreation<Te,Tei> increator { get; }
		IEntryCreation<Tb,Tbi> outcreator { get; }

		// creator for dietinstance
		IEnumerable<GetValuesPage> CreationPages(IValueRequestFactory factory);
		IEnumerable<GetValuesPage> EditPages(D editing, IValueRequestFactory factory);
		D New();
		void Edit (D toEdit);
	}
	public class TrackerDialect
	{
		public readonly String 
            InputEntryVerb, OutputEntryVerb, 
            InputInfoPlural, OutputInfoPlural,
            InputInfoVerbPast, OutputInfoVerbPast;
        public readonly String TrackerTypeName;
        public TrackerDialect(
            String TrackerTypeName,
            String InputEntryVerb, String OutpuEntryVerb, 
            String InputInfoPlural, String OutputInfoPlural,
            String InputInfoVerbPast, String OutputInfoVerbPast
            )
		{
            this.TrackerTypeName = TrackerTypeName;
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
	// just for generic polymorphis, intermal, not used by clients creating diets. they make idietmodel
	delegate void EntryCallback(BaseEntry entry);

    enum ItemType { Instance, Entry, Info };
    public enum DBChangeType { Insert, Delete, Edit };
	class TrackerModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType>
		where DietInstType : TrackerInstance, new()
		where EatType : BaseEntry, new()
		where EatInfoType : BaseInfo, new()
		where BurnType : BaseEntry, new()
		where BurnInfoType : BaseInfo, new()
	{
        public event Action<DietVMChangeType, DBChangeType, Action> ToChange = delegate { };

        readonly SQLiteConnection conn;
		public readonly ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model;
		public readonly EntryHandler<DietInstType, EatType, EatInfoType> inhandler;
		public readonly EntryHandler<DietInstType, BurnType, BurnInfoType> outhandler;
		public TrackerModelAccessLayer(SQLiteConnection conn, ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model)
		{
			this.conn = conn;
			this.model = model;
			conn.CreateTable<DietInstType> ();
			inhandler = new EntryHandler<DietInstType, EatType, EatInfoType> (conn, model.increator);
            inhandler.ToChange+= (c, t, p) => ToChange(Convert(c, true), t, p);
			outhandler = new EntryHandler<DietInstType, BurnType, BurnInfoType> (conn, model.outcreator);
            outhandler.ToChange += (c, t, p) => ToChange(Convert(c, false), t, p);
        }
        DietVMChangeType Convert(ItemType itp, bool input)
        {
            switch (itp)
            {
                default:
                case ItemType.Instance: return DietVMChangeType.Instances;
                case ItemType.Entry: return input ? DietVMChangeType.EatEntries : DietVMChangeType.BurnEntries;
                case ItemType.Info: return input ? DietVMChangeType.EatInfos: DietVMChangeType.BurnInfos;
            }
        }
	
		public void StartNewTracker()
		{
            ToChange(DietVMChangeType.Instances, DBChangeType.Insert, () =>
            {
                var di = model.New();
                conn.Insert(di as DietInstType);
            });
		}
		public void EditTracker(DietInstType diet)
		{
            ToChange(DietVMChangeType.Instances, DBChangeType.Edit, () =>
            {
                model.Edit(diet);
                conn.Update(diet);
            });
		}
		public void RemoveTracker(DietInstType rem)
		{
            ToChange(DietVMChangeType.EatEntries | DietVMChangeType.BurnEntries | DietVMChangeType.Instances, DBChangeType.Delete, () =>
            {
                conn.Table<EatType>().Delete(et => et.trackerinstanceid == rem.id);
                conn.Table<BurnType>().Delete(et => et.trackerinstanceid == rem.id);
                conn.Delete<DietInstType>(rem.id);
            });
        }
		public IEnumerable<DietInstType> GetTrackers()
		{
			var tab = conn.Table<DietInstType> ();
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
        public event Action<ItemType, DBChangeType, Action> ToChange = delegate { };
		readonly SQLiteConnection conn;
		readonly IEntryCreation<EntryType, EntryInfoType> creator;
		public EntryHandler(SQLiteConnection conn, IEntryCreation<EntryType, EntryInfoType> creator)
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
            });
		}
		public void Remove (params EntryType[] tets)
		{
            ToChange(ItemType.Entry, DBChangeType.Delete, () =>
            {
                // FIXME drop where?
                foreach (var tet in tets)
                    conn.Delete<EntryType>(tet.id);
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
			var normalQuery = conn.Table<EntryType> ().Where 
				(d => d.trackerinstanceid == diet.id && d.entryWhen >= start && d.entryWhen < end && d.repeatType == RecurranceType.None);
			var repeatersQuery = conn.Table<EntryType> ().Where
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
			return conn.Table<EntryType> ().Count ();
		}
		public int Count (D diet)
		{
			return conn.Table<EntryType> ().Where(e=>e.trackerinstanceid==diet.id).Count ();
		}

		#endregion

		#region IInfoHandler implementation

		public void Add ()
		{
            ToChange(ItemType.Info, DBChangeType.Insert, () =>
            {
                var mod = creator.MakeInfo();
                conn.Insert(mod, typeof(EntryInfoType));
            });
		}

		public void Edit (EntryInfoType editing)
		{
            ToChange(ItemType.Info, DBChangeType.Edit, () =>
            {
                creator.MakeInfo(editing);
                conn.Update(editing, typeof(EntryInfoType));
            });
		}

		public void Remove (EntryInfoType removing)
		{
            ToChange(ItemType.Info, DBChangeType.Delete, () =>
            {
                conn.Delete(removing);
            });
		}

		#endregion
	}

	#endregion
}
