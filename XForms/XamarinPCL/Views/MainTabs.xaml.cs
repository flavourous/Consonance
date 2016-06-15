using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

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

            // List items
            private ObservableCollection<EntryLineVM> mInItems = new ObservableCollection<EntryLineVM>();
            public ObservableCollection<EntryLineVM> InItems { get { return mInItems; } }
            private ObservableCollection<EntryLineVM> mOutItems = new ObservableCollection<EntryLineVM>();
            public ObservableCollection<EntryLineVM> OutItems { get { return mOutItems; } }
            private ObservableCollection<TrackerInstanceVM> mPlanItems = new ObservableCollection<TrackerInstanceVM>();
            public ObservableCollection<TrackerInstanceVM> PlanItems { get { return mPlanItems; } }

            // selected item
            private TrackerInstanceVM mSelectedPlanItem;
            public TrackerInstanceVM SelectedPlanItem
            {
                get { return mSelectedPlanItem; }
                set
                {
                    Debug.WriteLine("view selecting tracker");
                    if (value == mSelectedPlanItem) return; // block reentrency
                    var use = PlanItems.Contains(value) ? value : null;
                    InTabName = value.dialect.InputEntryVerb;
                    OutTabName = value.dialect.OutputEntryVerb;
                    InManageName = value.dialect.InputInfoPlural;
                    OutManageName = value.dialect.OutputInfoPlural;
                    mSelectedPlanItem = use;
                    OnPlanSelected(use);
                    Debug.WriteLine("view finsihed selecting tracker");
                }
            }


            public Action<TrackerInstanceVM> OnPlanSelected = delegate { };

            // tracks
            private IEnumerable<TrackerTracksVM> inTrack;
            public IEnumerable<TrackerTracksVM> InTrack { get { return inTrack; }  set { inTrack = value; OnPropertyChanged("InTrack"); } }
            private IEnumerable<TrackerTracksVM> outTrack;
            public IEnumerable<TrackerTracksVM> OutTrack { get { return outTrack; }  set { outTrack = value; OnPropertyChanged("OutTrack"); } }

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

