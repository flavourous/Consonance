﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Consonance;

using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
    public class App : Application, IView
    {
        public App()
        {
            // The root page of your application
            MainPage = new MainView();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
