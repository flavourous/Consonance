using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Consonance;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
    class ValueRequestBuilder : IValueRequestBuilder
    {
		INavigation nav;
		public ValueRequestBuilder(INavigation nav)
		{
			this.nav = nav;
		}
		public ViewTask<bool> GetValues (IEnumerable<GetValuesPage> requestPages)
		{
			ValueRequestView vrv = null;
			TaskCompletionSource<bool> tcs_all = new TaskCompletionSource<bool> ();
			TaskCompletionSource<EventArgs> tcs_push = new TaskCompletionSource<EventArgs> ();
			Device.BeginInvokeOnMainThread(() => {
				// We're returning tasks to indicate bits in here completing, so this begininvoke is alright
				// ...i always seem to have trouble when awaiting PushAsync :/ so im making this method not wait on any ui ops
				// even though it should yield appropriately

				// Create the view!
				vrv = new ValueRequestView ();

				// Handler for when the requests we putting in change...
				ListChangedEventHandler leh = (s,e) => Requests_ListChanged(vrv, s as BindingList<object>, e);

				// this happens when next or ok or cancel is pressed	
				List<GetValuesPage> pages = new List<GetValuesPage>(requestPages);
				int npage = -1; // init hack
				Action<bool> PageCompletedHandler =null;
				PageCompletedHandler = suc =>
				{
					// Either way we need to unhook the previous page
					if(npage >=0) pages[npage].valuerequestsChanegd = delegate { };
					npage++;
					if(!suc || npage >= pages.Count) 
					{
						// We're done
						vrv.completed -= PageCompletedHandler;
						tcs_all.SetResult(suc);  
					}
					else 
					{
						// set up the next page.
						vrv.Title = pages[npage].title;
						pages[npage].valuerequestsChanegd = leh;
						leh(pages[npage].valuerequests, new ListChangedEventArgs(ListChangedType.Reset, -1));
					}
				};
				vrv.completed += PageCompletedHandler;
				PageCompletedHandler(true); // begin cycle

				// push the view - configure a callback to set the "im pushed" task.
				nav.PushAsync (vrv).ContinueWith(t=> tcs_push.SetResult(new EventArgs())); // push first.
			});
			return new ViewTask<bool> (() => nav.RemoveOrPop (vrv),tcs_push.Task, tcs_all.Task);
        }

		void Requests_ListChanged (ValueRequestView vrv, BindingList<Object> requests, ListChangedEventArgs e)
		{
			// the object can be modified from other threads of course.
			// im not putting an ordering lock...should be ok.
			Device.BeginInvokeOnMainThread(() => {
				switch (e.ListChangedType) {
				case ListChangedType.Reset:
					vrv.ClearRows ();
					foreach (var ob in requests)
						vrv.AddRow (ob as View);
					break;
				case ListChangedType.ItemAdded:
					vrv.InsertRow (e.NewIndex, requests [e.NewIndex] as View);
					break;
				case ListChangedType.ItemChanged:
				// do not #care
					break;
				case ListChangedType.ItemDeleted:
					vrv.RemoveRow (e.NewIndex);
					break;
				case ListChangedType.ItemMoved:
				// do not #care
					break;
				}
			});
		}

		readonly ValueRequestFactory vrf = new ValueRequestFactory();
		public IValueRequestFactory requestFactory { get { return vrf; } }
    }

	class ValueRequestFactory : IValueRequestFactory
	{
		#region IValueRequestFactory implementation
		public IValueRequest<string> StringRequestor (string name) { return new ValueRequestVM<String> (new ValueRequestTemplate (), name); }
		public IValueRequest<InfoSelectValue> InfoLineVMRequestor (string name) { return new ValueRequestVM<InfoSelectValue> (new ValueRequestTemplate (), name); }
		public IValueRequest<DateTime> DateRequestor (string name) { return new ValueRequestVM<DateTime> (new ValueRequestTemplate (), name); }
		public IValueRequest<TimeSpan> TimeSpanRequestor (string name) { return new ValueRequestVM<TimeSpan> (new ValueRequestTemplate (), name); }
		public IValueRequest<double> DoubleRequestor (string name) { return new ValueRequestVM<double> (new ValueRequestTemplate (), name); }
		public IValueRequest<bool> BoolRequestor (string name) { return new ValueRequestVM<bool> (new ValueRequestTemplate (), name); }
		public IValueRequest<EventArgs> ActionRequestor (string name) { return new ValueRequestVM<EventArgs> (new ValueRequestTemplate (), name, false); }
		public IValueRequest<Barcode> BarcodeRequestor (string name) { return new ValueRequestVM<Barcode> (new ValueRequestTemplate (), name); }
		public IValueRequest<int> IntRequestor (string name){ return new ValueRequestVM<int> (new ValueRequestTemplate (), name); }
		public IValueRequest<OptionGroupValue> OptionGroupRequestor (string name){ return new ValueRequestVM<OptionGroupValue> (new ValueRequestTemplate (), name); }
		public IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor (string name){ return new ValueRequestVM<RecurrsEveryPatternValue> (new ValueRequestTemplate (), name, false); }
		public IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor (string name){ return new ValueRequestVM<RecurrsOnPatternValue> (new ValueRequestTemplate (), name, false); }
		#endregion
	}
		
	interface IShowName { bool showName { get; } String name { get; } }
	class ValueRequestVM<T> : IValueRequest<T>, INotifyPropertyChanged, IShowName
	{
		public bool showName { get; private set; }
		public String name { get; set; }
		public ValueRequestVM(ValueRequestTemplate bound, String name, bool showName = true)
		{
			this.name=name;
			this.showName = showName;
			bound.BindingContext = this;
			_request = bound;
		}

		#region IValueRequest implementation
		public event Action changed = delegate { };
		public void ClearListeners () { changed = delegate { }; }

		// Request will reference the view that this VM is bound to 
		readonly ContentView _request;
		public object request { get { return _request; } }

		// This are all bindings for the view to use - the generic type T is actually only relevant for the factory.
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		public void OnPropertyChanged(String n) { PropertyChanged (this, new PropertyChangedEventArgs (n)); } 
		#endregion

		private T mvalue;
		public T value  { get { return mvalue; } set { mvalue = value; OnPropertyChanged("value"); changed (); } }

		private bool menabled;
		public bool enabled { get { return menabled; } set { menabled = value; OnPropertyChanged ("enabled"); } }

		private bool mvalid;
		public bool valid { get { return mvalid; } set { mvalid = value; OnPropertyChanged ("valid"); } }

		private bool mread_only;
		public bool read_only { get { return mread_only; } set { mread_only = value; OnPropertyChanged ("read_only"); } }
		#endregion
	}
}
