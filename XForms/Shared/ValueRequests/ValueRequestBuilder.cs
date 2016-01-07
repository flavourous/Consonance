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
			ViewWrapper.InvokeOnMainThread (async () => {
				vrv = new ValueRequestView ();
				bool success = false; 
				TaskCompletionSource<bool> tcs_each = new TaskCompletionSource<bool> ();
				Action<bool> each_handler = b => tcs_each.TrySetResult(b);
				vrv.completed += each_handler;
				ListChangedEventHandler leh = (s,e) => Requests_ListChanged(vrv, s as BindingList<object>, e);
				await nav.PushAsync (vrv); // push first.
				tcs_push.SetResult(new EventArgs());
				foreach(var requests in requestPages)
				{
					// set rows from this request, and hook changes.
					vrv.ClearRows ();
					vrv.Title = requests.title;
					foreach (var ob in requests.valuerequests)
						vrv.AddRow (ob as View);
					requests.valuerequestsChanegd = leh;

					success = await tcs_each.Task;
					tcs_each = new TaskCompletionSource<bool> (); // re-create for following ones.

					requests.valuerequestsChanegd = delegate { }; // unhook changes from this iteration.

					if(!success) break;
				}
				vrv.completed -= each_handler;
				tcs_all.SetResult(success);
			});
			return new ViewTask<bool> (() => nav.RemoveOrPop (vrv),tcs_push.Task, tcs_all.Task);
        }

		void Requests_ListChanged (ValueRequestView vrv, BindingList<Object> requests, ListChangedEventArgs e)
		{
			// the object can be modified from other threads of course.
			ViewWrapper.InvokeOnMainThread (() => {
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
		public IValueRequest<EventArgs> ActionRequestor (string name) { return new ValueRequestVM<EventArgs> (new ValueRequestTemplate (), name); }
		public IValueRequest<Barcode> BarcodeRequestor (string name) { return new ValueRequestVM<Barcode> (new ValueRequestTemplate (), name); }
		public IValueRequest<int> IntRequestor (string name){ return new ValueRequestVM<int> (new ValueRequestTemplate (), name); }
		public IValueRequest<OptionGroupValue> OptionGroupRequestor (string name){ return new ValueRequestVM<OptionGroupValue> (new ValueRequestTemplate (), name); }
		public IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor (string name){ return new ValueRequestVM<RecurrsEveryPatternValue> (new ValueRequestTemplate (), name); }
		public IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor (string name){ return new ValueRequestVM<RecurrsOnPatternValue> (new ValueRequestTemplate (), name); }
		#endregion
	}

	class ValueRequestVM<T> : IValueRequest<T>, INotifyPropertyChanged
	{
		public String name { get; set; }
		public ValueRequestVM(ContentView bound, String name)
		{
			this.name=name;
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
