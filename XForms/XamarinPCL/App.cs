﻿using System;
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
		public static void RemoveOrPop(this INavigation me, Page page)
		{
            App.platform.UIThread (() => {
				if (me.NavigationStack.Contains (page)) {
					if (me.NavigationStack [me.NavigationStack.Count - 1] == page) me.PopAsync ();
					else {
						Debug.WriteLine("Navigation Extension: Removing {0}", page);
						me.RemovePage (page);
					}
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
        public static IPlatform platform { get; private set; }
		public Task Presentation(IPlatform plat)
		{
			plat.Attach ((err, a) => ErrorDialog.Show (err, MainPage.Navigation, a));
            App.platform = plat;
			return Presenter.PresentTo(viewWrapper, plat, userInputWrapper, planCommandWrapper, defaultBuilder);
		}

        public App()
		{
			// some pages.
			var main = new MainTabs();
            var navigator = new NavigationPage(main);

			navigator.Pushed += (sender, e) => Debug.WriteLine("Navigation: Pushed {0}", e.Page);
			navigator.Popped += (sender, e) => Debug.WriteLine("Navigation: Popped {0}", e.Page);

			// The root page of your application
			MainPage = navigator;

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