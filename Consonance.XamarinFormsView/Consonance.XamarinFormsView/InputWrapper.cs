using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
    class UserInputWrapper : IUserInput
    {
        readonly NavigationPage nav;
        public UserInputWrapper(NavigationPage nav)
        {
            this.nav = nav;
        }

        public void SelectString(string title, IReadOnlyList<string> strings, int initial, Promise<int> completed)
        {
            throw new NotImplementedException();
        }

        public void ChoosePlan(string title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial, Promise<int> completed)
        {
            throw new NotImplementedException();
        }

        public void WarnConfirm(string action, Promise confirmed)
        {
            nav.DisplayAlert("Warning", action, "OK", "Cancel").ContinueWith(v => { if (v.Result) confirmed(); });
        }
    }
}
