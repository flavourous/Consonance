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

        // IView Properly //
		public TrackerInstanceVM currentTrackerInstance 
		{ 
			get { return main.viewmodel.SelectedPlanItem; } 
			set { App.platform.UIThread (() => main.viewmodel.SelectedPlanItem = value); }
		}

        readonly Dictionary<String, int> lkeys = new Dictionary<string, int>();
        readonly ContentPage loader = new ContentPage { Content = new Label { Text = "" } };
        class LS : ILoadingState
        {
            readonly String key;
            readonly ViewWrapper vw;
            public LS(ViewWrapper vw, String key) { this.vw = vw; this.key = key; }
            public void Complete()
            {
                // async but ordered
                App.platform.UIThread(() =>
                {
                    vw.states.Add("Complete Beginning");
                    if (--vw.lkeys[key] == 0)
                    {
                        vw.lkeys.Remove(key);
                        vw.DisplayProgress(false);
                    }
                    vw.states.Add("Complete Ending");
                });
            }
        }
        Object ll = new object();
        bool awaitingCallback = false, wantshow = false, isshow = false;
        void DisplayProgress(bool? wantShow) // callbacks with null
        {
            lock (ll)
            {
                if (wantShow.HasValue) wantshow = wantShow.Value; // store request
                if (awaitingCallback && wantShow.HasValue) return; // cause we'll be recalled
                if (!wantShow.HasValue) awaitingCallback = false; // we are now being recalled
                
                if(wantshow != isshow)
                {
                    awaitingCallback = true;
                    if (wantshow = isshow) main.Navigation.PushModalAsync(loader).ContinueWith(t => DisplayProgress(null));
                    else main.Navigation.PopModalAsync().ContinueWith(t => DisplayProgress(null));
                }
            }
        }
        List<String> states = new List<string>();
        public ILoadingState PushLoading(String key)
        {
            // async but ordered
            App.platform.UIThread(() =>
            {
                states.Add("Push Beginning");
                // show?
                var show = lkeys.Count == 0;

                // push counter
                if (!lkeys.ContainsKey(key)) lkeys[key] = 1;
                else lkeys[key]++;

                // display
                if (show)
                {
                    (loader.Content as Label).Text = "";
                    DisplayProgress(true);
                }

                // set text
                (loader.Content as Label).Text = String.Join("\n", lkeys.Keys);
                states.Add("Push Ending");
            });
            return new LS(this, key);
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
					if(OriginatorVM.OriginatorEquals(main.viewmodel.SelectedPlanItem, itm))
						main.viewmodel.SelectedPlanItem = itm;
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

		public void SetEatTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { App.platform.UIThread(() => main.viewmodel.InTrack = Combined (current, others)); }
		public void SetBurnTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { App.platform.UIThread(() => main.viewmodel.OutTrack = Combined (current, others)); }
		IEnumerable<TrackerTracksVM> Combined(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others)
		{
			yield return current;
			foreach (var tt in others)
				yield return tt;
		}
    }
}
