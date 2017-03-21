using Consonance.Protocol;
using LibRTP;
using LibSharpHelp;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Consonance.Presenter;

namespace Consonance.Invention
{
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
    class DataTableSEQ : IStringEquationFactory
    {
        class ms : IStringEquation
        {
            NCalc.Expression exp;
            public ms(String eq, String[] args)
            {
                equation = eq;
                
                exp = new NCalc.Expression(eq);
                arguments = args;
            }
            
            public string[] arguments{ get; set; }
            public string equation{ get; set; }
            public double calculate(params double[] args)
            {
                for (int i = 0; i < args.Length; i++)
                    exp.Parameters[arguments[i]] = args[i];
                var res = exp.Evaluate();
                var dres = Convert.ToDouble(res);
                return dres;
            }
        }
        public IStringEquation Create(string equation, params string[] args)
        {
            return new ms(equation, args);
        }
    }
    #endregion

    // Global viewmovel for all diet inventors..move
    public class InventedTrackerVM : OriginatorVM
    {
        public String name { get; set; }
        public String description { get; set; }
    }

    public class IInInfo : HBaseInfo { }
    public class IInEntry : HBaseEntry { }
    public class IOutInfo : HBaseInfo { }
    public class IOutEntry : HBaseEntry { }

    /* 
     * In here:
     *  - simple inventor (constructed once by AppPresenter, registered as an Inventor)
     *  - model and requestpages to generate simple invented descriptors
     *  - Hooks each invented descriptor to AppPresenter as Trackers
     */


    #region Tracker descriptor created by the simple inventor
    [TableIdentifier(4)]
    class SimpleTrackyHelpyInventionV1Model : BaseDB, IPrimaryKey
    {
        public static IDAL GDal;
        public SimpleTrackyHelpyInventionV1Model()
        {
            qod_in = GDal.CreateOneToMany<SimpleTrackyHelpyInventionV1Model, SimpleTrackyInfoQuantifierDescriptor>(this, 1);
            qod_out = GDal.CreateOneToMany<SimpleTrackyHelpyInventionV1Model, SimpleTrackyInfoQuantifierDescriptor>(this, 2);
            targets = GDal.CreateOneToMany<SimpleTrackyHelpyInventionV1Model, SimpleTrackyTrackingTargetDescriptor>(this, 3);
            inequations = GDal.CreateOneToMany<SimpleTrackyHelpyInventionV1Model, SimpleTrackyTrackingEquationDescriptor>(this,4);
            outequations = GDal.CreateOneToMany<SimpleTrackyHelpyInventionV1Model, SimpleTrackyTrackingEquationDescriptor>(this, 5);
        }

        // helper
        [Ignore]
        public IEnumerable<IKeyTo> Keyed
        {
            get
            {
                yield return qod_in;
                yield return qod_out;
                yield return targets;
                yield return inequations;
                yield return outequations;
            }
        }

        // Quantifier types foriegn relationship
        public IKeyTo<SimpleTrackyInfoQuantifierDescriptor> qod_in, qod_out;
        
        // targets!
        public IKeyTo<SimpleTrackyTrackingTargetDescriptor> targets;
        public String target_args { get; set; } // comma seperated, used by targets equations.

        // Entries
        public IKeyTo<SimpleTrackyTrackingEquationDescriptor> inequations, outequations;
        public String in_equations_args { get; set; }
        public String out_equations_args { get; set; }

        // descriptional
        public Guid uid { get; set; }
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
    [TableIdentifier(2)]
    class SimpleTrackyTrackingEquationDescriptor : BaseDB, IPrimaryKey
    {
        public String targetID { get; set; }
        public String equation { get; set; }
    }
    [TableIdentifier(3)]
    class SimpleTrackyTrackingTargetDescriptor : BaseDB, IPrimaryKey
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
            page.SetList(new ObservableCollection<object>((from p in p1vr select p.request).ToList()));
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
            page.SetList(new ObservableCollection<object>((from p in p2vr select p.request).ToList()));
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
        public static SimpleTrackyInventionRequestPages InfoQuantifiersPage(IValueRequestFactory fac, InfoFinderHelper iHelper)
        {
            var page = new GetValuesPage("How can infos be quantified?");
            var ioreq = fac.OptionGroupRequestor("For");
            ioreq.value = new OptionGroupValue(new[] { "Input", "Output" });
            var nreq = fac.StringRequestor("Name");
            var dvalmor = fac.IValueRequestOptionGroupRequestor("Type");

            String[] nams = new[] { "Number", "Duration" };
            var qnr = fac.DoubleRequestor(nams[0]);
            var qdr = fac.TimeSpanRequestor(nams[1]);
            var vls = new Func<Object>[] { () => qnr.value, () => qdr.value };

            dvalmor.value = new MultiRequestOptionValue(new[] { qnr.request, qdr.request, }, 0);
            var sdef = new Func<Object, double>[] { d => (double)d, d => (double)(int)d, d => ((TimeSpan)d).TotalHours };
            var addr = fac.ActionRequestor("Add");

            String[] headers = new[] { "For", "Name", "Units", "Default" };
            var megalist = fac.GenerateTableRequest();
            megalist.value = new TabularDataRequestValue(headers);

            addr.ValueChanged += () =>
            {
                var so = ioreq.value.SelectedOption;
                var ot = (InfoQuantifierTypes)dvalmor.value.SelectedRequest;
                var ov = vls[dvalmor.value.SelectedRequest]();
                megalist.value.Items.Add(new Stringy[] {
                        new Stringy(ioreq.value.OptionNames[so],so),
                        new Stringy(nreq.value,nreq.value),
                        new Stringy(nams[dvalmor.value.SelectedRequest],ot),
                        new Stringy(ov.ToString(),ov)
                    });
            };

            megalist.ValueChanged += () => megalist.valid = megalist.value.Items.Count > 0;

            page.SetList(new ObservableCollection<object> { ioreq.request, nreq.request, dvalmor.request, addr.request, megalist.request });
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
                    var qd = new SimpleTrackyInfoQuantifierDescriptor { defaultvalue = sdef[qtype](qdef), Name = name, quantifier_type = (InfoQuantifierTypes)qtype };
                    qd = iHelper.Find(qd) ?? qd; // is this degenerate with another quantifier?
                    if (inout == 0) inq.Add(qd);
                    else outq.Add(qd);
                }
                mod.qod_in.Replace(inq);
                mod.qod_out.Replace(outq);
            };
            megalist.value.Items.CollectionChanged += (a, b) => megalist.valid = megalist.value.Items.Count > 0;
            addr.valid = nreq.valid = dvalmor.valid = ioreq.valid = true; // not used - the list is...
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

            page.SetList(new ObservableCollection<object> { nreq.request, tidreq.request, rreq.request, rtreq.request, argreq.request, peqreq.request, teqreq.request, addr.request, megalist.request });
            Action<SimpleTrackyHelpyInventionV1Model> set = mod =>
            {
                // get targets
                var targets = new List<SimpleTrackyTrackingTargetDescriptor>();
                foreach (var q in megalist.value.Items)
                {
                    var qq = q as Stringy[];
                    targets.Add(new SimpleTrackyTrackingTargetDescriptor {
                        name = qq[0].o as String,
                        targetID = qq[1].o as String,
                        targetRange = qq[2].o as String,
                        rangetype = (AggregateRangeType)qq[3].o,
                        targertPattern = qq[4].o as String,
                        patternTarget = qq[5].o as String,
                        Shown=true,
                        Track=true
                    });
                }
                mod.targets.Replace(targets);
                mod.target_args = argreq.value;
            };

            Action ValidateEquations = () =>
            {
                argreq.valid = !String.IsNullOrWhiteSpace(argreq.value);
                bool sameNumberOfEquations = peqreq.value?.Split(',')?.Count() == teqreq.value?.Split(',')?.Count();
                bool equationsAreValid = true;
                peqreq.valid = teqreq.valid = rreq.valid = sameNumberOfEquations && equationsAreValid;
                tidreq.valid = !string.IsNullOrWhiteSpace(tidreq.value);
                nreq.valid = !string.IsNullOrWhiteSpace(nreq.value);
                addr.valid = nreq.valid && tidreq.valid && peqreq.valid && teqreq.valid && rreq.valid;
            };

            rtreq.valid = true; // always good
            addr.valid = false; // not to start

            tidreq.ValueChanged += ValidateEquations;
            nreq.ValueChanged += ValidateEquations;
            argreq.ValueChanged += ValidateEquations;
            rreq.ValueChanged += ValidateEquations;
            peqreq.ValueChanged += ValidateEquations;
            teqreq.ValueChanged += ValidateEquations;
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
                var s1 = new Stringy(tidreq.value, tidreq.value);
                var s2 = new Stringy(nams[inout], inout);
                var s3 = new Stringy(ereq.value, ereq.value);
                megalist.value.Items.Add(new Stringy[] {s1,s2,s3});
            };
            
            page.SetList(new ObservableCollection<object> { iargreq.request, oargreq.request, tidreq.request, ioreq.request, ereq.request, addr.request, megalist.request });
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
                Func<Object,int> sing = si => (int)(si as Stringy[])[1].o;
                bool sameNumberOfEquations = mvi.Where(d => sing(d) == 0).Count() == mvi.Where(d => sing(d) == 1).Count();
                bool equationsMatchArgs = true;
                megalist.valid = sameNumberOfEquations && equationsMatchArgs && megalist.value.Items.Count > 0;
            };

            addr.valid = tidreq.valid = ioreq.valid = ereq.valid = true; // not used - the list is...
            // FIXME which one?
            megalist.ValueChanged += ValidateEquations;
            megalist.value.Items.CollectionChanged += (a, b) => ValidateEquations();

            return new SimpleTrackyInventionRequestPages(page, set);
        }
    }
    #endregion

    #region Simple inventor, instanced once, creates descriptors and trackers based on them, managing thier lifetime
    class SimpleTrackyHelpyInventionV1 : IViewModelObserver<InventedTrackerVM, SimpleTrackyHelpyInventionV1, EventArgs>
        // FIXME combines presentation and modelling since it's simple
    {
        readonly Presenter pres;
        readonly IValueRequestBuilder build;
        readonly IUserInput input;
        readonly IDAL conn;
        readonly InfoFinderHelper iHelper;

        /// <summary>
        /// ... and here's how you can create more?
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="registerto"></param>
        public SimpleTrackyHelpyInventionV1(IDAL conn, Presenter registerto, IValueRequestBuilder builder, IUserInput input)
        {
            // appriase the presneter(it constructs us) of invented things.  Let it add/edit remove them.
            // behind the scenese, manage registration of tracker types through all that
            this.pres = registerto;
            this.build = builder;
            this.input = input;
            this.conn = conn;

            // ensure tables
            conn.CreateTable<SimpleTrackyInfoQuantifierDescriptor>();
            conn.CreateTable<SimpleTrackyTrackingTargetDescriptor>();
            conn.CreateTable<SimpleTrackyTrackingEquationDescriptor>();
            conn.CreateTable<SimpleTrackyHelpyInventionV1Model>();

            iHelper = new InfoFinderHelper(conn, delegate { });
            SimpleTrackyHelpyInventionV1Model.GDal = conn;
        }

        // Here we pass viewmodels about invented trackers - the view can display and command them in a managing view
        // it still gets "select tracker" creating new from the main registry.
        #region helpy appresenter, notify viewmodels, commands to edit/add/remove
        // REMEMBER - dont do any actions - fire this TOCHANGED with a prior to complete the change. Presenter handles it.
        public event DietVMChangeEventHandler<SimpleTrackyHelpyInventionV1, EventArgs> ViewModelsToChange = delegate { };

        Dictionary<InventedTrackerVM, SimpleTrackyHelperCreator.Holdy> registered = new Dictionary<InventedTrackerVM, SimpleTrackyHelperCreator.Holdy>();

        // this can get spammed - it's a good oppertunity to provide diet registraions cause you'll get "busy"
        // status, but, need to protect against those spams case only need reg once
        IStringEquationFactory seq = new DataTableSEQ();
        public IEnumerable<InventedTrackerVM> Instances()
        {
            // Get Models - calls to this dude are by unwritten contract on worker threads.
            foreach (var it in conn.Get<SimpleTrackyHelpyInventionV1Model>())
            {
                var vm = Present(it);
                if (!registered.ContainsKey(vm))
                {
                    var pp = registered[vm] = SimpleTrackyHelperCreator.Create(it, seq);
                    pp.state = pres.AddDietPair(pp, build, conn);
                }
                vm.sender = this;
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
                // the handler for the invented tracker is asked to delete everything (it will also call toChange of its own - 2x progress, one for trackers, one for inventor.
                handler.state.dal.DeleteAll(() =>
                {
                    // drop the tables now
                    handler.state.dal.DeleteAll(null, true);

                    // After thats done, we've got another tochange to fire here, for the inventor which is ready to kill now.
                    ViewModelsToChange(this, new DietVMToChangeEventArgs<EventArgs> { 
                        changeType = new EventArgs(),
                        toChange = () =>
                        {
                            var o = (dvm.originator as SimpleTrackyHelpyInventionV1Model);
                            // clear foreign keys on invented tracker
                            o.Keyed.Act(k=>k.Clear());
                            // AFTER that is done, we delete the inventor modelrow
                            conn.Delete<SimpleTrackyHelpyInventionV1Model>(d => d.id == o.id);
                            // and we deregister the whole thing from the apppresenter
                            handler.state.Deregister();
                            return null;
                        }
                    });
                }, false);
            };

            // If we have some data, show a warning!
            if ((nTrackers + nTotalInfos + nTotalEntries > 0) && warn)
                input.WarnConfirm(
                    String.Format("That inventor still has {0} instances with {1} entries and {2} infos, they will be removed if you continue.",
                    nTrackers, nTotalEntries, nTotalInfos
                    )
                ).Result.ContinueWith(t => { if (t.Result) PlatformGlobal.Run(Removal); }); 
            else Removal(); // otherwise, just go for it.
        }
        public Task StartNewTracker() // instances() handles registration
        {
            var page1 = SimpleTrackyInventionRequestPages.TrackerDescriptionPage(build.requestFactory);
            var page2 = SimpleTrackyInventionRequestPages.EntryDescriptionPage(build.requestFactory);
            var page3 = SimpleTrackyInventionRequestPages.InfoQuantifiersPage(build.requestFactory, iHelper);
            var page4 = SimpleTrackyInventionRequestPages.TargetDesciptorsPage(build.requestFactory);
            var page5 = SimpleTrackyInventionRequestPages.EntryInfoEquations(build.requestFactory);

            var vt = build.GetValues(new[] { page1.page, page2.page, page3.page, page4.page, page5.page });
            return vt.ContinueWith(t =>
            {
                if (vt.Result)
                {
                    ViewModelsToChange(
                        this,
                        new DietVMToChangeEventArgs<EventArgs>
                        {
                            changeType = new EventArgs(),
                            toChange = () =>
                            {
                                var mod = new SimpleTrackyHelpyInventionV1Model
                                {
                                    uid = Guid.NewGuid()
                                };
                                page1.set(mod);
                                page2.set(mod);
                                page3.set(mod);
                                page4.set(mod);
                                page5.set(mod);
                                conn.Commit(mod);
                                mod.Keyed.Act(k => k.Commit()); // commit all foreign keyed stuff (now has rowid)
                                return null; // nothing after datachanages
                            }
                        }
                    );
                }
            });
        }
        public Task EditTracker(InventedTrackerVM dvm)
        {
            var mod = dvm.originator as SimpleTrackyHelpyInventionV1Model;
            // these are safe to change
            var page1 = SimpleTrackyInventionRequestPages.TrackerDescriptionPage(build.requestFactory);
            var page2 = SimpleTrackyInventionRequestPages.EntryDescriptionPage(build.requestFactory);
            var vt = build.GetValues(new[] { page1.page, page2.page });
            return vt.ContinueWith(rt => {
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
                                conn.Commit(mod); // no fk stuff to commit.
                                return null;
                            }
                        }
                    );
                }
            });
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
            public RoutedHelpyModel(InventedTracker tmh, SimpleTrackyHelpyInventionV1Model model) : base(tmh)
            {
                var mu = model.uid.ToString();
                var tracked = model.targets.Get().Select(d => d.targetID).Distinct();
                info = new Dictionary<Type, desc>
                {
                    { typeof(TrackerInstance), new desc(String.Format("{0}_instance", mu),model.target_args.Split(','), model.id*5) },
                    { typeof(IInEntry), new desc(String.Format("{0}_entry_in", mu), tracked.ToArray() ,model.id*5+1) },
                    { typeof(IOutEntry), new desc(String.Format("{0}_entry_out", mu), tracked.ToArray(),model.id*5+2 ) },
                    { typeof(IInInfo), new desc(String.Format("{0}_info_in", mu), model.in_equations_args.Split(','),model.id*5+3 ) },
                    { typeof(IOutInfo), new desc(String.Format("{0}_info_out", mu), model.out_equations_args.Split(',') ,model.id*5+4) },
                };
            }

            public bool GetTableIdentifier<T>(out int id)
            {
                desc dsc = info?[typeof(T)];
                id = dsc?.fmid ?? 0;
                return dsc != null;
            }

            readonly Dictionary<Type, desc> info;
            class desc
            {
                public int fmid;
                public String table; public String[] cols; public Type[] types;
                public desc(String tn, String[] args, int fmid)
                {
                    this.fmid = fmid;
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

        class InventedTracker : IExtraRelfectedHelpy<TrackerInstance, IInInfo, IOutInfo>
        {
            static String NiceArgName(String arg)
            {
                return arg[0].ToString().ToUpper()
                    + arg.Substring(1).Replace('_', ' ');
            }
            static Func<Object, T> ArgGet<T>(String name)
            {
                return o => (T)((BaseDB)o)[name];
            }
            static Action<Object, T> ArgSet<T>(String name)
            {
                return (o, v) => ((BaseDB)o)[name] = v;
            }
            static Func<Object, double> InfoGet(String name)
            {
                return o => (double?)((BaseDB)o)[name] ?? 0.0;
            }
            static Action<Object, double> InfoSet(String name)
            {
                return (o, v) => ((BaseDB)o)[name] = v;
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
            static TargetEquationsRet TargetEquations(SimpleTrackyTrackingTargetDescriptor des, IStringEquationFactory seq, String[] Args)
            {
                var n = 0;
                var peq = des.targertPattern.Split(',');
                var teq = des.patternTarget.Split(',');
                Debug.Assert((n = peq.Length) == teq.Length, "Error in pattern equation lengths");

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

                TrackerDetails = new TrackerDetailsVM(model.uid, model.Name, model.Description, model.Category);
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
                        0.0, Validate, NiceArgName(m), ArgGet<Object>(m), ArgSet<Object>(m), d => d.DoubleRequestor)
                    ).ToArray();

                var ia = model.in_equations_args.Split(',');
                var oa = model.out_equations_args.Split(',');
                input = new ITT<IInInfo>
                {
                    quantifier_choices = model.qod_in.Get().ToArray(),
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
                    quantifier_choices = model.qod_out.Get().ToArray(),
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

            public bool Validate(object[] fv)
            {
                var dv = (from f in fv select f is double ? (double)f : 0.0).ToArray();
                foreach (var t in targets)
                    if ((from s in t.target_patterns select (int)s.calculate(dv)).Sum() <= 0)
                        return false;
                return true;
            }

            public SimpleTrackyTarget[] Calcluate(object[] fieldValues)
            {
                var dv = (from f in fieldValues select f is double ? (double)f : 0.0).ToArray();
                return (from t in targets
                        select new SimpleTrackyTarget(
                            t.des.name, t.des.targetID, t.des.Track, t.des.Shown,
                            (int)t.range_equation.calculate(dv), t.des.rangetype,
                            (from s in t.target_patterns select (int)s.calculate(dv)).ToArray(),
                            (from s in t.pattern_targets select s.calculate(dv)).ToArray())
                     ).ToArray();
            }
        }
        class ITT<T> : IReflectedHelpyQuants<T> where T : HBaseInfo
        {
            public InstanceValue<double>[] calculation { get; set; }
            public Expression<Func<T, bool>> InfoComplete { get; set; }
            public SimpleTrackyInfoQuantifierDescriptor[] quantifier_choices { get; set; }
            public IReflectedHelpyCalc[] calculators { get; set; }
        }
        class ITTe : IReflectedHelpyCalc
        {
            public InstanceValue<double> direct { get; set; }
            public string TargetID { get; set; }
            public IStringEquation equation { get; set; }
            public double Calculate(double[] values) { return equation.calculate(values); }
        }

        public class Holdy : SimpleTrackerHolder<TrackerInstance, IInEntry, IInInfo, IOutEntry, IOutInfo>
        {
            public Presenter.AddDietPairState state;
            public SimpleTrackyHelpy<TrackerInstance, IInEntry, IInInfo, IOutEntry, IOutInfo> mod { set { _model = value; } }
            public SimpleTrackyHelpyPresenter<TrackerInstance, IInEntry, IInInfo, IOutEntry, IOutInfo> pre { set { _presenter = value; } }
        }

        static Expression<Func<T,bool>> GenIC<T>(String[] args) where T : BaseDB
        {
            return d => args.All(s => d[s] != null);
        }

        public static Holdy Create(SimpleTrackyHelpyInventionV1Model model, IStringEquationFactory exp)
        {
            var irh = new InventedTracker(model, exp);
            return new Holdy
            {
                mod = new RoutedHelpyModel(irh, model),
                pre = new SimpleTrackyHelpyPresenter<TrackerInstance, IInEntry, IInInfo, IOutEntry, IOutInfo>(irh)
            };
        }
    }
}
