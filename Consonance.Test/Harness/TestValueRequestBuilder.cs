using Consonance.Protocol;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Consonance.Test
{
    class TestValueRequestBuilder : IValueRequestBuilder
    {
        public IValueRequestFactory requestFactory { get; } = new TestValueRequestFactory();
        public class GetValuesExpected : ExpectBase
        {
            public Func<bool> complete;
            public ExpectedPage[] page_actions;
            public class ExpectedPage
            {
                public String title;
                public int[] indexes;
                public Object[] values;
                public State initial, final;
                public class State
                {
                    public int nvalrequests;
                    public bool[] valid, read_only, enabled;
                    public ITypeValue[] vals;
                    public void AssertState(TestRequest[] vrs)
                    {
                        Assert.AreEqual(nvalrequests, vrs.Length);
                        Assert.AreEqual(nvalrequests, valid.Length);
                        Assert.AreEqual(nvalrequests, read_only.Length);
                        Assert.AreEqual(nvalrequests, enabled.Length);
                        Assert.AreEqual(nvalrequests, vals.Length);

                        StringBuilder slnb = new StringBuilder(Environment.NewLine);
                        slnb.Append("valid: "); slnb.AppendLine(String.Join(", ", vrs.Select(s => s.valid)));
                        slnb.Append("read_only: "); slnb.AppendLine(String.Join(", ", vrs.Select(s => s.read_only)));
                        slnb.Append("enabled: "); slnb.AppendLine(String.Join(", ", vrs.Select(s => s.enabled)));
                        slnb.Append("name: "); slnb.AppendLine(String.Join(", ", vrs.Select(s => s.name)));
                        slnb.Append("ovalue: "); slnb.AppendLine(String.Join(", ", vrs.Select(s => s.ovalue)));
                        slnb.Append("type of ovalue: "); slnb.AppendLine(String.Join(", ", vrs.Select(s => s.ovalue?.GetType()?.Name ?? "null")));
                        String sln = slnb.ToString();

                        for (int i = 0; i < nvalrequests; i++)
                        {
                            Assert.AreEqual(valid[i], vrs[i].valid, "#" + i + ":valid" + sln);
                            Assert.AreEqual(read_only[i], vrs[i].read_only, "#" + i + ":read_only" + sln);
                            Assert.AreEqual(enabled[i], vrs[i].enabled, "#" + i + ":enabled" + sln);
                            Assert.AreEqual(vals[i].n, vrs[i].name, "#" + i + ":name" + sln);
                            Assert.AreEqual(vals[i].t, vrs[i].otype, "#" + i + ":type" + sln);
                            if (vals[i].care)
                                if (!DeltaAssert.AreClose(vals[i].v, vrs[i].ovalue, "#" + i + ":value" + sln))
                                    HAssert(vals[i].v, vrs[i].ovalue);
                        }
                    }
                    void HAssert(object a, object b)
                    {
                        if (a == null && b == null) return;
                        if (a == null || b == null) Assert.Fail();

                        if (CastCall<OptionGroupValue>((x,y) =>
                        {
                            CollectionAssert.AreEqual(x.OptionNames, y.OptionNames);
                            Assert.AreEqual(x.SelectedOption, y.SelectedOption);
                        },a,b)) return;

                        Assert.AreEqual(a, b);
                    }
                    bool CastCall<T>(Action<T,T> act, object a, object b) 
                    {
                        var bth = a is T && b is T;
                        if (bth) act((T)a, (T)b);
                        return bth;
                    }
                }
            }
        }
        public readonly Queue<GetValuesExpected> GetValuesExpect = new Queue<GetValuesExpected>();
        public Task<bool> GetValues(IEnumerable<GetValuesPage> requestPages)
        {
            // must be true
            return GetValuesExpect.DTest(exp =>
            {
                bool c;
                using (var rps = requestPages.GetEnumerator())
                {
                    GetValuesPage current = new GetValuesPage("none");
                    foreach (var e in exp.page_actions)
                    {
                        // was valid to continue
                        Assert.IsTrue(current.valuerequests.All(r => (r as TestRequest).valid));
                        Assert.IsTrue(rps.MoveNext());
                        current = rps.Current;

                        // title
                        Assert.AreEqual(e.title, current.title);

                        // initial state check
                        e.initial.AssertState(current.valuerequests.Cast<TestRequest>().ToArray());

                        // Set values
                        Assert.AreEqual(e.indexes.Length, e.values.Length);
                        Func<int,TestRequest> vrsa =i=> current.valuerequests.Cast<TestRequest>().ElementAt(i);
                        for (int i = 0; i < e.indexes.Length; i++)
                        {
                            var v = e.values[i];
                            var r = vrsa(e.indexes[i]);
                            if (v != null && v.GetType() != r.otype) // null always ok(?)
                                throw new InvalidOperationException(v.GetType().Name +" not compatible with " + r.otype.Name + " ( " + r.name + ")");
                            r.ovalue = v;
                        }

                        // resulting state check
                        e.final.AssertState(current.valuerequests.Cast<TestRequest>().ToArray());
                    }
                    if (c=exp.complete()) Assert.IsFalse(rps.MoveNext());
                }
                return Task.FromResult(c);
            });
        }
    }
    interface TestRequest
    {
        String name { get; }
        Type otype { get;}
        Object request { get; }  
        event Action ValueChanged;
        void ClearListeners();
        bool enabled { get; set; }
        bool valid { get; set; } 
        bool read_only { get; set; }
        Object ovalue { get; set; }
    }
    class TestValueRequest<T> : IValueRequest<T>, TestRequest
    {
        public TestValueRequest(String name, Object extra = null)
        {
            this.name = name;
            this.extra = extra;
            valid = true;
        }
        public Type otype { get { return typeof(T); } }
        public Object extra { get; set; }
        public String name { get; set; }
        public bool read_only { get; set; }
        public object request { get { return this; } }
        public bool valid { get; set; }
        T _ = default(T);
        public T value
        {
            get { return _; }
            set { _ = value; ValueChanged(); }
        }
        public object ovalue
        {
            get { return this.value; }
            set { this.value = (T)value; }
        }

        private bool _enabled = true;
        public bool enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public event Action ValueChanged = delegate { };
        public void ClearListeners() { ValueChanged = delegate { }; }
    }
    class TestValueRequestFactory : IValueRequestFactory
    {
        public IValueRequest<EventArgs> ActionRequestor(string name) => new TestValueRequest<EventArgs>(name);
        public IValueRequest<Barcode> BarcodeRequestor(string name) => new TestValueRequest<Barcode>(name);
        public IValueRequest<bool> BoolRequestor(string name) => new TestValueRequest<bool>(name);
        public IValueRequest<DateTime> DateRequestor(string name) => new TestValueRequest<DateTime>(name);
        public IValueRequest<DateTime> DateTimeRequestor(string name) => new TestValueRequest<DateTime>(name);
        public IValueRequest<double> DoubleRequestor(string name) => new TestValueRequest<double>(name);
        public IValueRequest<TabularDataRequestValue> GenerateTableRequest() => new TestValueRequest<TabularDataRequestValue>(null);
        public IValueRequest<InfoLineVM> InfoLineVMRequestor(string name, InfoManageType imt) => new TestValueRequest<InfoLineVM>(name, imt);
        public IValueRequest<int> IntRequestor(string name) => new TestValueRequest<int>(name);
        public IValueRequest<MultiRequestOptionValue> IValueRequestOptionGroupRequestor(string name) => new TestValueRequest<MultiRequestOptionValue>(name);
        public IValueRequest<DateTime?> nDateRequestor(string name) => new TestValueRequest<DateTime?>(name);
        public IValueRequest<OptionGroupValue> OptionGroupRequestor(string name) => new TestValueRequest<OptionGroupValue>(name);
        public IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor(string name) => new TestValueRequest<RecurrsEveryPatternValue>(name);
        public IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor(string name) => new TestValueRequest<RecurrsOnPatternValue>(name);
        public IValueRequest<string> StringRequestor(string name) => new TestValueRequest<string>(name);
        public IValueRequest<TimeSpan> TimeSpanRequestor(string name) => new TestValueRequest<TimeSpan>(name);
    }
}