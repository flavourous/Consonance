using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using Consonance.Protocol;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Consonance;
using Xamarin.Forms;
using System.Collections.Specialized;
using System.Diagnostics;
using LibSharpHelp;
using System.Collections;

namespace Consonance.XamarinFormsView.PCL
{
    class ValueRequestBuilder : IValueRequestBuilder
    {
		readonly CommonServices srv;
		public ValueRequestBuilder(CommonServices srv)
		{
            this.srv = srv;
            this.requestFactory = new ValueRequestFactory(srv);
        }

        public IInputResponse<bool> GetValues(IEnumerable<GetValuesPage> requestPages)
        {
            return GetValuesImpl(requestPages, new ValueRequestView());
        }

        IInputResponse<bool> GetValuesImpl(IEnumerable<GetValuesPage> requestPages, ValueRequestView vrv)
        { 
			TaskCompletionSource<bool> tcs_all = new TaskCompletionSource<bool> ();
			TaskCompletionSource<EventArgs> tcs_push = new TaskCompletionSource<EventArgs> ();
			App.UIThread(() => {
                // We're returning tasks to indicate bits in here completing, so this begininvoke is alright
                // ...i always seem to have trouble when awaiting PushAsync :/ so im making this method not wait on any ui ops
                // even though it should yield appropriately

                // Handler for when the requests we putting in change...
                NotifyCollectionChangedEventHandler leh = (s,e) =>
                    // the object can be modified from other threads of course.
                    // im not putting an ordering lock...should be ok.
                    App.UIThread(() => Requests_ListChanged(vrv, s as IList<object>, e)).Wait();

				// this happens when next or ok or cancel is pressed	
				List<GetValuesPage> pages = new List<GetValuesPage>(requestPages);
				int npage = -1; // init hack
				Action<bool> PageCompletedHandler =null;
                GetValuesPage lastPushed = null;
                PageCompletedHandler = suc =>
                {
                    if (npage >= 0)
                    {
                        pages[npage].valuerequests_CollectionChanged -= leh;
                        vrv.vlist.ClearRows();
                    }
                    // Either way we need to unhook the previous page
                    if (!suc || npage + 1 >= pages.Count)
                    {
                        // We're done
                        vrv.completed -= PageCompletedHandler;
                        tcs_all.SetResult(suc);
                    }
                    else
                    {
                        npage++;
                        // set up the next page.
                        vrv.ignorevalidity = true; // dont redbox stuff thats wrong. yet.
                        vrv.Title = pages[npage].title;
                        (lastPushed = pages[npage]).valuerequests_CollectionChanged += leh;
                        Requests_ListChanged(vrv, pages[npage].valuerequests, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

                        // already UI thread for pagecomplete handler
                        var plr = pages[npage].valuerequests.FirstOrDefault(r=>r is IValueRequest<TabularDataRequestValue>);
                        if (plr != null) vrv.ListyMode(plr as IValueRequest<TabularDataRequestValue>);
                        else vrv.NormalMode();
                    }
                };
				vrv.completed = PageCompletedHandler;
				PageCompletedHandler(true); // begin cycle

				// push the view - configure a callback to set the "im pushed" task.
				srv.nav.PushAsync (vrv).ContinueWith(t=> tcs_push.SetResult(new EventArgs())); // push first.
			});
			return new ViewTask<bool> (() => srv.nav.RemoveOrPopAsync (vrv),tcs_push.Task, tcs_all.Task);
        }

		void Requests_ListChanged (ValueRequestView vrv, IList<Object> requests, NotifyCollectionChangedEventArgs e)
		{
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    vrv.vlist.ClearRows();
                    foreach (var ob in requests)
                        vrv.vlist.AddRow((ob as Func<ValueRequestTemplate>)());
                    break;
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++)
                        vrv.vlist.InsertRow(e.NewStartingIndex + i, (e.NewItems[i] as Func<ValueRequestTemplate>)());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    for (int i = e.OldItems.Count; i >=0; i--)
                        vrv.vlist.RemoveRow(e.OldStartingIndex+i); 
                    break;
            }
		}

        public IValueRequestFactory requestFactory { get; private set; }
    }

	class ValueRequestFactory : IValueRequestFactory
	{
        readonly CommonServices srv;
        public ValueRequestFactory(CommonServices srv)
        {
            this.srv = srv;
        }

		#region IValueRequestFactory implementation
		public IValueRequest<string> StringRequestor (string name) { return RequestCreator<String, StringRequest> (name); }
		public IValueRequest<InfoLineVM> InfoLineVMRequestor (string name, InfoManageType mt)
        {
            return RequestCreator<InfoLineVM, InfoSelectRequest>(name, true, isr =>
            {
                isr.requestit = ivm => srv.U_InfoView(true, true, mt, ivm);
            });
        }
		public IValueRequest<DateTime> DateTimeRequestor (string name) { return RequestCreator<DateTime, DateTimeRequest> (name); }
        public IValueRequest<DateTime> DateRequestor(string name) { return RequestCreator<DateTime, DateRequest>(name); }
        public IValueRequest<DateTime?> nDateRequestor(string name) { return RequestCreator<DateTime?, nDateRequest>(name); }
        public IValueRequest<TimeSpan> TimeSpanRequestor (string name) { return RequestCreator<TimeSpan, TimeSpanRequest> (name); }
        public IValueRequest<double> DoubleRequestor (string name) { return RequestCreator<double, DoubleRequest> (name); }
        public IValueRequest<bool> BoolRequestor (string name) { return RequestCreator<bool, BoolRequest> (name); }
		public IValueRequest<EventArgs> ActionRequestor (string name) { return RequestCreator<EventArgs, ActionRequest> (name, false); }
		public IValueRequest<Barcode> BarcodeRequestor (string name) { return RequestCreator<Barcode, BarcodeRequest> (name); }
		public IValueRequest<int> IntRequestor (string name){ return RequestCreator<int, IntRequest> (name); }
		public IValueRequest<OptionGroupValue> OptionGroupRequestor (string name){ return RequestCreator<OptionGroupValue, OptionGroupValueRequest> (name); }
		public IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor (string name){ return RequestCreator<RecurrsEveryPatternValue, RecurrsEveryPatternValueRequest> (name); }
		public IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor (string name){ return RequestCreator<RecurrsOnPatternValue, RecurrsOnPatternValueRequest> (name); }
        public IValueRequest<MultiRequestOptionValue> IValueRequestOptionGroupRequestor(String name) { return RequestCreator<MultiRequestOptionValue, MultiRequestCombo>(name); }
        public IValueRequest<TabularDataRequestValue> GenerateTableRequest() { return RequestCreator<TabularDataRequestValue, ContentView>(null, false, delegate { }); }

        #endregion

        IValueRequest<T> RequestCreator<T, V>(String name, bool showName = true, Action<V> init = null) where V : View, new()
		{
            return new ValueRequestVM<T, V>(name, showName, init);
		}
    }
		
	interface IValueRequestVM : INotifyPropertyChanged
    {
        bool showName { get; }
        String name { get; }
        bool valid { get; }
        bool ignorevalid { get; set; }
        void ClearPropChanged();
        void RaiseValueChanged();
    }
	class ValueRequestVM<T,V> : IValueRequest<T>, IValueRequestVM where V : View, new()
	{
		public bool showName { get; private set; }
		public String name { get; set; }
        readonly Action<V> init;
		public ValueRequestVM(String name, bool showName, Action<V> init)
		{
			this.name=name;
			this.showName = showName && name != null;
            this.init = init;
		}

		#region IValueRequest implementation
		public event Action ValueChanged = delegate { };
		public void ClearListeners () { ValueChanged = delegate { }; }
        public void ClearPropChanged() { PropertyChanged = delegate { }; }
        public void RaiseValueChanged() { OnPropertyChanged("value"); }

        // Request will reference the view that this VM is bound to 
		public object request
        {
            get
            {
                return new Func<ValueRequestTemplate>(() =>
                {
                    var v = new V();
                    var ret = new ValueRequestTemplate(v) { BindingContext = this };
                    init?.Invoke(v);
                    return ret;
                });
            }
        }

		// This are all bindings for the view to use - the generic type T is actually only relevant for the factory.
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		void OnPropertyChanged(String n) 
		{
            App.UIThread(() =>
            {
                PropertyChanged(this, new PropertyChangedEventArgs(n));
                if (n == "value")
                {
                    if(onvalue != null) onvalue.PropertyChanged -= Onvalue_PropertyChanged;
                    onvalue = value as INotifyPropertyChanged;
                    if (onvalue != null) onvalue.PropertyChanged += Onvalue_PropertyChanged;
                    ValueChanged();
                }
            });
		}

        void Onvalue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("value"); // chained
        }
        #endregion

        private INotifyPropertyChanged onvalue = null;
		private T mvalue;
		public T value  { get { return mvalue; } set { mvalue = value; OnPropertyChanged("value"); } }

		private bool menabled;
		public bool enabled { get { return menabled; } set { menabled = value; OnPropertyChanged ("enabled"); } }

		private bool mvalid;
		public bool valid { get { return mvalid; } set { mvalid = value; OnPropertyChanged ("valid"); OnPropertyChanged("vvalid"); } }

        private bool mignorevalid;
        public bool ignorevalid { get { return mignorevalid; } set { mignorevalid = value; OnPropertyChanged("ignorevalid"); OnPropertyChanged("vvalid"); } }

        private bool mread_only;
		public bool read_only { get { return mread_only; } set { mread_only = value; OnPropertyChanged ("read_only"); } }
		#endregion

        public bool vvalid { get { return ignorevalid || valid; } }
	}
}
