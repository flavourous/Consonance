using Consonance.Protocol;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ST = Consonance.Test.TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State;

namespace Consonance.Test
{
    interface ITypeValue
    {
        Type t { get; }
        Object v { get; set; }
        bool care { get; }
        String n { get; }
    }
    class TypeValue<T> : ITypeValue
    {
        public TypeValue(String name) { this.n = name; }
        public TypeValue(String name, Object v) : this(name) { this.v = v; }
        public String n { get; set; }
        public Type t { get { return typeof(T); } }
        Object _v;
        public Object v { get { return _v; } set { _v = value; care = true; } }
        public bool care { get; private set; }
    }
    static class EBExt
    {
        public static X DTest<T, X>(this Queue<T> @this, Func<T, X> test) where T : ExpectBase
        {
            Assert.IsTrue(@this.Count > 0);
            var exp = @this.Dequeue();
            X result;
            try { result = test(exp); }
            catch (Exception e) { exp.finished.SetException(e); throw; }
            exp.finished.TrySetResult(new EventArgs());
            return result;
        }
    }
    class TestInputResponse<T> : IInputResponse<T>, IInputResponse
    {
        readonly Func<Task> close;
        public TestInputResponse(T res, Action close = null)
        {
            Result = Task.FromResult<T>(res);
            this.close = async () => { await Task.Yield(); close?.Invoke(); };
        }
        public TestInputResponse(Action close = null) : this(default(T), close) { }
        public Task<T> Result { get; set; }
        Task IInputResponse.Result { get { return Result; } }
        public Task Close() => close();
    }
    public class ExpectBase
    {
        public readonly TaskCompletionSource<EventArgs> finished = new TaskCompletionSource<EventArgs>();
    }
    
    class CCHelp<T> : IComparer
    {
        readonly Func<T, IEnumerable<Object>> values;
        public CCHelp(Func<T, IEnumerable<Object>> values) { this.values = values; }

        public int Compare(object x, object y)
        {
            if (x is T && y is T)
            {
                var vx = values((T)x).ToArray();
                var vy = values((T)y).ToArray();
                if (vx.Length != vy.Length)
                    return vx.Length - vy.Length;
                for (int i = 0; i < vx.Length; i++)
                    if (!vx[i].Equals(vy[i]))
                        return 1;
                return 0;
            }
            else if (y is T && !(x is T)) return -1;
            else if (x is T && !(y is T)) return 1;
            else return 0; // :(
        }
    }

    static class CTH
    {
        public class SG
        {
            public static Object IgnoreValue = new object();
            public String name;
            public ITypeValue value;
            public bool valid, read_only, enabled;
            private SG() { }
            public static SG Gen<Z>(String name, Object value, bool valid = true, bool read_only = false, bool enabled = true)
            {
                var @this = new SG();
                @this.name = name;
                @this.value = value == IgnoreValue ? new TypeValue<Z>(name) : new TypeValue<Z>(name, value);
                @this.valid = valid;
                @this.read_only = read_only;
                @this.enabled = enabled;
                return @this;
            }
        }
        public static TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State GenState(params SG[] vals)
        {
            return new TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State
            {
                nvalrequests = vals.Length,
                read_only = vals.Select(v => v.read_only).ToArray(),
                enabled = vals.Select(v => v.enabled).ToArray(),
                valid = vals.Select(v => v.valid).ToArray(),
                vals = vals.Select(v => v.value).ToArray()
            };
        }

        public static DateTime Today
        {
            get
            {
                var n = DateTime.Now;
                return new DateTime(n.Year, n.Month, n.Day);
            }
        }

        public static TestValueRequestBuilder.GetValuesExpected GetVE(TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State init, TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State final, int[] indexes, object[] values, String title, Func<bool> complete)
        {
            return new TestValueRequestBuilder.GetValuesExpected
            {
                complete = complete,
                page_actions = new TestValueRequestBuilder.GetValuesExpected.ExpectedPage[]
                {
                    new TestValueRequestBuilder.GetValuesExpected.ExpectedPage
                    {
                        title = title, initial=init,
                        indexes = indexes, values=values, final = final
                    }
                }
            };
        }

        public static ST BudInstDefault
        {
            get
            {
                return GenState(
                    SG.Gen<double>("Target", 0.0),
                    SG.Gen<String>("Finance Name", "", false),
                    SG.Gen<DateTime>("Start Date", Today),
                    SG.Gen<bool>("Tracked", true)
                );
            }
        }

        public static ST BudInDefault
        {
            get
            {
                return GenState(
                    SG.Gen<InfoLineVM>("Incomes", null),
                    SG.Gen<double>("Amount", 0.0),
                    SG.Gen<String>("Name", "", false),
                    SG.Gen<DateTime>("When", DateTime.Now),
                    SG.Gen<OptionGroupValue>("Repeat", SG.IgnoreValue)
                );
            }
        }
        public static ST BudInInfoMode
        {
            get
            {
                return GenState(
                    SG.Gen<InfoLineVM>("Incomes", SG.IgnoreValue),
                    SG.Gen<double>("Quantity", 0.0),
                    SG.Gen<double>("Amount", 0.0, true, true),
                    SG.Gen<String>("Name", "", false),
                    SG.Gen<DateTime>("When", DateTime.Now),
                    SG.Gen<OptionGroupValue>("Repeat", SG.IgnoreValue)
                );
            }
        }
        public static ST BudInInfoDefault
        {
            get
            {
                return GenState(
                    SG.Gen<String>("Name", ""),
                    SG.Gen<MultiRequestOptionValue>("Amount", SG.IgnoreValue),
                    SG.Gen<double>("Amount", 0.0)
                );
            }
        }

        public static ST CalInstDefault
        {
            get
            {
                return GenState(
                    SG.Gen<double>("Calorie Limit", 0.0),
                    SG.Gen<bool>("Track Daily", true),
                    SG.Gen<bool>("Track Weekly", false),
                    SG.Gen<String>("Diet Name", "", false),
                    SG.Gen<DateTime>("Start Date", Today),
                    SG.Gen<bool>("Tracked", true)
                );
            }
        }
        public static ST CalInDefaultWith(ELA v)
        {
            var ret = CalInDefault;
            ret.valid[2] = true;
            ret.vals[1].v = v.value;
            ret.vals[2].v = v.name;
            ret.vals[3].v = v.when;
            return ret;
        }
        public static TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State CalInDefault
        {
            get
            {
                return GenState(
                    SG.Gen<InfoLineVM>("Foods", null),
                    SG.Gen<double>("Calories", 0.0),
                    SG.Gen<String>("Name", "", false),
                    SG.Gen<DateTime>("When", DateTime.Now),
                    SG.Gen<OptionGroupValue>("Repeat", SG.IgnoreValue)
                );
            }
        }
        public static ST CalOutDefaultWith(ELA v)
        {
            var ret = CalOutDefault;
            ret.valid[2] = true;
            ret.vals[1].v = v.value;
            ret.vals[2].v = v.name;
            ret.vals[3].v = v.when;
            return ret;
        }
        public static ST CalOutDefault
        {
            get
            {
                return GenState(
                    SG.Gen<InfoLineVM>("Exercises", null),
                    SG.Gen<double>("Calories", 0.0),
                    SG.Gen<String>("Name", "", false),
                    SG.Gen<DateTime>("When", DateTime.Now),
                    SG.Gen<OptionGroupValue>("Repeat", SG.IgnoreValue)
                );
            }
        }
        public static TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State AlterState(TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State bs, int[] indexes, object[] values)
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                var x = indexes[i];
                bs.valid[x] = true;
                bs.vals[x].v = values[i];
            }
            return bs;
        }

        public class TLA
        {
            public TLA(String name, String desc, DateTime started, bool tracked,
                        TrackerDialect dialect, KVPList<String, double> displayAmounts)
            {
                this.name = name;
                this.desc = desc;
                this.started = started;
                this.tracked = tracked;
                this.dialect = dialect;
                this.displayAmounts = displayAmounts;
            }
            public readonly String name, desc;
            public readonly DateTime started;
            public readonly bool tracked;
            public readonly TrackerDialect dialect;
            public readonly KVPList<String, double> displayAmounts;
        }

        public static void TrackerLineAssertion(IEnumerable<TrackerInstanceVM> vals, params TLA[] lines)
        {
            var eva = vals.ToArray();
            Assert.AreEqual(lines.Length, eva.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                var a = eva[i];
                var e = lines[i];
                Assert.AreEqual(e.name, a.name);
                Assert.AreEqual(e.desc, a.desc);
                Assert.AreEqual(e.started, a.started);
                Assert.AreEqual(e.tracked, a.tracked);
                Assert.AreEqual(e.dialect.InputEntryVerb, a.dialect.InputEntryVerb);
                Assert.AreEqual(e.dialect.OutputEntryVerb, a.dialect.OutputEntryVerb);
                Assert.AreEqual(e.dialect.OutputInfoPlural, a.dialect.OutputInfoPlural);
                Assert.AreEqual(e.dialect.InputInfoPlural, a.dialect.InputInfoPlural);
                Assert.AreEqual(e.dialect.InputInfoVerbPast, a.dialect.InputInfoVerbPast);
                Assert.AreEqual(e.dialect.OutputInfoVerbPast, a.dialect.OutputInfoVerbPast);
                Func<KVPList<String, double>, IEnumerable<Object>> sel =
                    d => d.SelectMany(c => new Object[] { c.Key, c.Value });
                CollectionAssert.AreEqual(sel(e.displayAmounts), sel(a.displayAmounts));
            }
        }

        
        public class ELA
        {
            public bool is_input = true;
            public string name;
            public double value;
            public DateTime when = DateTime.Now;
            public String desc = "Quick Entry";
        }
        public class ILA
        {
            public bool is_input = true;
            public string name, vname, qname;
            public double value, qvalue;
        }
        public static void InfoLineAssertion(IEnumerable<InfoLineVM> vals, params ILA[] lines)
        {
            var eva = vals.ToArray();
            Assert.AreEqual(lines.Length, eva.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                var act = eva[i];
                var exp = lines[i];
                Assert.AreEqual(exp.name, act.name);
                CollectionAssert.AreEqual(act.displayAmounts, new KVPList<string, double> { { exp.vname, exp.value }, { exp.qname, exp.qvalue } });
            }
        }
        public static void EntryLineAssertion(IEnumerable<EntryLineVM> vals, String trak, params ELA[] lines)
        {
            // Assert entry is in viewmodels
            var eva = vals.ToArray();
            Assert.AreEqual(lines.Length, eva.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                var act = eva[i];
                var exp = lines[i];
                Assert.AreEqual(exp.name, act.name);
                Assert.AreEqual(exp.desc, act.desc);
                Assert.AreEqual(TimeSpan.Zero, act.duration);
                DeltaAssert.AreClose(exp.when, act.start);
                CollectionAssert.AreEqual(act.displayAmounts, new KVPList<string, double> { { trak, exp.value } });
            }
        }

        public static void TrackerTracksAssertion(IList<TrackerTracksVM> ttvm, String instName, String modName, Object inst, String inVerbP, String outVerbP, ELA track, params ELA[] vals)
        {
            var match = ttvm.Where(d => d.instanceName == instName && d.modelName == modName && d.instance == inst);
            Assert.AreEqual(1, match.Count());
            var m = match.First();
            Assert.AreEqual(1, m.tracks.Count());
            var mt = m.tracks.First();
            Assert.AreEqual(inVerbP, mt.inValuesName);
            Assert.AreEqual(outVerbP, mt.outValuesName);
            Assert.AreEqual(track.name, mt.targetValueName);
            Assert.AreEqual(track.value, mt.targetValue);

            // text stuff done, time for calcs
            var in_t = vals.Where(d => d.is_input).ToArray();
            var out_t = vals.Where(d => !d.is_input).ToArray();
            Assert.AreEqual(in_t.Length, mt.inValues.Length);
            Assert.AreEqual(out_t.Length, mt.outValues.Length);

            for (int i = 0; i < mt.inValues.Length; i++)
            {
                var v = mt.inValues[i];
                Assert.AreEqual(v.name, in_t[i].name);
                Assert.AreEqual(v.value, in_t[i].value);
            }
            for (int i = 0; i < mt.outValues.Length; i++)
            {
                var v = mt.outValues[i];
                Assert.AreEqual(v.name, out_t[i].name);
                Assert.AreEqual(v.value, out_t[i].value);
            }
        }

        public static TestInput.ChoosePlanExpected ChoosePlan(String name)
        {
            var ret = new TestInput.ChoosePlanExpected
            {
                title = "Select Tracker Type",
                initial = -1,
                expect = new Protocol.ItemDescriptionVM[]
                {
                    new Protocol.ItemDescriptionVM("Calorie diet", "Simple calorie-control diet with a daily target.  If enabled, weekly tracking starts from the start date of the diet.", "Diet"),
                    new Protocol.ItemDescriptionVM("Scavenger calorie diet", "Calorie controlled diet, using periods of looser control followed by periods of stronger control.", "Diet"),
                    new Protocol.ItemDescriptionVM("Finance budget", "Track spending goals and other finances.", "Finance"),
                }
            };
            var mt = ret.expect.Select((d, i) => new { d = d, i = i }).Where(d => d.d.name == name);
            Assert.IsTrue(mt.Count() > 0);
            ret.choose = mt.First().i;
            return ret;
        }

        public static void BusyAssert(TestApp app, Waiter[] busies )
        {
            String fm;
            var ok = busies.WaitAll(out fm);
            if(ok) app.view.AssertNoFailsFromExtraLoads();
            Assert.IsTrue(ok, fm);
        }
        public class V
        {
            public int index { get; private set; }
            public object value { get; private set; }
            public bool read_only { get; private set; }
            private V() { }
            public static V C(int index, object val, bool read_only = false)
            {
                return new V { index = index, value = val, read_only = read_only };
            }
        }
        static ST ItemerCommon<T>(Func<ST> init, ST final, String GVTitle, Bound<T> bcol, Waiter[] busies, T edit, TestApp app, params V[] selecta)
            where T : class
        {
            var use = selecta.Where(x => !x.read_only);
            Func<IEnumerable<V>, int[]> indexes = v => v.Select(x => x.index).ToArray();
            Func<IEnumerable<V>, object[]> values = v => v.Select(x => x.value).ToArray();
            final = AlterState(final ?? init(), indexes(selecta), values(selecta));
           
            var new_item = GetVE(init(), final, indexes(use), values(use), GVTitle, () => true);
            app.builder.GetValuesExpect.Enqueue(new_item);

            if (edit != null) bcol.Edit(edit, app.builder);
            else bcol.Add(app.builder);

            new_item.finished.Task.AssertWaitResult();
            BusyAssert(app, busies);

            return final;
        }
        public static ST Itemer(Func<ST> init, ST final, String GVTitle, bool input, EntryLineVM edit, TestApp app, params V[] selecta)
        {
            return ItemerCommon(init, final, GVTitle,
                (input ? app.plan_commands._eat : app.plan_commands._burn),
                new[] {
                    (input ? app.view.InEntries : app.view.OutEntries).QueueWaitForBusy(true, false),
                    app.view.InTrack.QueueWaitForBusy(true, false),
                    app.view.OutTrack.QueueWaitForBusy(true, false)
                },
                edit, app, selecta);
        }
        public static ST InfoItemer(Func<ST> init, ST final, String GVTitle, bool input, InfoLineVM edit, TestApp app, params V[] selecta)
        {
            return ItemerCommon(init, final, GVTitle,
                (input ? app.plan_commands._eatinfo : app.plan_commands._burninfo),
                new[] {
                    (input ? app.view.InEntries : app.view.OutEntries).QueueWaitForBusy(true, false),
                    (input ? app.view.InInfos : app.view.OutInfos).QueueWaitForBusy(true, false),
                    app.view.InTrack.QueueWaitForBusy(true, false),
                    app.view.OutTrack.QueueWaitForBusy(true, false)
                }, 
                edit, app, selecta);
        }
    }
}
