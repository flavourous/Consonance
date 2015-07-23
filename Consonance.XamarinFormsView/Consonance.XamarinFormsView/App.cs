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
        public App()
        {
			var main = new MainTabs();
            var navigator = new NavigationPage(main);
            // The root page of your application
			MainPage = navigator;

            viewWrapper = new ViewWrapper(main);
            planCommandWrapper = new PlanCommandsWrapper();
			defaultBuilder = new ValueRequestBuilder(navigator.Navigation);
            userInputWrapper = new UserInputWrapper(navigator);
        }

        readonly ViewWrapper viewWrapper;
        readonly PlanCommandsWrapper planCommandWrapper;
        readonly ValueRequestBuilder defaultBuilder;
        readonly UserInputWrapper userInputWrapper;
        protected override void OnStart()
        {
            // Handle when your app starts
            Presenter.Singleton.PresentTo(viewWrapper, userInputWrapper, planCommandWrapper, defaultBuilder);
        }
    }
    
}
