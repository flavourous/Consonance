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
        readonly NavigationPage nav;
        public UserInputWrapper(NavigationPage nav)
        {
            this.nav = nav;
			pv.chosen += v => pv_callback(v);//this is async!
        }

        public void SelectString(string title, IReadOnlyList<string> strings, int initial, Promise<int> completed)
        {
            throw new NotImplementedException();
        }

		Func<int, Task> pv_callback = async delegate { };
        readonly ChoosePlanView pv = new ChoosePlanView();
        public void ChoosePlan(string title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial, Promise<int> completed)
        {
			// make this async, which means it can await and continue nicely
			pv_callback = async cv => {
				pv_callback = async delegate { }; // no double call pls.
				await nav.PopAsync (); // wait till we popped to complete
				completed (cv);
			};
            pv.PlanChoices.Clear();
            foreach (var pi in choose_from)
                pv.PlanChoices.Add(pi);
			nav.PushAsync (pv);
        }

        public void WarnConfirm(string action, Promise confirmed)
        {
            nav.DisplayAlert("Warning", action, "OK", "Cancel").ContinueWith(v => { if (v.Result) confirmed(); });
        }
    }
}
