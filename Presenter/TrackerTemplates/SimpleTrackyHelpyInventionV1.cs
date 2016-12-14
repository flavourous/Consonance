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
    // Global viewmovel for all diet inventors
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
            this.mid = mid;
            this.pid = pid;
            this.id = id;
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
        public void Clear()
        {
            GlobalForKeyTo.conn.DeleteAll<T>();
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
        const int keyto_id = 1;

        // functional
        public String tracked { get; set; }

        KeyTo<SimpleTrackyInfoQuantifierDescriptor> _qod_out, _qod_in;
        public KeyTo<SimpleTrackyInfoQuantifierDescriptor> qod_in
        {
            get { return _qod_in ?? (_qod_in = new KeyTo<SimpleTrackyInfoQuantifierDescriptor>(id, 1, keyto_id)); }
        }
        public KeyTo<SimpleTrackyInfoQuantifierDescriptor> qod_out
        {
            get { return _qod_out ?? (_qod_out = new KeyTo<SimpleTrackyInfoQuantifierDescriptor>(id, 1, keyto_id)); }
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
    class SimpleTrackyInfoQuantifierDescriptor : KeyableBaseDB
    {
        public InfoQuantifier.InfoQuantifierTypes type { get; set; }
        public double defaultvalue { get; set; }
        public String Name { get; set; }
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


        public static SimpleTrackyInventionRequestPages Page1(IValueRequestFactory fac)
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
                mod.tracked = mod.TrackedName.ToNiceAscii();
            };
            foreach (var vr in p1vr) vr.ValueChanged += () => vr.valid = !String.IsNullOrWhiteSpace(vr.value);
            return new SimpleTrackyInventionRequestPages(page, set);
        }
        public static SimpleTrackyInventionRequestPages Page2(IValueRequestFactory fac)
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
        public static SimpleTrackyInventionRequestPages Page3(IValueRequestFactory fac, IValueRequest<TabularDataRequestValue> megalist)
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
                mod.qod_in.Clear();
                mod.qod_out.Clear();
                mod.qod_in.Add(inq);
                mod.qod_out.Add(outq);
            };
            megalist.value.Items.CollectionChanged += (a, b) => megalist.valid = megalist.value.Items.Count > 0;
            addr.valid = nreq.valid = dvalmor.valid = ioreq.valid = true; // not used - the list is...
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
            conn.CreateTable<SimpleTrackyHelpyInventionV1Model>();
        }

        // Here we pass viewmodels about invented trackers - the view can display and command them in a managing view
        // it still gets "select tracker" creating new from the main registry.
        #region helpy appresenter, notify viewmodels, commands to edit/add/remove
        // REMEMBER - dont do any actions - fire this TOCHANGED with a prior to complete the change. Presenter handles it.
        public event DietVMChangeEventHandler<SimpleTrackyHelpyInventionV1, EventArgs> ViewModelsToChange = delegate { };

        // this can get spammed - it's a good oppertunity to provide diet registraions cause you'll get "busy"
        // status, but, need to protect against those spams case only need reg once
        public IEnumerable<InventedTrackerVM> Instances()
        {
            // Get Models - calls to this dude are by unwritten contract on worker threads.
            foreach (var it in conn.Table<SimpleTrackyHelpyInventionV1Model>())
            {
                var vm = Present(it);
                // set sender? :/
                yield return vm;
            }
        }

        public void RemoveTracker(InventedTrackerVM dvm, bool warn = true)
        {
            throw new NotImplementedException();
            return;
        }

        public Task StartNewTracker()
        {
            var page1 = SimpleTrackyInventionRequestPages.Page1(build.requestFactory);
            var page2 = SimpleTrackyInventionRequestPages.Page2(build.requestFactory);
            var page3 = SimpleTrackyInventionRequestPages.Page3(build.requestFactory, build.GenerateTableRequest());

            var vt = build.GetValues(new[] { page1.page, page2.page, page3.page });
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
            var page1 = SimpleTrackyInventionRequestPages.Page1(build.requestFactory);
            var page2 = SimpleTrackyInventionRequestPages.Page2(build.requestFactory);
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
        public class IInfo : HBaseInfo { }
        public class IEntry : HBaseEntry { }
        class tt : IExtraRelfectedHelpy<TrackerInstance, IInfo, IInfo>
        {
            public IReflectedHelpyQuants<IInfo> input { get; set; }
            public VRVConnectedValue[] instanceValueFields { get; set; }
            public IReflectedHelpyQuants<IInfo> output { get; set; }
            public TrackerDetailsVM TrackerDetails { get; set; }
            public TrackerDialect TrackerDialect { get; set; }
            public SimpleTrackyTarget[] Calcluate(object[] fieldValues)
            {
                throw new NotImplementedException();
            }
        }

        public static SimpleTrackyHelpy<TrackerInstance, IEntry, IInfo, IEntry, IInfo> Create()
        {
            return new SimpleTrackyHelpy<TrackerInstance, IEntry, IInfo, IEntry, IInfo>(new tt());
        }
    }
}
