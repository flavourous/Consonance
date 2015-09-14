using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Consonance;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
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
			viewWrapper = new ViewWrapper(main, iman);
			defaultBuilder = new ValueRequestBuilder(navigator.Navigation);
			userInputWrapper = new UserInputWrapper(navigator);
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
