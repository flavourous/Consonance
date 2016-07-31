using LibRTP;
using LibSharpHelp;
using SQLite.Net;
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Consonance.Invention
{
    // higer scope stuff
    class TrackerTypesContainer
    {
        public Type instance, input, output, inputinfo, outputinfo;
    }

    public class InventedTrackerVM : OriginatorVM
    {
        public String name { get; set; }
        public String description { get; set; }
    }

    public class KeyTo<T> where T : BaseDB
    {
        public static SQLiteConnection conn;
        readonly Expression<Func<T, bool>> match;
        readonly Action<T> set;
        public KeyTo(Expression<Func<T, bool>> match, Action<T> set)
        {
            this.match = match;
            this.set = set;
        }
        public IEnumerable<T> Get()
        {
            return conn.Table<T>().Where(match);
        }
        public void Set(IEnumerable<T> values)
        {
            // reset.
            conn.Table<T>().Delete(match);
            foreach (var v in values) set(v);
            conn.InsertAll(values);
        }
    }

    // model stuff
    class SimpleTrackyHelpyInventionV1Model : BaseDB
    {
        // functional
        public String tracked { get; set; }

        KeyTo<SimpleTrackyInfoQuantifierDescriptor> _qod_in;
        public KeyTo<SimpleTrackyInfoQuantifierDescriptor> qod_in
        {
            get
            {
                return _qod_in ?? new KeyTo<SimpleTrackyInfoQuantifierDescriptor>
                (s => s.helpymodel == id && s.helpyproperty == 0,
                s => { s.helpymodel = id; s.helpyproperty = 0; });
            }
        }
        KeyTo<SimpleTrackyInfoQuantifierDescriptor> _qod_out;
        public KeyTo<SimpleTrackyInfoQuantifierDescriptor> qod_out
        {
            get
            {
                return _qod_out ?? new KeyTo<SimpleTrackyInfoQuantifierDescriptor>
                (s => s.helpymodel == id && s.helpyproperty == 1,
                s => { s.helpymodel = id; s.helpyproperty = 1; });
            }
        }

        // managment
        public String tablename_tracker { get; set; }
        public String tablename_input { get; set; }
        public String tablename_inputinfo { get; set; }
        public String tablename_output { get; set; }
        public String tablename_outputinfo { get; set; }

        // descriptional
        public String Name { get; set; }
        public String Description { get; set; }
        public String Category { get; set; }
        public String TrackedName { get; set; }

        // dialect
        public String InputEntryVerb { get; set; }
        public String OutputEntryVerb { get; set; }
        public String InputInfoPlural { get; set; }
        public String OutputInfoPlural { get; set; }
        public String InputInfoVerbPast { get; set; }
        public String OutputInfoVerbPast { get; set; }
    }
    

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class ForeignKeyAttribute : Attribute
    {
        public Type To { get; set; }
    }

    class SimpleTrackyInfoQuantifierDescriptor : BaseDB
    {
        

        // relation
        public int helpymodel { get; set; }
        public int helpyproperty { get; set; }

        public InfoQuantifier.InfoQuantifierTypes type { get; set; }
        public double defaultvalue { get; set; }
        public String Name { get; set; }
    }

    /// <summary>
    /// Lowest level definition, inclding:
    ///  - tracked name.
    ///  - in-info and out-info options
    ///  - descriptions
    ///  
    /// Calls registration method with types generated in constructor. But itself is not generic.
    /// 
    /// Also lets you make and edit them.
    /// </summary>
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
            KeyTo<BaseDB>.conn = conn;

            // ensure tables
            conn.CreateTable<SimpleTrackyInfoQuantifierDescriptor>();
            conn.CreateTable<SimpleTrackyHelpyInventionV1Model>();
        }

        // Here we pass viewmodels about invented trackers - the view can display and command them in a managing view
        // it still gets "select tracker" creating new from the main registry.
        #region helpy appresenter, notify viewmodels, commands to edit/add/remove
        // REMEMBER - dont do any actions - fire this TOCHANGED with a prior to complete the change. Presenter handles it.
        public event DietVMChangeEventHandler<SimpleTrackyHelpyInventionV1,EventArgs> ViewModelsToChange = delegate { };
        
        // this can get spammed - it's a good oppertunity to provide diet registraions cause you'll get "busy"
        // status, but, need to protect against those spams case only need reg once
        class DR
        {
            public Presenter.AddDietPairState registrationState;
            public SimpleTrackyHelpyInventionV1Model inventedModel;
            public TrackerTypesContainer emittedTypes;
        }
        Dictionary<int, DR> deregistrationActions = new Dictionary<int, DR>();
        public IEnumerable<InventedTrackerVM> Instances()
        {
            // Get Models - calls to this dude are by unwritten contract on worker threads.
            foreach (var it in conn.Table<SimpleTrackyHelpyInventionV1Model>())
            {
                if (!deregistrationActions.ContainsKey(it.id)) // picks up new ones
                {
                    // regisiter the ones we're returning the first time here. create types. etc.
                    var e = HelpyGenV1.CreateTypes(it);

                    // Call the generator
                    Object model, presenter;
                    HelpyGenV1.Gen(e, it, out model, out presenter);

                    // Call registrar!
                    var adpfunc = PlatformGlobal.platform.GetMethodInfo(typeof(Presenter), "AddDietPair");
                    var gen = adpfunc.MakeGenericMethod(e.instance, e.input, e.inputinfo, e.output, e.outputinfo);
                    var ret = gen.Invoke(pres, new[] { model, presenter });

                    // catalog the registration for later reversal etc etc
                    deregistrationActions[it.id] = new DR
                    {
                        registrationState = ret as Presenter.AddDietPairState,
                        emittedTypes = e,
                        inventedModel = it
                    };
                }
                // call presentation method and yield;
                var vm = Present(it);
                // set sender? :/
                yield return vm;
            }
        }

        public void RemoveTracker(InventedTrackerVM dvm, bool warn = true)
        {
            // get it
            var id = (dvm.originator as SimpleTrackyHelpyInventionV1Model).id;
            var reg = deregistrationActions[id];

            // tell dal do remove all entries for everything FIXME this should be all transactional on the DB.
            // if sth breaks here, the app is really going to die.
            reg.registrationState.dal.DeleteAll(() => // this is taskmapped to instances/entries etc
            {
                // which apppresenter will map to instances entries, selected etc changed. done now
                // this is the ___> "after" <___ callback.  It's on worker thread.

                // so lets deregister the handler from the presenter
                reg.registrationState.remove();

                // and finally drop the tables that the handler created.
                conn.DropTable(reg.emittedTypes.input);
                conn.DropTable(reg.emittedTypes.inputinfo);
                conn.DropTable(reg.emittedTypes.output);
                conn.DropTable(reg.emittedTypes.outputinfo);
                conn.DropTable(reg.emittedTypes.instance);

                // remove the invention entry from the table
                conn.Delete(reg.inventedModel);

                // remove from registry, mostly so when this pk reused it actually registeres it!
                deregistrationActions.Remove(id);

                // and tell view that invention models have changed
                ViewModelsToChange(this, new DietVMToChangeEventArgs<EventArgs>()); // this is mapped to invention but meh cause already thread.
            });
        }

        class RequestPage
        {
            private RequestPage(GetValuesPage page, Action<SimpleTrackyHelpyInventionV1Model> set, IValueRequest<TabularDataRequestValue> tdr = null)
            {
                this.page = page;
                this.set = set;
                this.tdr = tdr;
            }

            public readonly IValueRequest<TabularDataRequestValue> tdr;
            public readonly GetValuesPage page;
            public readonly Action<SimpleTrackyHelpyInventionV1Model> set;
            

            public static RequestPage Page1(IValueRequestFactory fac)
            {
                var page = new GetValuesPage("What's it called?");
                var p1vr = (from s in new[] { "Name", "Description", "Category", "Tracking" }
                            select fac.StringRequestor(s)).ToArray();
                page.SetList(new BindingList<object>((from p in p1vr select p.request).ToList()));
                Action<SimpleTrackyHelpyInventionV1Model> set = mod =>
                {
                    //stuff
                    mod.Name = p1vr[0].value;
                    mod.Description = p1vr[1].value;
                    mod.Category = p1vr[2].value;
                    mod.TrackedName = p1vr[3].value;
                    mod.tracked = HelpyGenV1.DBName(mod.TrackedName);
                };
                foreach (var vr in p1vr) vr.ValueChanged += () => vr.valid = !String.IsNullOrWhiteSpace(vr.value);
                return new RequestPage(page,set);
            }
            public static RequestPage Page2(IValueRequestFactory fac)
            {
                var page = new GetValuesPage("How are entries called?");
                var p2vr = (from s in new[] {
                        "InputEntryVerb","OutputEntryVerb","InputInfoPlural",
                        "OutputInfoPlural","InputInfoVerbPast","OutputInfoVerbPast" }
                            select fac.StringRequestor(s)).ToArray();
                page.SetList(new BindingList<object> (( from p in p2vr select p.request ).ToList()));
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
                return new RequestPage(page,set);
            }
            public static RequestPage Page3(IValueRequestFactory fac, IValueRequest<TabularDataRequestValue> megalist)
            {
                var page = new GetValuesPage("How can infos be quantified?");
                var ioreq = fac.OptionGroupRequestor("For");
                ioreq.value = new OptionGroupValue(new[] { "Input", "Output" });
                var nreq = fac.StringRequestor("Name");
                var dvalmor = fac.IValueRequestOptionGroupRequestor("Type");

                var qnr = fac.DoubleRequestor("Number");
                var qqr = fac.IntRequestor("Quantity");
                var qdr = fac.TimeSpanRequestor("Duration");
                var vls = new Func<Object>[] { () => qnr.value, () => qqr.value, () => qdr.value };

                dvalmor.value = new MultiRequestOptionValue(new[] { qnr.request, qqr.request, qdr.request, }, 0);
                var sdef = new Func<Object, double>[] { d => (double)d, d => (double)(int)d, d => ((TimeSpan)d).TotalHours };
                var addr = fac.ActionRequestor("Add");

                String[] headers = new[] { "For", "Name", "Units", "Default" };
                megalist.value = new TabularDataRequestValue(headers);

                addr.ValueChanged += () =>
                {
                    var ot = (InfoQuantifier.InfoQuantifierTypes)dvalmor.value.SelectedRequest;
                    var ov = vls[dvalmor.value.SelectedRequest]();
                    megalist.value.Items.Add(new object[] { ioreq.value.SelectedOption, nreq.value, ot, ov });
                };

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
                        var inout = (int)q[0];
                        var name = (q[1] as String);
                        var qtype = (int)q[2];
                        var qdef = q[3];
                        var qd = new SimpleTrackyInfoQuantifierDescriptor { defaultvalue = sdef[qtype](qdef), id = i++, Name = name, type = (InfoQuantifier.InfoQuantifierTypes)qtype };
                        if (inout == 0) inq.Add(qd);
                        else outq.Add(qd);
                    }
                    mod.qod_in.Set(inq);
                    mod.qod_out.Set(outq);
                };
                megalist.value.Items.CollectionChanged += (a,b) => megalist.valid = megalist.value.Items.Count > 0;
                nreq.ValueChanged += () => nreq.valid = !String.IsNullOrWhiteSpace(nreq.value);
                dvalmor.valid = ioreq.valid = true;
                return new RequestPage(page, set, megalist);
            }
        }

        public Task StartNewTracker()
        {
            var page1 = RequestPage.Page1(build.requestFactory);
            var page2 = RequestPage.Page2(build.requestFactory);
            var page3 = RequestPage.Page3(build.requestFactory, build.GenerateTableRequest());
            
            //var vt = build.GetValues(new[] { page1.page, page2.page, page3.page });
            var vt = build.GetValuesWithList(new[] {  page3.page }, page3.tdr);
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
                                var mod = new SimpleTrackyHelpyInventionV1Model();
                                page1.set(mod);
                                page2.set(mod);
                                page3.set(mod);
                                conn.Insert(mod); 
                                return null; 
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
            var page1 = RequestPage.Page1(build.requestFactory);
            var page2 = RequestPage.Page2(build.requestFactory);
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

    static class HelpyGenV1
    {
        public static String DBName(string friendlyname)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < friendlyname.Length; i++)
            {
                var c = friendlyname[i];
                sb.Append((c < 17 || c > 128) ? '_' : c);
            }
            return sb.ToString();
        }
        interface ISTHI
        {
            String TargetName { get; set; }
            string targetField { get; set; }
            TrackerDetailsVM TrackerDetails { get; set; }
            TrackerDialect TrackerDialect { get; set; }
        }
        class STHI<A, B> : IExtraRelfectedHelpy<A, B>, ISTHI where A : HBaseInfo, new() where B : HBaseInfo, new()
        {
            public String TargetName { get; set; }
            public string targetField { get; set; }

            public IReflectedHelpyQuants<A> input { get; set; }
            public IReflectedHelpyQuants<B> output { get; set; }
            public TrackerDetailsVM TrackerDetails { get; set; }
            public TrackerDialect TrackerDialect { get; set; }
            public VRVConnectedValue[] instanceValueFields
            {
                get { return new[] { VRVConnectedValue.FromType<double>(0.0, TargetName, targetField, v => v.DoubleRequestor) }; }
            }
            public SimpleTrackyTarget[] Calcluate(object[] fieldValues)
            {
                List<SimpleTrackyTarget> targs = new List<SimpleTrackyTarget>();
                targs.Add(new SimpleTrackyTarget(TargetName, true, true, 1, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { (double)fieldValues[0] }));
                return targs.ToArray();
            }
        }

        interface IIRHQ
        {
            String trackedField { get; set; }
            String trackedName { get; set; }
            InfoQuantifier[] quantifier_choices { get; set; }
        }
        class IRHQ<A> : IReflectedHelpyQuants<A> where A : HBaseInfo, new()
        {
            public String trackedField { get; set; }
            public String trackedName { get; set; }

            public InstanceValue<double> tracked { get { return new InstanceValue<double>(trackedName, trackedField, 0.0); } }
            public InstanceValue<double>[] calculation { get { return new[] { new InstanceValue<double>(trackedName, trackedField, 0.0) }; } }
            public double Calcluate(double[] values) { return values[0]; }
            public Expression<Func<A, bool>> InfoComplete { get { return fi => true; } }
            public InfoQuantifier[] quantifier_choices { get; set; }
        }

        public static void Gen(TrackerTypesContainer e, SimpleTrackyHelpyInventionV1Model it, out Object model, out Object presenter)
        {
            // these are the simpletrackyhelpers we will create.
            var tht = typeof(SimpleTrackyHelpy<,,,,>).MakeGenericType(e.instance, e.input, e.inputinfo, e.output, e.outputinfo);
            var tpt = typeof(SimpleTrackyHelpyPresenter<,,,,>).MakeGenericType(e.instance, e.input, e.inputinfo, e.output, e.outputinfo);

            // but to do that we need to create a IReflectedExtraHelper<e.inputinfo,e.outputinfo>
            var iid = typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(e.inputinfo, typeof(bool)));
            var oid = typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(e.outputinfo, typeof(bool)));
            var iidi = Activator.CreateInstance(iid, new Func<Object, bool>(args => true));
            var oidi = Activator.CreateInstance(oid, new Func<Object, bool>(args => true));

            // which needs theeemmm....
            var input_irhq = typeof(IRHQ<>).MakeGenericType(e.inputinfo);
            var output_irhq = typeof(IRHQ<>).MakeGenericType(e.outputinfo);

            // built on tese
            var stii = typeof(STHI<,>).MakeGenericType(e.inputinfo, e.outputinfo);

            // so we can actually build one
            var iirhq = Activator.CreateInstance(input_irhq) as IIRHQ;
            var oirhq = Activator.CreateInstance(output_irhq) as IIRHQ;
            var sthi = Activator.CreateInstance(stii) as ISTHI;
            PlatformGlobal.platform.GetPropertyInfo(stii, "input").SetValue(sthi, iirhq);
            PlatformGlobal.platform.GetPropertyInfo(stii, "output").SetValue(sthi, oirhq);

            // set the juicy bits!
            sthi.targetField = it.tracked;
            sthi.TargetName = it.TrackedName;
            sthi.TrackerDetails = new TrackerDetailsVM(it.Name, it.Description, it.Category);
            sthi.TrackerDialect = new TrackerDialect(it.InputEntryVerb, it.OutputEntryVerb, it.InputInfoPlural, it.OutputInfoPlural, it.InputInfoVerbPast, it.OutputInfoVerbPast);
            iirhq.trackedField = it.tracked;
            iirhq.trackedName = it.TrackedName;
            PlatformGlobal.platform.GetPropertyInfo(input_irhq, "InfoComplete").SetValue(iirhq, iidi);
            iirhq.quantifier_choices = (from f in it.qod_in.Get() select InfoQuantifier.FromType(f.type, f.Name, f.id, f.defaultvalue)).ToArray();
            oirhq.trackedField = it.tracked;
            oirhq.trackedName = it.TrackedName;
            PlatformGlobal.platform.GetPropertyInfo(output_irhq, "InfoComplete").SetValue(oirhq, oidi);
            oirhq.quantifier_choices = (from f in it.qod_out.Get() select InfoQuantifier.FromType(f.type, f.Name, f.id, f.defaultvalue)).ToArray();

            // build the helpy!
            model = Activator.CreateInstance(tht, sthi);
            presenter = Activator.CreateInstance(tht, sthi);
        }
        public static TrackerTypesContainer CreateTypes(SimpleTrackyHelpyInventionV1Model model)
        {
            // since we cannot actually remove types emmitted into assemblies, only unload the assembly, which could be disaserous
            // however you do it (think about dangling pointers in the GC), we've gotta make unique tablenames, and store them
            // explicitly on the model for loading.
            String t = model.tablename_tracker = GenerateUniqueTypeName(model.Name + "_instance"),
                   i = model.tablename_input = GenerateUniqueTypeName(model.Name + "_input"),
                   ii = model.tablename_inputinfo = GenerateUniqueTypeName(model.Name + "_inputinfo"),
                   o = model.tablename_output = GenerateUniqueTypeName(model.Name + "_output"),
                   oi = model.tablename_outputinfo = GenerateUniqueTypeName(model.Name + "_outputinfo");

            // happen when you delete a invention and then recreate it with the same name.  
            // This way we can invent things with same names, if desired too.

            // creating them.
            Func<String, Type, String[], Type[], Type> mf = PlatformGlobal.platform.emit.CreateClass;
            String mt = model.tracked; Type dt = typeof(double);
            return new TrackerTypesContainer
            {
                instance = mf(t, typeof(TrackerInstance), new[] { mt + "_target" }, new[] { dt }),
                input = mf(i, typeof(HBaseEntry), new[] { mt }, new[] { dt }),
                inputinfo = mf(ii, typeof(HBaseInfo), new[] { mt }, new[] { dt }),
                output = mf(o, typeof(HBaseEntry), new[] { mt }, new[] { dt }),
                outputinfo = mf(oi, typeof(HBaseInfo), new[] { mt }, new[] { dt }),
            };
        }
        static String GenerateUniqueTypeName(String trythis)
        {
            String ret = trythis;
            int i = 1;
            while (PlatformGlobal.platform.emit.TypeExists(ret))
                ret = String.Format("{0}_{1}", trythis, i++);
            return ret;
        }
    }
}
