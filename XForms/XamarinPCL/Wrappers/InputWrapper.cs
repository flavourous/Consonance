using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;
using Consonance.Invention;
using Consonance.Protocol;

namespace Consonance.XamarinFormsView.PCL
{
	class UserInputWrapper : IUserInput
    {
		public static Action<String> message = delegate { };
        public IInputResponse<InfoLineVM> InfoView(bool choose, bool manage, InfoManageType mt, IList<InfoLineVM> toManage, InfoLineVM initiallySelected)
		{
            TaskCompletionSource<EventArgs> pushed = new TaskCompletionSource<EventArgs>();
            var tt = srv.Current;
            var tcs = new TaskCompletionSource<InfoManageView.ctr>();
            InfoManageView iman_c = null;
            App.UIThread (() => {
                iman_c = new InfoManageView(choose, manage);
                srv.AttachToCommander(iman_c, mt);
				iman_c.Title = mt == InfoManageType.In ? tt.dialect.InputInfoPlural : tt.dialect.OutputInfoPlural;
				iman_c.Items = toManage;
				iman_c.initiallySelectedItem = iman_c.selectedItem = initiallySelected; // null works.
				iman_c.completedTask = tcs;
                srv.nav.PushAsync(iman_c).ContinueWith(t => pushed.SetResult(new EventArgs()));
			});
            return new ViewTask<InfoLineVM>(() =>
            {
                if (tcs.Task.IsCompleted && tcs.Task.Result.popping)
                    return Task.FromResult(true); // dont do it!
                return srv.nav.RemoveOrPopAsync(iman_c);
            }, pushed.Task, tcs.Task.ContinueWith(t=>t.Result.vm)); // return result, or initial if it gave null (wich is null if it really was and no change)
		}

        public IInputResponse<EventArgs> ManageInvention(IList<InventedTrackerVM> toManage)
        {
            TaskCompletionSource<EventArgs> pushed = new TaskCompletionSource<EventArgs>();
            InventionManageView iman_c = null;
            App.UIThread(() => {
                iman_c = new InventionManageView();
                srv.AttachToCommander(iman_c);
                iman_c.Items = toManage;
                srv.nav.PushAsync(iman_c).ContinueWith(t => pushed.SetResult(new EventArgs()));
            });
            return new ViewTask<EventArgs>(() => srv.nav.RemoveOrPopAsync(iman_c), pushed.Task, null); // return result, or initial if it gave null (wich is null if it really was and no change)
        }

        readonly CommonServices srv;
		public UserInputWrapper(CommonServices srv)
        {
            this.srv = srv;
			pv.chosen += v => pv_callback(v);
			message = s => Message (s);
            fv = new InfoFindView(new ValueRequestFactory(srv));
        }

		readonly InfoFindView fv;
		public IInputResponse<InfoLineVM> Choose (IFindList<InfoLineVM> ifnd)
		{
			TaskCompletionSource<InfoLineVM> tcs = new TaskCompletionSource<InfoLineVM> ();
            App.UIThread ( async () => {
				await srv.nav.PushAsync(fv);
				tcs.SetResult (await fv.Choose (ifnd));
			});
            return new ViewTask<InfoLineVM>(tcs.Task);
		}

		public IInputResponse<String> SelectString(string title, IReadOnlyList<string> strings, int initial)
        {
			throw new NotImplementedException();
        }

		Action<int> pv_callback = delegate { };
        readonly ChoosePlanView pv = new ChoosePlanView();
		public IInputResponse<int> ChoosePlan(string title, IReadOnlyList<ItemDescriptionVM> choose_from, int initial)
		{
			TaskCompletionSource<EventArgs> tcs = new TaskCompletionSource<EventArgs> ();
			TaskCompletionSource<int> res = new TaskCompletionSource<int> ();
            App.UIThread (async () => {
				// make this async, which means it can await and continue nicely
				pv_callback = cv => {
					pv_callback = delegate { }; // overwrite it, no double call
					res.SetResult (cv);
				};
				pv.mPlanChoices.Clear ();
				foreach (var pi in choose_from)
					pv.mPlanChoices.Add (pi);
				pv.choicey = null;
				await srv.nav.PushAsync (pv);
				tcs.SetResult (new EventArgs ());
			});
			return new ViewTask<int> (() => srv.nav.RemoveOrPopAsync (pv),tcs.Task,res.Task);
		}

		public IInputResponse<bool> WarnConfirm(string action)
        {
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
            App.UIThread(async () => tcs.SetResult(await srv.root.DisplayAlert ("Warning", action, "OK", "Cancel")));
            return new ViewTask<bool>(tcs.Task);
        }
		public IInputResponse Message(string msg)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
			App.UIThread (async () => {
				await srv.root.DisplayAlert ("Message", msg, "OK");
				tcs.SetResult (false);
			});
            return new ViewTask(tcs.Task);
        }
    }
}
