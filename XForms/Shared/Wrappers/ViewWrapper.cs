﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace Consonance.XamarinFormsView
{
	class ViewWrapper : IView, ICollectionEditorLooseCommands<TrackerInstanceVM>, INotifyPropertyChanged
    {
		readonly MainTabs main;
		readonly InfoManageView iman;
		public ViewWrapper(MainTabs main, InfoManageView iman)
        {
            this.main = main;
			this.iman = iman;
			main.daypagerContext = this;
			main.InInfoManage += () => manageInfo (InfoManageType.In);
			main.OutInfoManage += () =>  manageInfo (InfoManageType.Out);
			Debug.WriteLine ("viewwrapper injected");
        }

        // IView Properly //
		public TrackerInstanceVM currentTrackerInstance 
		{ 
			get { return main.SelectedPlanItem; } 
			set { Device.BeginInvokeOnMainThread (() => main.SelectedPlanItem = value); }
		}
		
        public void SetEatLines(IEnumerable<EntryLineVM> lineitems)
        {
			Device.BeginInvokeOnMainThread (() => {
				main.InItems.Clear ();
				foreach (var itm in lineitems)
					main.InItems.Add (itm);
			});
        }
        public void SetBurnLines(IEnumerable<EntryLineVM> lineitems)
        {
			Device.BeginInvokeOnMainThread (() => {
				main.OutItems.Clear ();
				foreach (var itm in lineitems)
					main.OutItems.Add (itm);
			});
        }
		readonly Dictionary<TrackerInstanceVM, bool> toKeep_TI = new Dictionary<TrackerInstanceVM, bool> ();
        public void SetInstances(IEnumerable<TrackerInstanceVM> instanceitems)
		{
			Device.BeginInvokeOnMainThread(() => {
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
		public Task ManageInfos(InfoManageType mt, ObservableCollection<InfoLineVM> toManage) 
		{
			TaskCompletionSource<EventArgs> tcs = new TaskCompletionSource<EventArgs>();
			iman.Title = "Manage " + (mt == InfoManageType.In ? currentTrackerInstance.dialect.InputInfoPlural : currentTrackerInstance.dialect.OutputInfoPlural);
			iman.Items = toManage;
			iman.imt = mt;
			iman.completedTask = tcs;
			Device.BeginInvokeOnMainThread (() => main.Navigation.PushAsync (iman));
			return tcs.Task;
		}

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

        // IView Unimplimented Properly //
        public void SetEatTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { }
        public void SetBurnTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { }
    }

}
