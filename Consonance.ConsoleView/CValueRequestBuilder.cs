using System;
using System.Linq;
using Consonance;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Consonance.ConsoleView
{
	public class CValueRequestBuilder : IValueRequestBuilder
	{
		public CValueRequestBuilder()
		{
			requestFactory = new CValueRequestFactory();
		}
		class GetValuesConsolePage : IConsolePage
		{
			public bool allowDefaultActions { get { return true; } }
			List<IValueRequestFromString> creqs = new List<IValueRequestFromString>();
			GetValuesPage mpage;
			public GetValuesPage page {
				set {
					if (mpage != null) mpage.valuerequestsChanegd -= ReqsChanged;
					mpage = value;
					mpage.valuerequestsChanegd += ReqsChanged;
					ReqsChanged (null, null);
				}
			}
			void ReqsChanged (object sender, ListChangedEventArgs e)
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
						pageBuilder.AppendFormat (i++ + ":    {0}       {1}       {2}   {3}: {4}\n", bs (!vr.enabled), bs (vr.read_only), bs (vr.valid), vr.name, vr.ToString ());
					}
					return pageBuilder.ToString ();
				}
			}
			String bs(bool e)
			{
				return e ? "x" : " ";
			}
			public Action ok = delegate { };
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
						action = args => {
							int idx = -1;
							int.TryParse (args [0], out idx);
							if (idx < 0 || idx >= PageRequests.Count || PageRequests [idx].read_only) {
								Console.WriteLine ("Cant edit that...");
								Console.ReadKey ();
								return;
							}
							var p = PageRequests [idx];
							if (!p.FromString (args.Length == 2 ? args[1] : null)) {
								Console.WriteLine ("Error with input!  Press any key to continue...");
								Console.ReadKey ();
							}
							
						}
					};
					yield return new ConsolePageAction() 
					{
						name = "ok/done",
						argumentNames = new String[0],
						action = _=> ok()
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
		public ViewTask<bool> GetValues (IEnumerable<GetValuesPage> requestPages)
		{
			var rps = new List<GetValuesPage> (requestPages);
			TaskCompletionSource<EventArgs> pushed = new TaskCompletionSource<EventArgs> ();
			TaskCompletionSource<bool> chosen = new TaskCompletionSource<bool> ();
			var pcp = new GetValuesConsolePage ();
			int pidx = 0;
			Action doNext = delegate {
				if(pidx < rps.Count)
				{
					pcp.page = rps[pidx];
					pcp.pageChanged = true;
					// aaand wait for next??
					pidx++;
				}
				else chosen.SetResult(true);
			};
			pcp.ok = doNext;
			MainClass.consolePager.Push (pcp);
			doNext ();
			var vt = new ViewTask<bool> (() => MainClass.consolePager.Pop(pcp), pushed.Task, chosen.Task);
			pushed.SetResult(null);
			return vt;
		}
		public IValueRequestFactory requestFactory { get; private set; }
		#endregion
	}
	class CValueRequestFactory : IValueRequestFactory
	{
		#region IValueRequestFactory implementation
		public IValueRequest<string> StringRequestor (string name) { return new RequestFromString<String> (name); }
		public IValueRequest<DateTime> DateRequestor (string name) { return new RequestFromString<DateTime> (name, DateTime.Parse); }
		public IValueRequest<TimeSpan> TimeSpanRequestor (string name) { return new RequestFromString<TimeSpan> (name, TimeSpan.Parse); }
		public IValueRequest<double> DoubleRequestor (string name) { return new RequestFromString<double> (name, double.Parse); }
		public IValueRequest<int> IntRequestor (string name) { return new RequestFromString<int> (name, int.Parse); }
		public IValueRequest<bool> BoolRequestor (string name) { return new RequestFromString<bool> (name, bool.Parse); }
		public IValueRequest<EventArgs> ActionRequestor (string name) { return new RequestFromString<EventArgs> (name, s => { return new EventArgs (); }); }
		public IValueRequest<Barcode> BarcodeRequestor (string name) { return new RequestFromString<Barcode> (name, s => new Barcode () { value = long.Parse (s) });}
		public IValueRequest<InfoSelectValue> InfoLineVMRequestor (string name) { return new RequestFromString<InfoSelectValue> (name, isv => isv.OnChoose ()); }
		public IValueRequest<OptionGroupValue> OptionGroupRequestor (string name)  {
			return new RequestFromString<OptionGroupValue> (name, (s, ogv) => {
				var idx = int.Parse (s);
				if(idx < 0 || idx >= ogv.OptionNames.Count)
					throw new IndexOutOfRangeException();
				ogv.SelectedOption = idx;
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
	class RequestFromString<T> : IValueRequest<T>, IValueRequestFromString
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

		public String name { get; private set;}
		Converter<String,T> cdel = t => (T)Convert.ChangeType(t,typeof(T));
		Converter<T,String> sdel = t => t == null ? "" : t.ToString();
		Action<String,T> actOnExisting =null;
		Action<T> onlyAct = null;
		public override string ToString () { return sdel (value); }
		public bool FromString (String s) { 
			try {
				if (onlyAct != null)
				{
					onlyAct (value);
					changed ();
				}
				else if (actOnExisting != null) 
				{
					actOnExisting (s, value);
					changed ();
				} else
					value = cdel (s); 
				Invalidated ();
			} catch {
				return false;
			}
			return true;
		}
		public event Action Invalidated = delegate { };
		#region IValueRequest implementation
		public event Action changed = delegate { };
		public void ClearListeners () { changed = delegate { }; }
		public object request { get { return this; } }

		T mvalue;
		public T value { 
			get { return mvalue; } 
			set { mvalue = value; Invalidated (); changed(); } 
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

