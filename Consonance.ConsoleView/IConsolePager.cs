using LibSharpHelp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Consonance.ConsoleView
{
    public static class ConsoleWrap
    {
        public static char ReadKey()
        {
            if (alternateStream.Count > 0)
                return alternateStream.Dequeue();
            var c = Console.ReadKey().KeyChar;
            ReadFromConsole(new string(c, 1));
            return c;
        }
        public static String ReadLine()
        {
            char breaker = '\n';
            if (alternateStream.Contains(breaker))
            {
                StringBuilder b = new StringBuilder();
                char c;
                while (true)
                {
                    c = alternateStream.Dequeue();
                    if (c == breaker) break;
                    b.Append(c);
                }
                return b.ToString();
            }
            var s = Console.ReadLine();
            ReadFromConsole(s + "\n");
            return s;
        }
        public static bool KeyAvailable
        {
            get
            {
                return alternateStream.Count > 0 || Console.KeyAvailable;
            }
        }
        public static Queue<char> alternateStream = new Queue<char>();
        public static event Action<String> ReadFromConsole = delegate { };
    }
	class ConsolePager
	{
        class ReplayPage : IConsolePage
        {
            String[] replays = new string[0];
            readonly ConsolePager io;
            public ReplayPage(ConsolePager io)
            {
                this.io = io;
                Refrep();
            }
            void Refrep()
            {
                replays = Directory.GetFiles(".").Where(f => f.EndsWith(".replay")).ToArray();
                pageChanged = true;
            }
            public bool allowDefaultActions { get; } = true;
            public bool pageChanged { get; set; }
            public string pageData
            {
                get
                {
                    return String.Join("\n", replays.Select((s, i) => i + ": " + s));
                }
            }
            public class SavePage : IConsolePage
            {
                readonly String data;
                String name = DateTime.Now.ToString();
                Action pop;
                public SavePage(String data, Action pop)
                {
                    this.pop = pop;
                    this.data = data;
                }
                public bool allowDefaultActions { get; } = false;
                public ConsolePageAction[] pageActions
                {
                    get
                    {
                        return new ConsolePageAction[]
                        {
                            new ConsolePageAction
                            {
                                action = args =>
                                {
                                    name = args[0];
                                    pageChanged=true;
                                },
                                name = "rename",
                                argumentNames = new[] { "name" }
                            },
                            new ConsolePageAction
                            {
                                action = args =>
                                {
                                    File.WriteAllText(name + ".replay",data);
                                    this.pop();
                                },
                                name = "save",
                                argumentNames = new String[0]
                            }
                        };
                    }
                }
                public bool pageChanged { get; set; }
                public string pageData
                {
                    get
                    {
                        return "Name: " + name + "\n\n_Actions_\n" + data;
                    }
                }
            }
            public ConsolePageAction mainAction
            {
                get
                {
                    if(Recording) return new ConsolePageAction()
                    {
                        name = "Save Replay",
                        argumentNames = new string[0],
                        action = _ =>
                        {
                            ConsoleWrap.ReadFromConsole -= Io_legitKey; // no listening
                            Recording = false;
                            currentRecording.Remove(currentRecording.Length - 1, 1); // last key was save action
                            io.Push(new SavePage(currentRecording.ToString(), () => { io.Pop(); Refrep(); }));
                        }
                    };
                    else return new ConsolePageAction()
                    {
                        name = "Replay Menu",
                        argumentNames = new string[0],
                        action = _ => io.Push(this)
                    };
                }
            }
            StringBuilder currentRecording = new StringBuilder();
            private void Io_legitKey(String obj)
            {
                currentRecording.Append(obj);
            }
            bool Recording = false;
            public ConsolePageAction[] pageActions
            {
                get
                {
                    return new ConsolePageAction[]
                    {
                        new ConsolePageAction
                        {
                            argumentNames = new string[] { },
                            name = "Record",
                            action = args =>
                            {
                                io.PopToRoot();
                                currentRecording.Clear();
                                ConsoleWrap.ReadFromConsole += Io_legitKey;
                                Recording=true;
                            }
                        },
                        new ConsolePageAction
                        {
                            argumentNames = new string[] { "Recording" },
                            name = "Replay",
                            action = args =>
                            {
                                io.PopToRoot();
                                var rp = replays[int.Parse(args[0])];
                                ConsoleWrap.alternateStream=new Queue<char>(File.ReadAllText(rp).ToArray());
                            }
                        },
                        new ConsolePageAction
                        {
                            argumentNames = new string[] { "Recording" },
                            name = "Delete",
                            action = args =>
                            {
                                File.Delete(replays[int.Parse(args[0])]);
                                Refrep();
                            }
                        }
                    };
                }
            }
            
        }
        readonly ConsolePageAction ShowAction, BackAction; 
		readonly List<IConsolePage> viewStack = new List<IConsolePage> ();
		readonly Dictionary<String,Action> inputResponses = new Dictionary<string, Action>();
        readonly ReplayPage replay;
		public ConsolePager (IConsolePage root)
		{
			root.pageChanged = true;
			viewStack.Add (root);
			ShowAction = new ConsolePageAction() { name = "Show", argumentNames = new string[0], action = _=> { return; } };
			BackAction = new ConsolePageAction() { name = "Back", argumentNames = new string[0], action = _=> Pop() };
            replay = new ReplayPage(this);
        }
        
		public void Push(IConsolePage page) 
		{
			page.pageChanged = true;
			viewStack.Insert(0,page);
		}
        public void PopToRoot()
        {
            while (viewStack.Count > 1)
                Pop();
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
			foreach (var a in top.allowDefaultActions ? WithDefaultActions(top.pageActions) : top.pageActions ) {
				String k = (ct++).ToString ();
				inputResponses [k] = () =>
				{
					Console.Write(a.name + "(" + String.Join(",", a.argumentNames) + ") >");
					String[] argVals = new string[0];
                    if (a.argumentNames.Length > 0)
                    {
                        var cl = ConsoleWrap.ReadLine();
                        argVals = cl.QuotedSplit(',', new[] { '\'', '\"' }).ToArray();
                    }
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
            if(!(viewStack[0] is ReplayPage)) yield return replay.mainAction;
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
					while (!breaky && viewStack.Count > 0 && ConsoleWrap.KeyAvailable) {
						char next = ConsoleWrap.ReadKey ();
						if (next == 1)
							break; // from push/pop - need redisplay.
						// enter! parse the command
						cinput.Append (next); // ok append to buffer
						if (next == '\n' || cinput.Length-1 == inputResponses.Count/10) {
							String act = cinput.ToString ();
							Console.WriteLine ();
							if (inputResponses.ContainsKey (act))
								inputResponses [act] ();
							else {
								Console.WriteLine ("Invalid action, press any key");
								ConsoleWrap.ReadKey ();
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
		bool allowDefaultActions { get; }
	}
	public class ConsolePageAction
	{
		public String name;
		public String[] argumentNames;
		public Action<String[]> action;
	}
}