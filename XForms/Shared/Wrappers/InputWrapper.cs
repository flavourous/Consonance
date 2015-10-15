using System;
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
			iman.choiceEnabled = (calltype & InfoCallType.AllowSelect) == InfoCallType.AllowSelect;
			iman.manageEnabled = (calltype & InfoCallType.AllowManage) == InfoCallType.AllowManage;
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
			await Task.Yield ();
			throw new NotImplementedException();
        }

		Action<int> pv_callback = delegate { };
        readonly ChoosePlanView pv = new ChoosePlanView();
		public ViewTask<int> ChoosePlan(string title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial)
		{
			TaskCompletionSource<EventArgs> tcs = new TaskCompletionSource<EventArgs> ();
			TaskCompletionSource<int> res = new TaskCompletionSource<int> ();
			Device.BeginInvokeOnMainThread (async () => {
				// make this async, which means it can await and continue nicely
				pv_callback = cv => {
					pv_callback = delegate { }; // overwrite it, no double call
					res.SetResult (cv);
				};
				pv.PlanChoices.Clear ();
				foreach (var pi in choose_from)
					pv.PlanChoices.Add (pi);
				await nav.PushAsync (pv);
				tcs.SetResult (new EventArgs ());
			});
			return new ViewTask<int> (res.Task, tcs.Task, () => nav.RemoveOrPop (pv));
		}

		public async Task WarnConfirm(string action, Promise confirmed)
        {
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
			Device.BeginInvokeOnMainThread(async () => tcs.SetResult(await root.DisplayAlert ("Warning", action, "OK", "Cancel")));
			if(await tcs.Task) await confirmed ();
        }
    }
}
