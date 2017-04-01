using Consonance.Protocol;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Consonance.Test.CTH;
using ST = Consonance.Test.TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State;

namespace Consonance.Test
{
    [TestFixture]
    class EntryTests : AppStarterFix
    {
        protected override string id { get; } = "EntryTests";

        [Order(1)]
        [Test]
        public void T1_StartNewTracker()
        {
            var v = app.view;
            var choose_type_traker = ChoosePlan("Calorie diet");
 
            var indexes = new int[] { 0, 3 };
            var values = new object[] { 123.0, "testyD" };
            var final = AlterState(CalInstDefault, indexes, values);

            var new_tracker = GetVE(CalInstDefault, final, indexes, values, "Calorie diet", ()=>true);
            var busies = new[]
            {
                v.OutInfos.QueueWaitForBusy(true, false),
                v.OutEntries.QueueWaitForBusy(true, false),
                v.OutTrack.QueueWaitForBusy(true, true, true, false),
                v.InInfos.QueueWaitForBusy(true, false),
                v.InEntries.QueueWaitForBusy(true, false),
                v.InTrack.QueueWaitForBusy(true, true, true, false),
                v.Instances.QueueWaitForBusy(true, false),
            };

            // Arrange
            app.input.ChoosePlanExpect.Enqueue(choose_type_traker);
            app.builder.GetValuesExpect.Enqueue(new_tracker);

            // Act
            app.view._plan.Add();

            // Assert (or join to them here :) )
            choose_type_traker.finished.Task.AssertWaitResult();
            new_tracker.finished.Task.AssertWaitResult();
            BusyAssert(app, busies);

            TrackerLineAssertion(app.view.Instances.val, new TLA("testyD", "", Today, true,
                new TrackerDialect("Eat", "Burn", "Foods", "Food", "Exercises", "Exercise", "Eaten", "Burned"),
                new KVPList<string, double> { { "calories per day", 123.0 } }));
        }

        [Order(2)]
        [Test]
        public void T2_RemoveAddTest()
        {
            var busies = new[]
             {
                app.view.OutInfos.QueueWaitForBusy(true, false, true, false),
                app.view.OutEntries.QueueWaitForBusy(true, true, true, true,true, false),
                app.view.OutTrack.QueueWaitForBusy(true,true,true,true, true , true , true , true , true , true , true , false ),
                app.view.InInfos.QueueWaitForBusy(true, false, true, false),
                app.view.InEntries.QueueWaitForBusy(true, true, true, true,true, false),
                app.view.InTrack.QueueWaitForBusy(true,true,true,true, true , true , true , true , true , true , true , false ),
                app.view.Instances.QueueWaitForBusy(true, false),
            };
            app.view._plan.Remove(app.view.Instances.val[0]);
            BusyAssert(app, busies);
            T1_StartNewTracker();
        }

        

        [Order(3)]
        [Test]
        public void T3_Add_InItems_Today_CheckItemsTracking()
        {
            Func<String, double, ELA> adder = (n, v) =>
            {
                Itemer(() => CalInDefault, null, "Eat", true, null, app, V.C(1, v), V.C(2, n));
                return new ELA { value = v, name = n, is_input = true };
            };

            this.es.AddRange(new[] 
            {
                adder("Nom nom", 123.0),
                adder("nomax", 92.4),
                adder("testinator", 0.1)
            });

            EntryLineAssertion(app.view.InEntries.val, "Calories", es.ToArray());
            var tm = app.view.Instances.val[0];
            var ttm = app.view.InTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "Calorie diet", tm, "Eaten", "Burned",
                new ELA { name = "Calories", value = 123.0 },
                es.ToArray()
            );
            
        }

        // For next test.
        readonly List<ELA> es = new List<ELA>();

        [Order(4)]
        [Test]
        public void T4_Add_OutItems_Today_CheckItemsTracking()
        {
            Func<String, double, ELA> adder = (n, v) =>
            {
                Itemer(() => CalOutDefault, null, "Burn", false, null, app, V.C(1, v), V.C(2, n));
                return new ELA { value = v, name = n, is_input = false };
            };
            var es2 = new[]
            {
                adder("burn burn", 123.0),
                adder("init s", 92.4),
                adder("make beter", 0.1)
            };

            Assert.AreEqual(3, es.Count);
            es.AddRange(es2);

            EntryLineAssertion(app.view.OutEntries.val, "Calories", es2);
            var tm = app.view.Instances.val[0];
            var ttm = app.view.OutTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "Calorie diet", tm, "Eaten", "Burned",
                new ELA { name = "Calories", value = 123.0 },
                es.ToArray()
            );
        }

        [Order(5)]
        [Test]
        public void T5_Remove_SomeItems_Today_CheckItemsTracking()
        {
            var busies = new[] {
                app.view.InEntries.QueueWaitForBusy(true, false),
                app.view.OutEntries.QueueWaitForBusy(true, false),
                app.view.InTrack.QueueWaitForBusy(true, false,true,false),
                app.view.OutTrack.QueueWaitForBusy(true, false,true,false)
            };

            app.plan_commands._eat.Remove(app.view.InEntries.val[1]);
            app.plan_commands._burn.Remove(app.view.OutEntries.val[1]);
            es.RemoveAt(4); es.RemoveAt(1);

            BusyAssert(app, busies);
            AssertTest5();
        }

        void AssertTest5()
        {
            EntryLineAssertion(app.view.InEntries.val, "Calories", es.Take(2).ToArray());
            EntryLineAssertion(app.view.OutEntries.val, "Calories", es.Skip(2).ToArray());
            var tm = app.view.Instances.val[0];
            var ttm = app.view.InTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "Calorie diet", tm, "Eaten", "Burned",
                new ELA { name = "Calories", value = 123.0 },
                es.ToArray()
            );
        }

        [Order(6)]
        [Test]
        public void T6_MoveNextDay_Add_MoveBack()
        {
            AssertDayChange(app, app.view.day.AddDays(1));
            Assert.AreEqual(0, app.view.InEntries.val.Count);
            Assert.AreEqual(0, app.view.OutEntries.val.Count);

            var ut = app.view.day.AddHours(3);
            Itemer(() => CalInDefault, null, "Eat", true, null, app, V.C(2, "new day test"), V.C(1, 99.12), V.C(3, ut));
            var e = new ELA { name = "new day test", value = 99.12, when = ut };
            EntryLineAssertion(app.view.InEntries.val, "Calories", e);
            var tm = app.view.Instances.val[0];
            var ttm = app.view.InTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "Calorie diet", tm, "Eaten", "Burned",
                new ELA { name = "Calories", value = 123.0 },
                e
            );

            AssertDayChange(app, app.view.day.AddDays(-1));

            AssertTest5();
        }

        [Order(8), Test]
        public void T8_EditItems_Check()
        {
            Itemer(() => CalInDefaultWith(es[0]), null, "Eat", true, app.view.InEntries.val[0], app,
                V.C(2, "new wordy words"), V.C(1, 1024.12));
            es[0].name = "new wordy words";
            es[0].value = 1024.12;
            Itemer(() => CalOutDefaultWith(es[2]), null,"Burn", false, app.view.OutEntries.val[0], app,
                V.C(2, "some new wordy words"), V.C(1, 104.12));
            es[2].name = "some new wordy words";
            es[2].value = 104.12;
            AssertTest5();
        }

    }
}