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
    class InfoTests : AppStarterFix
    {
        protected override string id { get; } = "InfoTests";

        [Order(1)]
        [Test]
        public void T1_StartNewTracker()
        {
            var v = app.view;
            var choose_type_traker = ChoosePlan("Finance budget");

            var indexes = new int[] { 0, 1 };
            var values = new object[] { 1923.0, "testyD" };
            var final = AlterState(BudInstDefault, indexes, values);

            var new_tracker = GetVE(BudInstDefault, final, indexes, values, "Finance budget", () => true);
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
                new TrackerDialect("Earn", "Spend", "Incomes", "Income", "Expenses", "Expense", "Earned", "Spent"),
                new KVPList<string, double> { { "balance per day", 1923.0 } }));
        }

        V[] cw;
        [Test, Order(2)]
        public void T2_Add_InInfo()
        {
            cw = new V[] { V.C(0, "te info"), V.C(2, 9912.3) };
            InfoItemer(() => BudInfoDefault, null, "Create a Income", true, null, app, cw);
            InfoLineAssertion(app.view.InInfos.val,
                new ILA
                {
                    name = "te info",
                    vname = "Balance",
                    qname = "Quantity",
                    qvalue = 1,
                    is_input = true,
                    value = 9912.3
                });
        }


        DateTime dtn;
        ST altered_final;
        [Test, Order(3)]
        public void T3_Add_InItem_WithThatInfo()
        {
            altered_final = Itemer(() => BudInDefault, BudInInfoMode, "Earn", true, null, app,
                V.C(0, app.view.InInfos.val[0]), // use dat info
                V.C(1, 2.0), // measure
                V.C(2, 2 * 9912.3, true), // readonly amount!
                V.C(3, "te") // name
                );
            dtn = DateTime.Now;
            EntryLineAssertion(app.view.InEntries.val, "Balance",
                new ELA { is_input = true, name = "te", desc = "2.00 of te info", value = 2 * 9912.3 });
            var tm = app.view.Instances.val[0];
            var ttm = app.view.InTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "Finance budget", tm, "Earned", "Spent",
                new ELA { name = "Balance per day", value = 1923.0 },
                new ELA { name = "te", is_input = true, value = 2 * 9912.3 }
            );

        }

        
        [Test, Order(4)]
        public void T4_EditInfo_CheckDependencies()
        {


            var vml = app.view.InInfos.val[0];
            Func<ST> ns=  ()=> AlterState(BudInfoDefault, cw.Select(s => s.index).ToArray(), cw.Select(s => s.value).ToArray());
            InfoItemer(ns, null, "Edit Income", true, vml, app,
            V.C(0, "altered nfo"), // name
            V.C(2, 400.2) // value
            );
            InfoLineAssertion(app.view.InInfos.val,
                iex1=new ILA
                {
                    name = "altered nfo",
                    vname = "Balance",
                    qname = "Quantity",
                    qvalue = 1,
                    is_input = true,
                    value = 400.2
                });

            EntryLineAssertion(app.view.InEntries.val, "Balance",
                eex1=new ELA { when= dtn, is_input = true, name = "te", desc = "2.00 of altered nfo", value = 2 * 400.2 });
            var tm = app.view.Instances.val[0];
            var ttm = app.view.InTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "Finance budget", tm, "Earned", "Spent",
                new ELA { name = "Balance per day", value = 1923.0 },
                tex1 =new ELA { name = "te", is_input = true, value = 2 * 400.2  }
            );

        }

        ILA iex1;
        ELA eex1;
        ELA tex1;

        ILA iex2;
        ELA eex2;
        ELA tex2;

        [Test, Order(5)]
        public void T5_Add_OutInfo()
        {
            cw = new V[] { V.C(0, "spendor"), V.C(2, 9912.3) };
            InfoItemer(() => BudInfoDefault, null, "Create a Expense", false, null, app, cw);
            InfoLineAssertion(app.view.OutInfos.val,
                new ILA
                {
                    name = "spendor",
                    vname = "Balance",
                    qname = "Quantity",
                    qvalue = 1,
                    is_input = false,
                    value = 9912.3
                });
        }

        [Test, Order(6)]
        public void T6_Add_OutItem_WithThatInfo()
        {
            Itemer(() => BudOutDefault, BudOutInfoMode, "Spend", false, null, app,
                V.C(0, app.view.OutInfos.val[0]), // use dat info
                V.C(1, 2.0), // measure
                V.C(2, 2 * 9912.3, true), // readonly amount!
                V.C(3, "nom") // name
                );

            EntryLineAssertion(app.view.OutEntries.val, "Balance",
                new ELA { is_input = false, name = "nom", desc = "2.00 of spendor", value = 2 * 9912.3 });
            var tm = app.view.Instances.val[0];
            var ttm = app.view.OutTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "Finance budget", tm, "Earned", "Spent",
                new ELA { name = "Balance per day", value = 1923.0 },
                tex1, new ELA { name = "nom", is_input = false, value = 2 * 9912.3 }
            );

        }

        [Test, Order(7)]
        public void T7_EditInfo_CheckDependencies()
        {
            var vml = app.view.OutInfos.val[0];
            Func<ST> ns = () => AlterState(BudInfoDefault, cw.Select(s => s.index).ToArray(), cw.Select(s => s.value).ToArray());
            InfoItemer(ns, null, "Edit Expense", false, vml, app,
            V.C(0, "altered nfo x2"), // name
            V.C(2, 800.1) // value
            );
            InfoLineAssertion(app.view.OutInfos.val,
                iex2= new ILA
                {
                    name = "altered nfo x2",
                    vname = "Balance",
                    qname = "Quantity",
                    qvalue = 1,
                    is_input = false,
                    value = 800.1
                });
            EntryLineAssertion(app.view.OutEntries.val, "Balance",
                eex2 = new ELA { is_input = false, name = "nom", desc = "2.00 of altered nfo x2", value = 2 * 800.1 });
            var tm = app.view.Instances.val[0];
            var ttm = app.view.OutTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "Finance budget", tm, "Earned", "Spent",
                new ELA { name = "Balance per day", value = 1923.0 },
                tex1, tex2 = new ELA { name = "nom", is_input = false, value = 2 * 800.1 }
            );
        }

        [Test,Order(8)]
        public void T8_Delete_Info_Check_Dependencies()
        {
            var busies = new[]
            {
                app.view.OutEntries.QueueWaitForBusy(true, false),
                app.view.OutInfos.QueueWaitForBusy(true, false),
                app.view.InTrack.QueueWaitForBusy(true, false),
                app.view.OutTrack.QueueWaitForBusy(true, false)
            };

            app.plan_commands._burninfo.Remove(app.view.OutInfos.val[0]);
            BusyAssert(app, busies);
            InfoLineAssertion(app.view.OutInfos.val); // none
            eex2.desc = "Quick Entry"; // resets toa  qucik
            EntryLineAssertion(app.view.OutEntries.val, "Balance", eex2); // be same
            var tm = app.view.Instances.val[0];
            var ttm = app.view.OutTrack.val;
            TrackerTracksAssertion( // samey
                ttm, "testyD", "Finance budget", tm, "Earned", "Spent",
                new ELA { name = "Balance per day", value = 1923.0 },
                tex1, tex2
            );

        }
    }
}
