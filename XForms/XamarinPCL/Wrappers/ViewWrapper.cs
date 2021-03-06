﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using LibSharpHelp;
using Consonance.Invention;
using Consonance.Protocol;

namespace Consonance.XamarinFormsView.PCL
{
	class ViewWrapper : IView, ICollectionEditorSelectableLooseCommands<TrackerInstanceVM>
    {
		static int? mainID;
		public readonly MainTabs main;
        readonly CommonServices srv;
		public ViewWrapper(MainTabs main, CommonServices srv)
        {
			mainID = Task.CurrentId;
            this.main = main;
            this.srv = srv;
            main.InInfoManage += () => InfoView(InfoManageType.In);
			main.OutInfoManage += () => InfoView(InfoManageType.Out);
            main.manageInvention += () => srv.Invent();
            main.ShiftDay += Main_ForwardsDay;
        }

        private void Main_ForwardsDay(int obj)
        {
            ChangeDay(day.AddDays(obj));
        }

        void InfoView(InfoManageType mt)
        {
            if(currentTrackerInstance == null)
                UserInputWrapper.message("Create a plan first!");
            else srv.U_InfoView(false, true, mt, null);
        }
        // IView Properly //
		public TrackerInstanceVM currentTrackerInstance 
		{ 
			get { return main.viewmodel.SelectedPlanItem; } 
			set { App.UIThread (() => main.viewmodel.SelectedPlanItem = value); }
		}
		
        // passons
        public void SetEatTrack(IVMList<TrackerTracksVM> others) { SetIVM(others,v => main.viewmodel.InTrack = v); }
        public void SetBurnTrack(IVMList<TrackerTracksVM> others) { SetIVM(others,v => main.viewmodel.OutTrack = v); }
        public void SetEatLines(IVMList<EntryLineVM> lineitems) { SetIVM(lineitems,v => main.viewmodel.InModel.Items = v); }
        public void SetBurnLines(IVMList<EntryLineVM> lineitems) { SetIVM(lineitems, v => main.viewmodel.OutModel.Items = v); }
        public void SetEatInfos(IVMList<InfoLineVM> lineInfos) { SetIVM(lineInfos,v => main.viewmodel.InInfos = v); }
        public void SetBurnInfos(IVMList<InfoLineVM> lineInfos) { SetIVM(lineInfos, v => main.viewmodel.OutInfos = v); }
        public void SetInstances(IVMList<TrackerInstanceVM> instanceitems) { SetIVM(instanceitems, v => main.viewmodel.PlanModel.Items = v); }
        public void SetInventions(IVMList<InventedTrackerVM> items) { SetIVM(items, v => main.viewmodel.InventedPlans = v); }

        void SetIVM<T>(IVMList<T> list, Action<IVMList<T>> setter)
        {
            list.Dispatcher = a => App.UIThread(a); 
            App.UIThread(() => setter(list));
        }

		// plan commands
        public ICollectionEditorSelectableLooseCommands<TrackerInstanceVM> plan { get { return this; } }
        public event Action add { add { main.AddPlan += value; } remove { main.AddPlan -= value; } }
		public event Action<TrackerInstanceVM> select { add { main.PlanItemSelected += value; } remove { main.PlanItemSelected -= value; } }
		public event Action<TrackerInstanceVM> remove { add { main.PlanItemDelete += value; } remove { main.PlanItemDelete -= value; } }
		public event Action<TrackerInstanceVM> edit { add { main.PlanItemEdit += value; } remove { main.PlanItemEdit -= value; } }

        // dayyys
        public void ChangeDay(DateTime day) { changeday(day); }
		public event Action<DateTime> changeday = delegate { };
		public DateTime day { get => main.viewmodel.Day;  set => main.viewmodel.Day = value;  }

        // invention, is add/edit/remove on another screen soooo let it be set??
        public ICollectionEditorLooseCommands<InventedTrackerVM> invention { get; set; }

    }
}
