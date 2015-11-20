using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Consonance;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Consonance.XamarinFormsView
{
	public static class Extensions
	{
		public static void RemoveOrPop(this INavigation me, Page page)
		{
			ViewWrapper.InvokeOnMainThread (() => {
				if (me.NavigationStack.Contains (page)) {
					if (me.NavigationStack [me.NavigationStack.Count - 1] == page) me.PopAsync ();
					else me.RemovePage (page);
				}
			});
		}
	}
    public class App : Application
    {
		readonly ViewWrapper viewWrapper;
		readonly PlanCommandsWrapper planCommandWrapper;
		readonly ValueRequestBuilder defaultBuilder;
		readonly UserInputWrapper userInputWrapper;

        public App()
		{
			// some pages.
			var iman = new InfoManageView ();
			var main = new MainTabs();
            var navigator = new NavigationPage(main);

			// The root page of your application
			MainPage = navigator;

			// instantiate wrappers
			viewWrapper = new ViewWrapper(main);
			defaultBuilder = new ValueRequestBuilder(navigator.Navigation);
			userInputWrapper = new UserInputWrapper(navigator, iman, () => viewWrapper.currentTrackerInstance.sender as IAbstractedTracker);
			planCommandWrapper = new PlanCommandsWrapper(defaultBuilder, main, iman);
        }

        protected override void OnStart()
        {
            // Handle when your app starts

			// just let go of this async loader method.
			Presenter.PresentTo(viewWrapper, userInputWrapper, planCommandWrapper, defaultBuilder);
        }
    }
    
}
