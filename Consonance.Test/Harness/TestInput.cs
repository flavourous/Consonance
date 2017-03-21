using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Consonance.Protocol;
using NUnit.Framework;
using System.Collections;

namespace Consonance.Test
{
    public class InputExpectBase : ExpectBase
    {
        public Action completing = delegate { }, closing = delegate { };
    }
    public class TestInput : IUserInput
    {
        public class ExpectedChoose : InputExpectBase
        {
            public String mode;
            public Object[] values_set;
            public InfoLineVM[] results;
            public int choose;
        }
        public readonly Queue<ExpectedChoose> ChooseExpect = new Queue<ExpectedChoose>();
        public IInputResponse<InfoLineVM> Choose(IFindList<InfoLineVM> ifnd)
        {
            return ChooseExpect.DTest(exp =>
            {
                CollectionAssert.Contains(ifnd.FindModes, exp.mode);
                var reqs = ifnd.UseFindMode(exp.mode, new TestValueRequestFactory());
                CollectionAssert.AllItemsAreInstancesOfType(reqs, typeof(TestRequest));
                Assert.AreEqual(exp.values_set.Length, reqs.Length);
                for (int i = 0; i < exp.values_set.Length; i++)
                    (reqs[i] as TestRequest).ovalue = exp.values_set[i];
                var res = ifnd.Find();
                var cc = new CCHelp<InfoLineVM>(v =>
                    new[] { v.name }
                    .Concat(v.displayAmounts
                        .SelectMany(kv => new Object[] { kv.Key, kv.Value })
                        )
                );
                CollectionAssert.AreEqual(res, exp.results, cc);
                Assert.Less(exp.choose, res.Count);
                exp.completing();

                return new TestInputResponse<InfoLineVM>(res[exp.choose], exp.closing);
            });
        }

        public class ChoosePlanExpected : InputExpectBase
        {
            public String title;
            public ItemDescriptionVM[] expect;
            public int initial, choose;
        }
        public readonly Queue<ChoosePlanExpected> ChoosePlanExpect = new Queue<ChoosePlanExpected>();
        public IInputResponse<int> ChoosePlan(string title, IReadOnlyList<ItemDescriptionVM> choose_from, int initial)
        {
            return ChoosePlanExpect.DTest(exp =>
            {
                Assert.AreEqual(exp.title, title);
                Assert.AreEqual(exp.initial, initial);
                var cc = new CCHelp<ItemDescriptionVM>(v => new[] { v.name, v.category, v.description });
                CollectionAssert.AreEqual(exp.expect, choose_from, cc);
                Assert.Less(exp.choose, choose_from.Count);
                exp.completing();
                return new TestInputResponse<int>(exp.choose, exp.closing);
            });
        }

        public class MessageExpect : InputExpectBase { public String msg; }
        public readonly Queue<MessageExpect> ExpectMessage = new Queue<MessageExpect>();
        public IInputResponse Message(string msg)
        {
            return ExpectMessage.DTest(exp =>
            {
                Assert.AreEqual(exp.msg, msg);
                exp.completing();
                return new TestInputResponse<EventArgs>(exp.closing);
            });
        }

        public class SelectStringExpected : InputExpectBase { public String title; public String[] choices; public int init, choose; }
        public readonly Queue<SelectStringExpected> SelectStringExpect = new Queue<SelectStringExpected>();
        public IInputResponse<string> SelectString(string title, IReadOnlyList<string> strings, int initial)
        {
            return SelectStringExpect.DTest(exp =>
            {
                Assert.AreEqual(exp.title, title);
                CollectionAssert.AreEqual(exp.choices, strings);
                Assert.AreEqual(exp.init, initial);
                Assert.Less(exp.choose, strings.Count);
                exp.completing();
                return new TestInputResponse<String>(strings[exp.choose], exp.closing);
            });
        }

        public class WarnConfirmExpected : InputExpectBase { public String action; public bool respond; }
        public readonly Queue<WarnConfirmExpected> WarnConfirmExpect = new Queue<WarnConfirmExpected>();
        public IInputResponse<bool> WarnConfirm(string action)
        {
            return WarnConfirmExpect.DTest(exp =>
            {
                Assert.AreEqual(exp.action, action);
                exp.completing();
                return new TestInputResponse<bool>(exp.respond, exp.closing);
            });
        }
    }
}
