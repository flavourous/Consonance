﻿using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;

namespace Consonance.XamarinFormsView
{
	class UserInputWrapper : IUserInput
    {
		readonly InfoManageView iman;
		public Task<InfoLineVM> InfoView(InfoCallType calltype, InfoManageType mt, ObservableCollection<InfoLineVM> toManage, InfoLineVM initiallySelected)
		{
			var tt = getCurrent ();
			TaskCompletionSource<InfoLineVM> tcs = new TaskCompletionSource<InfoLineVM>();
			iman.Title = mt == InfoManageType.In ? tt.dialect.InputInfoPlural : tt.dialect.OutputInfoPlural;
			iman.choiceEnabled = (calltype | InfoCallType.AllowSelect) == InfoCallType.AllowSelect;
			iman.manageEnabled = (calltype | InfoCallType.AllowManage) == InfoCallType.AllowManage;
			iman.Items = toManage;
			iman.initiallySelectedItem  = iman.selectedItem = initiallySelected; // null works.
			iman.imt = mt;
			iman.completedTask = tcs;
			Device.BeginInvokeOnMainThread (() => nav.PushAsync (iman));
			return tcs.Task; // return result, or initial if it gave null (wich is null if it really was and no change)
		}

		readonly Func<IAbstractedTracker> getCurrent;
		readonly Page root;
		INavigation nav { get { return root.Navigation; } }
		public UserInputWrapper(Page root, InfoManageView iman, Func<IAbstractedTracker> getCurrent)
        {
			this.getCurrent = getCurrent;
			this.iman = iman;
			this.root = root;
			pv.chosen += v => pv_callback(v);
        }

		InfoFindView fv = new InfoFindView();
		public Task<InfoLineVM> Choose (IFindList<InfoLineVM> ifnd)
		{
			TaskCompletionSource<InfoLineVM> tcs = new TaskCompletionSource<InfoLineVM> ();
			Device.BeginInvokeOnMainThread ( async () => {
				await nav.PushAsync(fv);
				tcs.SetResult (await fv.Choose (ifnd));
			});
			return tcs.Task;
		}

		public async Task SelectString(string title, IReadOnlyList<string> strings, int initial, Promise<int> completed)
        {
			throw new NotImplementedException();
        }

		Promise<int> pv_callback = async delegate { await Task.Yield(); };
        readonly ChoosePlanView pv = new ChoosePlanView();
		public Task ChoosePlan(string title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial, Promise<int> completed)
        {
			TaskCompletionSource<EventArgs> tcs = new TaskCompletionSource<EventArgs> ();
			Device.BeginInvokeOnMainThread (async () => {
				// make this async, which means it can await and continue nicely
				pv_callback = async cv => {
					pv_callback = async delegate { await Task.Yield(); }; // overwrite it, no double call
					await completed (cv);
					// if we are still on top of the stack, pop.
					if (nav.NavigationStack [nav.NavigationStack.Count - 1] == pv)
						await nav.PopAsync ();
				//otherwise, pull ourselves outta the stack.
				else
						nav.RemovePage (pv);
				};
				pv.PlanChoices.Clear ();
				foreach (var pi in choose_from)
					pv.PlanChoices.Add (pi);
				await nav.PushAsync (pv);
				tcs.SetResult(new EventArgs());
			});
			return tcs.Task;
        }

		public async Task WarnConfirm(string action, Promise confirmed)
        {
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
			Device.BeginInvokeOnMainThread(async () => tcs.SetResult(await root.DisplayAlert ("Warning", action, "OK", "Cancel")));
			if(await tcs.Task) await confirmed ();
        }
    }
}
