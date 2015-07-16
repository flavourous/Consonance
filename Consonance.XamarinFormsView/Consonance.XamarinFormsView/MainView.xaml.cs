using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
    public partial class MainView : TabbedPage
    {
        public MainView()
        {
            //InitializeComponent();
        }

        //////////////
        // Commands //
        //////////////
        public event Action AddPlan = delegate { };
        public void AddPlanClick(Object sender, EventArgs args) { AddPlan(); }

        ////////////////////////
        // Data Context stuff //
        ////////////////////////

        // Tab Names
        private String mInTabName = "In";
        public String InTabName { get { return mInTabName; } set { mInTabName = value; OnPropertyChanged("InTabName"); } }
        private String mOutTabName = "Out";
        public String OutTabName { get { return mOutTabName; } set { mOutTabName = value; OnPropertyChanged("OutTabName"); } }

        // List items
        public readonly BindingList<EntryLineVM> InItems = new BindingList<EntryLineVM>();
        public readonly BindingList<EntryLineVM> OutItems = new BindingList<EntryLineVM>();
        public readonly BindingList<TrackerInstanceVM> PlanItems = new BindingList<TrackerInstanceVM>();
    }
}
