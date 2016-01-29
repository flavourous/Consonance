using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Consonance;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace Consonance.XamarinFormsView
{
	public static class Extensions
	{
		public static void RemoveOrPop(this INavigation me, Page page)
		{
			Device.BeginInvokeOnMainThread (() => {
				if (me.NavigationStack.Contains (page)) {
					if (me.NavigationStack [me.NavigationStack.Count - 1] == page) me.PopAsync ();
					else me.RemovePage (page);
				}
			});
		}
	}
    public class App : Application
    {
		public static IValueRequestBuilder bld;
		
		readonly ViewWrapper viewWrapper;
		readonly PlanCommandsWrapper planCommandWrapper;
		readonly ValueRequestBuilder defaultBuilder;
		readonly UserInputWrapper userInputWrapper;

		public Task Presentation()
		{
			return Presenter.PresentTo(viewWrapper, new Platform((err,a) => ErrorDialog.Show(err, MainPage.Navigation, a)), userInputWrapper, planCommandWrapper, defaultBuilder);
		}

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
			bld = defaultBuilder = new ValueRequestBuilder(navigator.Navigation);
			userInputWrapper = new UserInputWrapper(navigator, iman, () => viewWrapper.currentTrackerInstance.sender as IAbstractedTracker);
			planCommandWrapper = new PlanCommandsWrapper(defaultBuilder, main, iman);

			App.TLog("app constructed");
        }

        protected override void OnStart()
        {
			App.TLog("app started - presenting now");
        }

		static DateTime? itm = null;
		public static void TLog(String msg, params String[] args)
		{
			var tuse = DateTime.Now;
			if(itm == null) itm = tuse;
			System.Diagnostics.Debug.WriteLine ("[" + (tuse-itm.Value).TotalMilliseconds.ToString("F1") + "ms]: " + msg, args);
		}
    }
    
}
