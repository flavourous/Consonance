using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Consonance.Invention;
using Consonance.Protocol;
using LibSharpHelp;
using System.Linq;

namespace Consonance.XamarinFormsView.PCL
{

	public partial class MainTabs : TabbedPage
	{
        public class MTVM : INotifyPropertyChanged
        {
            // Tab Names
            private String mInTabName = "In";
            public String InTabName { get { return mInTabName; } set { mInTabName = value; OnPropertyChanged("InTabName"); } }
            private String mOutTabName = "Out";
            public String OutTabName { get { return mOutTabName; } set { mOutTabName = value; OnPropertyChanged("OutTabName"); } }
            private String mInManageName = "Manage";
            public String InManageName { get { return mInManageName; } set { mInManageName = value; OnPropertyChanged("InManageName"); } }
            private String mOutManageName = "Manage";
            public String OutManageName { get { return mOutManageName; } set { mOutManageName = value; OnPropertyChanged("OutManageName"); } }

            private IList<EntryLineVM> inItems;
            private IList<EntryLineVM> outItems;
            private IList<InfoLineVM> inInfos;
            private IList<InfoLineVM> outInfos;
            private IList<TrackerInstanceVM> planItems;
            private IList<TrackerTracksVM> inTrack;
            private IList<TrackerTracksVM> outTrack;
            private IList<InventedTrackerVM> inventedPlans;

            // selected item
            private TrackerInstanceVM mSelectedPlanItem;
            public TrackerInstanceVM SelectedPlanItem
            {
                get { return mSelectedPlanItem; }
                set
                {
                    // This is simply so that the list can fire "onchanged"
                    Debug.WriteLine("selecting tracker");
                    InTabName = value?.dialect?.InputEntryVerb ?? "In";
                    OutTabName = value?.dialect?.OutputEntryVerb ?? "Out";
                    InManageName = value?.dialect?.InputInfoPlural ?? "Manage";
                    OutManageName = value?.dialect?.OutputInfoPlural ?? "Manage";
                    mSelectedPlanItem = value;
                    OnPlanSelected(mSelectedPlanItem);
                    Debug.WriteLine("finsihed selecting tracker");
                }
            }

            public IList<EntryLineVM> InItems { get { return inItems; } set { inItems = value; OnPropertyChanged("InItems"); } }
            public IList<EntryLineVM> OutItems { get { return outItems; } set { outItems = value; OnPropertyChanged("OutItems"); } }
            public IList<InfoLineVM> InInfos { get { return inInfos; } set { inInfos = value; OnPropertyChanged("InInfos"); } }
            public IList<InfoLineVM> OutInfos { get { return outInfos; } set { outInfos = value; OnPropertyChanged("OutInfos"); } }
            public IList<TrackerInstanceVM> PlanItems { get { return planItems; } set { planItems = value; value.LogHook("PlanItems") ; OnPropertyChanged("PlanItems"); } }
            public IList<TrackerTracksVM> InTrack { get { return inTrack; } set { inTrack = value; OnPropertyChanged("InTrack"); } }
            public IList<TrackerTracksVM> OutTrack { get { return outTrack; } set { outTrack = value; OnPropertyChanged("OutTrack"); } }
            public IList<InventedTrackerVM> InventedPlans { get { return inventedPlans; } set { inventedPlans = value; OnPropertyChanged("InventedPlans"); } }

            public Action<TrackerInstanceVM> OnPlanSelected = delegate { };
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            public void OnPropertyChanged(string prop)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        public MTVM viewmodel = new MTVM();
		public MainTabs ()
		{
			InitializeComponent ();
            viewmodel.OnPlanSelected = this.OnPlanSelected;
			BindingContext = viewmodel;
        }
		public Object daypagerContext { set { daypagerIn.BindingContext = daypagerOut.BindingContext = daypagerPlan.BindingContext = value; } }

        void OnPlanSelected(TrackerInstanceVM use)
        {
            PlanList.SelectedItem = use;
            PlanItemSelected(use);
        }

        public event Action manageInvention = delegate { };
        void ManageInvention(Object sender, EventArgs args)
        {
            manageInvention(); 
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
        
		// Other bits
		public event Action<TrackerInstanceVM> PlanItemSelected = delegate { };		
	}

}

