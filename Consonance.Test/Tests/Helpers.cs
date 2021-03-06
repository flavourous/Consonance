﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Consonance.Test
{
    [TestFixture]
    abstract class AppStarterFix
    {
        protected abstract String id { get; }
        protected TestApp app;
        [OneTimeSetUp]
        public void SetUp()
        {
            app = new TestApp(id);
        }

        [Order(0)]
        [Test]
        public void T00_InitialState()
        {
            var v = app.view;
            var busies = new[]
            {
                v.OutInfos.QueueWaitForBusy(true, false),
                v.OutEntries.QueueWaitForBusy(true, false, true, false),
                v.OutTrack.QueueWaitForBusy(true, true, true, false,true,true,true,false),
                v.InInfos.QueueWaitForBusy(true, false),
                v.InEntries.QueueWaitForBusy(true, false, true, false),
                v.InTrack.QueueWaitForBusy(true, true, true, false,true,true,true,false),
                v.Instances.QueueWaitForBusy(true, false),
                v.Inventions.QueueWaitForBusy(true, false)
            };
            app.StartPresenter().Wait();

            CTH.BusyAssert(app, busies);

            // And now all this should be true
            Assert.AreEqual(0, app.view.OutInfos.val.Count);
            Assert.AreEqual(0, app.view.OutEntries.val.Count);
            Assert.AreEqual(0, app.view.OutTrack.val.Count);
            Assert.AreEqual(0, app.view.InInfos.val.Count);
            Assert.AreEqual(0, app.view.InEntries.val.Count);
            Assert.AreEqual(0, app.view.InTrack.val.Count);
            Assert.AreEqual(0, app.view.Instances.val.Count);
            Assert.AreEqual(0, app.view.Inventions.val.Count);

        }
    }

        public static class DeltaAssert
    {
        public class Config
        {
            public double delta_double = 1e-10; // should be relative to the value
            public TimeSpan delta_timespan = TimeSpan.FromMilliseconds(1000); // who knows
        }
        public static bool AreClose(Object expected, Object actual, String message = null, Config c = null)
        {
            c = c ?? new Config();
            var e = expected; var a = actual;
            if (e is DateTime && a is DateTime)
            {
                var dms = Math.Abs(((DateTime)e - (DateTime)a).TotalMilliseconds);
                Assert.Less(TimeSpan.FromMilliseconds(dms), c.delta_timespan, message);
            }
            else if (e is TimeSpan && a is TimeSpan)
            {
                var dms = Math.Abs(((TimeSpan)e - (TimeSpan)a).TotalMilliseconds);
                Assert.Less(TimeSpan.FromMilliseconds(dms), c.delta_timespan, message);
            }
            else if (e is double && a is double)
            {
                double de = (double)e, da = (double)a;
                var diff = Math.Abs(de - da);
                var mag = (Math.Abs(de) + Math.Abs(da)) / 2.0;
                var ord = diff / mag; // % wrongness aka tolerance
                Assert.Less(ord, c.delta_double, message);
            }
            else return false;
            return true;
        }
    }
    public class SMR { public bool val, hit; }
    public class IVMListStore<T> 
    {
        public readonly String id;
        public IVMListStore([CallerMemberName]String id = null)
        {
            this.id = id;
        }
        public class SM { public bool x; public TaskCompletionSource<bool> ts; }
        IVMList<T> v;
        public IVMList<T> val
        {
            get { return v; }
            set
            {
                if (v != null) v.PropertyChanged -= val_pc;
                v = value;
                v.PropertyChanged += val_pc;
            }
        }
        void val_pc(object o, PropertyChangedEventArgs pea)
        {
            if (pea.PropertyName == "busy")
            {
                Debug.WriteLine("(TEST) {0} is {1}", id, val.busy);
                BusyChanged(val.busy);
            }
        }
        
        readonly Queue<SMR> rl = new Queue<SMR>();
        public IEnumerable<SMR> Results()
        {
            while (rl.Count > 0)
                yield return rl.Dequeue();
        }
        readonly Queue<SM> ql = new Queue<SM>();
        void BusyChanged(bool val)
        {
            lock (ql)
            {
                rl.Enqueue(new SMR { val = val, hit = ql.Count > 0 });
                if (ql.Count > 0)
                {
                    var b = ql.Dequeue();
                    b.ts.SetResult(b.x == val);
                }
            }
        }
        public Waiter QueueWaitForBusy(params bool[] enqueue)
        {
            var w = new Waiter(id);
            foreach (var e in enqueue)
            {
                lock (ql)
                {
                    var tt = new TaskCompletionSource<bool>();
                    ql.Enqueue(new SM { x = e, ts = tt });
                    w.Add(tt.Task);
                }
            }
            return w;
        }
    }
    public static class WaiterExtension
    {
        public static bool WaitAll(this Waiter[] @this, out String message, int timeout = 5000)
        {
            StringBuilder mes = new StringBuilder();
            String m;
            var res = @this.Select(w =>
            {
                var wt = w.Wait(out m, timeout);
                mes.AppendLine(m);
                return wt;
            });

            var ret = res.All(d=>d);
            message = mes.ToString();
            return ret;
        }
    }
    public static class TaskWaiterExtensions
    {
        public static T AssertWaitResult<T>(this Task<T> t, string m=null)
        {
            Assert.IsTrue(t.Wait(10000), m);
            return t.Result;
        }
    }
    public class Waiter
    {
        public readonly string id;
        public Waiter(String id)
        {
            this.id = id;
        }
        List<Task<bool>> tsk = new List<Task<bool>>();
        public void Add(Task<bool> wt)
        {
            tsk.Add(wt);
        }
        public bool Wait(out String failures, int timeout = 5000)
        {
            bool?[] res = new bool?[tsk.Count];
            for (int i = 0; i < tsk.Count; i++)
            {
                if (!tsk[i].Wait(timeout)) res[i] = null;
                else res[i] = tsk[i].Result;
            }
            failures = id+": " + String.Join(",",res.Select(
                (r, i) => "#" + i + " " + (r.HasValue ? r.Value ? "OK" : "Fail" : "Timeout")
            ));
            var ret = res.All(d => d.HasValue && d.Value);
            return ret;
        }
    }
}
