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
using Consonance.Invention;
using System.Linq;
using System.Linq.Expressions;
using LibSharpHelp;
using Consonance.Protocol;

namespace Consonance
{
    /// <summary>
    /// Command router for entry and info actions
    /// </summary>
    class PlanCommandManager
    {
        readonly Func<TrackerInstanceVM> getCurrent;
        readonly IUserInput input;
        public PlanCommandManager(IPlanCommands commands, Func<TrackerInstanceVM> getCurrent, IUserInput input)
        {
            // remember it
            this.getCurrent = getCurrent;
            this.input = input;

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
                input.Message("You need to create a tracker before you can do that");
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


        class Serv : IServices
        {
            public Serv(IPlatform plat)
            {
                dal = new SqliteDal(plat);
            }
            public IDAL dal { get; set; }
        }
        Serv services;

        // present app logic domain to this view.
        IView view;
        IUserInput input;
        PlanCommandManager pcm_refholder;
        Task PresentToImpl(IView view, IPlatform platform, IUserInput input, IPlanCommands commands, IValueRequestBuilder defBuilder)
        {
            // Start fast!
            return PlatformGlobal.Run(() => {

                // load DB
                services = new Serv(platform);

                Debug.WriteLine("PresntToImpl: presenting");
                this.view = view;
                this.input = input;

                // Builtins
                AddDietPair(CalorieDiets.simple, defBuilder, services.dal);
                AddDietPair(CalorieDiets.scav, defBuilder, services.dal);
                AddDietPair(Budgets.simpleBudget, defBuilder, services.dal);

                // Inventors
                var basic = new SimpleTrackyHelpyInventionV1(services.dal, this, defBuilder, input);
                inventors.Add(new InventorType
                {
                    name = "Basic",
                    category = "Inventors",
                    description = "Create basic tracker type which tracks a single quantitiy",
                    inventor = basic
                });
                basic.ViewModelsToChange += (o, e) => TaskMapper(TrackerChangeType.Inventions, e.toChange, true);


                Debug.WriteLine("PresntToImpl: commands");
                // commanding...
                view.plan.add += Handleadddietinstance;
                view.plan.select += View_trackerinstanceselected;
                view.plan.remove += Handleremovedietinstance;
                view.plan.edit += View_editdietinstance;
                view.invention.add += Invention_add;
                view.invention.edit += Invention_edit;
                view.invention.remove += Invention_remove;

                // more commanding...
                view.changeday += ChangeDay;

                // command router
                pcm_refholder = new PlanCommandManager(commands, () => view.currentTrackerInstance, input);

                // set vm datasources
                view.SetInventions(inventions);
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
                TaskMapper(TrackerChangeType.Instances | TrackerChangeType.Inventions, null, true);
            });
        }

        class InventorType
        {
            public String name;
            public String description;
            public String category;
            public IViewModelHandler<InventedTrackerVM> inventor;
        }
        readonly IList<InventorType> inventors = new List<InventorType>();

        private void Invention_remove(InventedTrackerVM obj)
        {
            (obj.sender as IViewModelHandler<InventedTrackerVM>).RemoveTracker(obj);
        }
        private void Invention_edit(InventedTrackerVM obj)
        {
            (obj.sender as IViewModelHandler<InventedTrackerVM>).EditTracker(obj);
        }
        private void Invention_add()
        {
            if (inventors.Count == 1) inventors[0].inventor.StartNewTracker();
            else
            {
                // or choose.
                var names = (from i in inventors select new TrackerDetailsVM(i.name, i.description, i.category)).ToList();
                var chooseViewTask = input.ChoosePlan("Select invention type", names, -1);
                chooseViewTask.Result.ContinueWith(async index =>
                {
                    var addViewTask = inventors[index.Result].inventor.StartNewTracker();
                    await addViewTask;
                    await chooseViewTask.Close();
                });
            }
        }

        // vm holders
        ReplacingVMList<InventedTrackerVM> inventions = new ReplacingVMList<InventedTrackerVM>("inventions");
        ReplacingVMList<TrackerInstanceVM> tracker_instances = new ReplacingVMList<TrackerInstanceVM>("instances");
        ReplacingVMList<EntryLineVM> inEntries = new ReplacingVMList<EntryLineVM>("in");
        ReplacingVMList<EntryLineVM> outEntries = new ReplacingVMList<EntryLineVM>("out");
        ReplacingVMList<InfoLineVM> inInfos = new ReplacingVMList<InfoLineVM>("in.i");
        ReplacingVMList<InfoLineVM> outInfos = new ReplacingVMList<InfoLineVM>("out.i");
        ReplacingVMList<TrackerTracksVM> inTracks = new ReplacingVMList<TrackerTracksVM>("in.t");
        ReplacingVMList<TrackerTracksVM> outTracks = new ReplacingVMList<TrackerTracksVM>("out.t");

        #endregion

        #region handlers that end up mapped to tasks later
        void View_trackerinstanceselected(TrackerInstanceVM obj)
        {
            TaskMapper(
                TrackerChangeType.EatInfos |
                TrackerChangeType.BurnInfos |
                TrackerChangeType.EatEntries |
                TrackerChangeType.BurnEntries,
                null, true);
        }
        DateTime ds, de;
        void ChangeDay(DateTime to)
        {
            ds = to.StartOfDay();
            de = ds.AddDays(1);
            view.day = ds;
            TaskMapper(TrackerChangeType.EatEntries | TrackerChangeType.BurnEntries, null, true);
        }
        void Handleadddietinstance()
        {
            List<IAbstractedTracker> saveDiets = new List<IAbstractedTracker>(dietHandlers);
            List<TrackerDetailsVM> dietnames = new List<TrackerDetailsVM>();
            foreach (var ad in saveDiets) dietnames.Add(ad.details);
            var chooseViewTask = input.ChoosePlan("Select Diet Type", dietnames, -1);
            chooseViewTask.Result.ContinueWith(async index => {
                var addViewTask = saveDiets[index.Result].StartNewTracker();
                await addViewTask;
                await chooseViewTask.Close();
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
        public class AddDietPairState { public Action remove; public IAbstractedDAL dal; }
        public AddDietPairState AddDietPair<D, E, Ei, B, Bi>(ITracker<D, E, Ei, B, Bi> dietModel, IValueRequestBuilder defBuilder, IDAL private_dal)
            where D : TrackerInstance, new()
            where E : BaseEntry, new()
            where Ei : BaseInfo, new()
            where B : BaseEntry, new()
            where Bi : BaseInfo, new()
        {
            dietModel.services = services;

            var presentationHandler = new TrackerPresentationAbstractionHandler<D, E, Ei, B, Bi>(
                defBuilder, 
                input,
                private_dal, 
                dietModel.model, 
                dietModel.presenter,
                dietModel.config.ShareInfo ? services.dal : private_dal
            );
            dietHandlers.Add(presentationHandler);
            presentationHandler.ViewModelsToChange += HandleViewModelChange;

            // helpers callers to undo what they did!
            return new AddDietPairState
            {
                remove = () =>
                    {
                        // removal action
                        presentationHandler.ViewModelsToChange -= HandleViewModelChange;
                        dietHandlers.Remove(presentationHandler);
                    },
                dal = presentationHandler.modelHandler
            };
        }

        void HandleViewModelChange(IAbstractedTracker sender, DietVMToChangeEventArgs<TrackerChangeType> args)
        {
            // Always map everything cause it needs to be actioned at least!
            var ti = view.currentTrackerInstance;
            TaskMapper(args.changeType, args.toChange, 
                args.changeType.HasFlag(TrackerChangeType.Instances) ||
                args.changeType.HasFlag(TrackerChangeType.Inventions) ||
                (ti != null && Object.ReferenceEquals(ti.sender, sender)));
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

        Dictionary<TrackerChangeType, IBusyMaker[]> busyMap;
        Dictionary<TrackerChangeType, Action<TrackerInstanceVM>> taskMap;
        Dictionary<TrackerChangeType, TrackerChangeType[]> cMap;
        void InitMaps()
        {
            cMap = new Dictionary<TrackerChangeType, TrackerChangeType[]>
            {
                { TrackerChangeType.Instances, new[] {  TrackerChangeType.Tracking } },
                { TrackerChangeType.EatEntries,new[] { TrackerChangeType.Tracking } },
                { TrackerChangeType.BurnEntries,new[] { TrackerChangeType.Tracking} },
            };
            busyMap = new Dictionary<TrackerChangeType, IBusyMaker[]>
            {
                { TrackerChangeType.Inventions, new IBusyMaker[] { inventions } },
                { TrackerChangeType.Instances, new IBusyMaker[] { tracker_instances } },
                { TrackerChangeType.EatEntries, new IBusyMaker[] { inEntries, inTracks, outTracks } },
                { TrackerChangeType.BurnEntries,new IBusyMaker[] { outEntries, inTracks, outTracks } },
                { TrackerChangeType.EatInfos, new IBusyMaker[] { inInfos } },
                { TrackerChangeType.BurnInfos,new IBusyMaker[] { outInfos } }
            };
            taskMap = new Dictionary<TrackerChangeType, Action<TrackerInstanceVM>>
            {
                { TrackerChangeType.Inventions,ti=>
                    {
                        List<InventedTrackerVM> toreplace = new List<InventedTrackerVM>();
                        foreach(var iv in inventors)
                            toreplace.AddRange(iv.inventor.Instances());
                        inventions.SetItems(toreplace);
                    }
                },
                { TrackerChangeType.Instances,ti=>
                    {
                        List<TrackerInstanceVM> toreplace = new List<TrackerInstanceVM>();
                        foreach (var dh in dietHandlers)
                            toreplace.AddRange(dh.Instances());
                        lock(tracker_instances) tracker_instances.SetItems(toreplace);

                        // change current tracker if either old one is no more, or, we didnt have one selected.
                        var ai = tracker_instances.Where(i => OriginatorVM.OriginatorEquals(i, ti));
                        if (ai.Count() == 0) View_trackerinstanceselected(view.currentTrackerInstance = tracker_instances.Count > 0 ? tracker_instances[0] : null);
                        else view.currentTrackerInstance =ai.First(); // could be updated
                    }
                },
                {TrackerChangeType.EatEntries,ti=>
                    {
                        var ad = (ti?.sender) as IAbstractedTracker;
                        inEntries.SetItems(ad?.InEntries(ti, ds, de));
                    }
                 },
                {TrackerChangeType.BurnEntries, ti=>
                    {
                        var ad = (ti?.sender) as IAbstractedTracker;
                        outEntries.SetItems(ad?.OutEntries(ti, ds, de));
                    }
                },
                {TrackerChangeType.EatInfos, ti=>
                    {
                        var ad = (ti?.sender) as IAbstractedTracker;
                        inInfos.SetItems(ad?.InInfos(false));
                    }
                },
                {TrackerChangeType.BurnInfos, ti=>
                    {
                        var ad = (ti?.sender) as IAbstractedTracker;
                        outInfos.SetItems(ad?.OutInfos(false));
                    }
                },
                { TrackerChangeType.Tracking, ti=> SetTracking(ti) }
            };
        }


        void SetTracking(TrackerInstanceVM cti)
        {
            // creators
            Func<TrackerInstanceVM, TrackerTracksVM> git = lti => new TrackerTracksVM { instance = lti, tracks = (lti.sender as IAbstractedTracker).GetInTracking(lti, ds).ToArray() };
            Func<TrackerInstanceVM, TrackerTracksVM> got = lti => new TrackerTracksVM { instance = lti, tracks = (lti.sender as IAbstractedTracker).GetOutTracking(lti, ds).ToArray() };

            // results (current first)
            List<TrackerTracksVM> in_t = new List<TrackerTracksVM>();
            List<TrackerTracksVM> out_t = new List<TrackerTracksVM>();
            if (cti != null)
            {
                in_t.Add(git(cti));
                out_t.Add(got(cti));
            }
            lock (tracker_instances)
            {
                foreach (var d in tracker_instances)
                {
                    if (!OriginatorVM.OriginatorEquals(d, cti))
                    {
                        in_t.Add(git(d));
                        out_t.Add(got(d));
                    }
                }
            }
            inTracks.SetItems(in_t);
            outTracks.SetItems(out_t);
        }

        void TaskMapper(TrackerChangeType action, Func<Action> prior, bool perform_mapping)
        {
            // Set Busy
            List<Action> madeBusy = new List<Action>();

            if (perform_mapping)
            {
                foreach (var flag in ((uint)action).SplitAsFlags())
                    foreach (var bl in busyMap[(TrackerChangeType)flag])
                        madeBusy.Add(bl.BusyMaker());
            }


            // begin task
            PlatformGlobal.Run(() =>
            {
                // run the prior to make this state true
                var after = prior?.Invoke();

                while (perform_mapping)
                {

                    // Coaelsecse attachde tasks (1 run iit)
                    var flags = ((uint)action).SplitAsFlags();
                    foreach (var flag in flags)
                    {
                        var f = (TrackerChangeType)flag;
                        if (cMap.ContainsKey(f))
                            foreach (var connected in cMap[f])
                                action |= connected;
                    }

                    TrackerInstanceVM cs;
                    do
                    {
                        // remember currently selected dude
                        cs = view.currentTrackerInstance;

                        // run associated tasks
                        foreach (var flag in ((uint)action).SplitAsFlags())
                            taskMap[(TrackerChangeType)flag](cs);
                    } while (!OriginatorVM.OriginatorEquals(cs, view.currentTrackerInstance));

                    // run anything to be done after mapping completed.
                    after?.Invoke();

                    // complete busies off
                    foreach (var b in madeBusy) b();
                    perform_mapping = false;
                }
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
        String name { get; }
    }
    class ReplacingVMList<T> : DispatchedObservableCollection<T>, IVMList<T>, IBusyMaker
    {
        public string name { get; set; }
        public ReplacingVMList(String name)
        {
            this.name = name;
        }

        public bool busy { get { return bc > 0; } }
        protected int bc = 0;
        public Action BusyMaker()
        {
            bc++;
            BC();
            return () => { bc--; BC(); };
        }

        public void BC()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("busy"));
        }

        public void SetItems(IEnumerable<T> items)
        {
            SuspendNotifications();
            Clear();
            foreach (var i in items ?? new T[0])
                Add(i);
            ResumeNotifications();
        }
    }

    public interface IVMList<T> : IList<T>, IVMList
    {
    }
    public interface IVMList : INotifyPropertyChanged, INotifyCollectionChanged, ICanDispatch
    {
        bool busy { get; }
    }

    public class TrackerTracksVM
    {
        public TrackerInstanceVM instance;
        public String modelName { get { return (instance.sender as IAbstractedTracker).details.name; } }
        public String instanceName { get { return instance.name; } }
        public TrackingInfoVM[] tracks = new TrackingInfoVM[0];
    }

    /// <summary>
    /// definition on the application view
    /// </summary>
    public interface IView
    {
        // This only needs to be set up once.
        void SetEatTrack(IVMList<TrackerTracksVM> tracks_current_first);
        void SetBurnTrack(IVMList<TrackerTracksVM> tracks_current_first);
        void SetEatLines(IVMList<EntryLineVM> lineitems);
        void SetBurnLines(IVMList<EntryLineVM> lineitems);
        void SetEatInfos(IVMList<InfoLineVM> lineitems);
        void SetBurnInfos(IVMList<InfoLineVM> lineitems);
        void SetInstances(IVMList<TrackerInstanceVM> instanceitems);
        void SetInventions(IVMList<InventedTrackerVM> inventionitems);

        event Action<DateTime> changeday;
        DateTime day { get; set; }
        TrackerInstanceVM currentTrackerInstance { get; set; }
        ICollectionEditorSelectableLooseCommands<TrackerInstanceVM> plan { get; }
        ICollectionEditorLooseCommands<InventedTrackerVM> invention { get; }
    }
    public interface IPlatform
    {
        Task UIThread(Action method);
        ISQLitePlatform sqlite { get; }
        ITasks TaskOps { get; }
        void Attach(Action<String, Action> showError);
        IFSOps filesystem { get; }
        bool CreateDirectory(String ifdoesntexist);
    }

    public interface IFSOps
    {
        string AppData { get; }
        void Delete(String file);
        byte[] ReadFile(String file);
    }
    public interface ITasks
    {
        Task RunTask(Func<Task> asyncMethod);
        Task RunTask(Action syncMethod);
        Task<T> RunTask<T>(Func<Task<T>> asyncMethod);
        Task<T> RunTask<T>(Func<T> syncMethod);
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
    }
    public interface ICollectionEditorSelectableLooseCommands<T> : ICollectionEditorLooseCommands<T>
    {
        event Action<T> select;
    }

    public interface IInputResponse
    {
        Task Result { get; }
        Task Close();
    }
    public interface IInputResponse<T> 
    {
        Task<T> Result { get; }
        Task Close();
    }

    public interface IUserInput
    {
        // User Input
        IInputResponse<String> SelectString(String title, IReadOnlyList<String> strings, int initial);
        IInputResponse<int> ChoosePlan(String title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial);
        IInputResponse<bool> WarnConfirm(String action);
        IInputResponse Message(String msg);
        IInputResponse<InfoLineVM> Choose(IFindList<InfoLineVM> ifnd);
    }
    
    #endregion

    
    public delegate void Undo();
    public static class IVRExtensions
    {
        public static Undo ValidWhen<T>(this IValueRequest<T> @this, Predicate<T> pred)
        {
            Action vcp = () => @this.valid = pred(@this.value);
            @this.ValueChanged += vcp;
            vcp();
            return () => @this.ValueChanged -= vcp;
        }
    }

}
