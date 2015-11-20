using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Consonance.ConsoleView
{
	class ConsolePager
	{
		readonly ConsolePageAction ShowAction, BackAction; 
		readonly List<IConsolePage> viewStack = new List<IConsolePage> ();
		readonly Dictionary<String,Action> inputResponses = new Dictionary<string, Action>();
		public ConsolePager (IConsolePage root)
		{
			root.pageChanged = true;
			viewStack.Add (root);
			ShowAction = new ConsolePageAction() { name = "Show", argumentNames = new string[0], action = _=> { return; } };
			BackAction = new ConsolePageAction() { name = "Back", argumentNames = new string[0], action = _=> Pop() };
		}
		public void Push(IConsolePage page) 
		{
			page.pageChanged = true;
			viewStack.Insert(0,page);
		}
		public void Pop()
		{
			viewStack.RemoveAt (0);
			if (viewStack.Count > 0)
				viewStack[0].pageChanged = true;
		}
		public bool Pop(IConsolePage page)
		{
			bool rem = viewStack.Remove (page);
			if (viewStack.Count > 0)
				viewStack[0].pageChanged = true;
			return rem;
		}
		void ShowTopPage()
		{
			// get page
			var top = viewStack[0];

			// show data
			Console.WriteLine (top.pageData);

			// show and register actions
			inputResponses.Clear ();
			int ct = 1;
			foreach (var a in WithDefaultActions(top.pageActions)) {
				String k = (ct++).ToString ();
				inputResponses [k] = () =>
				{
					Console.Write(a.name + "(" + String.Join(",", a.argumentNames) + ") >");
					String[] argVals = new string[0];
					if(a.argumentNames.Length > 0) argVals = Console.ReadLine().Split(',');
					a.action(argVals);
				};
				Console.Write (k + "-" + a.name + "   ");
			}
			Console.WriteLine ();
		}

		IEnumerable<ConsolePageAction> WithDefaultActions(ConsolePageAction[] acts)
		{
//			yield return ShowAction;
			yield return BackAction;
			foreach(var a in acts) yield return a;
		}


		public void RunLoop()
		{
			StringBuilder cinput = new  StringBuilder ();
			while (viewStack.Count > 0) {
				// show this page

				Console.Clear ();
				viewStack[0].pageChanged = false;
				ShowTopPage ();

				//ask for actions(include any data entered previously after refresh)
				Console.Write ("Select Action > " + cinput);

				// get any new input until a newline
				bool breaky = false;
				while (!breaky && !viewStack[0].pageChanged) {
					while (!breaky && viewStack.Count > 0 && Console.KeyAvailable) {
						ConsoleKeyInfo next = Console.ReadKey ();
						if (next.KeyChar == 1)
							break; // from push/pop - need redisplay.
						// enter! parse the command
						cinput.Append (next.KeyChar); // ok append to buffer
						if (next.Key == ConsoleKey.Enter || cinput.Length-1 == inputResponses.Count/10) {
							String act = cinput.ToString ();
							Console.WriteLine ();
							if (inputResponses.ContainsKey (act))
								inputResponses [act] ();
							else {
								Console.WriteLine ("Invalid action, press any key");
								Console.ReadKey ();
							}
							cinput.Clear ();
							breaky = true; // gotta breaky
						}
					}
					Thread.Sleep (20); // sleepy bit poll this page changed...
				}
			}
		}
	}
	interface IConsolePage 
	{
		bool pageChanged { get; set; }
		String pageData { get; }
		ConsolePageAction[] pageActions { get; }
	}
	class ConsolePageAction
	{
		public String name;
		public String[] argumentNames;
		public Action<String[]> action;
	}
}