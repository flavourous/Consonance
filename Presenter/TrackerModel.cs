using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Diagnostics;
using LibRTP;
using SQLite.Net;
using LibSharpHelp;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using Consonance.Protocol;
using System.Collections.ObjectModel;

namespace Consonance
{
    #region DIET_MODELS_PRESENTER_HANDLER
    public interface IAbstractedDAL
    {
        void DeleteAll(Action after, bool drop = false);
        void CountAll(out int instances, out int entries, out int infos);
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

        public readonly IDAL conn, sconn;
		public readonly ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model;
		public readonly EntryHandler<DietInstType, EatType, EatInfoType> inhandler;
		public readonly EntryHandler<DietInstType, BurnType, BurnInfoType> outhandler;
		public TrackerModelAccessLayer(IDAL conn, IDAL sconn, ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model )
		{
            // Various DB schema routing
            this.conn = model is IModelRouter ? conn.Routed(model as IModelRouter) : conn;
            this.sconn = model is IModelRouter ? sconn.Routed(model as IModelRouter) : sconn;

            this.model = model;
			this.conn.CreateTable<DietInstType> ();
			inhandler = new EntryHandler<DietInstType, EatType, EatInfoType> (this.conn,this.sconn, model.increator);
            inhandler.ToChange += (c, t, p) => ToChange(Convert(c, true), t, p);
			outhandler = new EntryHandler<DietInstType, BurnType, BurnInfoType> (this.conn,this.sconn, model.outcreator);
            outhandler.ToChange += (c, t, p) => ToChange(Convert(c, false), t, p);
        }
        TrackerChangeType Convert(ItemType itp, bool input)
        {
            switch (itp)
            {
                default:
                case ItemType.Instance: return TrackerChangeType.Instances;
                case ItemType.Entry: return input ? TrackerChangeType.InEntries : TrackerChangeType.OutEntries;
                case ItemType.Info: return input ? TrackerChangeType.InInfos: TrackerChangeType.OutInfos;
            }
        }
	
		public void StartNewTracker()
		{
            ToChange(TrackerChangeType.Instances, DBChangeType.Insert, () =>
            {
                var di = model.New();
                conn.Commit(di as DietInstType);
                return null;
            });
		}
		public void EditTracker(DietInstType diet)
		{
            ToChange(TrackerChangeType.Instances, DBChangeType.Edit, () =>
            {
                model.Edit(diet);
                conn.Commit(diet);
                return null;
            });
		}
		public void RemoveTracker(DietInstType rem)
		{
            ToChange(TrackerChangeType.InEntries | TrackerChangeType.OutEntries | TrackerChangeType.Instances, DBChangeType.Delete, () =>
            {
                conn.Delete<EatType>(et => et.trackerinstanceid == rem.id);
                conn.Delete<BurnType>(et => et.trackerinstanceid == rem.id);
                conn.Delete<DietInstType>(et => et.id == rem.id);
                return null;
            });
        }
        public void CountAll(out int instances, out int entries, out int infos)
        {
            instances = conn.Count<DietInstType>();
            entries = conn.Count<BurnType>() + conn.Count<EatType>();
            infos = sconn.Count<EatInfoType>() + sconn.Count<BurnInfoType>();
        }
        void Rem<T>(bool drop) where T : BaseDB
        {
            if (drop) conn.DropTable<T>();
            else conn.Delete<T>();
        }
        public void DeleteAll(Action after, bool drop = false)
        {
            var all = TrackerChangeType.InEntries | TrackerChangeType.InInfos | TrackerChangeType.OutEntries | TrackerChangeType.OutInfos | TrackerChangeType.Instances;
            ToChange(drop ? TrackerChangeType.None : all, DBChangeType.Delete, () =>
             {
                 Rem<EatType>(drop);
                 Rem<EatInfoType>(drop);
                 Rem<BurnType>(drop);
                 Rem<BurnInfoType>(drop);
                 Rem<DietInstType>(drop);
                 return after;
             });
        }
		public IEnumerable<DietInstType> GetTrackers()
		{
			return conn.Get<DietInstType> ();
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
		readonly IDAL conn, shared_conn;
		readonly IEntryCreation<EntryType, EntryInfoType> creator;
		public EntryHandler(IDAL conn, IDAL shared_conn, IEntryCreation<EntryType, EntryInfoType> creator)
		{
			this.conn = conn;
            this.shared_conn = conn;
			this.creator = creator;

			// ensure tables are there.
			conn.CreateTable<EntryType> ();
			this.shared_conn.CreateTable<EntryInfoType> ();
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
                conn.Commit(ent as EntryType);
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
                conn.Commit(ent as EntryType);
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
                ent.insyncwithinfo = true;
                conn.Commit(ent as EntryType);
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
                conn.Commit(ent as EntryType);
                return null;
            });
		}
		public void Remove (params EntryType[] tets)
		{
            ToChange(ItemType.Entry, DBChangeType.Delete, () =>
            {
                // FIXME drop where?
                foreach (var tet in tets)
                    conn.Delete<EntryType>(d=>d.id==tet.id);
                return null;
            });
		}
		delegate bool RecurrGetter(byte[] data, out IRecurr rec);
		Dictionary<RecurranceType,RecurrGetter> patcreators = new Dictionary<RecurranceType, RecurrGetter> {
			{ RecurranceType.RecurrsOnPattern, RecurrsOnPattern.TryFromBinary },
			{ RecurranceType.RecurrsEveryPattern, RecurrsEveryPattern.TryFromBinary },
		};
        DummyFactory df = new DummyFactory();
        void SyncInfo(EntryType e)
        {
            if(e.infoinstanceid.HasValue && !e.insyncwithinfo)
            {
                var iv = shared_conn.Get<EntryInfoType>(d => d.id == e.infoinstanceid.Value);
                if (!iv.Any()) e.infoinstanceid = null;
                else
                {
                    creator.EditFields(e, df, iv.First());
                    creator.Edit(e);
                    e.insyncwithinfo = true;
                    conn.Commit(e); // update it back in - committing is only read-lockde.  it table create/drop thats write-locked
                }
            }
        }
		public IEnumerable<EntryType> Get (D diet, DateTime start, DateTime end)
		{
			// Get the noraml and repeating ones, then repeat the repeating ones
			var normalQuery = conn.Get<EntryType>
				(d => d.trackerinstanceid == diet.id && d.entryWhen >= start && d.entryWhen < end && d.repeatType == RecurranceType.None);
			var repeatersQuery = conn.Get<EntryType>
				(d => d.trackerinstanceid == diet.id && d.repeatType != RecurranceType.None);
            foreach (var ent in normalQuery)
            {
                SyncInfo(ent);
                yield return ent;
            }

			// Repeaters .. do second half of the "query" here.
			foreach (EntryType ent in repeatersQuery) {
                SyncInfo(ent);
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
            return conn.Count<EntryType>(e => e.trackerinstanceid == diet.id);
		}

		#endregion

		#region IInfoHandler implementation

		public void Add ()
		{
            ToChange(ItemType.Info, DBChangeType.Insert, () =>
            {
                var mod = creator.MakeInfo();
                shared_conn.Commit<EntryInfoType>(mod);
                return null;
            });
		}

		public void Edit (EntryInfoType editing)
		{
            ToChange(ItemType.Info, DBChangeType.Edit, () =>
            {
                creator.MakeInfo(editing);
                shared_conn.Commit<EntryInfoType>(editing);
                conn.Update<EntryType, bool>(d => d.insyncwithinfo, false, d => d.infoinstanceid == editing.id);
                return null;
            });
		}

		public void Remove (EntryInfoType removing)
		{
            ToChange(ItemType.Info, DBChangeType.Delete, () =>
            {
                shared_conn.Delete<EntryInfoType>(d => d.id == removing.id);
                conn.Update<EntryType, bool>(d => d.insyncwithinfo, false, d => d.infoinstanceid == removing.id);
                return null;
            });
		}

		#endregion
	}

	#endregion
}
