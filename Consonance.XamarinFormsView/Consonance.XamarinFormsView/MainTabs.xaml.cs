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
		public event Action AddPlan = delegate { };
		void AddPlanClick(Object sender, EventArgs args) { AddPlan(); }

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

	}
}

