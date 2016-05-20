using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Consonance;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace Consonance.XamarinFormsView.PCL
{
	public static class Extensions
	{
        /// <summary>
        /// same spec as PopAsync
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static async Task RemoveOrPopAsync(this INavigation me, Page page)
        {
            if (me.NavigationStack.Contains(page))
            {
                if (me.NavigationStack[me.NavigationStack.Count - 1] == page)
                {
                    await me.PopAsync();
                }
                else
                {
                    Debug.WriteLine("Navigation Extension: Removing {0}", page);
                    me.RemovePage(page);
                    await Task.Yield();
                }
            }
        }
	}

    
        public class App : Application
    {
		public static IValueRequestBuilder bld;
		readonly ViewWrapper viewWrapper;
		readonly PlanCommandsWrapper planCommandWrapper;
		readonly ValueRequestBuilder defaultBuilder;
		readonly UserInputWrapper userInputWrapper;
        public static IPlatform platform { get; private set; }
		public Task Presentation(IPlatform plat)
		{
			plat.Attach ((err, a) => ErrorDialog.Show (err, MainPage.Navigation, a));
            App.platform = plat;
			return Presenter.PresentTo(viewWrapper, plat, userInputWrapper, planCommandWrapper, defaultBuilder);
		}

        static Dictionary<Page, Queue<Action>> cb = new Dictionary<Page, Queue<Action>>();
        public static void RegisterPoppedCallback(Page page, Action callback)
        {
            if (!cb.ContainsKey(page))
                cb[page] = new Queue<Action>();
            cb[page].Enqueue(callback);
        }

        private void Navigator_Popped(object sender, NavigationEventArgs e)
        {
            if (cb.ContainsKey(e.Page))
            {
                while (cb[e.Page].Count > 0)
                    cb[e.Page].Dequeue()();
                cb.Remove(e.Page);
            }
        }

        public App()
		{
			// some pages.
			var main = new MainTabs();
            var navigator = new NavigationPage(main);

			// The root page of your application
			MainPage = navigator;
            navigator.Popped += Navigator_Popped;

			// instantiate wrappers
			viewWrapper = new ViewWrapper(main);
			bld = defaultBuilder = new ValueRequestBuilder(navigator.Navigation);
			planCommandWrapper = new PlanCommandsWrapper(defaultBuilder, main);
			userInputWrapper = new UserInputWrapper(navigator, (c,m) => {
				var im = new InfoManageView (c,m);
				planCommandWrapper.Attach(im);
				return im;
			}, () => viewWrapper.currentTrackerInstance.sender as IAbstractedTracker);

        }


        protected override void OnStart()
        {
            base.OnStart();
        }

    }
}
