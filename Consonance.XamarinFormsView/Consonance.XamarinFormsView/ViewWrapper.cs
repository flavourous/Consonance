using System;
using System.Collections.Generic;
using System.Text;

namespace Consonance.XamarinFormsView
{
    class ViewWrapper : IView, ICollectionEditorLooseCommands<TrackerInstanceVM>
    {
        readonly MainView main;
        public ViewWrapper(MainView main)
        {
            this.main = main;
        }

        // IView Properly //
        public TrackerInstanceVM currentDiet { get; set; }
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
        public ICollectionEditorLooseCommands<TrackerInstanceVM> plan { get { return this; } }
        public event Action add { add { main.AddPlan += value; } remove { main.AddPlan -= value; } }

        // IView Unimplimented Properly //
        public void SetEatTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { }
        public void SetBurnTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { }
        public event Action<InfoManageType> manageInfo;
        public void ManageInfos(InfoManageType mt, ChangeTriggerList<InfoLineVM> toManage, IFindList<InfoLineVM> finder, Action finished) { }
        public event Action<DateTime> changeday;
        public DateTime day { get; set; }
        public event Action<TrackerInstanceVM> remove = delegate{ };
        public event Action<TrackerInstanceVM> edit = delegate{ };
        public event Action<TrackerInstanceVM> select =  delegate{ };
    }
}
