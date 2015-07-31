using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace Consonance.XamarinFormsView
{
    class UserInputWrapper : IUserInput
    {
		readonly Page root;
		INavigation nav { get { return root.Navigation; } }
		public UserInputWrapper(Page root)
        {
			this.root = root;
			pv.chosen += v => pv_callback(v);
        }

		public async Task SelectString(string title, IReadOnlyList<string> strings, int initial, Promise<int> completed)
        {
			throw new NotImplementedException();
        }

		Promise<int> pv_callback = async delegate { };
        readonly ChoosePlanView pv = new ChoosePlanView();
		public async Task ChoosePlan(string title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial, Promise<int> completed)
        {
			// make this async, which means it can await and continue nicely
			pv_callback = async cv => {
				pv_callback = async delegate { }; // no double call pls.
				await completed (cv);
				// if we are still on top of the stack, pop.
				if(nav.NavigationStack[nav.NavigationStack.Count-1] == pv)
					await nav.PopAsync();
				//otherwise, pull ourselves outta the stack.
				else nav.RemovePage(pv);
			};
            pv.PlanChoices.Clear();
            foreach (var pi in choose_from)
                pv.PlanChoices.Add(pi);
			await nav.PushAsync (pv);
        }

		public async Task WarnConfirm(string action, Promise confirmed)
        {
            await root.DisplayAlert("Warning", action, "OK", "Cancel").ContinueWith(v => { if (v.Result) confirmed(); });
        }
    }
}
