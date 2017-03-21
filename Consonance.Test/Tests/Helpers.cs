using NUnit.Framework;
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
        protected TestApp app;
        [OneTimeSetUp]
        public void SetUp()
        {
            app = new TestApp();
        }

        [Order(0)]
        [Test]
        public void T0_InitialState()
        {
            var v = app.view;
            var busies = new[]
            {
                v.BurnInfos.QueueWaitForBusy(true, false),
                v.BurnLines.QueueWaitForBusy(true, false, true, false),
                v.BurnTrack.QueueWaitForBusy(true, true, true, false,true,true,true,false),
                v.EatInfos.QueueWaitForBusy(true, false),
                v.EatLines.QueueWaitForBusy(true, false, true, false),
                v.EatTrack.QueueWaitForBusy(true, true, true, false,true,true,true,false),
                v.Instances.QueueWaitForBusy(true, false),
                v.Inventions.QueueWaitForBusy(true, false)
            };
            app.StartPresenter().Wait();

            CTH.BusyAssert(app, busies);

            // And now all this should be true
            Assert.AreEqual(0, app.view.BurnInfos.val.Count);
            Assert.AreEqual(0, app.view.BurnLines.val.Count);
            Assert.AreEqual(0, app.view.BurnTrack.val.Count);
            Assert.AreEqual(0, app.view.EatInfos.val.Count);
            Assert.AreEqual(0, app.view.EatLines.val.Count);
            Assert.AreEqual(0, app.view.EatTrack.val.Count);
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
        public static void AreClose(Object expected, Object actual, String message = null, Config c = null)
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
                var dms = Math.Abs(((double)e - (double)a));
                Assert.Less(dms, c.delta_double, message);
            }
            else Assert.AreEqual(expected, actual, message);
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
                Debug.WriteLine("{0} is {1}", id, val.busy);
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
        public static bool WaitAll(this Waiter[] @this, out String message)
        {
            StringBuilder mes = new StringBuilder();
            String m;
            var res = @this.Select(w =>
            {
                var wt = w.Wait(out m);
                mes.AppendLine(m);
                return wt;
            });

            message = mes.ToString();
            return res.All(d => d);
        }
    }
    public static class TaskWaiterExtensions
    {
        public static T AssertWaitResult<T>(this Task<T> t, string m=null)
        {
            Assert.IsTrue(t.Wait(5000), m);
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
        public bool Wait(out String failures)
        {
            bool?[] res = new bool?[tsk.Count];
            for (int i = 0; i < tsk.Count; i++)
            {
                if (!tsk[i].Wait(5000)) res[i] = null;
                else res[i] = tsk[i].Result;
            }
            failures = id+": " + String.Join(",",res.Select(
                (r, i) => "#" + i + " " + (r.HasValue ? r.Value ? "OK" : "Fail" : "Timeout")
            ));
            return res.All(d => d.HasValue && d.Value);
        }
    }
}
