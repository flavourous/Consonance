using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using LibSharpHelp;

namespace Consonance.XamarinFormsView
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
			Platform.UIThread(a);
		}

        // IView Properly //
		public TrackerInstanceVM currentTrackerInstance 
		{ 
			get { return main.SelectedPlanItem; } 
			set { Platform.UIThread (() => main.SelectedPlanItem = value); }
		}


		void TLS(TabbedPage tp, ListView lv, bool loading)
		{
			lv.IsEnabled = !loading;
		}


		public void SetLoadingState (LoadThings thing, bool active)
		{
			Platform.UIThread (() => {
				switch (thing) {
				case LoadThings.EatItems: 
					main.load1=active;
					break;
				case LoadThings.BurnItems: 
					main.load2=active;
					break;
				case LoadThings.Instances: 
					main.load3=active;
					break;
				}
			});
		}
		
        public void SetEatLines(IEnumerable<EntryLineVM> lineitems)
        {
			Platform.UIThread (() => {
				main.InItems.Clear ();
				foreach (var itm in lineitems)
					main.InItems.Add (itm);
			});
        }
        public void SetBurnLines(IEnumerable<EntryLineVM> lineitems)
        {
			Platform.UIThread (() => {
				main.OutItems.Clear ();
				foreach (var itm in lineitems)
					main.OutItems.Add (itm);
			});
        }
		readonly Dictionary<TrackerInstanceVM, bool> toKeep_TI = new Dictionary<TrackerInstanceVM, bool> ();
        public void SetInstances(IEnumerable<TrackerInstanceVM> instanceitems)
		{
			Platform.UIThread(() => {
				main.PlanItems.Clear (); // lets figure out why no get here after add....
				foreach (var itm in instanceitems)
				{
					main.PlanItems.Add (itm);

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
			PropertyChanged(this, new PropertyChangedEventArgs(property));
		}
		#endregion

		public void SetEatTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { main.InTrack = Combined (current, others); }
		public void SetBurnTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { main.OutTrack = Combined (current, others); }
		IEnumerable<TrackerTracksVM> Combined(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others)
		{
			yield return current;
			foreach (var tt in others)
				yield return tt;
		}
    }

}
