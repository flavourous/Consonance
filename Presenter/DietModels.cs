using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;
using SQLite;
using System.Diagnostics;

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
	public class BaseDB
	{
		[PrimaryKey, AutoIncrement]
		public int id{ get; set; }
	}
	public class BaseEntry : BaseDB, ICloneable
	{
		// keys
		public int trackerinstanceid{ get; set; }
		public int? infoinstanceid{ get; set; }

		// entry data
		public String entryName{ get; set; }
		public DateTime entryWhen { get; set; }

		// repetition info (starts at when)
		public RecurranceType repeatType { get; set; }
		public int repeatData { get; set; }
		public bool repeatForever { get; set; }
		public DateTime repeatEnd { get; set; }

		// clone
		#region ICloneable implementation
		public virtual object Clone () { return MemberwiseClone (); }
		#endregion

		// Helper
		public Object CloneWithDate(DateTime dt)
		{
			var ret = (BaseEntry)Clone ();
			ret.entryWhen = dt;
			return ret;
		}
	}
	[Flags]
	public enum RecurranceType { None = 0, DayOfMonth = 1, EveryNDays = 2 }

	// when we're doing a diet here, created by diet class
	public class TrackerInstance : BaseDB
	{
		public string name{get;set;}
		public DateTime started{get;set;}
		public DateTime ended{get;set;}
		public bool hasEnded {get;set;}
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
		public readonly String InputEntryVerb, OutputEntrytVerb, InputInfoPlural, OutputInfoPlural;
		public TrackerDialect(String InputEntryVerb, String OutpuEntrytVerb, String InputInfoPlural, String OutputInfoPlural)
		{
			this.InputEntryVerb = InputEntryVerb;
			this.OutputEntrytVerb = OutpuEntrytVerb;
			this.InputInfoPlural = InputInfoPlural;
			this.OutputInfoPlural = OutputInfoPlural;
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

	class TrackerModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType>
		where DietInstType : TrackerInstance, new()
		where EatType : BaseEntry, new()
		where EatInfoType : BaseInfo, new()
		where BurnType : BaseEntry, new()
		where BurnInfoType : BaseInfo, new()
	{
		readonly MyConn conn;
		public readonly ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model;
		public readonly EntryHandler<DietInstType, EatType, EatInfoType> inhandler;
		public readonly EntryHandler<DietInstType, BurnType, BurnInfoType> outhandler;
		public TrackerModelAccessLayer(MyConn conn, ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model)
		{
			this.conn = conn;
			this.model = model;
			conn.CreateTable<DietInstType> ();
			inhandler = new EntryHandler<DietInstType, EatType, EatInfoType> (conn, model.increator);
			outhandler = new EntryHandler<DietInstType, BurnType, BurnInfoType> (conn, model.outcreator);
		}
			
	
		public DietInstType StartNewTracker()
		{
			PDebug.WriteWithShortType ("Comitting model for new tracker", GetType ());
			var di = model.New ();
			conn.Insert (di as DietInstType);
			return di;
		}
		public void EditTracker(DietInstType diet)
		{
			// actually call edit promise...
			model.Edit(diet);

			List<BaseEntry> orphans = new List<BaseEntry>();
			orphans.AddRange (inhandler.GetOrphans (diet));
			orphans.AddRange (outhandler.GetOrphans (diet));
			foreach (var e in orphans)
				if (e.entryWhen < diet.started)
					diet.started = e.entryWhen;
			if (diet.hasEnded)
				foreach (var e in orphans)
					if (e.entryWhen > diet.ended)
						diet.ended = e.entryWhen;
			conn.Update (diet, typeof(DietInstType)); 
		}
		public void RemoveTracker(DietInstType rem)
		{
			conn.Delete<EatType>("trackerinstanceid = " + rem.id);
			conn.Delete<BurnType>("trackerinstanceid = " + rem.id);
			conn.Delete<DietInstType> (rem.id);
		}
		public IEnumerable<DietInstType> GetTrackers()
		{
			var tab = conn.Table<DietInstType> ();
			return tab;
		}
		public IEnumerable<DietInstType> GetTrackers(DateTime st, DateTime en)
		{
			//  | is i1/i2 \ is st/en		i1>st	i2>en	i1>en	i2>st		WANTED
			// |    \    \     |		= 	false	true	false	true		true		
			// \  |    \   |			=	true	true	false	true		true
			// |    \    |   \ 			=	false	false	false	true		true
			// \    |  |       \		=	true	false	true	true		true
			// \  \   |   |				=	true	true	true	true		false
			// |  |   \   \				=	false	false	false	false		false

			// what works is (i1>st ^ i2 > en) | (i1>en ^ i2>st)

			return conn.Table<DietInstType> ().Where (
				d => ((!d.hasEnded && (st >= d.started || en >= d.started))
					|| ((d.started >= st ^ d.ended >= en) || (d.started >= en ^ d.ended >= st))));
		}
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
		void CheckOrphans(D diet, EntryType entry) // FIXME SLOW
		{
			bool updateDiet = false;
			if (diet.started > entry.entryWhen) {
				updateDiet = true;
				diet.started = entry.entryWhen;
			}
			if (diet.hasEnded && diet.ended < entry.entryWhen) {
				updateDiet = true;
				diet.ended = entry.entryWhen;
			}
			if (updateDiet)
				conn.Update (diet, typeof(D));
		}
		public void Add (D diet, EntryInfoType info)
		{
			EntryType ent = creator.Calculate (info, ShouldComplete());
			ent.trackerinstanceid = diet.id;
			ent.infoinstanceid = info.id;
			CheckOrphans (diet, ent);
			conn.Insert (ent as EntryType);
		}
		public void Add (D diet)
		{
			EntryType ent = creator.Create ();
			ent.trackerinstanceid = diet.id;
			ent.infoinstanceid = null;
			CheckOrphans (diet, ent);
			conn.Insert (ent as EntryType);
		}
		public void Edit(EntryType ent, D diet, EntryInfoType info)
		{
			creator.Edit(ent, info, ShouldComplete());
			ent.trackerinstanceid = diet.id;
			ent.infoinstanceid = info.id;
			CheckOrphans (diet, ent);
			conn.Update (ent as EntryType);
		}
		public void Edit(EntryType ent, D diet)
		{
			creator.Edit (ent);
			ent.trackerinstanceid = diet.id;
			ent.infoinstanceid = null;
			CheckOrphans (diet, ent);
			conn.Update (ent as EntryType);
		}
		public void Remove (params EntryType[] tets)
		{
			// FIXME drop where?
			foreach(var tet in tets)
				conn.Delete<EntryType> (tet.id);
		}
		public IEnumerable<EntryType> Get (D diet, DateTime start, DateTime end)
		{
			// Get the noraml and repeating ones, then repeat the repeating ones
			var normalQuery = conn.Table<EntryType> ().Where 
				(d => d.trackerinstanceid == diet.id && d.entryWhen >= start && d.entryWhen < end && d.repeatType == RecurranceType.None);
			var repeatersQuery = conn.Table<EntryType> ().Where
				(d => d.trackerinstanceid == diet.id && d.repeatType != RecurranceType.None &&
					d.entryWhen <= end &&	(d.repeatForever || d.repeatEnd >= start)
				);
			foreach (var ent in normalQuery) yield return ent;
			List<EntryType> ents = new List<EntryType>(repeatersQuery);
			foreach (var entl in ents) {
				EntryType ent = entl;
				//  we will return N entries, with changed entryWhens, all with the same id
				DateTime begin = ent.entryWhen; // this is fixed in the switch
				DateTime finish = (!ent.repeatForever && ent.repeatEnd < end) ? ent.repeatEnd: end;
				Func<DateTime, DateTime> inc = null;
				switch (ent.repeatType) {
				case RecurranceType.EveryNDays:
					if (begin < start) // oh ok, we gotta % the first drop into our range.
							begin = start.AddDays (ent.repeatData - (start - begin).TotalDays % ent.repeatData);
					inc = b => b.AddDays (ent.repeatData);
					break;
				case RecurranceType.DayOfMonth:
						// align to next occurance of day after the when
					var buse = start > begin ? start : begin;
					if (buse.Day > ent.repeatData)
						buse = buse.AddMonths (1);
					buse = buse.AddDays (ent.repeatData - buse.Day);
					inc = b => b.AddMonths (1);
					begin = buse;
					break;
				}
				if (inc == null) break; // srsly I saw it happen.
				while (begin < end) {
					yield return (EntryType)ent.CloneWithDate (begin);
					begin = inc (begin);
				}
			}
		}
		public IEnumerable<EntryType> GetOrphans(D trackerInstance)
		{
			return conn.Table<EntryType> ().Where (d => 
				d.trackerinstanceid == trackerInstance.id &&  (
					d.entryWhen < trackerInstance.started || 
					(trackerInstance.hasEnded && d.entryWhen > trackerInstance.ended)
				)
			);
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
			var mod = creator.MakeInfo ();
			conn.Insert (mod, typeof(EntryInfoType));
		}

		public void Edit (EntryInfoType editing)
		{
			creator.MakeInfo (editing);
			conn.Update (editing, typeof(EntryInfoType));
		}

		public void Remove (EntryInfoType removing)
		{
			conn.Delete (removing);
		}

		#endregion
	}

	#endregion
}
