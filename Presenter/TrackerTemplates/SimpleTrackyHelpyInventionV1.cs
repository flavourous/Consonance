using LibRTP;
using LibSharpHelp;
using SQLite.Net;
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Consonance.Invention
{

    // interface for an as yet unknown lib to generate and evaluate expressions from stringys
    #region String equation evaluator
    interface IStringEquationFactory
    {
        IStringEquation Create(String equation, params String[] args);
    }
    interface IStringEquation
    {
        String equation { get; }
        String[] arguments { get; }
        double calculate(params double[] args);
    }
    class MockSEQ : IStringEquationFactory
    {
        class ms : IStringEquation
        {
            public string[] arguments{ get; set; }
            public string equation{ get; set; }
            public double calculate(params double[] args)
            {
                return 0.0;
            }
        }
        public IStringEquation Create(string equation, params string[] args)
        {
            return new ms
            {
                arguments = args,
                equation = equation
            };
        }
    }
    #endregion

    // Global viewmovel for all diet inventors..move
    public class InventedTrackerVM : OriginatorVM
    {
        public String name { get; set; }
        public String description { get; set; }
    }

    #region Helpers (move?)
    // Helpers for more complex models here
    public static class GlobalForKeyTo
    {
        public static SQLiteConnection conn;
    }
    public class KeyableBaseDB : BaseDB
    {
        public int fk { get; set; }
        public int fk_mid { get; set; }
        public int fk_pid { get; set; }
    }
    public class KeyTo<T> where T : KeyableBaseDB
    {
        int pid, mid, id;
        public KeyTo(int id, int pid, int mid)
        {
            this.mid = mid; // id of model (class)  
            this.pid = pid; // id of prop on model
            this.id = id; // id of instance of model connecting to.
        }
        public IEnumerable<T> Get()
        {
            return GlobalForKeyTo.conn.Table<T>().Where(t => t.fk == id && t.fk_mid == mid && t.fk_pid == pid);
        }
        public void Remove(IEnumerable<T> values)
        {
            HashSet<int> pks = new HashSet<int>(from v in values select v.id);
            GlobalForKeyTo.conn.Table<T>().Delete(k => pks.Contains(k.id));
        }
        public void Add(IEnumerable<T> values)
        {
            // reset.
            foreach (var v in values)
            {
                v.fk = id;
                v.fk_mid = mid;
                v.fk_pid = pid;
                GlobalForKeyTo.conn.Insert(v);
            }
        }
        public void Replace(IEnumerable<T> values)
        {
            Clear();
            Add(values);
        }
        public void Clear()
        {
            GlobalForKeyTo.conn.Table<T>().Delete(t => t.fk_mid == mid && t.fk_pid == pid);
        }
    }
    #endregion

    /* 
     * In here:
     *  - simple inventor (constructed once by AppPresenter, registered as an Inventor)
     *  - model and requestpages to generate simple invented descriptors
     *  - Hooks each invented descriptor to AppPresenter as Trackers
     */

    #region Tracker descriptor created by the simple inventor
    class SimpleTrackyHelpyInventionV1Model : BaseDB
    {
        // helper
        public void DeleteAllForiegnKeyedThings()
        {
            qod_in.Clear();
            qod_out.Clear();
            targets.Clear();
            outequations.Clear();
            inequations.Clear();
        }

        const int keyto_id = 1;

        // Quantifier types foriegn relationship
        int qod_in_pid = 1, qod_out_pid = 2;
        KeyTo<SimpleTrackyInfoQuantifierDescriptor> _qod_out, _qod_in;
        public KeyTo<SimpleTrackyInfoQuantifierDescriptor> qod_in
        {
            get { return _qod_in ?? (_qod_in = new KeyTo<SimpleTrackyInfoQuantifierDescriptor>(id, qod_in_pid, keyto_id)); }
        }
        public KeyTo<SimpleTrackyInfoQuantifierDescriptor> qod_out
        {
            get { return _qod_out ?? (_qod_out = new KeyTo<SimpleTrackyInfoQuantifierDescriptor>(id, qod_out_pid, keyto_id)); }
        }

        // targets!
        int targets_pid = 3;
        KeyTo<SimpleTrackyTrackingTargetDescriptor> _targets;
        public KeyTo<SimpleTrackyTrackingTargetDescriptor> targets
        {
            get { return _targets ?? (_targets = new KeyTo<SimpleTrackyTrackingTargetDescriptor>(id, targets_pid, keyto_id)); }
        }
        public String target_args { get; set; } // comma seperated, used by targets equations.

        // Entries
        int inequations_pid = 4, outequations_pid = 5;
        KeyTo<SimpleTrackyTrackingEquationDescriptor> _inequations;
        public KeyTo<SimpleTrackyTrackingEquationDescriptor> inequations
        {
            get { return _inequations ?? (_inequations = new KeyTo<SimpleTrackyTrackingEquationDescriptor>(id, inequations_pid, keyto_id)); }
        }
        KeyTo<SimpleTrackyTrackingEquationDescriptor> _outequations;
        public KeyTo<SimpleTrackyTrackingEquationDescriptor> outequations
        {
            get { return _outequations ?? (_outequations = new KeyTo<SimpleTrackyTrackingEquationDescriptor>(id, outequations_pid, keyto_id)); }
        }
        public String in_equations_args { get; set; }
        public String out_equations_args { get; set; }

        // managment
        public Guid uid { get; set; }

        // descriptional
        public String Name { get; set; }
        public String Description { get; set; }
        public String Category { get; set; }

        // dialect
        public String InputEntryVerb { get; set; }
        public String OutputEntryVerb { get; set; }
        public String InputInfoPlural { get; set; }
        public String OutputInfoPlural { get; set; }
        public String InputInfoVerbPast { get; set; }
        public String OutputInfoVerbPast { get; set; }
    }
    class SimpleTrackyInfoQuantifierDescriptor : KeyableBaseDB
    {
        public InfoQuantifier.InfoQuantifierTypes type { get; set; }
        public double defaultvalue { get; set; }
        public String Name { get; set; }
    }
    class SimpleTrackyTrackingEquationDescriptor : KeyableBaseDB
    {
        public String targetID { get; set; }
        public String equation { get; set; }
    }
    class SimpleTrackyTrackingTargetDescriptor : KeyableBaseDB
    {
        public String targetID { get; set; }
        // note on equations, "1" is an equation

        // display config
        public String name { get; set; }
        public bool Track { get; set; } // can tracking be toggled in instance
        public bool Shown { get; set; } // shown on tracker list VM thingy or not

        // Aggregation calc
        public String targetRange { get; set; } // fixed or calc (either e.g. "1" or "12*arg2")
        public AggregateRangeType rangetype { get; set; } // fixed or user, no calc.

        // Target calc
        public String targertPattern { get; set; } // spec for below eg "1,2,1" (fixed) or "arg1*arg2+arg3, arg2" (from calc / args).  so the same.
        public String patternTarget { get; set; } // same for values - should match in count of above.
    }
    #endregion

    #region Generators to create the simple inventor request pages
    class SimpleTrackyInventionRequestPages
    {
        class Stringy
        {
            readonly String s;
            public readonly object o;
            public Stringy(String s, Object o)
            {
                this.s = s;
                this.o = o;
            }
            public override string ToString()
            {
                return s;
            }
        }

        private SimpleTrackyInventionRequestPages(GetValuesPage page, Action<SimpleTrackyHelpyInventionV1Model> set)
        {
            this.page = page;
            this.set = set;
        }

        public readonly GetValuesPage page;
        public readonly Action<SimpleTrackyHelpyInventionV1Model> set;


        public static SimpleTrackyInventionRequestPages TrackerDescriptionPage(IValueRequestFactory fac)
        {
            var page = new GetValuesPage("What's it called?");
            var p1vr = (from s in new[] { "Name", "Description", "Category" }
                        select fac.StringRequestor(s)).ToArray();
            page.SetList(new BindingList<object>((from p in p1vr select p.request).ToList()));
            Action<SimpleTrackyHelpyInventionV1Model> set = mod =>
            {
                //stuff
                mod.Name = p1vr[0].value;
                mod.Description = p1vr[1].value;
                mod.Category = p1vr[2].value;
            };
            foreach (var vr in p1vr) vr.ValueChanged += () => vr.valid = !String.IsNullOrWhiteSpace(vr.value);
            return new SimpleTrackyInventionRequestPages(page, set);
        }
        public static SimpleTrackyInventionRequestPages EntryDescriptionPage(IValueRequestFactory fac)
        {
            var page = new GetValuesPage("How are entries called?");
            var p2vr = (from s in new[] {
                        "InputEntryVerb","OutputEntryVerb","InputInfoPlural",
                        "OutputInfoPlural","InputInfoVerbPast","OutputInfoVerbPast" }
                        select fac.StringRequestor(s)).ToArray();
            page.SetList(new BindingList<object>((from p in p2vr select p.request).ToList()));
            Action<SimpleTrackyHelpyInventionV1Model> set = mod =>
            {
                mod.InputEntryVerb = p2vr[0].value;
                mod.OutputEntryVerb = p2vr[1].value;
                mod.InputInfoPlural = p2vr[2].value;
                mod.OutputInfoPlural = p2vr[3].value;
                mod.InputInfoVerbPast = p2vr[4].value;
                mod.OutputInfoVerbPast = p2vr[5].value;
            };
            foreach (var vr in p2vr) vr.ValueChanged += () => vr.valid = !String.IsNullOrWhiteSpace(vr.value); ;
            return new SimpleTrackyInventionRequestPages(page, set);
        }
        public static SimpleTrackyInventionRequestPages InfoQuantifiersPage(IValueRequestFactory fac)
        {
            var page = new GetValuesPage("How can infos be quantified?");
            var ioreq = fac.OptionGroupRequestor("For");
            ioreq.value = new OptionGroupValue(new[] { "Input", "Output" });
            var nreq = fac.StringRequestor("Name");
            var dvalmor = fac.IValueRequestOptionGroupRequestor("Type");

            String[] nams = new[] { "Number", "Quantity", "Duration" };
            var qnr = fac.DoubleRequestor(nams[0]);
            var qqr = fac.IntRequestor(nams[1]);
            var qdr = fac.TimeSpanRequestor(nams[2]);
            var vls = new Func<Object>[] { () => qnr.value, () => qqr.value, () => qdr.value };

            dvalmor.value = new MultiRequestOptionValue(new[] { qnr.request, qqr.request, qdr.request, }, 0);
            var sdef = new Func<Object, double>[] { d => (double)d, d => (double)(int)d, d => ((TimeSpan)d).TotalHours };
            var addr = fac.ActionRequestor("Add");

            String[] headers = new[] { "For", "Name", "Units", "Default" };
            var megalist = fac.GenerateTableRequest();
            megalist.value = new TabularDataRequestValue(headers);

            addr.ValueChanged += () =>
            {
                var so = ioreq.value.SelectedOption;
                var ot = (InfoQuantifier.InfoQuantifierTypes)dvalmor.value.SelectedRequest;
                var ov = vls[dvalmor.value.SelectedRequest]();
                megalist.value.Items.Add(new Stringy[] {
                        new Stringy(ioreq.value.OptionNames[so],so),
                        new Stringy(nreq.value,nreq.value),
                        new Stringy(nams[dvalmor.value.SelectedRequest],ot),
                        new Stringy(ov.ToString(),ov)
                    });
            };

            megalist.ValueChanged += () => megalist.valid = megalist.value.Items.Count > 0;

            page.SetList(new BindingList<object> { ioreq.request, nreq.request, dvalmor.request, addr.request, megalist.request });
            Action<SimpleTrackyHelpyInventionV1Model> set = mod =>
            {
                // get quantifiers
                int i = 0;
                List<SimpleTrackyInfoQuantifierDescriptor>
                    inq = new List<SimpleTrackyInfoQuantifierDescriptor>(),
                    outq = new List<SimpleTrackyInfoQuantifierDescriptor>();
                foreach (var q in megalist.value.Items)
                {
                    var qq = q as Stringy[];
                    var inout = (int)qq[0].o;
                    var name = (qq[1].o as String);
                    var qtype = (int)qq[2].o;
                    var qdef = qq[3].o;
                    var qd = new SimpleTrackyInfoQuantifierDescriptor { defaultvalue = sdef[qtype](qdef), id = i++, Name = name, type = (InfoQuantifier.InfoQuantifierTypes)qtype };
                    if (inout == 0) inq.Add(qd);
                    else outq.Add(qd);
                }
                mod.qod_in.Replace(inq);
                mod.qod_out.Replace(outq);
            };
            megalist.value.Items.CollectionChanged += (a, b) => megalist.valid = megalist.value.Items.Count > 0;
            addr.valid = nreq.valid = dvalmor.valid = ioreq.valid = true; // not used - the list is...
            page.SetListyRequest(megalist);
            return new SimpleTrackyInventionRequestPages(page, set);
        }
        public static SimpleTrackyInventionRequestPages TargetDesciptorsPage(IValueRequestFactory fac)
        {
            var page = new GetValuesPage("What targets should be calculated?");


            var nreq = fac.StringRequestor("Name");
            var tidreq = fac.StringRequestor("TargetID");
            var argreq = fac.StringRequestor("Equation arguments");
            var rreq = fac.StringRequestor("Target range equation");
            var rtreq = fac.OptionGroupRequestor("Target range Type");
            rtreq.value = new OptionGroupValue(new[] { "Days from start", "Days about now" });
            var peqreq = fac.StringRequestor("Pattern equations");
            var teqreq = fac.StringRequestor("Target equations");
            var addr = fac.ActionRequestor("Add");

            String[] headers = new[] { "Name", "Id", "Range", "Type", "Patterns", "Targets" };
            var megalist = fac.GenerateTableRequest();
            megalist.value = new TabularDataRequestValue(headers);

            addr.ValueChanged += () =>
            {
                var ot = (AggregateRangeType)rtreq.value.SelectedOption;
                var ots = rtreq.value.OptionNames[rtreq.value.SelectedOption];
                megalist.value.Items.Add(new Stringy[] {
                        new Stringy(nreq.value,nreq.value),
                        new Stringy(tidreq.value,tidreq.value),
                        new Stringy(rreq.value,rreq.value),
                        new Stringy(ots,ot),
                        new Stringy(peqreq.value,peqreq.value),
                        new Stringy(teqreq.value,teqreq.value),
                    });
            };

            // FIXME which one?
            megalist.ValueChanged += () => megalist.valid = megalist.value.Items.Count > 0;
            megalist.value.Items.CollectionChanged += (a, b) => megalist.valid = megalist.value.Items.Count > 0;

            page.SetList(new BindingList<object> { nreq, tidreq, rreq, rtreq, argreq, peqreq, teqreq });
            Action<SimpleTrackyHelpyInventionV1Model> set = mod =>
            {
                // get targets
                var targets = new List<SimpleTrackyTrackingTargetDescriptor>();
                foreach (var q in megalist.value.Items)
                {
                    var qq = q as Stringy[];
                    targets.Add(new SimpleTrackyTrackingTargetDescriptor {
                        name = qq[0].o as String,
                        targetRange = qq[1].o as String,
                        rangetype = (AggregateRangeType)qq[2].o,
                        targertPattern = qq[3].o as String,
                        patternTarget = qq[4].o as String,
                    });
                }
                mod.targets.Replace(targets);
                mod.target_args = argreq.value;
            };

            Action ValidateEquations = () =>
            {
                argreq.valid = !String.IsNullOrWhiteSpace(argreq.value);
                bool sameNumberOfEquations = peqreq.value.Split(',').Count() == teqreq.value.Split(',').Count();
                bool equationsAreValid = true;
                peqreq.valid = teqreq.valid = rreq.valid = sameNumberOfEquations && equationsAreValid;
            };

            addr.valid = nreq.valid = rtreq.valid = true; // not used - the list is...
            argreq.ValueChanged += ValidateEquations;
            rreq.ValueChanged += ValidateEquations;
            peqreq.ValueChanged += ValidateEquations;
            teqreq.ValueChanged += ValidateEquations;
            page.SetListyRequest(megalist);
            return new SimpleTrackyInventionRequestPages(page, set);
        }
        public static SimpleTrackyInventionRequestPages EntryInfoEquations(IValueRequestFactory fac)
        {
            var page = new GetValuesPage("What are the equations for entries?");

            // Overall
            var iargreq = fac.StringRequestor("In Equation arguments");
            var oargreq = fac.StringRequestor("Out Equation arguments");

            //each eq        
            var tidreq = fac.StringRequestor("TargetID");
            var ereq = fac.StringRequestor("Equation");
            var ioreq = fac.OptionGroupRequestor("For");
            var nams = new[] { "Input", "Output" };
            ioreq.value = new OptionGroupValue(nams);
            var addr = fac.ActionRequestor("Add");

            String[] headers = new[] { "Name", "For", "Equation" };
            var megalist = fac.GenerateTableRequest();
            megalist.value = new TabularDataRequestValue(headers);

            addr.ValueChanged += () =>
            {
                var inout = ioreq.value.SelectedOption;
                megalist.value.Items.Add(new Stringy[] {
                        new Stringy(tidreq.value,tidreq.value),
                        new Stringy(nams[inout],inout),
                        new Stringy(ereq.value,ereq.value),
                    });
            };

            

            page.SetList(new BindingList<object> { tidreq, ioreq, ereq });
            Action<SimpleTrackyHelpyInventionV1Model> set = mod =>
            {
                // get equations
                var iequations = new List<SimpleTrackyTrackingEquationDescriptor>();
                var oequations = new List<SimpleTrackyTrackingEquationDescriptor>();
                foreach (var q in megalist.value.Items)
                {
                    var qq = q as Stringy[];
                    var eq = new SimpleTrackyTrackingEquationDescriptor
                    {
                        targetID = qq[0].o as String,
                        equation = qq[2].o as string
                    };
                    if ((int)qq[1].o == 0) iequations.Add(eq);
                    else oequations.Add(eq);
                }
                mod.in_equations_args = iargreq.value;
                mod.out_equations_args = oargreq.value;
                mod.inequations.Replace(iequations);
                mod.outequations.Replace(oequations);
            };

            Action ValidateEquations = () =>
            {
                foreach(var argreq in new[] { iargreq, oargreq })
                    argreq.valid = !String.IsNullOrWhiteSpace(argreq.value);

                var mvi = megalist.value.Items;
                bool sameNumberOfEquations = mvi.Where(d => (int)d[1] == 0).Count() == mvi.Where(d => (int)d[1] == 1).Count();
                bool equationsMatchArgs = true;
                megalist.valid = sameNumberOfEquations && equationsMatchArgs && megalist.value.Items.Count > 0;
            };

            addr.valid = tidreq.valid = ioreq.valid = ereq.valid = true; // not used - the list is...
            // FIXME which one?
            megalist.ValueChanged += ValidateEquations;
            megalist.value.Items.CollectionChanged += (a, b) => ValidateEquations();

            page.SetListyRequest(megalist);
            return new SimpleTrackyInventionRequestPages(page, set);
        }
    }
    #endregion

    #region Simple inventor, instanced once, creates descriptors and trackers based on them, managing thier lifetime
    class SimpleTrackyHelpyInventionV1 : IViewModelObserver<InventedTrackerVM, SimpleTrackyHelpyInventionV1, EventArgs> // FIXME combines presentation and modelling since it's simple
    {
        readonly SQLiteConnection conn;
        readonly Presenter pres;
        readonly IValueRequestBuilder build;
        readonly IUserInput input;

        /// <summary>
        /// ... and here's how you can create more?
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="registerto"></param>
        public SimpleTrackyHelpyInventionV1(SQLiteConnection conn, Presenter registerto, IValueRequestBuilder builder, IUserInput input)
        {
            // appriase the presneter(it constructs us) of invented things.  Let it add/edit remove them.
            // behind the scenese, manage registration of tracker types through all that
            this.conn = conn;
            this.pres = registerto;
            this.build = builder;
            this.input = input;

            // helper
            GlobalForKeyTo.conn = conn;

            // ensure tables
            conn.CreateTable<SimpleTrackyInfoQuantifierDescriptor>();
            conn.CreateTable<SimpleTrackyTrackingTargetDescriptor>();
            conn.CreateTable<SimpleTrackyTrackingEquationDescriptor>();
            conn.CreateTable<SimpleTrackyHelpyInventionV1Model>();
        }

        // Here we pass viewmodels about invented trackers - the view can display and command them in a managing view
        // it still gets "select tracker" creating new from the main registry.
        #region helpy appresenter, notify viewmodels, commands to edit/add/remove
        // REMEMBER - dont do any actions - fire this TOCHANGED with a prior to complete the change. Presenter handles it.
        public event DietVMChangeEventHandler<SimpleTrackyHelpyInventionV1, EventArgs> ViewModelsToChange = delegate { };

        Dictionary<InventedTrackerVM, SimpleTrackyHelperCreator.Holdy> registered = new Dictionary<InventedTrackerVM, SimpleTrackyHelperCreator.Holdy>();

        // this can get spammed - it's a good oppertunity to provide diet registraions cause you'll get "busy"
        // status, but, need to protect against those spams case only need reg once
        IStringEquationFactory seq = new MockSEQ();
        public IEnumerable<InventedTrackerVM> Instances()
        {
            // Get Models - calls to this dude are by unwritten contract on worker threads.
            foreach (var it in conn.Table<SimpleTrackyHelpyInventionV1Model>())
            {
                var vm = Present(it);
                if (!registered.ContainsKey(vm))
                {
                    var pp = registered[vm] = SimpleTrackyHelperCreator.Create(it, seq);
                    pp.state = pres.AddDietPair(pp.model, pp.pres, build);
                }
                vm.sender = it;
                // set sender? :/
                yield return vm;
            }
        }

        public void RemoveTracker(InventedTrackerVM dvm, bool warn = true) // dereigster
        {
            // Get some info
            var handler = registered[dvm];
            int nTrackers = 0, nTotalEntries =0, nTotalInfos=0;
            handler.state.dal.CountAll(out nTrackers, out nTotalEntries, out nTotalInfos);

            // Define the removal action...
            Action Removal = () =>
            {
                // Pass to the TaskMapper (if you look) for the inventor to run in the pool...
                ViewModelsToChange(this, new DietVMToChangeEventArgs<EventArgs>
                {
                    //...which will run this when progress is showing etc
                    toChange = () =>
                    {
                        // the handler for the invented tracker is asked to delete everything (it will also call toChange of its own - 2x progress, one for trackers, one for inventor.
                        handler.state.dal.DeleteAll(() =>
                        {
                            // clear foreign keys on invented tracker
                            (dvm.sender as SimpleTrackyHelpyInventionV1Model).DeleteAllForiegnKeyedThings();
                            // AFTER that is done, we delete the inventor modelrow
                            conn.Delete(dvm.sender);
                            // and we deregister the whole thing from the apppresenter
                            handler.state.remove();
                        });
                        return null; // prior, but no post action (do nothing after this is all done)
                     },
                    changeType=  new EventArgs() // we dont have a changetype on this handler. just "it changed".
                });
            };

            // If we have some data, show a warning!
            if ((nTrackers + nTotalInfos + nTotalEntries > 0) && warn)
                input.WarnConfirm(
                    String.Format("That inventor still has {0} instances with {1} entries and {2} infos, they will be removed if you continue.",
                    nTrackers, nTotalEntries, nTotalInfos
                    ),
                    async () => await PlatformGlobal.Run(Removal) // warnconfirm needs a promise, so lazily do this. could wrap a TaskCompleteionSource instead in a helper or sth.
                );
            else Removal(); // otherwise, just go for it.
        }
        public Task StartNewTracker() // instances() handles registration
        {
            var page1 = SimpleTrackyInventionRequestPages.TrackerDescriptionPage(build.requestFactory);
            var page2 = SimpleTrackyInventionRequestPages.EntryDescriptionPage(build.requestFactory);
            var page3 = SimpleTrackyInventionRequestPages.InfoQuantifiersPage(build.requestFactory);
            var page4 = SimpleTrackyInventionRequestPages.TargetDesciptorsPage(build.requestFactory);
            var page5 = SimpleTrackyInventionRequestPages.EntryInfoEquations(build.requestFactory);

            var vt = build.GetValues(new[] { page1.page, page2.page, page3.page, page4.page, page5.page });
            vt.Completed.ContinueAfter(async () =>
            {
                await vt.Pop();
                if (vt.Completed.Result)
                {
                    ViewModelsToChange(
                        this,
                        new DietVMToChangeEventArgs<EventArgs>
                        {
                            changeType = new EventArgs(),
                            toChange = () =>
                            {
                                var mod = new SimpleTrackyHelpyInventionV1Model();
                                page1.set(mod);
                                page2.set(mod);
                                page3.set(mod);
                                page4.set(mod);
                                page5.set(mod);
                                conn.Insert(mod);
                                return null; // nothing after datachanages
                            }
                        }
                    );
                }
            });
            return vt.Pushed;
        }
        public Task EditTracker(InventedTrackerVM dvm)
        {
            var mod = dvm.originator as SimpleTrackyHelpyInventionV1Model;
            // these are safe to change
            var page1 = SimpleTrackyInventionRequestPages.TrackerDescriptionPage(build.requestFactory);
            var page2 = SimpleTrackyInventionRequestPages.EntryDescriptionPage(build.requestFactory);
            var vt = build.GetValues(new[] { page1.page, page2.page });
            vt.Completed.ContinueWith(async rt => {
                await vt.Pop();
                if (rt.Result)
                {
                    ViewModelsToChange(
                        this,
                        new DietVMToChangeEventArgs<EventArgs>
                        {
                            changeType = new EventArgs(),
                            toChange = () =>
                            {
                                page1.set(mod);
                                page2.set(mod);
                                conn.Update(mod);
                                return null;
                            }
                        }
                    );
                }
            });
            return vt.Pushed;
        }
        #endregion

        InventedTrackerVM Present(SimpleTrackyHelpyInventionV1Model model)
        {
            return new InventedTrackerVM() { originator = model, name = model.Name, description = model.Description };
        }
    }
    #endregion


    static class SimpleTrackyHelperCreator
    {
        class RoutedHelpyModel : SimpleTrackyHelpy<TrackerInstance, IInEntry, IInInfo, IOutEntry, IOutInfo>, IModelRouter
        {
            readonly SimpleTrackyHelpyInventionV1Model model;
            public RoutedHelpyModel(InventedTracker tmh, SimpleTrackyHelpyInventionV1Model model) : base(tmh)
            {
                var mu = model.uid.ToString();
                var tracked = model.targets.Get().Select(d => d.targetID).Distinct();
                var oi = 
                info = new Dictionary<Type, desc>
                {
                    { typeof(TrackerInstance), new desc(String.Format("{0}_entry_in", mu),model.target_args.Split(',')) },
                    { typeof(IInEntry), new desc(String.Format("{0}_entry_in", mu), tracked.ToArray() ) },
                    { typeof(IOutEntry), new desc(String.Format("{0}_entry_out", mu), tracked.ToArray() ) },
                    { typeof(IInInfo), new desc(String.Format("{0}_info_in", mu), model.in_equations_args.Split(',') ) },
                    { typeof(IOutInfo), new desc(String.Format("{0}_info_out", mu), model.out_equations_args.Split(',') ) },
                };
            }

            readonly Dictionary<Type, desc> info;
            class desc
            {
                public String table; public String[] cols; public Type[] types;
                public desc(String tn, String[] args)
                {
                    table = tn;
                    types = Enumerable.Repeat<Type>(typeof(double), args.Length).ToArray();
                    cols = args;
                }
            }

            public bool GetTableRoute<T>(out string tabl, out string[] columns, out Type[] colTypes)
            {
                desc dsc = info?[typeof(T)];
                tabl = dsc?.table;
                columns = dsc?.cols;
                colTypes = dsc?.types;
                return dsc != null;
            }

        }

        public class IInInfo : HBaseInfo { }
        public class IInEntry : HBaseEntry { }
        public class IOutInfo : HBaseInfo { }
        public class IOutEntry : HBaseEntry { }
        class InventedTracker : IExtraRelfectedHelpy<TrackerInstance, IInInfo, IOutInfo>
        {
            static String NiceArgName(String arg)
            {
                return arg[0].ToString().ToUpper() 
                    + arg.Substring(1).Replace('_', ' ');
            }
            static Func<Object,T> ArgGet<T>(String name)
            {
                return o => (T)((BaseDB)o).AdHoc[name];
            }
            static Action<Object, T> ArgSet<T>(String name)
            {
                return (o,v) => ((BaseDB)o).AdHoc[name] = v;
            }
            static Func<Object, double> InfoGet(String name)
            {
                return o => (double?)((BaseDB)o).AdHoc[name] ?? 0.0;
            }
            static Action<Object, double> InfoSet(String name)
            {
                return (o, v) => ((BaseDB)o).AdHoc[name] = v;
            }
            static InstanceValue<double> NSet(String n)
            {
                return new InstanceValue<double>(NiceArgName(n), ArgGet<double>(n), ArgSet<double>(n), 0.0);
            }

            public TrackerDetailsVM TrackerDetails { get; private set; }
            public TrackerDialect TrackerDialect { get; private set; }
            public VRVConnectedValue[] instanceValueFields { get; private set; }
            public IReflectedHelpyQuants<IInInfo> input { get; private set; }
            public IReflectedHelpyQuants<IOutInfo> output { get; private set; }

            class TargetEquationsRet
            {
                public SimpleTrackyTrackingTargetDescriptor des;
                public IStringEquation range_equation;
                public IStringEquation[] target_patterns;
                public IStringEquation[] pattern_targets;
            }
            static TargetEquationsRet TargetEquations(SimpleTrackyTrackingTargetDescriptor des, IStringEquationFactory seq,String[]Args)
            {
                var n = 0;
                var peq = des.targertPattern.Split(',');
                var teq = des.patternTarget.Split(',');
                Debug.Assert((n=peq.Length) == teq.Length, "Error in pattern equation lengths");

                var ret = new TargetEquationsRet
                {
                    des = des,
                    range_equation = seq.Create(des.targetRange, Args),
                    target_patterns = new IStringEquation[n],
                    pattern_targets = new IStringEquation[n]
                };

                for (int i = 0; i < n; i++)
                {
                    ret.target_patterns[i] = seq.Create(peq[i], Args);
                    ret.pattern_targets[i] = seq.Create(teq[i], Args);
                }

                return ret;
            }
            readonly TargetEquationsRet[] targets;
            public InventedTracker(SimpleTrackyHelpyInventionV1Model model, IStringEquationFactory seq)
            {
                
                TrackerDetails = new TrackerDetailsVM(model.Name, model.Description, model.Category);
                TrackerDialect = new TrackerDialect(
                    model.InputEntryVerb, model.OutputEntryVerb,
                    model.InputInfoPlural, model.OutputInfoPlural,
                    model.InputInfoVerbPast, model.OutputInfoVerbPast);

                // Args used in target equations for a tracker
                var TargetArgs = model.target_args.Split(',');

                // cached the equaion espression thing
                targets = (from des in model.targets.Get() select TargetEquations(des, seq, TargetArgs)).ToArray();

                instanceValueFields = 
                    (
                        from m in TargetArgs select VRVConnectedValue.FromType(
                        0.0, NiceArgName(m), ArgGet<Object>(m), ArgSet<Object>(m), d => d.DoubleRequestor)
                    ).ToArray();

                var ia = model.in_equations_args.Split(',');
                var oa = model.out_equations_args.Split(',');
                input = new ITT<IInInfo>
                {
                    quantifier_choices = GetIQ(model.qod_in),
                    InfoComplete = GenIC<IInInfo>(ia),
                    calculation = (from a in ia
                                   select new InstanceValue<double>(NiceArgName(a),
                                   InfoGet(a), InfoSet(a), 0.0)).ToArray(),
                    calculators = (from e in model.inequations.Get() select
                                       new ITTe
                                       {
                                           TargetID = e.targetID,
                                           equation = seq.Create(e.equation, ia),
                                           direct = NSet(e.targetID)
                                       }).ToArray()
                };
                output = new ITT<IOutInfo>
                {
                    quantifier_choices = GetIQ(model.qod_out),
                    InfoComplete = GenIC<IOutInfo>(oa),
                    calculation = (from a in oa
                                   select new InstanceValue<double>(NiceArgName(a),
                                   InfoGet(a), InfoSet(a), 0.0)).ToArray(),
                    calculators = (from e in model.outequations.Get() select
                                        new ITTe
                                        {
                                            TargetID = e.targetID,
                                            equation = seq.Create(e.equation, oa),
                                            direct = NSet(e.targetID)
                                        }).ToArray()
                };
            }

            public SimpleTrackyTarget[] Calcluate(object[] fieldValues)
            {
                var dv = (from f in fieldValues select f is double ? (double)f : 0.0).ToArray();
                return (from t in targets
                 select new SimpleTrackyTarget(
                     t.des.name, t.des.targetID, t.des.Track, t.des.Shown,
                     (int)t.range_equation.calculate(dv), t.des.rangetype,
                     (from s in t.target_patterns select s.calculate(dv)).Cast<int>().ToArray(),
                     (from s in t.pattern_targets select s.calculate(dv)).ToArray())
                     ).ToArray();
            }
        }
        class ITT<T> : IReflectedHelpyQuants<T> where T : HBaseInfo
        {
            public InstanceValue<double>[] calculation { get; set; }
            public Expression<Func<T, bool>> InfoComplete { get; set; }
            public InfoQuantifier[] quantifier_choices { get; set; }
            public IReflectedHelpyCalc[] calculators { get; set; }
        }
        class ITTe : IReflectedHelpyCalc
        {
            public InstanceValue<double> direct { get; set; }
            public string TargetID { get; set; } 
            public IStringEquation equation { get; set; }
            public double Calculate(double[] values) { return equation.calculate(values); }
        }

        public class Holdy
        {
            public Presenter.AddDietPairState state;
            public ITrackModel<TrackerInstance, IInEntry, IInInfo, IOutEntry, IOutInfo> model;
            public ITrackerPresenter<TrackerInstance, IInEntry, IInInfo, IOutEntry, IOutInfo> pres;
        }

        static InfoQuantifier[] GetIQ(KeyTo<SimpleTrackyInfoQuantifierDescriptor> k2)
        {
            return (from q in k2.Get()
                    select HelpyInfoQuantifier.FromType(q.type, q.Name, q.id,
                    q.defaultvalue)).ToArray();
        }
        static Expression<Func<T,bool>> GenIC<T>(String[] args) where T : BaseDB
        {
            return d => args.All(s => d.AdHoc[s] != null);
        }

        public static Holdy Create(SimpleTrackyHelpyInventionV1Model model, IStringEquationFactory exp)
        {
            var irh = new InventedTracker(model, exp);
            return new Holdy { model = new RoutedHelpyModel(irh, model), pres = new SimpleTrackyHelpyPresenter<TrackerInstance, IInEntry, IInInfo, IOutEntry, IOutInfo>(irh) };
        }
    }
}
