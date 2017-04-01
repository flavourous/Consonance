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

namespace Consonance.Test.Tests
{
    class RecurrTests : AppStarterFix
    {
        protected override string id { get; } = "RecurrTests";

        [Order(1)]
        [Test]
        public void T1_StartNewTracker()
        {
            var v = app.view;
            var choose_type_traker = ChoosePlan("Scavenger calorie diet");

            var indexes = new int[] { 0, 1, 2, 3, 4 };
            var values = new object[] { 1, 500.0, 1, 200.0, "testscav" };
            var final = AlterState(ScavInstDefault, indexes, values);

            var new_tracker = GetVE(ScavInstDefault, final, indexes, values, "Scavenger calorie diet", () => true);
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

            TrackerLineAssertion(app.view.Instances.val, new TLA("testscav", "", Today, true,
                new TrackerDialect("Eat", "Burn", "Foods", "Food", "Exercises", "Exercise", "Eaten", "Burned"),
                new KVPList<string, double>
                {
                    { "calories for 1 day", 500.0 },
                    { "calories for 1 day", 200.0 },
                }
            ));
        }

        [Order(3)]
        [Test]
        public void T3_Add_OutItem_RecurrEvery_CheckIT()
        {
            var tw = DateTime.Now;
            Itemer(() => CalOutDefault, CalOutRepeatEvery, "Burn", false, null, app,
                V.C(1, 100.0),
                V.C(2, "repeating"),
                V.C(3, tw),
                V.C(4, ogv(2)),
                V.C(5, new RecurrsEveryPatternValue(tw, RecurrSpan.Day, 1)),
                V.C(6, null),
                V.C(7, null)
            );

            // check a few
            Action<DateTime,double> AssertAt = (t,g) =>
            {
                var ala = new ELA { desc = "Quick Entry", name = "repeating", is_input = false, value = 100.0, when = t };
                EntryLineAssertion(app.view.OutEntries.val, "Calories", ala);
                var tm = app.view.Instances.val[0];
                var ttm = app.view.OutTrack.val;
                TrackerTracksAssertion(
                    ttm, "testscav", "Scavenger calorie diet", tm, "Eaten", "Burned",
                    new ELA { name = "Calories", value = g }, ala
                );
            };

            AssertAt(tw,500.0);
            AssertDayChange(app, tw.Subtract(TimeSpan.FromDays(1)));
            AssertAt(tw.Subtract(TimeSpan.FromDays(1)),200.0);
            AssertDayChange(app, tw.Add(TimeSpan.FromDays(1)));
            AssertAt(tw.Add(TimeSpan.FromDays(1)),200.0);
        }

        [Order(4)]
        [Test]
        public void T4_Remove_That_One_Crazee()
        {
            var busies = new[] {
                app.view.OutEntries.QueueWaitForBusy(true, false),
                app.view.InTrack.QueueWaitForBusy(true, false),
                app.view.OutTrack.QueueWaitForBusy(true, false)
            };
            app.plan_commands._burn.Remove(app.view.OutEntries.val[0]);
            BusyAssert(app, busies);
            EntryLineAssertion(app.view.OutEntries.val, "Calories");
        }

        [Order(5)]
        [Test]
        public void T5_Add_OutItem_RecurrOn_CheckIT()
        {
            var tw = new DateTime(2012,1,2);
            Itemer(() => CalOutDefault, CalOutRepeatOn, "Burn", false, null, app,
                V.C(1, 100.0),
                V.C(2, "repeating"),
                V.C(4, ogv(1)), // on state alteration it be overwritten ok
                V.C(4, new RecurrsOnPatternValue(RecurrSpan.Day | RecurrSpan.Month, new[] { 1, 2 })),
                V.C(5, null),
                V.C(6, null)
            );

            // check a few
            Action<DateTime, double> AssertAt = (t, g) =>
            {
                AssertDayChange(app, t);
                var ala = new ELA { desc = "Quick Entry", name = "repeating", is_input = false, value = 100.0, when = t };
                EntryLineAssertion(app.view.OutEntries.val, "Calories", ala);
                var tm = app.view.Instances.val[0];
                var ttm = app.view.OutTrack.val;
                TrackerTracksAssertion(
                    ttm, "testscav", "Scavenger calorie diet", tm, "Eaten", "Burned",
                    new ELA { name = "Calories", value = g }, ala
                );
            };

            AssertAt(new DateTime(2011, 11, 1), 500.0);
            AssertAt(new DateTime(2012, 3, 1), 200.0);
            AssertAt(new DateTime(2012, 5, 1), 500.0);

        }


    }
}