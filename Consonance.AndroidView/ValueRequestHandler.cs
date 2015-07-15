using System;
using Android.Content;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using System.ComponentModel;
using Consonance;

namespace Consonance.AndroidView
{
	interface IAndroidRequestSite 
	{
		void SetPage (int current, int total);
		event Action<bool> completed;
		void AddRequestView(View v);
		void InsertRequestView(View v, int index);
		void RemoveRequestViewAt(int index);
		void ClearRequestViews();
		void SetRequestTitle(String title);
	}
	class DialogRequestBuilder : IAndroidRequestSite
	{
		#region IAndroidRequestSite implementation

		public event Action<bool> completed = delegate { };

		public void SetPage (int current, int total)
		{
			ofn.Text = total == 1 ? "Ok" : current == total - 1 ? "Finish" : "Next";
			if (current == 0) {
				// do wrapping layout and show
				gvDialog.SetContentView (rootLayout);
				gvDialog.Window.SetLayout (ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
				gvDialog.Show ();
			}
		}

		public void AddRequestView(View v)
		{
			requestArea.AddView (v);
		}

		public void InsertRequestView (View v, int index)
		{
			requestArea.AddView (v, index);
		}

		public void RemoveRequestViewAt (int index)
		{
			requestArea.RemoveViewAt (index);
		}

		public void ClearRequestViews ()
		{
			requestArea.RemoveAllViews ();
		}

		public void SetRequestTitle (string title)
		{
			ttv.Text = title;
		}

		#endregion

		readonly TextView ttv;
		readonly Dialog gvDialog;
		readonly LinearLayout requestArea;
		readonly Button cancel, ofn;
		readonly RelativeLayout rootLayout;
		public DialogRequestBuilder(Activity parent)
		{
			// root layout
			rootLayout = new RelativeLayout(parent);
			rootLayout.LayoutParameters = new ViewGroup.LayoutParams (ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

			// init dialog
			gvDialog = new Dialog (parent);
			gvDialog.RequestWindowFeature ((int)WindowFeatures.NoTitle);
			gvDialog.SetCanceledOnTouchOutside (true);

			//title bit
			// title
			ttv = new TextView (parent) { TextSize = 16 };
			RelativeLayout.LayoutParams lp1 = new RelativeLayout.LayoutParams (RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.WrapContent);
			ttv.Id = 1;
			rootLayout.AddView (ttv);

			// Value request area
			requestArea = new LinearLayout(parent);
			requestArea.Orientation = Orientation.Vertical;
			RelativeLayout.LayoutParams lp2 = new RelativeLayout.LayoutParams (RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.WrapContent);
			lp2.AddRule (LayoutRules.Below, 1);
			requestArea.LayoutParameters = lp2;
			requestArea.Id = 3;
			rootLayout.AddView (requestArea);

			// processing view
			TextView proc = new TextView(parent) { Text = "Processing", Gravity = GravityFlags.Center, TextSize = 14 };
			proc.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent,ViewGroup.LayoutParams.WrapContent);
			proc.SetPadding(10,10,10,10);

			// cancel event
			bool success = false;
			gvDialog.CancelEvent += (sender, e) => {
				if(success) gvDialog.Window.SetContentView(proc);
				completed(success);
			};

			// cancel button
			cancel = new Button (parent) { Text = "Cancel" };
			RelativeLayout.LayoutParams lp3 = new RelativeLayout.LayoutParams (RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
			lp3.AddRule (LayoutRules.AlignParentLeft);
			lp3.AddRule (LayoutRules.Below, 3);
			cancel.Click += (sender, e) => gvDialog.Cancel ();
			cancel.LayoutParameters = lp3;
			cancel.Id = 2;
			rootLayout.AddView (cancel);

			// ok/finish/next button
			ofn = new Button (parent);
			RelativeLayout.LayoutParams lp4 = new RelativeLayout.LayoutParams (RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
			lp4.AddRule (LayoutRules.AlignParentRight);
			lp4.AddRule (LayoutRules.Below, 3);
			ofn.Click += (sender, e) => {
				// set loading message and ping completed
				success=true;
				gvDialog.Cancel (); // just d7ont complete the promise! ok they are good.
			};
			ofn.LayoutParameters = lp4;
			rootLayout.AddView (ofn);
		}
	}
	class AndroidRequestBuilder : IValueRequestBuilder
	{
		readonly IAndroidRequestSite site;
		readonly ValueRequestFactory vrf;
		Action<bool> site_completed = delegate{};
		public AndroidRequestBuilder(IAndroidRequestSite site, Activity parent)
		{
			this.site = site;
			vrf = new ValueRequestFactory(parent);
			site.completed += b => site_completed(b);		
		}

		#region IValueRequestBuilder<ValueRequestWrapper> implimentation
		public IValueRequestFactory requestFactory { get { return vrf; } }
		//Promise<AddedItemVM> GetValuesPromise;
		public void GetValues (String title, BindingList<Object> requests, Promise<bool> completed, int page, int pages)
		{
			// init layout area
			site.SetRequestTitle (title);
			site.ClearRequestViews ();
			foreach (var req in requests)
				site.AddRequestView ((req as ValueRequestWrapper).inputView);

			// handle requse object changes
			ListChangedEventHandler leh = (object sender, ListChangedEventArgs e) => 
			{
				switch (e.ListChangedType) {
				case ListChangedType.ItemAdded:
					site.InsertRequestView((requests[e.NewIndex] as ValueRequestWrapper).inputView, e.NewIndex);
					break;
				case ListChangedType.ItemDeleted:
					site.RemoveRequestViewAt(e.NewIndex);
					break;
				}
			};
			requests.ListChanged += leh;
			site_completed = s => {
				requests.ListChanged -= leh;//unhook
				completed (s);
			};
			site.SetPage (page, pages);
		}
		#endregion
	}

	public abstract class ValueRequestWrapper
	{
		public readonly String name;
		protected readonly Context c;
		protected readonly LayoutInflater layoutInflater;
		public ValueRequestWrapper(String name, Context c)
		{
			this.name=name;
			this.c = c;
			this.layoutInflater = (LayoutInflater)c.GetSystemService(Context.LayoutInflaterService);
			inputView =  layoutInflater.Inflate (inputID,null);

			// nice!
			inputView.FindViewById<TextView> (Resource.Id.name).Text = name;
		}
		public Object request { get { return this; } }
		public bool enabled { set { inputView.Enabled = value; } }
		public bool valid { get; set; } 

		public event Action changed = delegate { };
		public void ClearListeners()
		{
			foreach (var d in changed.GetInvocationList())
				changed -= (Action)d;
			changed = delegate { };
		}
		protected void OnChanged()  {  changed(); }
		public View inputView { get; private set;}
		protected abstract int inputID {get;}
	}
	class StringRequestWrapper : ValueRequestWrapper, IValueRequest<String>
	{
		public StringRequestWrapper(String n, Context c) : base(n,c) 
		{
			val.TextChanged += (sender, e) => OnChanged();
		}
		public string value 
		{ 
			get { return val.Text; }
			set { val.Text = value; }
		}
		EditText val { get { return inputView.FindViewById<EditText> (Resource.Id.value); } }
		protected override int inputID { get { return Resource.Layout.ValueRequests_String; } }
	}
	class DoubleRequestWrapper : ValueRequestWrapper, IValueRequest<double>
	{
		public DoubleRequestWrapper (String n, Context c) : base(n,c) 
		{
			val.TextChanged += (sender, e) => OnChanged();
		}
		public double value 
		{ 
			get {
				double ret = 0.0;
				double.TryParse (val.Text, out ret);
				return ret;
			}
			set { val.Text = value.ToString(); }
		}
		EditText val { get { return inputView.FindViewById<EditText> (Resource.Id.value); } }
		protected override int inputID { get { return Resource.Layout.ValueRequests_Double; } }
	}
	class InfoSelectRequestWrapper : ValueRequestWrapper, IValueRequest<InfoSelectValue>
	{
		public InfoSelectRequestWrapper (String n, Context c) : base(n,c) 
		{
			var spinny = inputView.FindViewById<Spinner> (Resource.Id.values);
			spinny.ItemSelected += (sender, e) => {
				_value.selected = e.Position - 1;
				OnChanged ();
			};
		}
		InfoSelectValue _value;
		public InfoSelectValue value 
		{
			set
			{
				_value = value;
				var spinny = inputView.FindViewById<Spinner> (Resource.Id.values);
				List<InfoLineVM> quickHack = new List<InfoLineVM> { new InfoLineVM () { name = "None (Quick Entry)" } };
				quickHack.AddRange (_value.choices);
				var adapt = new LAdapter<InfoLineVM> (
					layoutInflater, 
					quickHack, 
					Resource.Layout.InfoComboVal,
					(v, i) => v.FindViewById<TextView>(Resource.Id.value).Text = i.name
				);
				spinny.Adapter = adapt; // set adapt with these choices
			}
			get { return _value; }
		}
		protected override int inputID { get { return Resource.Layout.ValueRequests_InfoSelect; } }
	}
	class DateTimeRequestWrapper : ValueRequestWrapper, IValueRequest<DateTime>
	{
		DatePickerDialog picker;
		public DateTimeRequestWrapper (String n, Context c) : base(n,c) 
		{
			vtv.Click += Gvt_Click;
			picker = new DatePickerDialog (c, picker_callback, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
		}
		void Gvt_Click (object sender, EventArgs e) { picker.Show (); }
		void picker_callback(Object sender, DatePickerDialog.DateSetEventArgs e) { value = new DateTime (e.Year, e.MonthOfYear, e.DayOfMonth); OnChanged (); }
		DateTime dateSet = DateTime.Now;
		TextView vtv { get { return inputView.FindViewById<TextView> (Resource.Id.value); } }
		public DateTime value { get { return dateSet; } set { dateSet = value; vtv.Text = value.ToString (); } }
		protected override int inputID { get { return Resource.Layout.ValueRequests_DateTime; } }
	}
	class BoolRequestWrapper : ValueRequestWrapper, IValueRequest<bool>
	{
		public BoolRequestWrapper(String n, Context c) : base(n,c) 
		{
			cb.CheckedChange += (sender, e) => OnChanged ();
		}
		CheckBox cb { get { return inputView.FindViewById<CheckBox> (Resource.Id.name); } }
		public bool value { get { return cb.Checked; } set { cb.Checked = value; } }
		protected override int inputID { get { return Resource.Layout.ValueRequests_Bool; } }
	}
	class TimeSpanRequestWrapper : ValueRequestWrapper, IValueRequest<TimeSpan>
	{
		public TimeSpanRequestWrapper (String n, Context c) : base(n,c) 
		{
			h.TextChanged+= H_TextChanged;
			m.TextChanged+= H_TextChanged;
		}
		void H_TextChanged (object sender, Android.Text.TextChangedEventArgs e) { OnChanged (); }
		EditText h { get { return inputView.FindViewById<EditText> (Resource.Id.value_h); } }
		EditText m { get { return inputView.FindViewById<EditText> (Resource.Id.value_m); } }
		public TimeSpan value { 
			get 
			{
				int hr = 0, mn = 0;
				int.TryParse (h.Text, out hr);
				int.TryParse (m.Text, out mn);
				return new TimeSpan (hr, mn, 0);
			}
			set 
			{
				h.Text = value.Hours.ToString ();
				m.Text = value.Minutes.ToString ();
			}
		}
		protected override int inputID { get { return Resource.Layout.ValueRequests_TimeSpan; } }
	}
	class ValueRequestFactory : IValueRequestFactory
	{
		readonly Android.Content.Context context;
		public ValueRequestFactory(Android.Content.Context context)
		{
			this.context = context;
		}
		#region IValueRequestFactory implementation
		public IValueRequest<string> StringRequestor(String name) { return new StringRequestWrapper (name,context); }
		public IValueRequest<double> DoubleRequestor(String name) { return new DoubleRequestWrapper (name,context); }
		public IValueRequest<TimeSpan> TimeSpanRequestor (string name) { return new TimeSpanRequestWrapper (name,context); }
		public IValueRequest<DateTime> DateRequestor(String name) { return new DateTimeRequestWrapper (name,context); }
		public IValueRequest<InfoSelectValue> InfoLineVMRequestor(String name) { return new InfoSelectRequestWrapper (name,context); }
		public IValueRequest<bool> BoolRequestor(String name) { return new BoolRequestWrapper (name,context); }
		#endregion
	}
}

