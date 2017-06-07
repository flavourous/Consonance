using Consonance.Invention;
using Consonance.Protocol;
using LibRTP;
using LibSharpHelp;
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
    class InventionTests : AppStarterFix
    {
        protected override string id { get; } = "InventionTests";

        OptionGroupValue iog(bool input)
        {
            return new OptionGroupValue(new[] { "Input", "Output" }) { SelectedOption = input ? 0 : 1 };
        }
        OptionGroupValue trt(int dx)
        {
            return new OptionGroupValue(new[] { "Days from start", "Days about now" }) { SelectedOption = dx };
        }
        MultiRequestOptionValue genQmrv<T>(T val)
        {
            TestRequest[] tr;
            var v = new MultiRequestOptionValue(
                tr = new TestRequest[]
                {
                    new TestValueRequest<double>("Number"),
                    new TestValueRequest<TimeSpan>("Duration")
                }, 0
            );
            for (int i = 0; i < tr.Length; i++)
            {
                if (tr[i].otype == typeof(T))
                {
                    tr[i].ovalue = val;
                    v.SelectedRequest = i;
                }
            }
            return v;
        }
        

        [Order(1)]
        [Test]
        public void T01_CreateNewInvention()
        {
            var v = app.view;

            var namePage = GenExpect(
                "What's it called?",
                ()=>new[] 
                {
                    SG.Gen<String>("Name", null, false),
                    SG.Gen<String>("Description", null, false),
                    SG.Gen<String>("Category", null, false)
                },
                new[]
                {
                    V.C(0,"test_invention"),
                    V.C(1,"it tests stuff"),
                    V.C(2,"Test"),
                }
            );
            var dialectPage = GenExpect(
                "How are entries called?",
                () => new[]
                {
                    SG.Gen<String>("InputEntryVerb", null, false),
                    SG.Gen<String>("OutputEntryVerb", null, false),
                    SG.Gen<String>("InputInfoPlural", null, false),
                    SG.Gen<String>("InputInfoSingular", null, false),
                    SG.Gen<String>("OutputInfoPlural", null, false),
                    SG.Gen<String>("OutputInfoSingular", null, false),
                    SG.Gen<String>("InputInfoVerbPast", null, false),
                    SG.Gen<String>("OutputInfoVerbPast", null, false)
                },
                new[] 
                {
                    V.C(0,"A"),V.C(1,"B"),V.C(2,"C"),V.C(3,"D"),
                    V.C(4,"E"),V.C(5,"F"),V.C(6,"G"),V.C(7,"H")
                }
            );
            var quantPage = GenExpect(
                "How can infos be quantified?",
                () => new[] {
                SG.Gen<OptionGroupValue>("For", iog(true)),
                SG.Gen<String>("Name", null),
                SG.Gen<MultiRequestOptionValue>("Type",genQmrv(0.0)),
                SG.Gen<EventArgs>("Add", null),
                SG.Gen<TabularDataRequestValue>(null,
                    new TabularDataRequestValue(
                        new[] { "For", "Name", "Units", "Default" }
                    ), false)
                },
                new[] 
                {
                 V.C(1,"Nominator"),
                 V.C(2,genQmrv(20.0)),
                 V.C(3,new EventArgs()), //add
                 V.C(0, iog(false)),
                 V.C(1,"Outinator"),
                 V.C(2,genQmrv(TimeSpan.FromSeconds(1))),
                 V.C(3,new EventArgs()), //add
                },
                new[] 
                {
                    SG.Gen<OptionGroupValue>("For", iog(true)),
                    SG.Gen<String>("Name", String.Empty),
                    SG.Gen<MultiRequestOptionValue>("Type", genQmrv(0.0)),
                    SG.Gen<EventArgs>("Add", new EventArgs()),
                    SG.Gen<TabularDataRequestValue>(null,
                        new TabularDataRequestValue(
                            new[] { "For", "Name", "Units", "Default" }
                        ).PushItems(new[]
                        {
                            new object[] { 0, "Nominator", InfoQuantifierTypes.Number, 20.0 },
                            new object[] { 1, "Outinator", InfoQuantifierTypes.Duration, TimeSpan.FromSeconds(1) },
                        })
                        , true)
                }
            );
            var targetPage = GenExpect(
                "What targets should be calculated?",
                () => new[]
                {
                    SG.Gen<String>("Name", null),
                    SG.Gen<String>("TargetID", null),
                    SG.Gen<String>("Target range equation", null),
                    SG.Gen<OptionGroupValue>("Target range Type",trt(0)),
                    SG.Gen<String>("Equation arguments", null),
                    SG.Gen<String>("Pattern equations", null),
                    SG.Gen<String>("Target equations", null),
                    SG.Gen<EventArgs>("Add", null, false),
                    SG.Gen<TabularDataRequestValue>(null,
                        new TabularDataRequestValue(
                            new[] { "Name", "Id", "Range", "Type", "Patterns", "Targets" }
                        ), false)
                },
                new[] {
                    V.C(0,"testTarget"),V.C(1,"TG"),V.C(2,"1"),
                    V.C(4,"a,b,c"), V.C(5,"1"), V.C(6,"12+a*2-c*b"),
                    V.C(7, new EventArgs())
                },
                new[]
                {
                    SG.Gen<String>("Name", "testTarget"),
                    SG.Gen<String>("TargetID", "TG"),
                    SG.Gen<String>("Target range equation", "1"),
                    SG.Gen<OptionGroupValue>("Target range Type",trt(0)),
                    SG.Gen<String>("Equation arguments", "a,b,c"),
                    SG.Gen<String>("Pattern equations", "1"),
                    SG.Gen<String>("Target equations", "12+a*2-c*b"),
                    SG.Gen<EventArgs>("Add", new EventArgs()),
                    SG.Gen<TabularDataRequestValue>(null,
                        new TabularDataRequestValue(
                            new[] { "Name", "Id", "Range", "Type", "Patterns", "Targets" }
                        ).PushItems(new [] {
                            new object[] { "testTarget", "TG", "1", AggregateRangeType.DaysFromStart, "1","12+a*2-c*b" }
                        }))
                }
            );
            var infoEqnsPage = GenExpect(
                "What are the equations for entries?",
                () => new[]
                {
                    SG.Gen<String>("In Equation arguments", null),
                    SG.Gen<String>("Out Equation arguments", null),
                    SG.Gen<String>("TargetID", null),
                    SG.Gen<OptionGroupValue>("For", iog(true)),
                    SG.Gen<String>("Equation", null),
                    SG.Gen<EventArgs>("Add", null),
                    SG.Gen<TabularDataRequestValue>(null,
                        new TabularDataRequestValue(
                            new[] { "Name", "For", "Equation" }
                        ))
                },
                new[] {
                    V.C(0,"i1,i2"),V.C(1,"o1,o2,o3"), V.C(2,"TG"),
                    V.C(3,iog(true)), V.C(4,"i1+2*i2"), V.C(5, new EventArgs()),
                    V.C(3,iog(false)), V.C(4,"o1*o2+o3-12"), V.C(5, new EventArgs())
                },
                new[]
                {
                    SG.Gen<String>("In Equation arguments", "i1,i2"),
                    SG.Gen<String>("Out Equation arguments", "o1,o2,o3"),
                    SG.Gen<String>("TargetID", "TG"),
                    SG.Gen<OptionGroupValue>("For", iog(false)),
                    SG.Gen<String>("Equation", "o1*o2+o3-12"),
                    SG.Gen<EventArgs>("Add", new EventArgs()),
                    SG.Gen<TabularDataRequestValue>(null,
                        new TabularDataRequestValue(
                            new[] { "Name", "For", "Equation" }
                        ).PushItems(new[] {
                            new object[] { "TG", 0, "i1+2*i2" },
                            new object[] { "TG", 1, "o1*o2+o3-12" }
                        }))
                }
            );
            var ivae = new TestValueRequestBuilder.GetValuesExpected
            {
                page_actions = new[] { namePage, dialectPage, quantPage, targetPage, infoEqnsPage },
                complete = () => true
            };
            app.builder.GetValuesExpect.Enqueue(ivae);
            var busies = new Waiter[]
            {
                v.Inventions.QueueWaitForBusy(true, false),
            };

            v._invention.Add();

            ivae.finished.Task.Wait();
            BusyAssert(app, busies, 10000);

            Assert.AreEqual(1, app.view.Inventions.val.Count);
            var v1 = app.view.Inventions.val.First();
            Assert.AreEqual("test_invention", v1.name);
            Assert.AreEqual("it tests stuff", v1.description);
            Assert.IsInstanceOf<SimpleTrackyHelpyInventionV1>(v1.sender);
            Assert.IsInstanceOf<SimpleTrackyHelpyInventionV1Model>(v1.originator);

            var model = v1.originator as SimpleTrackyHelpyInventionV1Model;

            Assert.AreEqual("test_invention", model.Name);
            Assert.AreEqual("it tests stuff", model.Description);
            Assert.AreEqual("Test", model.Category);

            Assert.AreEqual("A", model.InputEntryVerb);
            Assert.AreEqual("B", model.OutputEntryVerb);
            Assert.AreEqual("C", model.InputInfoPlural);
            Assert.AreEqual("D", model.InputInfoSingular);
            Assert.AreEqual("E", model.OutputInfoPlural);
            Assert.AreEqual("F", model.OutputInfoSingular);
            Assert.AreEqual("G", model.InputInfoVerbPast);
            Assert.AreEqual("H", model.OutputInfoVerbPast);

            Assert.AreNotEqual(gg=model.uid, null);

            Assert.AreEqual(1, model.qod_in.Get().Count());
            Qassert(model.qod_in.Get().First(), "Nominator", 20.0, InfoQuantifierTypes.Number);
            Assert.AreEqual(1, model.qod_out.Get().Count());
            Qassert(model.qod_out.Get().First(), "Outinator", TimeSpan.FromSeconds(1).TotalHours, InfoQuantifierTypes.Duration);

            Assert.AreEqual("a,b,c", model.target_args);
            Assert.AreEqual(1, model.targets.Get().Count());
            Tassert(model.targets.Get().First(),
               "testTarget", "TG", "1", AggregateRangeType.DaysFromStart,
               "1", "12+a*2-c*b", true, true);

            Assert.AreEqual("i1,i2", model.in_equations_args);
            Assert.AreEqual(1, model.inequations.Get().Count());
            Eassert(model.inequations.Get().First(), "TG", "i1+2*i2");
            Assert.AreEqual("o1,o2,o3", model.out_equations_args);
            Assert.AreEqual(1, model.outequations.Get().Count());
            Eassert(model.outequations.Get().First(), "TG", "o1*o2+o3-12");
        }
        void Eassert(SimpleTrackyTrackingEquationDescriptor e, String tid, String eq)
        {
            Assert.AreEqual(e.targetID, tid);
            Assert.AreEqual(e.equation, eq);
        }
        void Tassert(SimpleTrackyTrackingTargetDescriptor t, String name, String id, string reqn, AggregateRangeType rt, String peqn, String teqn, bool show, bool track)
        {
            Assert.AreEqual(t.name, t.name);
            Assert.AreEqual(t.targetID, id);
            Assert.AreEqual(t.targetRange, reqn);
            Assert.AreEqual(t.rangetype, rt);
            Assert.AreEqual(t.targertPattern, peqn);
            Assert.AreEqual(t.patternTarget, teqn);
            Assert.AreEqual(t.Shown, show);
            Assert.AreEqual(t.Track, track);
        }
        void Qassert(SimpleTrackyInfoQuantifierDescriptor q1, String name, double dv, InfoQuantifierTypes t)
        {
            Assert.AreEqual(q1.Name, name);
            Assert.AreEqual(q1.quantifier_type, t);
            Assert.AreEqual(q1.defaultvalue, dv);
        }
        Guid gg;
        [Test, Order(2)]
        public void T02_EditInvention()
        {
            var v = app.view;
            var namePage = GenExpect(
                "What's it called?",
                () => new[]
                {
                    SG.Gen<String>("Name", null, false),
                    SG.Gen<String>("Description", null, false),
                    SG.Gen<String>("Category", null, false)
                },
                new[]
                {
                    V.C(0,"test_invention-ed"),
                    V.C(1,"it tests stuff-ed"),
                    V.C(2,"Test-ed"),
                }
            );
            var dialectPage = GenExpect(
                "How are entries called?",
                () => new[]
                {
                    SG.Gen<String>("InputEntryVerb", null, false),
                    SG.Gen<String>("OutputEntryVerb", null, false),
                    SG.Gen<String>("InputInfoPlural", null, false),
                    SG.Gen<String>("InputInfoSingular", null, false),
                    SG.Gen<String>("OutputInfoPlural", null, false),
                    SG.Gen<String>("OutputInfoSingular", null, false),
                    SG.Gen<String>("InputInfoVerbPast", null, false),
                    SG.Gen<String>("OutputInfoVerbPast", null, false)
                },
                new[]
                {
                    V.C(0,"A-ed"),V.C(1,"B-ed"),V.C(2,"C-ed"),V.C(3,"D-ed"),
                    V.C(4,"E-ed"),V.C(5,"F-ed"),V.C(6,"G-ed"),V.C(7,"H-ed")
                }
            );

            var ivae = new TestValueRequestBuilder.GetValuesExpected
            {
                page_actions = new[] { namePage, dialectPage },
                complete = () => true
            };
            app.builder.GetValuesExpect.Enqueue(ivae);
            var busies = new Waiter[]
            {
                v.Inventions.QueueWaitForBusy(true, false),
            };

            var v1 = app.view.Inventions.val.First();
            v._invention.Edit(v1);
            
            ivae.finished.Task.Wait();
            BusyAssert(app, busies, 10000);

            v1 = app.view.Inventions.val.First();
            Assert.AreEqual(1, app.view.Inventions.val.Count);
            Assert.AreEqual("test_invention-ed", v1.name);
            Assert.AreEqual("it tests stuff-ed", v1.description);
            Assert.IsInstanceOf<SimpleTrackyHelpyInventionV1>(v1.sender);
            Assert.IsInstanceOf<SimpleTrackyHelpyInventionV1Model>(v1.originator);

            var model = v1.originator as SimpleTrackyHelpyInventionV1Model;

            Assert.AreEqual("test_invention-ed", model.Name);
            Assert.AreEqual("it tests stuff-ed", model.Description);
            Assert.AreEqual("Test-ed", model.Category);

            Assert.AreEqual("A-ed", model.InputEntryVerb);
            Assert.AreEqual("B-ed", model.OutputEntryVerb);
            Assert.AreEqual("C-ed", model.InputInfoPlural);
            Assert.AreEqual("D-ed", model.InputInfoSingular);
            Assert.AreEqual("E-ed", model.OutputInfoPlural);
            Assert.AreEqual("F-ed", model.OutputInfoSingular);
            Assert.AreEqual("G-ed", model.InputInfoVerbPast);
            Assert.AreEqual("H-ed", model.OutputInfoVerbPast);

            Assert.AreEqual(model.uid, gg);
        }
        [Test, Order(3)]
        public void T03_RemoveInvention_AndResetTo1()
        {
            var v = app.view;
            var v1 = app.view.Inventions.val.First();
            var busies = new Waiter[] // it triggers instance changed stuff
            {
                v.OutInfos.QueueWaitForBusy(true,true,true,false),
                v.OutEntries.QueueWaitForBusy(true,true,true,false),
                v.OutTrack.QueueWaitForBusy(true,true,true,true,true,true,true,false),
                v.InInfos.QueueWaitForBusy(true,true,true,false),
                v.InEntries.QueueWaitForBusy(true,true,true,false),
                v.InTrack.QueueWaitForBusy(true,true,true,true,true,true,true,false),
                v.Instances.QueueWaitForBusy(true,false),
                v.Inventions.QueueWaitForBusy(true, false),
            };
            v._invention.Remove(v1);
            BusyAssert(app, busies, 10000);
            Assert.AreEqual(0, app.view.Inventions.val.Count);
            T01_CreateNewInvention(); // reset
        }
        double a, b, c;
        [Test, Order(4)]
        public void T04_CreateTrackerFromInvention()
        {
            var choose = ChoosePlan("test_invention", new ItemDescriptionVM("test_invention", "it tests stuff", "Test"));
            app.input.ChoosePlanExpect.Enqueue(choose);

            Func<ST> init = () => GenState(
                SG.Gen<double>("A", 0.0), // args for target equation to calculate TARGET "TG"
                SG.Gen<double>("B", 0.0),
                SG.Gen<double>("C", 0.0),
                SG.Gen<String>("Test Name", String.Empty, false),
                SG.Gen<DateTime>("Start Date", Today),
                SG.Gen<bool>("Tracked", true)
            );
            var indexes = new int[] { 3,0,1,2 };
            var values = new object[] { "testyD", a=1.2, b=33.4, c=-10.2 };
            var final = AlterState(init(), indexes, values);
            var newtrack = GetVE(init(), final, indexes, values, "test_invention", () => true);
            app.builder.GetValuesExpect.Enqueue(newtrack);

            var v = app.view;
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

            app.view._plan.Add();

            // Assert (or join to them here :) )
            choose.finished.Task.AssertWaitResult();
            newtrack.finished.Task.AssertWaitResult();
            BusyAssert(app, busies);

            TrackerLineAssertion(app.view.Instances.val, new TLA("testyD", "", Today, true,
                new TrackerDialect("A", "B", "C", "D", "E", "F", "G", "H"),
                new KVPList<string, double> { { "daily TG", 355.08 } }));
        }

        Func<ST> TInvInDefault =()=> GenState(
                SG.Gen<InfoLineVM>("C", null),
                SG.Gen<double>("TG", 0.0), // value, uncalculated, toward TG for entry
                SG.Gen<String>("Name", "", false),
                SG.Gen<DateTime>("When", DateTime.Now),
                SG.Gen<OptionGroupValue>("Repeat", SG.IgnoreValue)
            );

        [Test, Order(5)]
        public void T05_Add_Quick_InInstances_AssertStuffs()
        {

            Func<String, double, ELA> adder = (n, v) =>
            {
                Itemer(TInvInDefault, null, "A", true, null, app, V.C(1, v), V.C(2, n));
                return new ELA { value = v, name = n, is_input = true };
            };

            var es = new[]
            {
                adder("Nom nom", 123.0),
                adder("nomax", 92.4),
                adder("testinator", 0.1)
            };

            EntryLineAssertion(app.view.InEntries.val, "TG", es.ToArray());
            var tm = app.view.Instances.val[0];
            var ttm = app.view.InTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "test_invention", tm, "G", "H",
                new ELA { name = "daily testTarget", value = 12 + a * 2 - c * b },
                es.ToArray()
            );
        }

        [Test, Order(6)]
        public void T06_Delete_Cause_Effort()
        {
            Action del1 = () =>
            {
                var b1 = new[] {
                app.view.InEntries.QueueWaitForBusy(true, false),
                app.view.InTrack.QueueWaitForBusy(true, false),
                app.view.OutTrack.QueueWaitForBusy(true, false)
                };
                var nc = app.view.InEntries.val.Count -1;
                app.plan_commands._eat.Remove(app.view.InEntries.val.First());
                BusyAssert(app, b1);

                Assert.AreEqual(nc, app.view.InEntries.val.Count);
                Assert.AreEqual(nc, app.view.InTrack.val.First().tracks.First().inValues.Count());
                Assert.AreEqual(nc, app.view.OutTrack.val.First().tracks.First().inValues.Count());
            };
            del1();
            del1();
            del1();
        }

        double i1, i2, o1, o2, o3;
        [Test, Order(7)]
        public void T07_Add_InInfo()
        {
            Func<ST> init = () => GenState(
                    SG.Gen<String>("Name", ""),
                    SG.Gen<MultiRequestOptionValue>("Amount", OneOV("Nominator",20.0)),
                    SG.Gen<double>("I1", 0.0),
                    SG.Gen<double>("I2", 0.0)
                );
            InfoItemer(init, null, "Create a D", true, null, app,
                V.C(0,"tin1"),
                V.C(1, OneOV("Nominator", 100.0)),
                V.C(2,i1=3.0),
                V.C(3,i2=25.1)
                );
            InfoLineAssertion(app.view.InInfos.val, new ILA
            {
                is_input=true,
                name = "tin1",
                kvp = new KVPList<string, double>
                    {
                        { "TG", i1+2*i2 },
                        { "Nominator", 100.0 },
                    },
            });
        }

        [Test, Order(8)]
        public void T08_Add_OutInfo()
        {
            Func<ST> init = () => GenState(
                    SG.Gen<String>("Name", ""),
                    SG.Gen<MultiRequestOptionValue>("Amount", OneOV("Outinator", TimeSpan.FromSeconds(1))),
                    SG.Gen<double>("O1", 0.0),
                    SG.Gen<double>("O2", 0.0),
                    SG.Gen<double>("O3", 0.0)
                );
            InfoItemer(init, null, "Create a F", false, null, app,
                V.C(0, "tout1"),
                V.C(1, OneOV("Outinator", TimeSpan.FromHours(2))),
                V.C(2, o1 = 8.0),
                V.C(3, o2 = 2.1),
                V.C(4, o3 = 92.5)
                );
            InfoLineAssertion(app.view.OutInfos.val, new ILA
            {
                is_input = false,
                name = "tout1",
                kvp = new KVPList<string, double>
                    {
                        { "TG", o1*o2+o3-12 },
                        { "Outinator", 2.0 },
                    },
            });
        }

        Func<ST> TInvInInfoMode = () => GenState(
                SG.Gen<InfoLineVM>("C", SG.IgnoreValue),
                SG.Gen<double>("Nominator", 20.0),
                SG.Gen<double>("TG", 0.0, true, true),
                SG.Gen<String>("Name", "", false),
                SG.Gen<DateTime>("When", DateTime.Now),
                SG.Gen<OptionGroupValue>("Repeat", SG.IgnoreValue)
             );


        double am7;
        [Test, Order(9)]
        public void T09_AddInEntry()
        {
            var un = 10.0;
            var f = un / 100.0;
            am7 = f * i1 + f * 2 * i2;
            var altered_final = Itemer(TInvInDefault, TInvInInfoMode(), "A", true, null, app,
                V.C(0, app.view.InInfos.val[0]), // use dat info
                V.C(1, un), // measure
                V.C(2, am7, true), // readonly amount!
                V.C(3, "infoed_in") // name
                );
            var dtn = DateTime.Now;
            EntryLineAssertion(app.view.InEntries.val, "TG",
                new ELA { is_input = true, name = "infoed_in", desc = un.ToString("F2")+" of tin1", value = am7 });
            var tm = app.view.Instances.val[0];
            var ttm = app.view.InTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "test_invention", tm, "G", "H",
                new ELA { name = "daily testTarget", value = 12 + a * 2 - c * b },
                new ELA { name = "infoed_in", is_input = true, value = am7 }
            );
        }

        Func<ST> TInvOutDefault = () => GenState(
            SG.Gen<InfoLineVM>("E", null),
            SG.Gen<double>("TG", 0.0), // value, uncalculated, toward TG for entry
            SG.Gen<String>("Name", "", false),
            SG.Gen<DateTime>("When", DateTime.Now),
            SG.Gen<OptionGroupValue>("Repeat", SG.IgnoreValue)
        );


        Func<ST> TInvOutInfoMode = () => GenState(
            SG.Gen<InfoLineVM>("E", SG.IgnoreValue),
            SG.Gen<TimeSpan>("Outinator", TimeSpan.FromSeconds(1)),
            SG.Gen<double>("TG", 0.0, true, true),
            SG.Gen<String>("Name", "", false),
            SG.Gen<DateTime>("When", DateTime.Now),
            SG.Gen<OptionGroupValue>("Repeat", SG.IgnoreValue)
         );

        [Test, Order(10)]
        public void T10_AddOutEntry()
        {
            var un = TimeSpan.FromHours(6.0);
            var f = 3.0;
            var amt = f * o1 * f * o2 + f * o3 - 12;
            var altered_final = Itemer(TInvOutDefault, TInvOutInfoMode(), "B", false, null, app,
                V.C(0, app.view.OutInfos.val[0]), // use dat info
                V.C(1, un), // measure
                V.C(2, amt, true), // readonly amount!
                V.C(3, "infoed_out") // name
                );
            var dtn = DateTime.Now;
            EntryLineAssertion(app.view.OutEntries.val, "TG",
                new ELA { is_input = false, name = "infoed_out", desc = "6 hours of tout1", value = amt });
            var tm = app.view.Instances.val[0];
            var ttm = app.view.OutTrack.val;
            TrackerTracksAssertion(
                ttm, "testyD", "test_invention", tm, "G", "H",
                new ELA { name = "daily testTarget", value = 12 + a * 2 - c * b },
                new ELA { name = "infoed_in", is_input = true, value = am7 },
                new ELA { name = "infoed_out", is_input = false, value = amt }
            );
        }
    }
}