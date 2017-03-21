using Consonance.Protocol;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Consonance.Test.CTH;
using ST = Consonance.Test.TestValueRequestBuilder.GetValuesExpected.ExpectedPage.State;

namespace Consonance.Test.Tests
{
    [TestFixture]
    class InfoTests : AppStarterFix
    {
        [Order(1)]
        [Test]
        public void T1_StartNewTracker()
        {
            var v = app.view;
            var choose_type_traker = ChoosePlan("Finance budget");

            var indexes = new int[] { 0, 1 };
            var values = new object[] { 1923.0, "testyD" };
            var final = AlterState(BudInstDefault, indexes, values);

            var new_tracker = GetVE(BudInstDefault, final, indexes, values, "Finance budget", ()=>true);
            var busies = new[]
            {
                v.BurnInfos.QueueWaitForBusy(true, false),
                v.BurnLines.QueueWaitForBusy(true, false),
                v.BurnTrack.QueueWaitForBusy(true, true, true, false),
                v.EatInfos.QueueWaitForBusy(true, false),
                v.EatLines.QueueWaitForBusy(true, false),
                v.EatTrack.QueueWaitForBusy(true, true, true, false),
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
                new TrackerDialect("Earn", "Spend", "Incomes", "Expenses", "Earned", "Spent"),
                new KVPList<string, double> { { "balance per day", 1923.0 } }));
        }

        [Test, Order(2)]
        public void T2_Add_InItem_Creating_Info()
        {
            var entry = Itemer(() => CalInDefault, "Earn", true, null, app,
                V.C(1, "te"), // name
                V.C(0, new EventArgs()), // fire the info screen
                V.C(2,12.3) // amount
                );
            var info = Itemer(() => CalInDefault, "Info", true, null, app,
                V.C(1, "te"), // name
                V.C(0, new EventArgs()), // fire the info screen
                V.C(2, 12.3) // amount
                );


            //EntryLineAssertion(app.view.EatLines.val, "calories", el);
            //var tm = app.view.Instances.val[0];
            //var ttm = app.view.EatTrack.val;
            //TrackerTracksAssertion(
            //    ttm, "testyD", "Finance budget", tm, "Earned", "Spent",
            //    new ELA { name = "Budget", value = 1923.0 },
            //    new ELA { name = "te", is_input=true, value = 12.3 }
            //);

        }

    }

}
