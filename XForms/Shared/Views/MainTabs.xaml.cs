﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Consonance.XamarinFormsView
{
	public partial class MainTabs : TabbedPage
	{
		public MainTabs ()
		{
			InitializeComponent ();
		//	Resources.Add ("greynotcurrent", new BoolColorConverter (Color.Default, Color.Gray));
			BindingContext = this;
		}
		public Object daypagerContext { set { daypagerIn.BindingContext = daypagerOut.BindingContext = daypagerPlan.BindingContext = value; } }
		bool _l1,_l2,_l3;
		public bool load1{ get{ return _l1; } set{ _l1 = value; OnPropertyChanged ("load1"); } }
		public bool load2{ get{ return _l2; } set{ _l2 = value; OnPropertyChanged ("load2"); } }
		public bool load3{ get{ return _l3; } set{ _l3 = value; OnPropertyChanged ("load3"); } }


//		public void OnTest(Object s, EventArgs e)
//		{
//			var f = App.bld.requestFactory;
//			var rp = new GetValuesPage ("Test");
//			rp.SetList (new BindingList<object> (new Object[] {
//				f.RecurrOnRequestor ("Test #1").request,
//				f.RecurrEveryRequestor ("Test #2").request
//			}));
//			App.bld.GetValues (new [] { rp });
//		}

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
		private ObservableCollection<EntryLineVM> mInItems = new ObservableCollection<EntryLineVM>();
		public ObservableCollection<EntryLineVM> InItems { get { return mInItems; } }
		private ObservableCollection<EntryLineVM> mOutItems = new ObservableCollection<EntryLineVM>();
		public ObservableCollection<EntryLineVM> OutItems { get { return mOutItems; } }
		private ObservableCollection<TrackerInstanceVM> mPlanItems = new ObservableCollection<TrackerInstanceVM>();
		public ObservableCollection<TrackerInstanceVM> PlanItems { get { return mPlanItems; } }

		// Other bits
		public event Action<TrackerInstanceVM> PlanItemSelected = delegate { };
		private TrackerInstanceVM mSelectedPlanItem;
		public TrackerInstanceVM SelectedPlanItem {
			get { return mSelectedPlanItem; }
			set {
				if (value == mSelectedPlanItem) return; // block reentrency
				var use =  PlanItems.Contains(value) ? value : null;
				PlanList.SelectedItem = mSelectedPlanItem = use;
				InTabName = value.dialect.InputEntryVerb;
				OutTabName = value.dialect.OutputEntrytVerb;
				PlanItemSelected (use); 
			}
		}

		// tracks
		public IEnumerable<TrackerTracksVM> InTrack { get; set; }
		public IEnumerable<TrackerTracksVM> OutTrack { get; set; }
	}

}

