using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using scm = System.ComponentModel;
using SQLite.Net;
using LibSharpHelp;
using System.Diagnostics;
using System.Linq;
using Consonance.Protocol;

namespace Consonance
{

    interface IViewModelHandler<T>
    {
        IEnumerable<T> Instances();
        IInputResponse StartNewTracker();
        void RemoveTracker(T dvm, bool warn = true);
        IInputResponse EditTracker(T dvm);
    }

    interface IViewModelObserver<T,S,C> : IViewModelHandler<T>
    {
        event DietVMChangeEventHandler<S,C> ViewModelsToChange;
    }

	interface IAbstractedTracker : IViewModelObserver<TrackerInstanceVM, IAbstractedTracker, TrackerChangeType>
	{
		TrackerDetailsVM details { get; }
		TrackerDialect dialect  { get; }
		IEnumerable<EntryLineVM>  InEntries (TrackerInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<EntryLineVM>  OutEntries(TrackerInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<TrackingInfoVM> GetInTracking (TrackerInstanceVM instance, DateTime day);
		IEnumerable<TrackingInfoVM> GetOutTracking (TrackerInstanceVM instance, DateTime day);
		IEnumerable<InfoLineVM> InInfos (bool onlycomplete);
		IEnumerable<InfoLineVM> OutInfos (bool onlycomplete);
		IFindList<InfoLineVM> InFinder {get;}
		IFindList<InfoLineVM> OutFinder {get;}
        // entry ones
        IInputResponse AddIn (TrackerInstanceVM diet, IValueRequestBuilder bld);
		void RemoveIn (EntryLineVM evm);
        IInputResponse EditIn (EntryLineVM evm, IValueRequestBuilder bld);
        IInputResponse AddInInfo (IValueRequestBuilder bld);
		void RemoveInInfo (InfoLineVM ivm);
        IInputResponse EditInInfo (InfoLineVM ivm, IValueRequestBuilder bld);
        IInputResponse AddOut (TrackerInstanceVM diet, IValueRequestBuilder bld);
		void RemoveOut (EntryLineVM evm);
        IInputResponse EditOut (EntryLineVM evm, IValueRequestBuilder bld);
        IInputResponse AddOutInfo (IValueRequestBuilder bld);
		void RemoveOutInfo (InfoLineVM ivm);
        IInputResponse EditOutInfo (InfoLineVM ivm, IValueRequestBuilder bld);
	}
    [Flags]
	enum TrackerChangeType {
        None =0,
        Inventions = 1<<0,
        Instances =1<<1,
        InEntries =1<<2,
        OutEntries =1<<3,
        InInfos =1<<4,
        OutInfos =1<<5,
        Tracking = 1<<6 /*meta*/ ,
    };
	class DietVMToChangeEventArgs<T>
	{
		public T changeType;
        public Func<Action> toChange;
	}
	delegate void DietVMChangeEventHandler<S,T>(S sender, DietVMToChangeEventArgs<T> args);
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
		public readonly TrackerModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> modelHandler;
		public TrackerPresentationAbstractionHandler(
			IValueRequestBuilder instanceBuilder,
			IUserInput getInput,
			IDAL conn,
			ITrackModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model,
			ITrackerPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter,
            IDAL shared_conn
		)
		{
			// store objects
			this.details = presenter.details;
			this.dialect=presenter.dialect;
			this.instanceBuilder = instanceBuilder;
			this.getInput = getInput;
			this.presenter = presenter;
			this.modelHandler = new TrackerModelAccessLayer<DietInstType, EatType, EatInfoType, BurnType, BurnInfoType>(conn, shared_conn, model);
            modelHandler.ToChange += (t, ct, a) => ViewModelsToChange(this, new DietVMToChangeEventArgs<TrackerChangeType> { changeType = t, toChange = a });
		}

		public event DietVMChangeEventHandler<IAbstractedTracker, TrackerChangeType> ViewModelsToChange = delegate { };
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
			return ConvertMtoVM(
				eatModels, 
				(e,i) => {
                    Debug.WriteLine("Entry " + e.id + ": " + e.entryWhen);
					var vm= presenter.GetRepresentation(e,i);
					vm.originator = e;
					return vm;
				}, 
				ee => GetInfo<EatInfoType>(ee)
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
				ee => GetInfo<BurnInfoType>(ee)
			);
		}
		public IEnumerable<TrackingInfoVM> GetOutTracking (TrackerInstanceVM instance, DateTime day)
		{
			EntryRetriever<EatType> eatModels = (s,e) => modelHandler.inhandler.Get (instance.originator as DietInstType, s, e);
			EntryRetriever<BurnType> burnModels = (s,e) =>  modelHandler.outhandler.Get (instance.originator as DietInstType, s,e);
			return presenter.DetermineOutTrackingForDay (instance.originator as DietInstType, eatModels, burnModels, day);
		}
		T GetInfo<T>(BaseEntry ent) where T : BaseInfo, new()
		{
            T info = null;
            if (ent.infoinstanceid.HasValue)
            {
                var res = modelHandler.conn.Get<T>(e => e.id == ent.infoinstanceid.Value);
                if (res.Count() == 0) ent.infoinstanceid = null;
                else info = res.First();
            }
            return info;
		}
		public IEnumerable<InfoLineVM> InInfos(bool complete)
		{
			// Pull models, generate viewmodels - this can be async wrapped in a new Ilist class
			var fis = modelHandler.conn.Get<EatInfoType> ();
			if(complete) fis = modelHandler.conn.Get(modelHandler.model.increator.IsInfoComplete);
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
			var fis = modelHandler.conn.Get< BurnInfoType> ();
			if (complete) fis = modelHandler.conn.Get(modelHandler.model.outcreator.IsInfoComplete);
			foreach (var m in fis)
			{
				var vm = presenter.GetRepresentation (m);
				vm.originator = m;
				yield return vm;
			}
		}

		public IFindList<InfoLineVM> InFinder { get { return InfoFindersManager.GetFinder<EatInfoType> (presenter.GetRepresentation, modelHandler.conn); } }
		public IFindList<InfoLineVM> OutFinder { get { return InfoFindersManager.GetFinder<BurnInfoType> (presenter.GetRepresentation, modelHandler.conn); } }

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
		public IInputResponse StartNewTracker()
		{
            var pages = new List<GetValuesPage>(modelHandler.model.CreationPages(instanceBuilder.requestFactory));
            return instanceBuilder.GetValues(pages).ContinueWith(t =>
            {
                if (t.Result) modelHandler.StartNewTracker();
            });
        }
		public IInputResponse EditTracker (TrackerInstanceVM dvm)
		{
			var pages = new List<GetValuesPage> (modelHandler.model.EditPages (dvm.originator as DietInstType, instanceBuilder.requestFactory));
            return instanceBuilder.GetValues(pages).ContinueWith(t=>
            {
				if (t.Result) modelHandler.EditTracker (dvm.originator as DietInstType);
            });
		}
		public void RemoveTracker (TrackerInstanceVM dvm, bool warn = true)
		{
			var diet = dvm.originator as DietInstType;
			int ct = 0;
            if ((ct = modelHandler.outhandler.Count(diet) + modelHandler.inhandler.Count(diet)) > 0 && warn)
                getInput.WarnConfirm("That instance still has " + ct + " entries, they will be removed if you continue.")
                    .ContinueWith(t =>
                    {
                        if (t.Result) PlatformGlobal.Run(() => modelHandler.RemoveTracker(diet));
                    });
            else modelHandler.RemoveTracker(diet);
		}

		public IInputResponse AddIn(TrackerInstanceVM to, IValueRequestBuilder bld)
		{
			return Full<EatType,EatInfoType> (to.originator as DietInstType, modelHandler.model.increator, modelHandler.inhandler, true, presenter.GetRepresentation, bld);
		}
		public IInputResponse AddOut(TrackerInstanceVM to, IValueRequestBuilder bld)
		{
			return Full<BurnType,BurnInfoType> (to.originator as DietInstType, modelHandler.model.outcreator, modelHandler.outhandler, false, presenter.GetRepresentation, bld);
		}

		IInputResponse Full<T,I>(DietInstType diet, IEntryCreation<T,I> creator, 
			EntryHandler<DietInstType,T,I> handler, bool true_if_in,
			Func<I,InfoLineVM> rep, IValueRequestBuilder getValues, T editing = null) 
				where T : BaseEntry, new()
				where I : BaseInfo, new()
		{
			// reset to start
			creator.ResetRequests();

			// get a request object for infos
			String info_plural = true_if_in ? presenter.dialect.InputInfoPlural : presenter.dialect.OutputInfoPlural;
            var infoRequest = getValues.requestFactory.InfoLineVMRequestor(info_plural, true_if_in ? InfoManageType.In : InfoManageType.Out);
			infoRequest.valid = true; // always true
			InfoLineVM sinfo = null;
			if(editing != null && editing.infoinstanceid.HasValue)
			{
				var imod = GetInfo<I> (editing);
				sinfo = rep (imod);
				sinfo.originator = imod; // dont forget to set this this time...
			}
            infoRequest.value = sinfo;
		
			// Set up for editing
			Func<IList<Object>> flds = () => {
				var si = infoRequest.value;
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
				var si = infoRequest.value;
				if (si == null) {
					if (editing == null) handler.Add (diet);
					else handler.Edit (editing, diet);						
				} else {
					if (editing == null) handler.Add (diet, si.originator as I);
					else handler.Edit (editing, diet, si.originator as I);
				}
			};

			// Get the page ready
			String entryVerb = true_if_in ? presenter.dialect.InputEntryVerb : presenter.dialect.OutputEntryVerb;
			var requests = new GetValuesPage (entryVerb);

            // Initialise and keep requests up to date
            InfoLineVM last = new InfoLineVM { name = "Dummy" };
			Action checkFields = () => {
                // break out before we get flds again
                if (last == infoRequest.value) return;
                last = infoRequest.value;

                // Refresh requests
				var nrq = flds();
				nrq.Insert (0, infoRequest.request);
				requests.SetList(nrq);
			};
			checkFields ();
			infoRequest.ValueChanged += checkFields;
            last = infoRequest.value;

            return getValues.GetValues(new[] { requests }).ContinueWith(t =>
            {
				if (t.Result) editit ();
				infoRequest.ValueChanged -= checkFields;
            });
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
			var dis = modelHandler.conn.Get<DietInstType>( inf => inf.id == be.trackerinstanceid);
			return dis.Count() == 0 ? null : dis.First();
		}

		public IInputResponse EditIn (EntryLineVM ed,IValueRequestBuilder bld) {
			var eat = ed.originator as EatType;
			return Full<EatType,EatInfoType> (getit(eat), modelHandler.model.increator, modelHandler.inhandler, true, presenter.GetRepresentation,bld,eat);
		}
		public IInputResponse EditOut (EntryLineVM ed,IValueRequestBuilder bld) {
			var burn = ed.originator as BurnType;
			return Full<BurnType,BurnInfoType> (getit(burn), modelHandler.model.outcreator, modelHandler.outhandler, false, presenter.GetRepresentation, bld,burn);
		}

		public IInputResponse AddInInfo(IValueRequestBuilder bld)
		{
			return DoInfo<EatInfoType> ("Create a " + dialect.InputInfoSingular,InFinder, modelHandler.model.increator, modelHandler.inhandler, bld);
		}
		public IInputResponse EditInInfo(InfoLineVM ivm, IValueRequestBuilder bld)
		{
			return DoInfo<EatInfoType> ("Edit " + dialect.InputInfoSingular ,InFinder, modelHandler.model.increator, modelHandler.inhandler, bld, ivm.originator as EatInfoType);
		}
		public void RemoveInInfo(InfoLineVM ivm)
		{
			modelHandler.inhandler.Remove (ivm.originator as EatInfoType);
		}

		IInputResponse DoInfo<I>(String title,IFindList<InfoLineVM> finder, IInfoCreation<I> creator, IInfoHandler<I> handler, IValueRequestBuilder builder, I toEdit = null)  where I : BaseInfo, new()
		{
			bool editing = toEdit != null;
			var vros = editing ? creator.InfoFields (builder.requestFactory) : new ValueRequestFactory_FinderAdapter<I> (finder, creator, builder.requestFactory, getInput).GetRequestObjects ();
			var gvp = new GetValuesPage (title);
			gvp.SetList (vros);
			if (editing) creator.FillRequestData (toEdit, builder.requestFactory);

            return builder.GetValues (new[]{ gvp }).ContinueWith(result => {
				if (result.Result) {
					if (editing) handler.Edit (toEdit);
					else handler.Add ();
				}
			});
		}

		public IInputResponse AddOutInfo(IValueRequestBuilder bld)
		{
			return DoInfo<BurnInfoType> ("Create a " + dialect.OutputInfoSingular,OutFinder, modelHandler.model.outcreator, modelHandler.outhandler, bld);
		}
		public IInputResponse EditOutInfo(InfoLineVM ivm, IValueRequestBuilder bld)
		{
			return DoInfo<BurnInfoType> ("Edit " + dialect.OutputInfoSingular, OutFinder, modelHandler.model.outcreator, modelHandler.outhandler, bld, ivm.originator as BurnInfoType);
		}
		public void RemoveOutInfo(InfoLineVM ivm)
		{
			modelHandler.outhandler.Remove (ivm.originator as BurnInfoType);
		}
	}
}

