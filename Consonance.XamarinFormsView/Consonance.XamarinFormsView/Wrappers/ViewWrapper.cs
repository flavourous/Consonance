using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
    class ViewWrapper : IView, ICollectionEditorLooseCommands<TrackerInstanceVM>
    {
		readonly MainTabs main;
		readonly InfoManageView iman;
		public ViewWrapper(MainTabs main, InfoManageView iman)
        {
            this.main = main;
			this.iman = iman;
			main.InInfoManage += () => manageInfo (InfoManageType.In);
			main.OutInfoManage += () =>  manageInfo (InfoManageType.Out);
        }

        // IView Properly //
		public TrackerInstanceVM currentTrackerInstance { get { return main.SelectedPlanItem; } set { main.SelectedPlanItem=value; } }
        public void SetEatLines(IEnumerable<EntryLineVM> lineitems)
        {
            main.InItems.Clear();
            foreach (var itm in lineitems)
                main.InItems.Add(itm);
        }
        public void SetBurnLines(IEnumerable<EntryLineVM> lineitems)
        {
            main.OutItems.Clear();
            foreach (var itm in lineitems)
                main.OutItems.Add(itm);
        }
        public void SetInstances(IEnumerable<TrackerInstanceVM> instanceitems)
        {
            main.PlanItems.Clear();
            foreach (var itm in instanceitems)
                main.PlanItems.Add(itm);
        }

		// plan commands
        public ICollectionEditorLooseCommands<TrackerInstanceVM> plan { get { return this; } }
        public event Action add { add { main.AddPlan += value; } remove { main.AddPlan -= value; } }
		public event Action<TrackerInstanceVM> select { add { main.PlanItemSelected += value; } remove { main.PlanItemSelected -= value; } }
		public event Action<TrackerInstanceVM> remove { add { main.PlanItemDelete += value; } remove { main.PlanItemDelete -= value; } }
		public event Action<TrackerInstanceVM> edit { add { main.PlanItemEdit += value; } remove { main.PlanItemEdit -= value; } }

		// info managment
		public event Action<InfoManageType> manageInfo = delegate { };
		public void ManageInfos(InfoManageType mt, BindingList<InfoLineVM> toManage, IFindList<InfoLineVM> finder, Action finished) 
		{
			iman.Title = "Manage " + (mt == InfoManageType.In ? currentTrackerInstance.dialect.InputInfoPlural : currentTrackerInstance.dialect.OutputInfoPlural);
			iman.finder = finder;
			iman.Items = toManage;
			iman.imt = mt;
			iman.finished = async () => {
				await main.Navigation.PopAsync();
				finished();
			};
			main.Navigation.PushAsync (iman);
		}

        // IView Unimplimented Properly //
        public void SetEatTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { }
        public void SetBurnTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { }
        public event Action<DateTime> changeday;
        public DateTime day { get; set; }
    }
}
