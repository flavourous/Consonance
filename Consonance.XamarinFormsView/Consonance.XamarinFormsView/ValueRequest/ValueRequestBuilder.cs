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
		public async Task GetValues(string title, BindingList<object> requests, Promise<bool> completed, int page, int pages)
		{
			// pile the template view into the container view! (and push pop etc)
			vrv.ClearRows();
			vrv.Title = title;
			foreach (var ob in requests)
				vrv.AddRow (ob as View);

			Promise<bool> cdel = async b =>  {
				vrv.completed = async delegate { }; // no more pls.
				if(page == pages-1 || !b) {
					await nav.PopAsync();
					pushed=false;
				}
				await completed(b);
			};
			vrv.completed = async b => await cdel (b);
			if (!pushed) {
				await nav.PushAsync (vrv);
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
		public IValueRequest<string> StringRequestor (string name) { return new ValueRequestVM<String> (new ValueRequestTemplate (), name); }
		public IValueRequest<InfoSelectValue> InfoLineVMRequestor (string name) { return new ValueRequestVM<InfoSelectValue> (new ValueRequestTemplate (), name); }
		public IValueRequest<DateTime> DateRequestor (string name) { return new ValueRequestVM<DateTime> (new ValueRequestTemplate (), name); }
		public IValueRequest<TimeSpan> TimeSpanRequestor (string name) { return new ValueRequestVM<TimeSpan> (new ValueRequestTemplate (), name); }
		public IValueRequest<double> DoubleRequestor (string name) { return new ValueRequestVM<double> (new ValueRequestTemplate (), name); }
		public IValueRequest<bool> BoolRequestor (string name) { return new ValueRequestVM<bool> (new ValueRequestTemplate (), name); }
		#endregion

		class ValueRequestVM<T> : IValueRequest<T>, INotifyPropertyChanged
		{
			public String name { get; set; }
			public ValueRequestVM(ContentView bound, String name)
			{
				bound.BindingContext = this;
				_request = bound;
				this.name=name;
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
