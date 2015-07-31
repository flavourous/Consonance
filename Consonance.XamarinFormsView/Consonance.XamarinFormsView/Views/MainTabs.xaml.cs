using System;
using System.ComponentModel;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class MainTabs : TabbedPage
	{
		public MainTabs ()
		{
			InitializeComponent ();
			BindingContext = this;
		}

		//////////////
		// Commands //
		//////////////

		/// Plan
		public event Action AddPlan = delegate { };
		void AddPlanClick(Object sender, EventArgs args) { AddPlan(); }
		public event Action<TrackerInstanceVM> PlanItemEdit = delegate { };
		void OnPlanItemEdit(Object s, EventArgs e) { PlanItemEdit ((((MenuItem)s).BindingContext as TrackerInstanceVM)); }
		public event Action<TrackerInstanceVM> PlanItemDelete = delegate { };
		void OnPlanItemDelete(Object s, EventArgs e) { PlanItemDelete ((((MenuItem)s).BindingContext as TrackerInstanceVM)); }

		/// In
		public event Action AddIn = delegate { };
		void AddInClick(Object sender, EventArgs args) { AddIn(); }
		public event Action<EntryLineVM> InItemEdit = delegate { };
		void OnInItemEdit(Object s, EventArgs e) { InItemEdit ((((MenuItem)s).BindingContext as EntryLineVM)); }
		public event Action<EntryLineVM> InItemDelete = delegate { };
		void OnInItemDelete(Object s, EventArgs e) { InItemDelete ((((MenuItem)s).BindingContext as EntryLineVM)); }
		public event Action InInfoManage = delegate { };
		void InInfoManageClick(Object s, EventArgs e) { InInfoManage (); }


		/// Out
		public event Action AddOut = delegate { };
		void AddOutClick(Object sender, EventArgs args) { AddOut(); }
		public event Action<EntryLineVM> OutItemEdit = delegate { };
		void OnOutItemEdit(Object s, EventArgs e) { OutItemEdit ((((MenuItem)s).BindingContext as EntryLineVM)); }
		public event Action<EntryLineVM> OutItemDelete = delegate { };
		void OnOutItemDelete(Object s, EventArgs e) { OutItemDelete ((((MenuItem)s).BindingContext as EntryLineVM)); }
		public event Action OutInfoManage = delegate { };
		void OutInfoManageClick(Object s, EventArgs e) { OutInfoManage (); }

		////////////////////////
		// Data Context stuff //
		////////////////////////

		// Tab Names
		private String mInTabName = "In";
		public String InTabName { get { return mInTabName; } set { mInTabName = value; OnPropertyChanged("InTabName"); } }
		private String mOutTabName = "Out";
		public String OutTabName { get { return mOutTabName; } set { mOutTabName = value; OnPropertyChanged("OutTabName"); } }

		// List items
        private BindingList<EntryLineVM> mInItems = new BindingList<EntryLineVM>();
        public BindingList<EntryLineVM> InItems { get { return mInItems; } }
        private BindingList<EntryLineVM> mOutItems = new BindingList<EntryLineVM>();
        public BindingList<EntryLineVM> OutItems { get { return mOutItems; } }
        private BindingList<TrackerInstanceVM> mPlanItems = new BindingList<TrackerInstanceVM>();
        public BindingList<TrackerInstanceVM> PlanItems { get { return mPlanItems; } }

		public event Action<TrackerInstanceVM> PlanItemSelected = delegate { };
		private TrackerInstanceVM mSelectedPlanItem;
		public TrackerInstanceVM SelectedPlanItem {
			get { return mSelectedPlanItem; }
			set {
				mSelectedPlanItem = value;
				OnPropertyChanged ("SelectedPlanItem");
				PlanItemSelected (value); 
			}
		}


	}

	public class KVPListConverter : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			List<String> kls = new List<string> ();
			var kl = value as KVPList<String,double>;
			foreach (var kv in kl)
				kls.Add (kv.Key + ": " + kv.Value);
			return String.Join ("\n", kls.ToArray ());
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}

