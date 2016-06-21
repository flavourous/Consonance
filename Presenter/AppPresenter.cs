using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using SQLite.Net;
using System.IO;
using System.Threading;
using System.Text;
using LibRTP;
using SQLite.Net.Interop;
using System.Reflection;
using SQLite.Net.Attributes;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.Collections;

namespace Consonance
{
    /// <summary>
    /// Command router for entry and info actions
    /// </summary>
    class PlanCommandManager
    {
        readonly Func<TrackerInstanceVM> getCurrent;
        readonly Func<String, Task> message;
        public PlanCommandManager(IPlanCommands commands, Func<TrackerInstanceVM> getCurrent, Func<String, Task> message)
        {
            // remember it
            this.getCurrent = getCurrent;
            this.message = message;

            // commanding for pland
            commands.eat.add += View_addeatitem;
            commands.eat.remove += View_removeeatitem;
            commands.eat.edit += View_editeatitem;
            commands.eatinfo.add += View_addeatinfo;
            commands.eatinfo.remove += View_removeeatinfo;
            commands.eatinfo.edit += View_editeatinfo;
            commands.burn.add += View_addburnitem;
            commands.burn.remove += View_removeburnitem;
            commands.burn.edit += View_editburnitem;
            commands.burninfo.add += View_addburninfo;
            commands.burninfo.remove += View_removeburninfo;
            commands.burninfo.edit += View_editburninfo;
        }

        void View_addeatitem(IValueRequestBuilder bld) { VerifyDiet((cdh, cd) => cdh.AddIn(cd, bld)); }
        void View_removeeatitem(EntryLineVM vm) { VerifyDiet((cdh, cd) => cdh.RemoveIn(vm)); }
        void View_editeatitem(EntryLineVM vm, IValueRequestBuilder bld) { VerifyDiet((cdh, cd) => cdh.EditIn(vm, bld)); }
        void View_addeatinfo(IValueRequestBuilder bld) { VerifyDiet((cdh, cd) => cdh.AddInInfo(bld)); }
        void View_removeeatinfo(InfoLineVM vm) { VerifyDiet((cdh, cd) => cdh.RemoveInInfo(vm)); }
        void View_editeatinfo(InfoLineVM vm, IValueRequestBuilder bld) { VerifyDiet((cdh, cd) => cdh.EditInInfo(vm, bld)); }
        void View_addburnitem(IValueRequestBuilder bld) { VerifyDiet((cdh, cd) => cdh.AddOut(cd, bld)); }
        void View_removeburnitem(EntryLineVM vm) { VerifyDiet((cdh, cd) => cdh.RemoveOut(vm)); }
        void View_editburnitem(EntryLineVM vm, IValueRequestBuilder bld) { VerifyDiet((cdh, cd) => cdh.EditOut(vm, bld)); }
        void View_addburninfo(IValueRequestBuilder bld) { VerifyDiet((cdh, cd) => cdh.AddOutInfo(bld)); }
        void View_removeburninfo(InfoLineVM vm) { VerifyDiet((cdh, cd) => cdh.RemoveOutInfo(vm)); }
        void View_editburninfo(InfoLineVM vm, IValueRequestBuilder bld) { VerifyDiet((cdh, cd) => cdh.EditOutInfo(vm, bld)); }

        public void VerifyDiet(Action<IAbstractedTracker, TrackerInstanceVM> acty, String load = null)
        {
            var cd = getCurrent();
            if (cd == null) // ping the view about being stupid.
                message("You need to create a tracker before you can do that");
            else acty(cd.sender as IAbstractedTracker, cd); // dont thread here, just route. DAL will thread.
        }
    }

    /// <summary>
    /// Stub thunk shim whatever.  
    /// For accessing platform stuff on IPlatform of native implimentations within presenter.
    /// </summary>
    static class PlatformGlobal
    {
        public static IPlatform platform;
        public static Task Run(Func<Task> asyncMethod) { return platform.TaskOps.RunTask(asyncMethod); }
        public static Task Run(Action syncMethod) { return platform.TaskOps.RunTask(syncMethod); }
        public static Task<T> Run<T>(Func<Task<T>> asyncMethod) { return platform.TaskOps.RunTask(asyncMethod); }
        public static Task<T> Run<T>(Func<T> syncMethod) { return platform.TaskOps.RunTask(syncMethod); }
    }

    public class Presenter
    {
        #region initialisation of app presentation
        // Singleton logic - lazily created
        static Presenter singleton;
        //public static Presenter Singleton { get { return singleton ?? (singleton = new Presenter()); } }
        public static async Task PresentTo(IView view, IPlatform platform, IUserInput input, IPlanCommands commands, IValueRequestBuilder defBuilder)
        {
            PlatformGlobal.platform = platform;
            singleton = new Presenter();
            await singleton.PresentToImpl(view, platform, input, commands, defBuilder);
        }
        SQLiteConnection conn;

        // present app logic domain to this view.
        IView view;
        IUserInput input;
        PlanCommandManager pcm_refholder;
        Task PresentToImpl(IView view, IPlatform platform, IUserInput input, IPlanCommands commands, IValueRequestBuilder defBuilder)
        {
            // Start fast!
            return PlatformGlobal.Run(() => {

                // load DB
                var datapath = platform.filesystem.AppData;
                platform.CreateDirectory(datapath);
                var maindbpath = Path.Combine(datapath, "manydiet.db");
                //platform.filesystem.Delete(maindbpath);
                //byte[] file = platform.filesystem.ReadFile(maindbpath);
                conn = new SQLiteConnection(platform.sqlite, maindbpath, false);

                Debug.WriteLine("PresntToImpl: presenting");
                this.view = view;
                this.input = input;
                AddDietPair(CalorieDiets.simple.model, CalorieDiets.simple.presenter, defBuilder);
                AddDietPair(CalorieDiets.scav.model, CalorieDiets.scav.presenter, defBuilder);
                AddDietPair(Budgets.simpleBudget.model, Budgets.simpleBudget.presenter, defBuilder);

                Debug.WriteLine("PresntToImpl: commands");
                // commanding...
                view.plan.add += Handleadddietinstance;
                view.plan.select += View_trackerinstanceselected;
                view.plan.remove += Handleremovedietinstance;
                view.plan.edit += View_editdietinstance;

                // more commanding...
                view.changeday += ChangeDay;

                // command router
                pcm_refholder = new PlanCommandManager(commands, () => view.currentTrackerInstance, input.Message);

                // set vm datasources
                view.SetInstances(tracker_instances);
                view.SetEatLines(inEntries);
                view.SetBurnLines(outEntries);
                view.SetEatInfos(inInfos);
                view.SetBurnInfos(outInfos);
                view.SetEatTrack(inTracks);
                view.SetBurnTrack(outTracks);
                InitMaps();

                // setup view
                ChangeDay(DateTime.UtcNow);
                TaskMapper(DietVMChangeType.Instances, null);
            });
        }

        // vm holders
        ReplacingVMList<TrackerInstanceVM> tracker_instances = new ReplacingVMList<TrackerInstanceVM>();
        ReplacingVMList<EntryLineVM> inEntries = new ReplacingVMList<EntryLineVM>();
        ReplacingVMList<EntryLineVM> outEntries = new ReplacingVMList<EntryLineVM>();
        ReplacingVMList<InfoLineVM> inInfos = new ReplacingVMList<InfoLineVM>();
        ReplacingVMList<InfoLineVM> outInfos = new ReplacingVMList<InfoLineVM>();
        ReplacingVMList<TrackerTracksVM> inTracks = new ReplacingVMList<TrackerTracksVM>();
        ReplacingVMList<TrackerTracksVM> outTracks = new ReplacingVMList<TrackerTracksVM>();

        #endregion

        #region handlers that end up mapped to tasks later
        void View_trackerinstanceselected(TrackerInstanceVM obj)
        {
            TaskMapper(DietVMChangeType.EatInfos | DietVMChangeType.BurnInfos | DietVMChangeType.EatEntries | DietVMChangeType.BurnEntries, null);
        }
        DateTime ds, de;
        void ChangeDay(DateTime to)
        {
            ds = to.StartOfDay();
            de = ds.AddDays(1);
            view.day = ds;
            TaskMapper(DietVMChangeType.EatEntries | DietVMChangeType.BurnEntries, null);
        }
        void Handleadddietinstance()
        {
            List<IAbstractedTracker> saveDiets = new List<IAbstractedTracker>(dietHandlers);
            List<TrackerDetailsVM> dietnames = new List<TrackerDetailsVM>();
            foreach (var ad in saveDiets) dietnames.Add(ad.details);
            var chooseViewTask = input.ChoosePlan("Select Diet Type", dietnames, -1);
            chooseViewTask.Completed.ContinueWith(async index => {
                var addViewTask = saveDiets[index.Result].StartNewTracker();
                await addViewTask;
                await chooseViewTask.Pop();
            });
        }
        void View_editdietinstance(TrackerInstanceVM obj)
        {
            (obj.sender as IAbstractedTracker).EditTracker(obj);
        }
        void Handleremovedietinstance(TrackerInstanceVM obj)
        {
            (obj.sender as IAbstractedTracker).RemoveTracker(obj);
        }
        #endregion

        #region tracker registry
        List<IAbstractedTracker> dietHandlers = new List<IAbstractedTracker>();
        void AddDietPair<D, E, Ei, B, Bi>(ITrackModel<D, E, Ei, B, Bi> dietModel, ITrackerPresenter<D, E, Ei, B, Bi> dietPresenter, IValueRequestBuilder defBuilder)
            where D : TrackerInstance, new()
            where E : BaseEntry, new()
            where Ei : BaseInfo, new()
            where B : BaseEntry, new()
            where Bi : BaseInfo, new()
        {
            var presentationHandler = new TrackerPresentationAbstractionHandler<D, E, Ei, B, Bi>(defBuilder, input, conn, dietModel, dietPresenter);
            dietHandlers.Add(presentationHandler);
            presentationHandler.ViewModelsToChange += HandleViewModelChange;
        }

        void HandleViewModelChange(IAbstractedTracker sender, DietVMToChangeEventArgs args)
        {
            // Always map instances, only map entries if of current instance
            var ti = view.currentTrackerInstance;
            if (args.changeType == DietVMChangeType.Instances || (ti != null && Object.ReferenceEquals(ti.sender, sender)))
                TaskMapper(args.changeType, args.toChange);
        }
        #endregion

        // Actions and Tasks:
        //
        // Actions
        // 1) DC - Instances - get&push instances and maybe fire (4)
        // 2) DC - Current Entries - get&push entries + tracking
        // 3) DC - Current Infos - get&push infos + tracking
        // 4) sel - Instance - get&push entries,infos + tracking
        // 5) sel - Day - get&push entries + tracking
        //
        // Tasks
        // 0) Associated from DAL (eg edit datbase)
        // 1) get&push instances
        // 2) get&push entries
        // 3) get&push infos
        // 4) get&push trackings

        Dictionary<DietVMChangeType, IBusyMaker[]> busyMap;
        Dictionary<DietVMChangeType, Action<TrackerInstanceVM>> taskMap;
        Dictionary<DietVMChangeType, DietVMChangeType[]> cMap;
        void InitMaps()
        {
            cMap = new Dictionary<DietVMChangeType, DietVMChangeType[]>
            {
                { DietVMChangeType.Instances, new[] {  DietVMChangeType.Tracking } },
                { DietVMChangeType.EatEntries,new[] { DietVMChangeType.Tracking } },
                { DietVMChangeType.BurnEntries,new[] { DietVMChangeType.Tracking} },
            };
            busyMap = new Dictionary<DietVMChangeType, IBusyMaker[]>
            {
                { DietVMChangeType.Instances, new IBusyMaker[] { tracker_instances } },
                { DietVMChangeType.EatEntries, new IBusyMaker[] { inEntries, inTracks, outTracks } },
                { DietVMChangeType.BurnEntries,new IBusyMaker[] { outEntries, inTracks, outTracks } },
                { DietVMChangeType.EatInfos, new IBusyMaker[] { inInfos } },
                { DietVMChangeType.BurnInfos,new IBusyMaker[] { outInfos } }
            };
            taskMap = new Dictionary<DietVMChangeType, Action<TrackerInstanceVM>>
            {
                { DietVMChangeType.Instances,ti=>
                    {
                        List<TrackerInstanceVM> toreplace = new List<TrackerInstanceVM>();
                        foreach (var dh in dietHandlers)
                            toreplace.AddRange(dh.Instances());
                        tracker_instances.SetItems(toreplace);
                    }
                },
                {DietVMChangeType.EatEntries,ti=>
                    {
                        var ad = (ti?.sender) as IAbstractedTracker;
                        inEntries.SetItems(ad?.InEntries(ti, ds, de));
                    }
                 },
                {DietVMChangeType.BurnEntries, ti=>
                    {
                        var ad = (ti?.sender) as IAbstractedTracker;
                        outEntries.SetItems(ad?.OutEntries(ti, ds, de));
                    }
                },
                {DietVMChangeType.EatInfos, ti=>
                    {
                        var ad = (ti?.sender) as IAbstractedTracker;
                        inInfos.SetItems(ad?.InInfos(false));
                    }
                },
                {DietVMChangeType.BurnInfos, ti=>
                    {
                        var ad = (ti?.sender) as IAbstractedTracker;
                        outInfos.SetItems(ad?.OutInfos(false));
                    }
                },
                { DietVMChangeType.Tracking, ti=> SetTracking(ti) }
            };
        }


        void SetTracking(TrackerInstanceVM cti)
        {
            // creators
            Func<TrackerInstanceVM, TrackerTracksVM> git = lti => new TrackerTracksVM { instance = lti, tracks = (lti.sender as IAbstractedTracker).GetInTracking(lti, ds) };
            Func<TrackerInstanceVM, TrackerTracksVM> got = lti => new TrackerTracksVM { instance = lti, tracks = (lti.sender as IAbstractedTracker).GetOutTracking(lti, ds) };

            // results (current first)
            List<TrackerTracksVM> in_t = new List<TrackerTracksVM>();
            List<TrackerTracksVM> out_t = new List<TrackerTracksVM>();
            if(cti!=null)
            {
                in_t.Add(git(cti));
                out_t.Add(got(cti));
            }

            foreach (var d in tracker_instances)
            {
                if (!OriginatorVM.OriginatorEquals(d, cti))
                {
                    in_t.Add(git(d));
                    out_t.Add(got(d));
                }
            }
            inTracks.SetItems(in_t);
            outTracks.SetItems(out_t);
        }

        void TaskMapper(DietVMChangeType action, Action prior)
        {
            // Set Busy
            List<Action> madeBusy = new List<Action>();
            foreach (var flag in ((uint)action).SplitAsFlags())
                foreach(var bl in busyMap[(DietVMChangeType)flag])
                    madeBusy.Add(bl.BusyMaker());

            // remember currently selected dude
            var cs = view.currentTrackerInstance;

            // begin task
            PlatformGlobal.Run(() =>
            {
                // run the prior to make this state true
                prior?.Invoke();

                // Coaelsecse attachde tasks (1 run iit)
                var flags = ((uint)action).SplitAsFlags();
                foreach (var flag in flags)
                {
                    var f = (DietVMChangeType)flag;
                    if(cMap.ContainsKey(f))
                        foreach (var connected in cMap[f])
                            action |= connected;
                }

                // run associated tasks
                foreach (var flag in ((uint)action).SplitAsFlags())
                    taskMap[(DietVMChangeType)flag](cs);

                // change current tracker if either old one is no more, or, we didnt have one selected.
                if (tracker_instances.items.FindAll(i => OriginatorVM.OriginatorEquals(i, cs)).Count == 0 && tracker_instances.Count > 0)
                    view.currentTrackerInstance = tracker_instances[0]; // is possible for TaskMapper to be recalled before this returns

                // complete busies off
                foreach (var b in madeBusy) b();
            });
        }
    }

    #region view interfaces
    public delegate bool Predicate();
    public delegate Task Promise();
    public delegate Task Promise<T>(T arg);

    interface IBusyMaker
    {
        Action BusyMaker();
    }
    class ReplacingVMList<T> : ObservableCollectionList<T>, IVMList<T>, IBusyMaker
    {
        public bool busy { get { return bc > 0; } }
        protected int bc = 0;
        public Action BusyMaker()
        {
            bc++;
            OnPropertyChanged("busy");
            return () => { bc--; OnPropertyChanged("busy"); };
        }

        public List<T> items { get { return backing; } }

        public void SetItems(IEnumerable<T> items)
        {
            backing.Clear();
            backing.AddRange(items ?? new T[0]);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
    public interface IVMList<T> : IObservableCollection<T>, IVMList
    {
    }
    public interface IVMList : INotifyPropertyChanged
    {
        bool busy { get; }
    }

	/// <summary>
	/// definition on the application view
	/// </summary>
	public interface IView
	{
        // This only needs to be set up once.
		void SetEatTrack(IVMList<TrackerTracksVM> tracks_current_first);
		void SetBurnTrack(IVMList<TrackerTracksVM> tracks_current_first);
		void SetEatLines (IVMList<EntryLineVM> lineitems);
		void SetBurnLines (IVMList<EntryLineVM> lineitems);
        void SetEatInfos(IVMList<InfoLineVM> lineitems);
        void SetBurnInfos(IVMList<InfoLineVM> lineitems);
        void SetInstances (IVMList<TrackerInstanceVM> instanceitems);

        event Action<DateTime> changeday;
		DateTime day { get; set; }
		TrackerInstanceVM currentTrackerInstance { get; set; }
		ICollectionEditorLooseCommands<TrackerInstanceVM> plan { get; }
	}
	public interface IPlatform
	{
        Task UIThread(Action method);
        ISQLitePlatform sqlite { get; }
        ITasks TaskOps { get; }
		void Attach (Action<String, Action> showError);
        IFSOps filesystem { get; }
        bool CreateDirectory(String ifdoesntexist);
        PropertyInfo GetPropertyInfo(Type T, String property);
	}
    public interface IFSOps
    {
        string AppData { get; }
        void Delete(String file);
        byte[] ReadFile(String file);
    }
	public interface ITasks
	{
		Task RunTask (Func<Task> asyncMethod);
		Task RunTask (Action syncMethod);
		Task<T> RunTask<T> (Func<Task<T>> asyncMethod);
		Task<T> RunTask<T> (Func<T> syncMethod);
	}
	public interface IPlanCommands
	{
		ICollectionEditorBoundCommands<EntryLineVM> eat { get; }
		ICollectionEditorBoundCommands<InfoLineVM> eatinfo { get; }
		ICollectionEditorBoundCommands<EntryLineVM> burn { get; }
		ICollectionEditorBoundCommands<InfoLineVM> burninfo { get; }
	}
	public interface ICollectionEditorBoundCommands<T> 
	{
		event Action<IValueRequestBuilder> add;
		event Action<T> remove;
		event Action<T, IValueRequestBuilder> edit;
	}
	public interface ICollectionEditorLooseCommands<T>
	{
		event Action add;
		event Action<T> remove;
		event Action<T> edit;
		event Action<T> select;
	}
	interface IViewTask 
	{
		Task Completed {get;}
		Task Pushed {get;}
		Task Pop();
	}
	public class ViewTask<TResult> : IViewTask
	{
		Task IViewTask.Completed { get { return Completed; } }
		public Task<TResult> Completed {get;private set;}
		public Task Pushed {get;private set;}
		public async Task Pop()
        {
            Task poptask = null;
            await PlatformGlobal.platform.UIThread(() => poptask = pop());
            await poptask;
        }
		readonly Func<Task> pop;
		public ViewTask(Func<Task> pop, Task pushed, Task<TResult> completed)
		{
			this.Pushed = pushed;
			this.pop = pop;
			this.Completed = completed;
		}
	}
	public interface IUserInput
	{
		// User Input
		Task SelectString (String title, IReadOnlyList<String> strings, int initial, Promise<int> completed);
		ViewTask<int> ChoosePlan (String title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial);
		Task WarnConfirm (String action, Promise confirmed);
		Task Message(String msg);
		Task<InfoLineVM> Choose (IFindList<InfoLineVM> ifnd);
	}
	public interface IValueRequestBuilder
	{
		// get generic set of values on a page thing
		ViewTask<bool> GetValues (IEnumerable<GetValuesPage> requestPages);

		// VRO Factory Method
		IValueRequestFactory requestFactory { get; }
	}
	public class Barcode
	{
		public long value; // I think this works?
	}
    public enum InfoManageType { In, Out };
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
		IValueRequest<Barcode> BarcodeRequestor (String name);
		IValueRequest<OptionGroupValue> OptionGroupRequestor(String name);
		IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor(String name);
		IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor(String name);
	}
    #endregion
    
    #region value types for valuerequests

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
		public RecurrsEveryPatternValue() : this(DateTime.Now, RecurrSpan.Day, 1) {
		}
		public bool IsValid 
		{ 
			get 
			{  
				return PatternType == RecurrSpan.Day ||
				       PatternType == RecurrSpan.Month ||
				       PatternType == RecurrSpan.Year ||
				       PatternType == RecurrSpan.Week;	
			}
		}

        void ChangeProperty(Action change, [CallerMemberName]String prop = null)
        {
            change();
            PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public RecurrsEveryPattern Create(DateTime? s, DateTime? e)
		{
			return new RecurrsEveryPattern (PatternFixed, PatternFrequency, PatternType, s, e);
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
		public RecurrsOnPatternValue():this(RecurrSpan.Day | RecurrSpan.Month, new[] { 1 }){
		}
		public bool IsValid 
		{
			get 
			{ 
				int pc = 0;
				foreach (var pt in PatternType.SplitFlags())
					pc++;
				bool s1 = PatternValues.Length > 1 && pc == PatternValues.Length;
				if (s1) 
				{
					try{ new RecurrsOnPattern(PatternValues, PatternType,null,null); }
					catch{ s1 = false; }
				}
				return s1;
			}
		}
		public RecurrsOnPattern Create(DateTime? s, DateTime? e)
		{
			return new RecurrsOnPattern (PatternValues, PatternType, s, e);
		}
	}
	public class OptionGroupValue 
	{
		public readonly IReadOnlyList<String> OptionNames;
		int selectedOption;
		public int SelectedOption {
			get {
				return selectedOption;
			}
			set {
				selectedOption = value;
			}
		}
		public OptionGroupValue(IEnumerable<String> options)
		{
			SelectedOption = 0;
			OptionNames = new List<String> (options);
		}
		public static implicit operator int(OptionGroupValue other)
		{
			return other.SelectedOption;
		}
		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder();
			for(int i=0;i<OptionNames.Count;i++)
			{
				if (i == SelectedOption) sb.Append ("[");
				sb.Append (OptionNames [i]);
				if (i == SelectedOption) sb.Append ("]");
				if (i != OptionNames.Count-1) sb.Append (" | ");
			}
			return sb.ToString ();
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
