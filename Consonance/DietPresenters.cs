using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;
using SQLite;

namespace Consonance
{
	public class OriginatorVM {
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
				return Object.Equals (fo, so);
		}
		// Dear views, do not modify this object, or I will kill you.
		public Object sender;
		public Object originator;
	}
	public class KVPList<T1,T2> : List<KeyValuePair<T1,T2>>
	{
		public void Add(T1 a, T2 b) 
		{
			Add (new KeyValuePair<T1, T2> (a, b));
		}
	}
	public class TrackerInstanceVM : OriginatorVM
	{
		public readonly DateTime start;
		public readonly bool hasended;
		public readonly DateTime end;
		public readonly String name;
		public readonly String desc;
		public readonly KVPList<string,double> displayAmounts;
		public readonly TrackerDialect dialect;

		public TrackerInstanceVM(TrackerDialect td, DateTime s, bool he, DateTime e, String n, String d, KVPList<string,double>  t)
		{
			this.dialect = td;
			start=s; end = e; hasended = he;
			name=n; desc=d;
			displayAmounts = t;
		}

		// Tracked property
		Action<bool> _trackChanged = delegate { };
		public Action<bool> trackChanged {set{ _trackChanged = value; }}
		bool _tracked = true;
		public bool tracked { get { return _tracked; } set { _tracked = value; _trackChanged (value); } }
	}
	public class EntryLineVM : OriginatorVM
	{
		public readonly DateTime start;
		public readonly TimeSpan duration;
		public readonly String name;
		public readonly String desc;
		public readonly KVPList<string,double> displayAmounts;

		public EntryLineVM(DateTime w, TimeSpan l, String n, String d, KVPList<string,double>  t)
		{
			duration = l;
			start=w; name=n; desc=d;
			displayAmounts = t;
		}
	}

	public class TrackerTracksVM
	{
		public TrackerInstanceVM instance;
		public String modelName { get { return (instance.sender as IAbstractedTracker).dialect.ModelName; } }
		public String instanceName { get { return instance.name; } }
		public IEnumerable<TrackingInfoVM> tracks = new TrackingInfoVM[0];
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
		public String valueName;
		public TrackingElementVM[] inValues;
		public TrackingElementVM[] outValues;
		public double targetValue;
	}
	public class InfoLineVM : OriginatorVM
	{
		public String name;
	}

	public interface ITrackerPresenter<DietInstType, EatType, EatInfoType, BurnType, BurnInfoType>
		where DietInstType : TrackerInstance, new()
		where EatType : BaseEntry, new()
		where EatInfoType : BaseInfo, new()
		where BurnType : BaseEntry, new()
		where BurnInfoType : BaseInfo, new()
	{
		TrackerDialect dialect { get; }

		EntryLineVM GetRepresentation (EatType entry, EatInfoType entryInfo);
		EntryLineVM GetRepresentation (BurnType entry, BurnInfoType entryInfo);

		InfoLineVM GetRepresentation (EatInfoType info);
		InfoLineVM GetRepresentation (BurnInfoType info);

		TrackerInstanceVM GetRepresentation (DietInstType entry);

		// Deals with goal tracking
		IEnumerable<TrackingInfoVM> DetermineInTrackingForRange(DietInstType di, IEnumerable<EatType> eats, IEnumerable<BurnType> burns, DateTime startBound,  DateTime endBound);
		IEnumerable<TrackingInfoVM> DetermineOutTrackingForRange(DietInstType di, IEnumerable<EatType> eats, IEnumerable<BurnType> burns, DateTime startBound,  DateTime endBound);
	}

	interface IAbstractedTracker
	{
		TrackerDialect dialect  { get; }
		IEnumerable<TrackerInstanceVM> Instances();
		IEnumerable<EntryLineVM>  InEntries (TrackerInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<EntryLineVM>  OutEntries(TrackerInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<TrackingInfoVM> GetInTracking (TrackerInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<TrackingInfoVM> GetOutTracking (TrackerInstanceVM instance, DateTime start, DateTime end);
		void StartNewTracker();
		void RemoveTracker (TrackerInstanceVM dvm);
		void EditTracker (TrackerInstanceVM dvm);
		IEnumerable<InfoLineVM> InInfos (bool onlycomplete);
		IEnumerable<InfoLineVM> OutInfos (bool onlycomplete);
		event DietVMChangeEventHandler ViewModelsChanged;
	}
	interface INotSoAbstractedDiet<IRO>
	{
		// entry ones
		void AddIn (TrackerInstanceVM diet, IValueRequestBuilder<IRO> bld);
		void RemoveIn (EntryLineVM evm);
		void EditIn (EntryLineVM evm, IValueRequestBuilder<IRO> bld);
		void AddInInfo (IValueRequestBuilder<IRO> bld);
		void RemoveInInfo (InfoLineVM ivm);
		void EditInInfo (InfoLineVM ivm, IValueRequestBuilder<IRO> bld);
		void AddOut (TrackerInstanceVM diet, IValueRequestBuilder<IRO> bld);
		void RemoveOut (EntryLineVM evm);
		void EditOut (EntryLineVM evm, IValueRequestBuilder<IRO> bld);
		void AddOutInfo (IValueRequestBuilder<IRO> bld);
		void RemoveOutInfo (InfoLineVM ivm);
		void EditOutInfo (InfoLineVM ivm, IValueRequestBuilder<IRO> bld);
	}
	enum DietVMChangeType { None, Instances, EatEntries, BurnEntries, EatInfos, BurnInfos };
	class DietVMChangeEventArgs
	{
		public DietVMChangeType changeType;
	}
	delegate void DietVMChangeEventHandler(IAbstractedTracker sender, DietVMChangeEventArgs args);
	delegate TrackerInstanceVM DVMPuller();
	/// <summary>
	/// This contains all the code which the app would want to do to get viewmodels out of
	/// implimentations, and retains the generic definitions therefore.
	/// But it does stuff that the IDietPresenter shouldnt have to impliment since it's specific to the 
	/// app logic, not problem logic.
	/// 
	/// As such, it should obey a non-generic contract that the AppPresenter can easily consume and
	/// query for data.
	/// </summary>
	class TrackerPresentationAbstractionHandler <IRO, DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> : IAbstractedTracker, INotSoAbstractedDiet<IRO>
		where DietInstType : TrackerInstance, new()
		where EatType : BaseEntry, new()
		where EatInfoType : BaseInfo, new()
		where BurnType : BaseEntry, new()
		where BurnInfoType : BaseInfo, new()
	{
		public TrackerDialect dialect {get;private set;}
		readonly IValueRequestBuilder<IRO> instanceBuilder;
		readonly IUserInput getInput;
		readonly ITrackerPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter;
		readonly TrackerModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> modelHandler;
		readonly MyConn conn;
		public TrackerPresentationAbstractionHandler(
			IValueRequestBuilder<IRO> instanceBuilder,
			IUserInput getInput,
			MyConn conn,
			ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model,
			ITrackerPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter
		)
		{
			// store objects
			this.dialect=presenter.dialect;
			this.instanceBuilder = instanceBuilder;
			this.getInput = getInput;
			this.presenter = presenter;
			this.modelHandler = new TrackerModelAccessLayer<DietInstType, EatType, EatInfoType, BurnType, BurnInfoType>(conn, model);
			this.conn = conn;
			conn.MyTableChanged += HandleMyTableChanged;
		}

		void HandleMyTableChanged (object sender, NotifyTableChangedEventArgs e)
		{
			DietVMChangeType changeType = DietVMChangeType.None;
			if (e.Table.MappedType.Equals (typeof(DietInstType)))
				changeType = DietVMChangeType.Instances;
			if (e.Table.MappedType.Equals (typeof(EatType)))
				changeType = DietVMChangeType.EatEntries;
			if (e.Table.MappedType.Equals (typeof(BurnType)))
				changeType = DietVMChangeType.BurnEntries;
			if (e.Table.MappedType.Equals (typeof(EatInfoType)))
				changeType = DietVMChangeType.EatInfos;
			if (e.Table.MappedType.Equals (typeof(BurnInfoType)))
				changeType = DietVMChangeType.BurnInfos;
			if (changeType != DietVMChangeType.None)
				ViewModelsChanged (this, new DietVMChangeEventArgs () { changeType = changeType });
		}

		public event DietVMChangeEventHandler ViewModelsChanged = delegate { };
		public IEnumerable<TrackerInstanceVM> Instances()
		{
			foreach (var dt in modelHandler.GetTrackers())
			{
				var rep = presenter.GetRepresentation (dt);
				rep.sender = this;
				rep.originator = dt;
				yield return rep;
			}
		}
		public IEnumerable<EntryLineVM> InEntries(TrackerInstanceVM instance, DateTime start, DateTime end)
		{
			var eatModels = modelHandler.inhandler.Get (instance.originator as DietInstType, start, end);
			return ConvertMtoVM<EntryLineVM, EatType, EatInfoType> (
				eatModels, 
				(e,i) => {
					var vm= presenter.GetRepresentation(e,i);
					vm.originator = e;
					return vm;
				}, 
				ee => GetInfo<EatInfoType>(ee.infoinstanceid)
			);
		}
		public IEnumerable<TrackingInfoVM> GetInTracking (TrackerInstanceVM instance, DateTime start, DateTime end)
		{
			var eatModels = modelHandler.inhandler.Get (instance.originator as DietInstType, start, end);
			var burnModels = modelHandler.outhandler.Get (instance.originator as DietInstType, start, end);
			return presenter.DetermineInTrackingForRange (instance.originator as DietInstType, eatModels, burnModels, start, end);
		}
		public IEnumerable<EntryLineVM> OutEntries(TrackerInstanceVM instance, DateTime start, DateTime end)
		{
			var burnModels = modelHandler.outhandler.Get (instance.originator as DietInstType, start, end);
			return ConvertMtoVM<EntryLineVM, BurnType, BurnInfoType> (
				burnModels, 
				(e,i) => {
					var vm= presenter.GetRepresentation(e,i);
					vm.originator = e;
					return vm;
				}, 
				ee => GetInfo<BurnInfoType>(ee.infoinstanceid)
			);
		}
		public IEnumerable<TrackingInfoVM> GetOutTracking (TrackerInstanceVM instance, DateTime start, DateTime end)
		{
			var eatModels = modelHandler.inhandler.Get (instance.originator as DietInstType, start, end);
			var burnModels = modelHandler.outhandler.Get (instance.originator as DietInstType, start, end);
			return presenter.DetermineOutTrackingForRange (instance.originator as DietInstType, eatModels, burnModels, start, end);
		}
		T GetInfo<T>(int? id) where T : BaseInfo, new()
		{
			if (!id.HasValue) return null;
			return conn.Table<T> ().Where (e => e.id == id.Value).First();
		}
		public IEnumerable<InfoLineVM> InInfos(bool complete)
		{
			// Pull models, generate viewmodels - this can be async wrapped in a new Ilist class
			var fis = conn.Table<EatInfoType> ();
			if(complete) fis=fis.Where (modelHandler.model.increator.IsInfoComplete);
			foreach (var m in fis)
			{
				var vm = presenter.GetRepresentation (m);
				vm.originator = m;
				yield return vm;
			}
		}
		public IEnumerable<InfoLineVM> OutInfos(bool complete)
		{
			// Pull models, generate viewmodels - this can be async wrapped in a new Ilist class
			var fis = conn.Table<BurnInfoType> ();
			if (complete) fis = fis.Where (modelHandler.model.outcreator.IsInfoComplete);
			foreach (var m in fis)
			{
				var vm = presenter.GetRepresentation (m);
				vm.originator = m;
				yield return vm;
			}
		}
		IEnumerable<O> ConvertMtoVM<O,I1,I2>(IEnumerable<I1> input, Func<I1,I2,O> convert, Func<I1,I2> findSecondInput)
		{
			foreach (I1 i in input)
				yield return convert (i,findSecondInput(i));
		}
		IEnumerable<O> ConvertMtoVM<O,I>(IEnumerable<I> input, Func<I,O> convert)
		{
			foreach (I i in input)
				yield return convert (i);
		}
		public void StartNewTracker()
		{
			PageIt (
				new List<TrackerWizardPage<IRO>> (modelHandler.model.CreationPages<IRO> (instanceBuilder.requestFactory)),
				() => modelHandler.StartNewTracker()
			);
		}
		public void EditTracker (TrackerInstanceVM dvm)
		{
			PageIt (
				new List<TrackerWizardPage<IRO>> (modelHandler.model.EditPages<IRO> (dvm.originator as DietInstType, instanceBuilder.requestFactory)),
				() => modelHandler.EditTracker (dvm.originator as DietInstType)
			);
		}
		void PageIt(List<TrackerWizardPage<IRO>> pages, Action complete, int page = 0)
		{
			instanceBuilder.GetValues(pages[page].title, pages[page].valuerequests, b => {
				if(++page < pages.Count) PageIt(pages, complete, page);
				else if(b) complete();
			}, page, pages.Count);
		}
		public void RemoveTracker (TrackerInstanceVM dvm)
		{
			var diet = dvm.originator as DietInstType;
			int ct = 0;
			if ((ct = modelHandler.outhandler.Count (diet) + modelHandler.inhandler.Count (diet)) > 0)
				getInput.WarnConfirm (
					"That instance still has " + ct + " entries, they will be removed if you continue.",
					() => modelHandler.RemoveTracker (diet)
				);
			else modelHandler.RemoveTracker (diet);
		}

		public void AddIn(TrackerInstanceVM to, IValueRequestBuilder<IRO> bld)
		{
			Full<EatType,EatInfoType> (to.originator as DietInstType, modelHandler.model.increator, modelHandler.inhandler, presenter.GetRepresentation, bld);
		}
		public void AddOut(TrackerInstanceVM to, IValueRequestBuilder<IRO> bld)
		{
			Full<BurnType,BurnInfoType> (to.originator as DietInstType, modelHandler.model.outcreator, modelHandler.outhandler, presenter.GetRepresentation, bld);
		}



		void Full<T,I>(DietInstType diet, IEntryCreation<T,I> creator, 
			EntryHandler<DietInstType,T,I> handler, 
			Func<I,InfoLineVM> rep, IValueRequestBuilder<IRO> getValues, T editing = null) 
				where T : BaseEntry, new()
				where I : BaseInfo, new()
		{
			// reset to start
			creator.ResetRequests();

			// get a request object for infos
			var infoRequest = getValues.requestFactory.InfoLineVMRequestor ("Select " + handler.infoName);

			// triggers code in factory
			var infos = conn.Table<I>();
			var fis = new List<InfoLineVM>();
			var isv = new InfoSelectValue () { choices = fis, selected = -1 };
			if (editing != null && editing.infoinstanceid.HasValue) {
				foreach(var fi in infos)
				{
					if (fi.id == editing.infoinstanceid.Value)
						isv.selected = fis.Count;
					fis.Add (rep(fi));
				}
			} else
				fis.AddRange (ConvertMtoVM<InfoLineVM,I>(infos, rep));
			infoRequest.value = isv;

			// Set up for editing
			int selectedInfo = -1;
			Func<IList<IRO>> flds = () => {
				if (selectedInfo < 0)
					return editing == null ?
						creator.CreationFields (getValues.requestFactory) :
						creator.EditFields (editing, getValues.requestFactory);						
				else 
					return editing == null ?
						creator.CalculationFields (getValues.requestFactory, fis[selectedInfo].originator as I) :
						creator.EditFields (editing, getValues.requestFactory, fis[selectedInfo].originator as I);
			};
			Action editit = () => {
				if (selectedInfo < 0) {
					if (editing == null)
						handler.Add (diet);
					else
						handler.Edit (editing, diet);						
				} else {
					if (editing == null)
						handler.Add (diet, fis[selectedInfo].originator as I);
					else
						handler.Edit (editing, diet, fis[selectedInfo].originator as I);
				}
			};

			// binfy
			BindingList<IRO> requests = new BindingList<IRO> ();
			requests.Add (infoRequest.request);

			Action checkFields = () => {
				selectedInfo = infoRequest.value ==  null ? -1 : infoRequest.value.selected;
				BindingList<IRO> nrq = new BindingList<IRO>(flds());
				nrq.Insert (0, infoRequest.request);
				CycleRequests(requests, nrq);
			};
			checkFields ();
			infoRequest.changed += checkFields;

			getValues.GetValues (handler.entryName, requests, c => {
				if (c) editit ();
				infoRequest.changed -= checkFields;
			}, 0, 1);
		}

		void CycleRequests(BindingList<IRO> exist, BindingList<IRO> want)
		{
			//remove gone
			for (int i = 0; i < exist.Count; i++)
				if (!want.Contains (exist [i]))
					exist.RemoveAt (i);
			//add new
			for (int i = 0; i < want.Count; i++)
				if (!exist.Contains (want [i]))
					exist.Insert (i, want [i]);
			//should respect ordering - doing all that so we dont remove ones that didnt change
		}

		public void RemoveIn (EntryLineVM toRemove)
		{
			modelHandler.inhandler.Remove (toRemove.originator as EatType);
		}
		public void RemoveOut (EntryLineVM toRemove)
		{
			modelHandler.outhandler.Remove (toRemove.originator as BurnType);
		}
			
		DietInstType getit(BaseEntry be)
		{
			var dis = conn.Table<DietInstType> ().Where ( inf => inf.id == be.dietinstanceid);
			return dis.Count() == 0 ? null : dis.First();
		}

		public void EditIn (EntryLineVM ed,IValueRequestBuilder<IRO> bld) {
			var eat = ed.originator as EatType;
			Full<EatType,EatInfoType> (getit(eat), modelHandler.model.increator, modelHandler.inhandler, presenter.GetRepresentation,bld,eat);
		}
		public void EditOut (EntryLineVM ed,IValueRequestBuilder<IRO> bld) {
			var burn = ed.originator as BurnType;
			Full<BurnType,BurnInfoType> (getit(burn), modelHandler.model.outcreator, modelHandler.outhandler, presenter.GetRepresentation, bld,burn);
		}

		public void AddInInfo(IValueRequestBuilder<IRO> bld)
		{
			DoInfo<EatInfoType> ("Create a Food", modelHandler.model.increator, modelHandler.inhandler, bld);
		}
		public void EditInInfo(InfoLineVM ivm, IValueRequestBuilder<IRO> bld)
		{
			DoInfo<EatInfoType> ("Edit Food", modelHandler.model.increator, modelHandler.inhandler, bld, ivm.originator as EatInfoType);
		}
		public void RemoveInInfo(InfoLineVM ivm)
		{
			modelHandler.inhandler.Remove (ivm.originator as EatInfoType);
		}

		void DoInfo<I>(String title, IInfoCreation<I> creator, IInfoHandler<I> handler, IValueRequestBuilder<IRO> builder, I toEdit = null)  where I : BaseInfo, new()
		{
			bool editing = toEdit != null;
			builder.GetValues (
				title,
				creator.InfoFields<IRO>(builder.requestFactory, toEdit),
				success => {
					if(editing) handler.Edit(toEdit);
					else handler.Add();
				},
				0,
				1);
		}

		public void AddOutInfo(IValueRequestBuilder<IRO> bld)
		{
			DoInfo<BurnInfoType> ("Create a Fire", modelHandler.model.outcreator, modelHandler.outhandler, bld);
		}
		public void EditOutInfo(InfoLineVM ivm, IValueRequestBuilder<IRO> bld)
		{
			DoInfo<BurnInfoType> ("Edit Food", modelHandler.model.outcreator, modelHandler.outhandler, bld, ivm.originator as BurnInfoType);
		}
		public void RemoveOutInfo(InfoLineVM ivm)
		{
			modelHandler.outhandler.Remove (ivm.originator as BurnInfoType);
		}
	}
}

