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
using System.Windows.Input;
using System.Globalization;
using System.Collections;
using System.Collections.Specialized;

namespace Consonance.XamarinFormsView.PCL
{
    class FirstTrack : BindableObject
    {
        public FirstTrack()
        {
            PropertyChanged += FirstTrack_PropertyChanged;
        }

        INotifyCollectionChanged previous = null;
        private void FirstTrack_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == TracksProperty.PropertyName)
            {
                if (previous != null) previous.CollectionChanged -= Previous_CollectionChanged;
                previous = Tracks as INotifyCollectionChanged;
                if (previous != null) previous.CollectionChanged += Previous_CollectionChanged;
                LookForFirst();
            }
        }

        private void Previous_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => LookForFirst();
        void LookForFirst()
        {
            Debug.WriteLine("(Tracks) Stage 1");
            if (Tracks == null || Tracks.Count() == 0) return;
            Debug.WriteLine("(Tracks) Stage 2");
            var track = Tracks.First().tracks;
            if (track == null || track.Count() == 0) return;
            var t = track.Take(1); // give enumerable...
            Debug.WriteLine("(Tracks) Stage 3 - " + t);
            FirstTrackFirstItem = t;
        }

        public IEnumerable<TrackingInfoVM> FirstTrackFirstItem { get => GetValue(FirstTrackFirstItemProperty) as IEnumerable<TrackingInfoVM>; set => SetValue(FirstTrackFirstItemProperty, value); }
        public static BindableProperty FirstTrackFirstItemProperty = BindableProperty.Create("FirstTrackFirstItem", typeof(IEnumerable<TrackingInfoVM>), typeof(FirstTrack));

        public IEnumerable<TrackerTracksVM> Tracks { get => GetValue(TracksProperty) as IEnumerable<TrackerTracksVM>; set => SetValue(TracksProperty, value); }
        public static BindableProperty TracksProperty = BindableProperty.Create("Tracks", typeof(IEnumerable<TrackerTracksVM>), typeof(FirstTrack));
    }
    class DebugBinding : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("(DebugBinding) "+parameter+"=" + value?.ToString() ?? "null");
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public partial class MainTabs : TabbedPage
    {
        public class MTVMItems<T> : BindableObject where T : class
        {
            public MTVMItems(Action<T> edit, Action<T> delete)
            {
                ItemEdit = new Command(d=>edit(d as T));
                ItemDelete = new Command(d=>delete(d as T));
            }
            public Command ItemEdit { get; }
            public Command ItemDelete { get; }

            public IList<T> Items { get { return (IList<T>)GetValue(ItemsProperty); } set { SetValue(ItemsProperty, value); } }
            public static BindableProperty ItemsProperty = BindableProperty.Create("Items", typeof(IList), typeof(MTVMItems<>));

            public T SelectedItem { get { return (T)GetValue(SelectedItemProperty); } set { SetValue(SelectedItemProperty, value); } }
            public static BindableProperty SelectedItemProperty = BindableProperty.Create("SelectedItem", typeof(T), typeof(MTVMItems<>));
        }
        public class MTVM : INotifyPropertyChanged
        {
            public MTVM(MTVMItems<EntryLineVM> inm, MTVMItems<EntryLineVM> outm, MTVMItems<TrackerInstanceVM> trackm)
            {
                InModel = inm;
                OutModel = outm;
                PlanModel = trackm;
                PlanModel.SetBinding(MTVMItems<TrackerInstanceVM>.SelectedItemProperty, new Binding("SelectedPlanItem", BindingMode.OneWayToSource, source: this));
            }
            // new models
            public MTVMItems<EntryLineVM> InModel { get; }
            public MTVMItems<EntryLineVM> OutModel { get; }
            public MTVMItems<TrackerInstanceVM> PlanModel { get; }

            // Tab Names
            private String mInTabName = "In";
            public String InTabName { get { return mInTabName; } set { mInTabName = value; OnPropertyChanged("InTabName"); } }
            private String mOutTabName = "Out";
            public String OutTabName { get { return mOutTabName; } set { mOutTabName = value; OnPropertyChanged("OutTabName"); } }
            private String mInManageName = "Manage";
            public String InManageName { get { return mInManageName; } set { mInManageName = value; OnPropertyChanged("InManageName"); } }
            private String mOutManageName = "Manage";
            public String OutManageName { get { return mOutManageName; } set { mOutManageName = value; OnPropertyChanged("OutManageName"); } }

            private IList<InfoLineVM> inInfos;
            private IList<InfoLineVM> outInfos;
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

            public DateTime day;
            public DateTime Day { get { return day; } set { day = value; OnPropertyChanged("Day"); } }

            public IList<InfoLineVM> InInfos { get { return inInfos; } set { inInfos = value; OnPropertyChanged("InInfos"); } }
            public IList<InfoLineVM> OutInfos { get { return outInfos; } set { outInfos = value; OnPropertyChanged("OutInfos"); } }
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
        

        public MTVM viewmodel { get; }
		public MainTabs ()
		{
			InitializeComponent ();
            BindingContext = viewmodel = new MTVM(
                new MTVMItems<EntryLineVM>(d => InItemEdit(d), d => InItemDelete(d)),
                new MTVMItems<EntryLineVM>(d => OutItemEdit(d), d => OutItemDelete(d)),
                new MTVMItems<TrackerInstanceVM>(d => PlanItemEdit(d), d => PlanItemDelete(d))
            );
            viewmodel.OnPlanSelected = this.OnPlanSelected;
        }


        void OnPlanSelected(TrackerInstanceVM use)
        {
            viewmodel.PlanModel.SelectedItem = use;
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
		public event Action<TrackerInstanceVM> PlanItemDelete = delegate { };

		/// In
		public event Action AddIn = delegate { };
		void AddInClick(Object sender, EventArgs args) { AddIn(); }
		public event Action<EntryLineVM> InItemEdit = delegate { };
		public event Action<EntryLineVM> InItemDelete = delegate { };
		public event Action InInfoManage = delegate { };
		void InInfoManageClick(Object s, EventArgs e) { InInfoManage (); }

		/// Out
		public event Action AddOut = delegate { };
		void AddOutClick(Object sender, EventArgs args) { AddOut(); }
		public event Action<EntryLineVM> OutItemEdit = delegate { };
		public event Action<EntryLineVM> OutItemDelete = delegate { };
		public event Action OutInfoManage = delegate { };
		void OutInfoManageClick(Object s, EventArgs e) { OutInfoManage (); }
        
		// Other bits
		public event Action<TrackerInstanceVM> PlanItemSelected = delegate { };

        // Swpes

        public event Action<int> ShiftDay = delegate { };
        private void ListSwipeLeft(object sender, EventArgs e) => ShiftDay(-1);
        private void ListSwipeRight(object sender, EventArgs e) => ShiftDay(+1);
    }
}

