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
        public void GetValues(string title, BindingList<object> requests, Promise<bool> completed, int page, int pages)
        {
			// pile the template view into the container view! (and push pop etc)
			vrv.ClearRows();
			vrv.Title = title;
			foreach (var ob in requests)
				vrv.AddRow (ob as View);

			Func<bool,Task> cdel = async b =>  {
				vrv.completed = delegate { }; // no more pls.
				if(page == pages-1 || !b) {
					await nav.PopAsync();
					pushed=false;
				}
				completed(b);
			};
			vrv.completed = b => cdel (b);
			if (!pushed) {
				nav.PushAsync (vrv);
				pushed = true;
			} 
        }


		bool pushed = false;
		readonly ValueRequestView vrv = new ValueRequestView();
		readonly ValueRequestFactory vrf = new ValueRequestFactory();
		public IValueRequestFactory requestFactory { get { return vrf; } }
    }

	class ValueRequestFactory : IValueRequestFactory
	{
		#region IValueRequestFactory implementation
		public IValueRequest<string> StringRequestor (string name) { return new ValueRequestVM<String> (new ValueRequestTemplate ()); }
		public IValueRequest<InfoSelectValue> InfoLineVMRequestor (string name) { return new ValueRequestVM<InfoSelectValue> (new ValueRequestTemplate ()); }
		public IValueRequest<DateTime> DateRequestor (string name) { return new ValueRequestVM<DateTime> (new ValueRequestTemplate ()); }
		public IValueRequest<TimeSpan> TimeSpanRequestor (string name) { return new ValueRequestVM<TimeSpan> (new ValueRequestTemplate ()); }
		public IValueRequest<double> DoubleRequestor (string name) { return new ValueRequestVM<double> (new ValueRequestTemplate ()); }
		public IValueRequest<bool> BoolRequestor (string name) { return new ValueRequestVM<bool> (new ValueRequestTemplate ()); }
		#endregion

		class ValueRequestVM<T> : IValueRequest<T>, INotifyPropertyChanged
		{
			public ValueRequestVM(ContentView bound)
			{
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
			void OnPropertyChanged(String n) { PropertyChanged (this, new PropertyChangedEventArgs (n)); } 
			#endregion

			private T mvalue;
			public T value  { get { return mvalue; } set { mvalue = value; OnPropertyChanged("value"); } }

			private bool menabled;
			public bool enabled { get { return menabled; } set { menabled = value; OnPropertyChanged ("enabled"); } }

			private bool mvalid;
			public bool valid { get { return mvalid; } set { mvalid = value; OnPropertyChanged ("valid"); } }
			#endregion
		}
	}


}
