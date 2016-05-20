using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Consonance;
using Xamarin.Forms;
using System.Diagnostics;
using LibSharpHelp;
using System.Collections;

namespace Consonance.XamarinFormsView.PCL
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
			App.platform.UIThread(() => {
                // We're returning tasks to indicate bits in here completing, so this begininvoke is alright
                // ...i always seem to have trouble when awaiting PushAsync :/ so im making this method not wait on any ui ops
                // even though it should yield appropriately

				// Create the view!
				vrv = new ValueRequestView ();

				// Handler for when the requests we putting in change...
				ListChangedEventHandler leh = (s,e) =>
                    // the object can be modified from other threads of course.
                    // im not putting an ordering lock...should be ok.
                    App.platform.UIThread(() => Requests_ListChanged(vrv, s as BindingList<object>, e)).Wait();

				// this happens when next or ok or cancel is pressed	
				List<GetValuesPage> pages = new List<GetValuesPage>(requestPages);
				int npage = -1; // init hack
				Action<bool> PageCompletedHandler =null;
                GetValuesPage lastPushed = null;
				PageCompletedHandler = suc =>
				{
					// Either way we need to unhook the previous page
					if(!suc || npage+1 >= pages.Count) 
					{
						// We're done
						vrv.completed -= PageCompletedHandler;
                        if (npage >= 0)
                        {
                            pages[npage].valuerequests.Clear();
                            pages[npage].valuerequestsChanegd = delegate { };
                        }
                        tcs_all.SetResult(suc);
					}
					else
					{
                        // set up the next page.
					    npage++;
                        vrv.ignorevalidity = true; // dont redbox stuff thats wrong. yet.
						vrv.Title = pages[npage].title;
						(lastPushed = pages[npage]).valuerequestsChanegd = leh; // testy 
						leh(pages[npage].valuerequests, new ListChangedEventArgs(ListChangedType.Reset, -1));
					}
				};
				vrv.completed += PageCompletedHandler;
				PageCompletedHandler(true); // begin cycle

				// push the view - configure a callback to set the "im pushed" task.
				nav.PushAsync (vrv).ContinueWith(t=> tcs_push.SetResult(new EventArgs())); // push first.
			});
			return new ViewTask<bool> (() => nav.RemoveOrPopAsync (vrv),tcs_push.Task, tcs_all.Task);
        }

		void Requests_ListChanged (ValueRequestView vrv, BindingList<Object> requests, ListChangedEventArgs e)
		{
			switch (e.ListChangedType) {
			case ListChangedType.Reset:
				vrv.ClearRows (vv=>(vv.BindingContext as IValueRequestVM).ClearPropChanged());
				foreach (var ob in requests)
					vrv.AddRow ((ob as Func<ValueRequestTemplate>)());
				break;
			case ListChangedType.ItemAdded:
				vrv.InsertRow (e.NewIndex, (requests [e.NewIndex] as Func<ValueRequestTemplate>)());
				break;
			case ListChangedType.ItemChanged:
				// do not #care
				break;
			case ListChangedType.ItemDeleted:
				var v = vrv.RemoveRow (e.OldIndex);
                (v.BindingContext as IValueRequestVM).ClearPropChanged(); // cause some disconnected (disposed?) views get fucked by propertychange - Xamarin bug?
                break;
			case ListChangedType.ItemMoved:
				// do not #care
				break;
			}
		}

		readonly ValueRequestFactory vrf = new ValueRequestFactory();
		public IValueRequestFactory requestFactory { get { return vrf; } }
    }

	class ValueRequestFactory : IValueRequestFactory
	{
		#region IValueRequestFactory implementation
		public IValueRequest<string> StringRequestor (string name) { return RequestCreator<String> (name); }
		public IValueRequest<InfoSelectValue> InfoLineVMRequestor (string name) { return RequestCreator<InfoSelectValue> (name); }
		public IValueRequest<DateTime> DateRequestor (string name) { return RequestCreator<DateTime> (name); }
		public IValueRequest<TimeSpan> TimeSpanRequestor (string name) { return RequestCreator<TimeSpan> (name); }
        public IValueRequest<double> DoubleRequestor (string name) { return RequestCreator<double> (name); }
        public IValueRequest<bool> BoolRequestor (string name) { return RequestCreator<bool> (name); }
		public IValueRequest<EventArgs> ActionRequestor (string name) { return RequestCreator<EventArgs> (name, false); }
		public IValueRequest<Barcode> BarcodeRequestor (string name) { return RequestCreator<Barcode> (name); }
		public IValueRequest<int> IntRequestor (string name){ return RequestCreator<int> (name); }
		public IValueRequest<OptionGroupValue> OptionGroupRequestor (string name){ return RequestCreator<OptionGroupValue> (name); }
		public IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor (string name){ return RequestCreator<RecurrsEveryPatternValue> (name, false); }
		public IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor (string name){ return RequestCreator<RecurrsOnPatternValue> (name, false); }
		#endregion

		IValueRequest<T> RequestCreator<T>(String name, bool showName = true)
		{
            return new ValueRequestVM<T>(name, showName);
		}
	}
		
	interface IValueRequestVM : INotifyPropertyChanged
    {
        bool showName { get; }
        String name { get; }
        bool valid { get; }
        bool ignorevalid { get; set; }
        void ClearPropChanged();
    }
	class ValueRequestVM<T> : IValueRequest<T>, IValueRequestVM
	{
		public bool showName { get; private set; }
		public String name { get; set; }
		public ValueRequestVM(String name, bool showName)
		{
			this.name=name;
			this.showName = showName;
		}

		#region IValueRequest implementation
		public event Action ValueChanged = delegate { };
		public void ClearListeners () { ValueChanged = delegate { }; }
        public void ClearPropChanged() { PropertyChanged = delegate { }; }

        // Request will reference the view that this VM is bound to 
		public object request
        {
            get
            {
                return new Func<ValueRequestTemplate>(() =>
                {
                    return new ValueRequestTemplate { BindingContext = this };
                });
            }
        }

		// This are all bindings for the view to use - the generic type T is actually only relevant for the factory.
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		void OnPropertyChanged(String n) 
		{
            App.platform.UIThread(() =>
            {
                PropertyChanged(this, new PropertyChangedEventArgs(n));
                if (n == "value") ValueChanged();
            });
		}
        #endregion

		private T mvalue;
		public T value  { get { return mvalue; } set { mvalue = value; OnPropertyChanged("value"); } }

		private bool menabled;
		public bool enabled { get { return menabled; } set { menabled = value; OnPropertyChanged ("enabled"); } }

		private bool mvalid;
		public bool valid { get { return mvalid; } set { mvalid = value; OnPropertyChanged ("valid"); } }

        private bool mignorevalid;
        public bool ignorevalid { get { return mignorevalid; } set { mignorevalid = value; OnPropertyChanged("ignorevalid"); } }

        private bool mread_only;
		public bool read_only { get { return mread_only; } set { mread_only = value; OnPropertyChanged ("read_only"); } }
		#endregion
	}
}
