using System;
using System.Linq;
using Consonance;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using LibSharpHelp;
using Consonance.Protocol;
using System.Collections.Specialized;

namespace Consonance.ConsoleView
{
    class InputResponse : IInputResponse
    {
        readonly IConsolePage page;
        public InputResponse(IConsolePage page, Task complete)
        {
            this.page = page;
            this.Result = complete;
        }

        public Task Opened { get { return Task.FromResult(0); } }
        public Task Result { get; set; }
        public Task Close()
        {
            MainClass.consolePager.Pop(page);
            return Task.FromResult<EventArgs>(null);
        }
    }
    class InputResponse<T> : InputResponse, IInputResponse<T>
    {
        public InputResponse(IConsolePage page, Task<T> tsk) : base(page, tsk)
        {
            this.tt = tsk;
        }
        readonly Task<T> tt;
        Task<T> IInputResponse<T>.Result { get { return tt; } }
    }
	public class CInput : IUserInput
	{
		readonly IValueRequestFactory df;
		readonly CPlanCommands pc;
		public CInput(CPlanCommands pc, IValueRequestFactory df)
		{
			this.pc = pc;
			this.df = df;
		}
		#region IUserInput implementation
		public IInputResponse<String> SelectString (string title, IReadOnlyList<string> strings, int initial)
		{
			Console.WriteLine ("Not Implimented");
			throw new NotImplementedException ();
		}
		class PlanChoosePage : IConsolePage
		{
			public bool allowDefaultActions { get { return true; } }
			readonly String title;
			readonly List<String> options;
			readonly Action<int> choose;
			public PlanChoosePage(String title, IEnumerable<String> options, Action<int> choose)
			{
				this.title=title;
				this.options = new List<string>(options);
				this.choose=choose;
			}

			#region IConsolePage implementation
			public bool pageChanged { get; set; }
			public string pageData 
			{
				get 
				{
					return "Choose a tracker";
				}
			}
			public ConsolePageAction[] pageActions {
				get {
					return new List<ConsolePageAction> (EnumActions()).ToArray ();
				}
			}
			IEnumerable<ConsolePageAction> EnumActions()
			{
				for(int i=0;i<options.Count;i++) {
					int li = i;
					yield return new ConsolePageAction () {
						name = options[i],
						action = _ => choose(li),
						argumentNames = new string[0]
					};	
				}
			}
			#endregion
		}
		public IInputResponse<int> ChoosePlan (string title, System.Collections.Generic.IReadOnlyList<ItemDescriptionVM> choose_from, int initial)
		{
			TaskCompletionSource<EventArgs> pushed = new TaskCompletionSource<EventArgs> ();
			TaskCompletionSource<int> chosen = new TaskCompletionSource<int> ();
			var pcp = new PlanChoosePage (title, from d in choose_from select d.name, chosen.SetResult);
			MainClass.consolePager.Push(pcp);
			var vt = new InputResponse<int> (pcp, chosen.Task);
			pushed.SetResult(null);
			return vt;
		}
		class WarnPage : IConsolePage
		{
			public bool allowDefaultActions { get { return false; } }
			readonly String msg;
			public readonly TaskCompletionSource<bool> fin = new TaskCompletionSource<bool>();
			public WarnPage(String action)
			{
				this.msg=action;
			}
			#region IConsolePage implementation
			public bool pageChanged { get; set; }
			public string pageData { get { return "Warning: " + msg; } }
			public ConsolePageAction[] pageActions {
				get {
					return new[] { 
						new ConsolePageAction () { name = "Ok", argumentNames = new String[0], action = _ => {
								MainClass.consolePager.Pop(this);
								fin.SetResult(true);
							}
						},
						new ConsolePageAction () { name = "Cancel", argumentNames = new String[0], action = _ => {
								MainClass.consolePager.Pop(this);
								fin.SetResult(false);
							}
						}
					};
				}
			}
			#endregion
		}
		public IInputResponse<bool> WarnConfirm (string action)
		{
			var v = new WarnPage (action);
			MainClass.consolePager.Push (v);
            return new InputResponse<bool>(v, v.fin.Task);
		}
		class MessagePage : IConsolePage
		{
			public bool allowDefaultActions { get { return false; } }
			readonly String msg;
			readonly TaskCompletionSource<EventArgs> fin;
			public MessagePage(String msg, TaskCompletionSource<EventArgs> fin)
			{
				this.msg=msg;
				this.fin = fin;
			}
			#region IConsolePage implementation
			public bool pageChanged { get; set; }
			public string pageData { get { return msg; } }
			public ConsolePageAction[] pageActions {
				get {
					return new[] { 
						new ConsolePageAction () { name = "Ok", argumentNames = new String[0], action = _ => {
								MainClass.consolePager.Pop (this);
								fin.SetResult (new EventArgs ());
							}
						},
					};
					}
				}

			#endregion
		}
		public IInputResponse Message (string msg)
		{
			TaskCompletionSource<EventArgs> ts = new TaskCompletionSource<EventArgs> ();
			var v = new MessagePage (msg, ts);
			MainClass.consolePager.Push (v);
            return new InputResponse(v, ts.Task);
		}
		class CInfoView : IConsolePage
		{
			public bool allowDefaultActions { get { return true; } }
			readonly bool sel;
			readonly TaskCompletionSource<InfoLineVM> select;
			readonly IList<InfoLineVM> items;
			readonly InfoLineVM selected;
			readonly CPlanCommands.CCollectionEditorBoundCommands<InfoLineVM> commands;
			public CInfoView(bool sel, TaskCompletionSource<InfoLineVM> select, IList<InfoLineVM> itemsThatUpdate, InfoLineVM initSel, CPlanCommands.CCollectionEditorBoundCommands<InfoLineVM> commands)
			{
				this.commands=commands;
				this.sel=sel;
				this.select = select;
				this.selected = initSel;
				this.items = itemsThatUpdate;
				if(itemsThatUpdate is INotifyCollectionChanged)
                    (itemsThatUpdate as INotifyCollectionChanged).CollectionChanged += (sender, e) => pageChanged = true;
			}
			#region IConsolePage implementation
			public bool pageChanged { get; set; }
			public string pageData {
				get {
					StringBuilder sb = new StringBuilder ();
					sb.AppendLine ((sel ? "Select " : "Manage ") + "Infos");
					sb.AppendLine ("============");
					for (int i = 0; i < items.Count; i++)
						sb.AppendFormat ("{0}: {1}\n", i, items [i].name);
					return sb.ToString ();
				}
			}
			public ConsolePageAction[] pageActions {
				get {
					List<ConsolePageAction> ret = new List<ConsolePageAction> ();
					ret.Add (new ConsolePageAction () { name = "Add", argumentNames = new String[0], action = _=> commands.Add() }); 
					ret.Add (new ConsolePageAction () { name = "Remove", argumentNames = new String[1], action = _=> IndexAction(_,commands.Remove) }); 
					ret.Add (new ConsolePageAction () { name = "Edit", argumentNames = new String[1], action = _=> IndexAction(_,commands.Edit) }); 
					ret.Add (new ConsolePageAction () { name = "Select", argumentNames = new String[1], action = _ => {
							// complete task source and pop
							IndexAction (_, a => {
								select.SetResult (a);
								MainClass.consolePager.Pop ();
							});
						}
					});
					ret.Add (new ConsolePageAction () { name = "Select Nothing", argumentNames = new String[0], action = _ => {
							select.SetResult (null);
							MainClass.consolePager.Pop ();
						}
					});
					return ret.ToArray ();
				}
			}
			void IndexAction(String[] idx, Action<InfoLineVM> act)
			{
				int i = 0;
				if(idx.Length != 1 || !int.TryParse(idx[0], out i))
				{
					Console.WriteLine ("Failed to parse...");
					ConsoleWrap.ReadKey ();
				}
				else 
				{
					act (items [i]);
				}
			}
			#endregion
		}
		class CFindyChooseView : IConsolePage
		{
			public bool allowDefaultActions { get { return true; } }
			IReadOnlyList<InfoLineVM> clines = new List<InfoLineVM>();
			IValueRequestFromString[] creqs;
			int modeSelected = -1;
			readonly IFindList<InfoLineVM> ifnd;
			readonly TaskCompletionSource<InfoLineVM> complete;
			readonly IValueRequestFactory fact;
			public CFindyChooseView(IFindList<InfoLineVM> ifnd, TaskCompletionSource<InfoLineVM> complete, IValueRequestFactory fact)
			{
				this.fact = fact;
				this.ifnd=ifnd;
				this.complete = complete;
			}
			#region IConsolePage implementation
			public bool pageChanged {get;set;}
			public string pageData {
				get {
					StringBuilder sb = new StringBuilder ();
					sb.Append ("ModeFindy\n====================\nModes: ");
					for(int i=0;i<ifnd.FindModes.Length;i++)
					{
						String ss = i == modeSelected ? "-" : "";
						sb.Append(ss + ifnd.FindModes [i] + ss + "  ");
					}
					if (creqs != null) 
					{
						sb.AppendLine ("\nSearch parameters:");
						for (int i = 0; i < creqs.Length; i++) {
							var r = creqs [i];
							sb.AppendLine (i + " - " + r.name + ":" + r.ToString ());					
						}
					}
					if (clines.Count > 0) {
						sb.Append("Results\n------------------\n");
						for (int i = 0; i < clines.Count; i++)
							sb.AppendLine(i + ": " + clines [i].name);
					}
					return sb.ToString ();
				}
			}
			public ConsolePageAction[] pageActions {
				get {
					List<ConsolePageAction> cpa = new List<ConsolePageAction> ();
					cpa.Add (
						new ConsolePageAction () { name = "Choose Mode", argumentNames = new[] { "Index" }, action = al => {
								int idx = -1;
								if (al.Length == 0 || !int.TryParse (al [0], out idx) || idx < 0 || idx >= ifnd.FindModes.Length) {
									Console.WriteLine ("input error");
									ConsoleWrap.ReadKey ();
								} else {
									modeSelected = idx;
									creqs = ifnd.UseFindMode (ifnd.FindModes [modeSelected], fact).MakeList(f=>f as IValueRequestFromString).ToArray();
								}
							}
						});
					if (creqs != null) {
						cpa.Add (
							new ConsolePageAction () { name = "Edit req", argumentNames = new[] { "Index", "Value" }, action = al => {
									int idx = -1;
									if (al.Length != 2 || !int.TryParse (al [0], out idx) || idx < 0 || idx >= creqs.Length
									    || !creqs [idx].FromString (al [1])) {
										Console.WriteLine ("input error");
										ConsoleWrap.ReadKey ();
									} 
								}
							});
						cpa.Add (new ConsolePageAction () { name = "Search", argumentNames = new string[0], action = al => {
								clines = ifnd.Find();
								pageChanged=true;
							}
						});
					}
					if (clines.Count > 0) {
						cpa.Add (new ConsolePageAction () { name = "Select Result", argumentNames = new [] { "index" } , action = al => {
								int idx = -1;
								if (al.Length == 0 || !int.TryParse (al [0], out idx) || idx < 0 || idx >= clines.Count) {
									Console.WriteLine ("input error");
									ConsoleWrap.ReadKey ();
								} else {
									complete.SetResult(clines[idx]);
									MainClass.consolePager.Pop (this);
								}
							}
						});
					}

					return cpa.ToArray ();
				}
			}
			#endregion
		}
		public IInputResponse<InfoLineVM> Choose (IFindList<InfoLineVM> ifnd)
		{
			TaskCompletionSource<InfoLineVM> chosen = new TaskCompletionSource<InfoLineVM> ();
			var view = new CFindyChooseView(ifnd, chosen, df);
			MainClass.consolePager.Push(view);
            return new InputResponse<InfoLineVM>(view, chosen.Task);
		}
		#endregion
	}
}

