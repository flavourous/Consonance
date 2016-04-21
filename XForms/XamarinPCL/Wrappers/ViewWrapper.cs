using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using LibSharpHelp;

namespace Consonance.XamarinFormsView.PCL
{
	class ViewWrapper : IView, ICollectionEditorLooseCommands<TrackerInstanceVM>, INotifyPropertyChanged
    {
		static int? mainID;
		readonly MainTabs main;
		public ViewWrapper(MainTabs main)
        {
			mainID = Task.CurrentId;
            this.main = main;
			main.daypagerContext = this;
			main.InInfoManage += () => manageInfo (InfoManageType.In);
			main.OutInfoManage += () =>  manageInfo (InfoManageType.Out);
			Debug.WriteLine ("viewwrapper injected");
        }

		public void BeginUIThread (Action a)
		{
			App.platform.UIThread(a);
		}

        // IView Properly //
		public TrackerInstanceVM currentTrackerInstance 
		{ 
			get { return main.SelectedPlanItem; } 
			set { App.platform.UIThread (() => main.SelectedPlanItem = value); }
		}

		public void SetLoadingState (LoadThings thing, bool active)
		{
			App.platform.UIThread (() => {
				switch (thing) {
				case LoadThings.EatItems: 
					main.viewmodel.load1=active;
					break;
				case LoadThings.BurnItems: 
					main.viewmodel.load2 = active;
					break;
				case LoadThings.Instances: 
					main.viewmodel.load3=active;
					break;
				}
			});
		}
		
        public void SetEatLines(IEnumerable<EntryLineVM> lineitems)
        {
			App.platform.UIThread (() => {
				main.viewmodel.InItems.Clear ();
				foreach (var itm in lineitems)
					main.viewmodel.InItems.Add (itm);
			});
        }
        public void SetBurnLines(IEnumerable<EntryLineVM> lineitems)
        {
			App.platform.UIThread (() => {
				main.viewmodel.OutItems.Clear ();
				foreach (var itm in lineitems)
					main.viewmodel.OutItems.Add (itm);
			});
        }
		readonly Dictionary<TrackerInstanceVM, bool> toKeep_TI = new Dictionary<TrackerInstanceVM, bool> ();
        public void SetInstances(IEnumerable<TrackerInstanceVM> instanceitems)
		{
			App.platform.UIThread(() => {
				main.viewmodel.PlanItems.Clear (); // lets figure out why no get here after add....
				foreach (var itm in instanceitems)
				{
					main.viewmodel.PlanItems.Add (itm);

					// and since these have new refs but old origiginators, we can keep the old one selected...
					if(OriginatorVM.OriginatorEquals(main.SelectedPlanItem, itm))
						main.SelectedPlanItem = itm;
				}
			});
		}

			
		// plan commands
        public ICollectionEditorLooseCommands<TrackerInstanceVM> plan { get { return this; } }
        public event Action add { add { main.AddPlan += value; } remove { main.AddPlan -= value; } }
		public event Action<TrackerInstanceVM> select { add { main.PlanItemSelected += value; } remove { main.PlanItemSelected -= value; } }
		public event Action<TrackerInstanceVM> remove { add { main.PlanItemDelete += value; } remove { main.PlanItemDelete -= value; } }
		public event Action<TrackerInstanceVM> edit { add { main.PlanItemEdit += value; } remove { main.PlanItemEdit -= value; } }

		// info managment
		public event Action<InfoManageType> manageInfo = delegate { };

		// dayyys
		public void ChangeDay(DateTime day) { changeday(day); }
		public event Action<DateTime> changeday = delegate { };
		private DateTime mday;
		public DateTime day { get { return mday; } set { mday = value; OnPropertyChanged ("day"); } }

		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		public void OnPropertyChanged(String property)
		{
            App.platform.UIThread(() => PropertyChanged(this, new PropertyChangedEventArgs(property)));
		}
		#endregion

		public void SetEatTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { App.platform.UIThread(() => main.InTrack = Combined (current, others)); }
		public void SetBurnTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { App.platform.UIThread(() => main.OutTrack = Combined (current, others)); }
		IEnumerable<TrackerTracksVM> Combined(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others)
		{
			yield return current;
			foreach (var tt in others)
				yield return tt;
		}
    }

}
