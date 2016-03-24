using System;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;
using SQLite;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Consonance
{
	public class ViewModelBase : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		protected void OnPropertyChanged([CallerMemberName]String pn = null)
		{
			PropertyChanged (this, new PropertyChangedEventArgs (pn));
		}
		#endregion
	}
	public class OriginatorVM : ViewModelBase {
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
	public class TrackerDetailsVM : ViewModelBase
	{
		// viewhakcs
		bool _selected = false;
		public bool selected { get { return _selected; } set { _selected=value; OnPropertyChanged (); } }

        public String name { get; private set; }
        public String description { get; private set; }
        public String category { get; private set; }
		public TrackerDetailsVM(String name, String description, String category)
		{
			this.name = name;
			this.description = description;
			this.category = category;
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
		public TrackerInstanceVM(TrackerDialect td, bool tracked, DateTime s, String n, String d, KVPList<string,double>  t)
		{
			this.tracked = tracked;
			this.dialect = td;
			started=s;
			name=n; desc=d;
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
		public String modelName { get { return (instance.sender as IAbstractedTracker).details.name; } }
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
		public String name { get; set; }
		public KVPList<string, double> displayAmounts { get; set; }

		// viewhakcs
		bool _selected = false;
		public bool selected { get { return _selected; } set { _selected=value; OnPropertyChanged (); } }
	}

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

		InfoLineVM GetRepresentation (EatInfoType info);
		InfoLineVM GetRepresentation (BurnInfoType info);

		EntryLineVM GetRepresentation (EatType entry, EatInfoType entryInfo);
		EntryLineVM GetRepresentation (BurnType entry, BurnInfoType entryInfo);

		TrackerInstanceVM GetRepresentation (DietInstType entry);

		// Deals with goal tracking
		IEnumerable<TrackingInfoVM> DetermineInTrackingForDay(DietInstType di, EntryRetriever<EatType> eats, EntryRetriever<BurnType> burns, DateTime dayStart);
		IEnumerable<TrackingInfoVM> DetermineOutTrackingForDay(DietInstType di, EntryRetriever<EatType> eats, EntryRetriever<BurnType> burns, DateTime dayStart);
	}

	interface IAbstractedTracker
	{
		TrackerDetailsVM details { get; }
		TrackerDialect dialect  { get; }
		IEnumerable<TrackerInstanceVM> Instances();
		IEnumerable<EntryLineVM>  InEntries (TrackerInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<EntryLineVM>  OutEntries(TrackerInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<TrackingInfoVM> GetInTracking (TrackerInstanceVM instance, DateTime day);
		IEnumerable<TrackingInfoVM> GetOutTracking (TrackerInstanceVM instance, DateTime day);
		Task StartNewTracker();
		void RemoveTracker (TrackerInstanceVM dvm);
		Task EditTracker (TrackerInstanceVM dvm);
		IEnumerable<InfoLineVM> InInfos (bool onlycomplete);
		IEnumerable<InfoLineVM> OutInfos (bool onlycomplete);
		IFindList<InfoLineVM> InFinder {get;}
		IFindList<InfoLineVM> OutFinder {get;}
		event DietVMChangeEventHandler ViewModelsChanged;
		// entry ones
		Task AddIn (TrackerInstanceVM diet, IValueRequestBuilder bld);
		void RemoveIn (EntryLineVM evm);
		Task EditIn (EntryLineVM evm, IValueRequestBuilder bld);
		Task AddInInfo (IValueRequestBuilder bld);
		void RemoveInInfo (InfoLineVM ivm);
		Task EditInInfo (InfoLineVM ivm, IValueRequestBuilder bld);
		Task AddOut (TrackerInstanceVM diet, IValueRequestBuilder bld);
		void RemoveOut (EntryLineVM evm);
		Task EditOut (EntryLineVM evm, IValueRequestBuilder bld);
		Task AddOutInfo (IValueRequestBuilder bld);
		void RemoveOutInfo (InfoLineVM ivm);
		Task EditOutInfo (InfoLineVM ivm, IValueRequestBuilder bld);
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
	class TrackerPresentationAbstractionHandler <DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> : IAbstractedTracker
		where DietInstType : TrackerInstance, new()
		where EatType : BaseEntry, new()
		where EatInfoType : BaseInfo, new()
		where BurnType : BaseEntry, new()
		where BurnInfoType : BaseInfo, new()
	{
		public TrackerDetailsVM details { get; private set; }
		public TrackerDialect dialect { get; private set; }
		readonly IValueRequestBuilder instanceBuilder;
		readonly IUserInput getInput;
		readonly ITrackerPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter;
		readonly TrackerModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> modelHandler;
		readonly MyConn conn;
		public TrackerPresentationAbstractionHandler(
			IValueRequestBuilder instanceBuilder,
			IUserInput getInput,
			MyConn conn,
			ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model,
			ITrackerPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter
		)
		{
			// store objects
			this.details = presenter.details;
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
			if (changeType != DietVMChangeType.None) {
				Debug.WriteLine ("DataChange - " + changeType.ToString(), GetType ().ToString ().Replace ("Consonance.", ""));	
				ViewModelsChanged (this, new DietVMChangeEventArgs () { changeType = changeType });
			}
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
		public IEnumerable<TrackingInfoVM> GetInTracking (TrackerInstanceVM instance, DateTime day)
		{
			EntryRetriever<EatType> eatModels = (s,e) => modelHandler.inhandler.Get (instance.originator as DietInstType, s, e);
			EntryRetriever<BurnType> burnModels = (s,e) =>  modelHandler.outhandler.Get (instance.originator as DietInstType, s,e);
			return presenter.DetermineInTrackingForDay (instance.originator as DietInstType, eatModels, burnModels, day);
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
		public IEnumerable<TrackingInfoVM> GetOutTracking (TrackerInstanceVM instance, DateTime day)
		{
			EntryRetriever<EatType> eatModels = (s,e) => modelHandler.inhandler.Get (instance.originator as DietInstType, s, e);
			EntryRetriever<BurnType> burnModels = (s,e) =>  modelHandler.outhandler.Get (instance.originator as DietInstType, s,e);
			return presenter.DetermineOutTrackingForDay (instance.originator as DietInstType, eatModels, burnModels, day);
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

		public IFindList<InfoLineVM> InFinder { get { return InfoFindersManager.GetFinder<EatInfoType> (presenter.GetRepresentation, conn); } }
		public IFindList<InfoLineVM> OutFinder { get { return InfoFindersManager.GetFinder<BurnInfoType> (presenter.GetRepresentation, conn); } }

		IEnumerable<O> ConvertMtoVM<O,I1,I2>(IEnumerable<I1> input, Func<I1,I2,O> convert, Func<I1,I2> findSecondInput)
			where O : OriginatorVM
		{
			foreach (I1 i in input) {
				var vm = convert (i, findSecondInput (i));
				vm.originator = i;
				yield return vm;
			}
		}
		IEnumerable<O> ConvertMtoVM<O,I>(IEnumerable<I> input, Func<I,O> convert)
			where O : OriginatorVM
		{
			foreach (I i in input) {
				var vm = convert (i);
				vm.originator = i;
				yield return vm;
			}
		}
		public Task StartNewTracker()
		{
			var pages = new List<GetValuesPage> (modelHandler.model.CreationPages (instanceBuilder.requestFactory));
			var vt = instanceBuilder.GetValues (pages);
			vt.Completed.ContinueWith(async rt => {
				vt.Pop();
				if (rt.Result) modelHandler.StartNewTracker ();
			});
			return vt.Pushed;
		}
		public Task EditTracker (TrackerInstanceVM dvm)
		{
			var pages = new List<GetValuesPage> (modelHandler.model.EditPages (dvm.originator as DietInstType, instanceBuilder.requestFactory));
			var vt = instanceBuilder.GetValues (pages);
			vt.Completed.ContinueWith(rt => {
				vt.Pop();
				if (rt.Result) modelHandler.EditTracker (dvm.originator as DietInstType);
			});
			return vt.Pushed;
		}
		public void RemoveTracker (TrackerInstanceVM dvm)
		{
			var diet = dvm.originator as DietInstType;
			int ct = 0;
			if ((ct = modelHandler.outhandler.Count (diet) + modelHandler.inhandler.Count (diet)) > 0)
				getInput.WarnConfirm (
					"That instance still has " + ct + " entries, they will be removed if you continue.",
					async () => await PTask.Run(() => modelHandler.RemoveTracker (diet))
				);
			else modelHandler.RemoveTracker (diet);
		}

		public Task AddIn(TrackerInstanceVM to, IValueRequestBuilder bld)
		{
			return Full<EatType,EatInfoType> (to.originator as DietInstType, modelHandler.model.increator, modelHandler.inhandler, true, presenter.GetRepresentation, bld);
		}
		public Task AddOut(TrackerInstanceVM to, IValueRequestBuilder bld)
		{
			return Full<BurnType,BurnInfoType> (to.originator as DietInstType, modelHandler.model.outcreator, modelHandler.outhandler, false, presenter.GetRepresentation, bld);
		}

		Task Full<T,I>(DietInstType diet, IEntryCreation<T,I> creator, 
			EntryHandler<DietInstType,T,I> handler, bool true_if_in,
			Func<I,InfoLineVM> rep, IValueRequestBuilder getValues, T editing = null) 
				where T : BaseEntry, new()
				where I : BaseInfo, new()
		{
			// reset to start
			creator.ResetRequests();

			// get a request object for infos
			String info_plural = true_if_in ? presenter.dialect.InputInfoPlural : presenter.dialect.OutputInfoPlural;
			var infoRequest = getValues.requestFactory.InfoLineVMRequestor (info_plural);
			infoRequest.valid = true; // always true
			Action chooseDelegate = null;
			chooseDelegate = () => PTask.Run (async () => {
				var imt = true_if_in ? InfoManageType.In : InfoManageType.Out;
				using (var hk = new HookedInfoLines (this, imt))
				{
					var res = await getInput.InfoView (InfoCallType.AllowManage | InfoCallType.AllowSelect, imt, hk.lines, infoRequest.value.selected);
					infoRequest.value = new InfoSelectValue() { selected = res }; // like this fires changed
					infoRequest.value.choose += chooseDelegate; // but re hook needed then!
				}
			});
			InfoLineVM sinfo = null;
			if(editing != null && editing.infoinstanceid.HasValue)
			{
				var imod = GetInfo<I> (editing.infoinstanceid);
				sinfo = rep (imod);
				sinfo.originator = imod; // dont forget to set this this time...
			}

			infoRequest.value = new InfoSelectValue () { selected = sinfo }; // has to....
			infoRequest.value.choose += chooseDelegate;
		
			// Set up for editing
			Func<BindingList<Object>> flds = () => {
				var si = infoRequest.value.selected;
				if (si == null)
					return editing == null ?
						creator.CreationFields (getValues.requestFactory) :
						creator.EditFields (editing, getValues.requestFactory);						
				else 
					return editing == null ?
						creator.CalculationFields (getValues.requestFactory, si.originator as I) :
						creator.EditFields (editing, getValues.requestFactory, si.originator as I);
			};
			Action editit = () => {
				var si = infoRequest.value.selected;
				if (si == null) {
					if (editing == null) handler.Add (diet);
					else handler.Edit (editing, diet);						
				} else {
					if (editing == null) handler.Add (diet, si.originator as I);
					else handler.Edit (editing, diet, si.originator as I);
				}
			};

			// binfy
			String entryVerb = true_if_in ? presenter.dialect.InputEntryVerb : presenter.dialect.OutputEntrytVerb;
			var requests = new GetValuesPage (entryVerb);
			requests.valuerequests.Add (infoRequest.request);

			Action checkFields = () => {
				var nrq = flds();
				nrq.Insert (0, infoRequest.request);
				requests.SetList(nrq);
			};
			checkFields ();
			infoRequest.changed += checkFields;

			var gv = getValues.GetValues (new[]{ requests });
			gv.Completed.ContinueWith(result => {
				gv.Pop (); 
				if (result.Result) editit ();
				infoRequest.changed -= checkFields;
			});
			return gv.Pushed;
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
			var dis = conn.Table<DietInstType> ().Where ( inf => inf.id == be.trackerinstanceid);
			return dis.Count() == 0 ? null : dis.First();
		}

		public Task EditIn (EntryLineVM ed,IValueRequestBuilder bld) {
			var eat = ed.originator as EatType;
			return Full<EatType,EatInfoType> (getit(eat), modelHandler.model.increator, modelHandler.inhandler, true, presenter.GetRepresentation,bld,eat);
		}
		public Task EditOut (EntryLineVM ed,IValueRequestBuilder bld) {
			var burn = ed.originator as BurnType;
			return Full<BurnType,BurnInfoType> (getit(burn), modelHandler.model.outcreator, modelHandler.outhandler, false, presenter.GetRepresentation, bld,burn);
		}

		public Task AddInInfo(IValueRequestBuilder bld)
		{
			return DoInfo<EatInfoType> ("Create a Food",InFinder, modelHandler.model.increator, modelHandler.inhandler, bld);
		}
		public Task EditInInfo(InfoLineVM ivm, IValueRequestBuilder bld)
		{
			return DoInfo<EatInfoType> ("Edit Food",InFinder, modelHandler.model.increator, modelHandler.inhandler, bld, ivm.originator as EatInfoType);
		}
		public void RemoveInInfo(InfoLineVM ivm)
		{
			modelHandler.inhandler.Remove (ivm.originator as EatInfoType);
		}

		Task DoInfo<I>(String title,IFindList<InfoLineVM> finder, IInfoCreation<I> creator, IInfoHandler<I> handler, IValueRequestBuilder builder, I toEdit = null)  where I : BaseInfo, new()
		{
			bool editing = toEdit != null;
			var vros = editing ? creator.InfoFields (builder.requestFactory) : new ValueRequestFactory_FinderAdapter<I> (finder, creator, builder.requestFactory, getInput).GetRequestObjects ();
			var gvp = new GetValuesPage (title);
			gvp.SetList (vros);
			if (editing) creator.FillRequestData (toEdit);
			var vr = builder.GetValues (new[]{ gvp });
			vr.Completed.ContinueWith(result => {
				vr.Pop ();
				if (result.Result) {
					if (editing) handler.Edit (toEdit);
					else handler.Add ();
				}
			});
			return vr.Pushed;
		}

		public Task AddOutInfo(IValueRequestBuilder bld)
		{
			return DoInfo<BurnInfoType> ("Create a Fire",OutFinder, modelHandler.model.outcreator, modelHandler.outhandler, bld);
		}
		public Task EditOutInfo(InfoLineVM ivm, IValueRequestBuilder bld)
		{
			return DoInfo<BurnInfoType> ("Edit Food", OutFinder, modelHandler.model.outcreator, modelHandler.outhandler, bld, ivm.originator as BurnInfoType);
		}
		public void RemoveOutInfo(InfoLineVM ivm)
		{
			modelHandler.outhandler.Remove (ivm.originator as BurnInfoType);
		}
	}
}

