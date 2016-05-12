using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;

namespace Consonance.XamarinFormsView.PCL
{
	public delegate InfoManageView CreateImanHandler (bool choice, bool manage);
	class UserInputWrapper : IUserInput
    {
		public static Action<String> message = delegate { };
		readonly CreateImanHandler iman;
		public Task<InfoLineVM> InfoView(InfoCallType calltype, InfoManageType mt, IObservableCollection<InfoLineVM> toManage, InfoLineVM initiallySelected)
		{
			var tt = getCurrent ();
			TaskCompletionSource<InfoLineVM> tcs = new TaskCompletionSource<InfoLineVM> ();
			bool choiceEnabled = (calltype & InfoCallType.AllowSelect) == InfoCallType.AllowSelect;
			bool manageEnabled = (calltype & InfoCallType.AllowManage) == InfoCallType.AllowManage;
            App.platform.UIThread (() => {
				var iman_c = iman (choiceEnabled, manageEnabled);
				iman_c.Title = mt == InfoManageType.In ? tt.dialect.InputInfoPlural : tt.dialect.OutputInfoPlural;
				iman_c.Items = toManage;
				iman_c.initiallySelectedItem = iman_c.selectedItem = initiallySelected; // null works.
				iman_c.imt = mt;
				iman_c.completedTask = tcs;
				nav.PushAsync (iman_c);
			});
			return tcs.Task; // return result, or initial if it gave null (wich is null if it really was and no change)
		}

		readonly Func<IAbstractedTracker> getCurrent;
		readonly Page root;
		INavigation nav { get { return root.Navigation; } }
		public UserInputWrapper(Page root, CreateImanHandler iman, Func<IAbstractedTracker> getCurrent)
        {
			this.getCurrent = getCurrent;
			this.iman = iman;
			this.root = root;
			pv.chosen += v => pv_callback(v);
			message = s => Message (s);
        }

		InfoFindView fv = new InfoFindView();
		public Task<InfoLineVM> Choose (IFindList<InfoLineVM> ifnd)
		{
			TaskCompletionSource<InfoLineVM> tcs = new TaskCompletionSource<InfoLineVM> ();
            App.platform.UIThread ( async () => {
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
            App.platform.UIThread (async () => {
				// make this async, which means it can await and continue nicely
				pv_callback = cv => {
					pv_callback = delegate { }; // overwrite it, no double call
					res.SetResult (cv);
				};
				pv.mPlanChoices.Clear ();
				foreach (var pi in choose_from)
					pv.mPlanChoices.Add (pi);
				pv.choicey = null;
				await nav.PushAsync (pv);
				tcs.SetResult (new EventArgs ());
			});
			return new ViewTask<int> (() => nav.RemoveOrPopAsync (pv),tcs.Task,res.Task);
		}

		public async Task WarnConfirm(string action, Promise confirmed)
        {
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
            App.platform.UIThread(async () => tcs.SetResult(await root.DisplayAlert ("Warning", action, "OK", "Cancel")));
			if(await tcs.Task) await confirmed ();
        }
		public async Task Message(string msg)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
			App.platform.UIThread (async () => {
				await root.DisplayAlert ("Message", msg, "OK");
				tcs.SetResult (false);
			});
			await tcs.Task;
		}
    }
}
