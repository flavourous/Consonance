using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using LibRTP;
using LibSharpHelp;
using scm = System.ComponentModel;
using Consonance.Protocol;
using static Consonance.DefaultEntryRequests;
using System.Collections.Specialized;

namespace Consonance.ConsoleView
{
	public class CValueRequestBuilder : IValueRequestBuilder
	{
		public CValueRequestBuilder()
		{
			crf = new CValueRequestFactory();
		}
		class GetValuesConsolePage : IConsolePage
		{
			public bool allowDefaultActions { get { return true; } }
			List<IValueRequestFromString> creqs = new List<IValueRequestFromString>();
			GetValuesPage mpage;
			public GetValuesPage page {
				set {
					if (mpage != null) mpage.valuerequests_CollectionChanged -= ReqsChanged;
					mpage = value;
					mpage.valuerequests_CollectionChanged += ReqsChanged;
					ReqsChanged (null, null);
				}
			}
			void ReqsChanged (object sender, NotifyCollectionChangedEventArgs e)
			{
				foreach(var c in creqs) c.Invalidated -= C_Invalidated;
				creqs = new List<IValueRequestFromString> (from s in mpage.valuerequests select s as IValueRequestFromString);
				foreach (var c in creqs) c.Invalidated += C_Invalidated;
				pageChanged = true;
			}

			void C_Invalidated ()
			{
				
			}

			#region IConsolePage implementation
			StringBuilder pageBuilder = new StringBuilder();
			public bool pageChanged { get; set; }
			public string pageData {
				get {
					pageBuilder.Clear ();
					pageBuilder.AppendLine ("  Enabled ReadOnly Valid");
					int i = 0;
					foreach (var vr in PageRequests) {
                        var ss1 = String.Format(i++ + ":    {0}       {1}       {2}   {3}: ", bs(!vr.enabled), bs(vr.read_only), bs(vr.valid), vr.name);
                        var vv = vr.ToString();
                        var pad = new string(' ', ss1.Length);
                        vv =vv.Replace(Environment.NewLine, Environment.NewLine + pad);
                        pageBuilder.AppendFormat("{0}{1}\n", ss1, vv);
					}
					return pageBuilder.ToString ();
				}
			}
			String bs(bool e)
			{
				return e ? "x" : " ";
			}
			public Action ok = delegate { }, last = delegate { };
			public ConsolePageAction[] pageActions {
				get {
					return Actions.ToArray<ConsolePageAction> ();
				}
			}
			IEnumerable<ConsolePageAction> Actions{
				get {
					yield return new ConsolePageAction () {
						name = "Edit a field",
						argumentNames = new[] { "Index", "Value" },
						action = argso => {
                            List<String> errs = new List<string>();
                            for (int i = 0; i < argso.Length; i+=2)
                            {
                                var k = argso[i];
                                var v = i + 1 < argso.Length ? argso[i + 1] : null;

                                int idx = -1;
                                int.TryParse(k, out idx);
                                if (idx < 0 || idx >= PageRequests.Count || PageRequests[idx].read_only)
                                {
                                    errs.Add(i + ":Can't edit that");
                                    continue;
                                }
                                var p = PageRequests[idx];
                                if (!p.FromString(v)) errs.Add(i + ":Error with input");
                            }
                            if(errs.Count > 0)
                            {
                                Console.WriteLine("Errors: {0}\n Press any key to continue...", String.Join("\n", errs));
                                ConsoleWrap.ReadKey();
                            }
						}
					};
					yield return new ConsolePageAction() 
					{
						name = "ok/done",
						argumentNames = new String[0],
						action = _=> ok()
					};
                    yield return new ConsolePageAction()
                    {
                        name = "last",
                        argumentNames = new String[0],
                        action = _ => last()
                    };
                }
			}
				IList<IValueRequestFromString> PageRequests {
				get {
					return new List<IValueRequestFromString> (from v in mpage.valuerequests
					                                              select v as IValueRequestFromString);
				}
			}
			#endregion
		}
		#region IValueRequestBuilder implementation
		public IInputResponse<bool> GetValues (IEnumerable<GetValuesPage> requestPages)
		{
			var rps = new List<GetValuesPage> (requestPages);
			TaskCompletionSource<bool> chosen = new TaskCompletionSource<bool> ();
			var pcp = new GetValuesConsolePage ();
			int pidx = -1;
			Action doNext = delegate {
			    pidx++;
                if (pidx < rps.Count)
                {
                    pcp.page = rps[pidx];
                    pcp.pageChanged = true;
                }
                else
                {
                    chosen.SetResult(true);
                }
			};
            Action doPrev = delegate {
                if (pidx > 0)
                {
                    pidx--;
                    pcp.page = rps[pidx];
                    pcp.pageChanged = true;
                }
            };
            pcp.ok = doNext;
            pcp.last = doPrev;
			MainClass.consolePager.Push (pcp);
			doNext ();
            return new InputResponse<bool>(pcp, chosen.Task);
		}

        public IValueRequestFactory requestFactory { get { return crf; } }
        public readonly CValueRequestFactory crf;
		#endregion
	}
	public class CValueRequestFactory : IValueRequestFactory
	{
        public InfoRequest OneInfo = new InfoRequest();
        #region IValueRequestFactory implementation
        public IValueRequest<string> StringRequestor (string name) { return new RequestFromString<String> (name); }
		public IValueRequest<DateTime> DateRequestor (string name) { return new RequestFromString<DateTime> (name, DateTime.Parse); }
		public IValueRequest<TimeSpan> TimeSpanRequestor (string name) { return new RequestFromString<TimeSpan> (name, TimeSpan.Parse); }
		public IValueRequest<double> DoubleRequestor (string name) { return new RequestFromString<double> (name, double.Parse); }
		public IValueRequest<int> IntRequestor (string name) { return new RequestFromString<int> (name, int.Parse); }
		public IValueRequest<bool> BoolRequestor (string name) { return new RequestFromString<bool> (name, bool.Parse); }
		public IValueRequest<EventArgs> ActionRequestor (string name) { return new RequestFromString<EventArgs> (name, s => { return new EventArgs (); }); }
		public IValueRequest<Barcode> BarcodeRequestor (string name) { return new RequestFromString<Barcode> (name, s => new Barcode () { value = long.Parse (s) });}
        public IValueRequest<InfoLineVM> InfoLineVMRequestor(string name, InfoManageType imt) { OneInfo.SetName(name); OneInfo.imt = imt; return OneInfo; }
		public IValueRequest<OptionGroupValue> OptionGroupRequestor (string name)  {
			return new RequestFromString<OptionGroupValue> (name, (s, ogv) => {
				var idx = int.Parse (s);
				if(idx < 0 || idx >= ogv.OptionNames.Count)
					throw new IndexOutOfRangeException();
				ogv.SelectedOption = idx;
			});
		}
		scm.DateTimeConverter dtc = new scm.DateTimeConverter ();
		public IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor (string name)
		{
			return new RequestFromString<RecurrsEveryPatternValue>(name,
				str => {
					var args = str.Split(' ');
					return new RecurrsEveryPatternValue(
						(DateTime)dtc.ConvertFromString(args[0]), 
						(Protocol.RecurrSpan)int.Parse(args[1]),
						int.Parse(args[2])
					);
				},
				patval => {
					return String.Join(" ",
						patval.PatternFixed.ToShortDateString(), 
						((int)patval.PatternType).ToString(),
						patval.PatternFrequency.ToString()
					) + String.Format(" :: Every {0} {1}s fixed at {2}", patval.PatternFrequency, patval.PatternType.ToString(), patval.PatternFixed.ToShortDateString());
				}
			);
		}
		public IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor (string name)
		{
			return new RequestFromString<RecurrsOnPatternValue>(name,
				str => {
					uint mask = 0;
					List<int> ons = new List<int>();
					var input = str.Split(' ');
					int i=0;
					for(;i<input.Length-1;i++)
					{
						var pv = input[i].Split('|');
						mask |= uint.Parse(pv[1]);
						ons.Add(int.Parse(pv[0]));
					}
					mask |= uint.Parse(input[i]);
					return new RecurrsOnPatternValue((Protocol.RecurrSpan)mask, ons.ToArray());
				},
				patval => {
					var masks = new List<uint>();
					foreach(var v in ((uint)patval.PatternType).SplitAsFlags())
						masks.Add(v);
					
					String exlp = "";
					int i=0;
					for(;i<masks.Count-1;i++)
						exlp += (i==0 ? "on " : "of ")+ (Protocol.RecurrSpan)masks[i] + " " + patval.PatternValues[i];
					exlp += " of the " + (Protocol.RecurrSpan)masks[i];

					String[] fmt = new string[masks.Count];
					for(i=0;i<masks.Count-1;i++)
						fmt[i] = patval.PatternValues[i] + "|" + masks[i];
					fmt[i] = masks[i].ToString();

					return String.Join(" ", fmt) + " :: " + exlp;
				}
			);
		}

        public IValueRequest<DateTime> DateTimeRequestor(string name)
        {
            return new RequestFromString<DateTime>(name,
                DateTime.Parse,
                val => val.ToString()
            );
        }

        public IValueRequest<DateTime?> nDateRequestor(string name)
        {
            return new RequestFromString<DateTime?>(name,
                str => str == null ? null : new DateTime?(DateTime.Parse(str)),
                val => val?.ToString() ?? "NULL"
            );
        }

        public IValueRequest<MultiRequestOptionValue> IValueRequestOptionGroupRequestor(string name)
        {
            MultiRequestOptionValue lpv = null;
            return new RequestFromString<MultiRequestOptionValue>(name,
                str => {
                    var args = str.Split(' ');
                    lpv.SelectedRequest = int.Parse(args[0]);
                    IValueRequestFromString rrs = null; int x = -1;
                    if (x == lpv.SelectedRequest)
                        rrs = lpv.HiddenRequest as IValueRequestFromString;
                    else foreach (var o in lpv.IValueRequestOptions)
                        if (++x == lpv.SelectedRequest)
                            rrs = o as IValueRequestFromString;
                    rrs.FromString(args[1]);
                    return lpv;
                },
                patval => {
                    if (patval == null) return "null";
                    lpv = patval;
                    int x = 0;
                    var opts = new List<String>();
                    Action<object, int> gg = (pv,xx) =>
                    {
                        var ss = patval.SelectedRequest == xx;
                        var gt = pv.GetType().GetGenericArguments()[0];
                        opts.Add(String.Format("{0}{1}: {2} {5}({4}){3}", ss ? "[" : "", xx, (pv as IValueRequestFromString).ToString(), ss ? "]" : "", gt.Name, (pv as IValueRequestFromString).name));
                    };
                    foreach(var pv in patval.IValueRequestOptions)
                        gg(pv, x++);
                    if(patval.SelectedRequest == -1)
                        gg(patval.HiddenRequest, -1);
                    return String.Join(" | ", opts);
                }
            );
        }

        public IValueRequest<TabularDataRequestValue> GenerateTableRequest()
        {
            return new RequestFromString<TabularDataRequestValue>("table", null,
                ts =>
                {
                    var allz = ts.Items.Concat(new object[][] { ts.Headers.Cast<Object>().ToArray() });
                    var mx = (from f in allz select f.Max(d => (d.ToString()??"").Length)).Max();

                    Func<Object[],int,String[]> padall = (arr,pad) => (from s in arr select s.ToString().PadRight(pad)).ToArray();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("|"+String.Join("|", padall(ts.Headers,mx))+"|");
                    var alll = String.Join(Environment.NewLine, (from sl in ts.Items select "|" + String.Join("|", padall(sl, mx)) + "|"));
                    sb.Append(alll);
                    return sb.ToString();
                });
        }

        #endregion
    }

    interface IValueRequestFromString 
	{
		event Action Invalidated;
		bool FromString (String s);
		String name { get; }
		bool enabled { get; }
		bool valid { get; } 
		bool read_only { get; } 
	}
    public class InfoRequest : RequestFromString<InfoLineVM>, IConsolePage
    {
        public IReadOnlyList<InfoLineVM> ininfos, outinfos;
        public void SetName(String n) { this.name = n; }
        public InfoManageType imt { get; set; }
        public InfoRequest() : base("")
        {
            onlyAct = _ => MainClass.consolePager.Push(this);
            sdel = vm => vm == null ? "none" : vm.name;
        }
        public bool allowDefaultActions { get; } = true;
        public bool pageChanged { get; set; }
        public string pageData
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                var use = imt == InfoManageType.In ? ininfos : outinfos;
                for(int i=0;i<use.Count;i++)
                    sb.AppendFormat("{0}{1}: {2}{0}", use[i] == value ? "***" : "", i, use[i].name);
                return sb.ToString();
            }
        }
        public ConsolePageAction[] pageActions
        {
            get
            {
                return new[]
                {
                    new ConsolePageAction
                    {
                        name = "choose",
                        argumentNames = new[] { "index(none)" },
                        action = args =>
                        {
                            var use = imt == InfoManageType.In ? ininfos : outinfos;
                            if(args[0] == "none") value=null;
                            else value = use[int.Parse(args[0])];
                            MainClass.consolePager.Pop();
                        }
                    },
                    new ConsolePageAction
                    {
                        name = "edit",
                        argumentNames = new[] { "index" },
                        action = args =>
                        {
                            var use = imt == InfoManageType.In ? ininfos : outinfos;
                            var cuse =(CPlanCommands.CCollectionEditorBoundCommands<InfoLineVM>) (imt == InfoManageType.In ? MainClass.plancommands.eatinfo : MainClass.plancommands.burninfo);
                            var ed = use[int.Parse(args[0])];
                            cuse.Edit(ed);
                        }
                    },
                    new ConsolePageAction
                    {
                        name = "delete",
                        argumentNames = new[] { "index" },
                        action = args =>
                        {
                            var use = imt == InfoManageType.In ? ininfos : outinfos;
                            var cuse =(CPlanCommands.CCollectionEditorBoundCommands<InfoLineVM>) (imt == InfoManageType.In ? MainClass.plancommands.eatinfo : MainClass.plancommands.burninfo);
                            var ed = use[int.Parse(args[0])];
                            cuse.Remove(ed);
                        }
                    },
                    new ConsolePageAction
                    {
                        name = "add",
                        argumentNames = new String[0],
                        action = args =>
                        {
                            var use = imt == InfoManageType.In ? ininfos : outinfos;
                            var cuse =(CPlanCommands.CCollectionEditorBoundCommands<InfoLineVM>) (imt == InfoManageType.In ? MainClass.plancommands.eatinfo : MainClass.plancommands.burninfo);
                            cuse.Add();
                        }
                    }
                };
            }
        }
    }
    public class RequestFromString<T> : IValueRequest<T>, IValueRequestFromString
	{
		public RequestFromString(String name) 
		{ this.name = name; }
		public RequestFromString(String name, Converter<String, T> convert) : this(name) 
		{ this.cdel = convert; }
		public RequestFromString(String name, Action<String,T> actOnExisting) : this(name) 
		{ this.actOnExisting= actOnExisting; }
		public RequestFromString(String name, Action<T> onlyAct) : this(name)
		{ this.onlyAct = onlyAct; } 
		public RequestFromString(String name, Converter<String, T> convert, Converter<T, String> convertBack) : this(name,convert) 
		{ this.sdel = convertBack; }

        public String name { get; protected set;}
        protected Converter<String,T> cdel = t => (T)Convert.ChangeType(t,typeof(T));
        protected Converter<T,String> sdel = t => t == null ? "" : t.ToString();
        protected Action<String,T> actOnExisting =null;
		protected Action<T> onlyAct = null;
		public override string ToString () { return sdel (value); }
		public bool FromString (String s) { 
			Action final = delegate { };
			try {
				if (onlyAct != null)
				{
					onlyAct (value);
					final = ValueChanged; 
				}
				else if (actOnExisting != null) 
				{
					actOnExisting (s, value);
					final = ValueChanged;
				} else
					value = cdel (s); 
			} catch(Exception exp) {
				return false;
			}
			final ();
			Invalidated ();
			return true;
		}
		public event Action Invalidated = delegate { };
		#region IValueRequest implementation
		public event Action ValueChanged = delegate { };
		public void ClearListeners () { ValueChanged = delegate { }; }
		public object request { get { return this; } }

		T mvalue;
		public T value { 
			get { return mvalue; } 
			set { mvalue = value; Invalidated (); ValueChanged(); } 
		}
		bool menabled;
		public bool enabled{ 
			get { return menabled; } 
			set { menabled = value; Invalidated (); } 
		}
		bool mvalid;
		public bool valid{ 
			get { return mvalid; } 
			set { mvalid = value; Invalidated (); } 
		}
		bool mread_only;
		public bool read_only{ 
			get { return mread_only; } 
			set { mread_only= value; Invalidated (); } 
		}
		#endregion
	}
    
}

