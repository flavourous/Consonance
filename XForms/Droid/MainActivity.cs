using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;

namespace Consonance.XamarinFormsView.Droid
{
	[Activity (Label = "Loading", Icon = "@drawable/icon", MainLauncher = true, NoHistory = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class SplashActivity : Activity
	{
		Bundle meloady;
		protected override void OnCreate (Bundle bundle)
		{
			App.TLog("native droid mainactivity oncreate begun");
			meloady = bundle;
			base.OnCreate (bundle);
			ActionBar.Hide ();

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			App.TLog("created native droid");
		}

		protected override void OnStart ()
		{
			base.OnStart ();
			App.TLog("native droid onstart - launching task to prepare for forms");
			Platform plat = new Platform (); //
			plat.RunTask (async () => {
				App.TLog ("forms task started, creating forms App+forms init on droid activity");
				global::Xamarin.Forms.Forms.Init (this, meloady);
				MainActivity.appy = new App ();
				App.TLog ("presenting to forms App");
				await MainActivity.appy.Presentation (plat);
				App.TLog ("starting forms activity");
				StartActivity (typeof(MainActivity));
			});
		}
	}

	[Activity (Label = "Consonance.XamarinFormsView", Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		public static App appy;
		protected override void OnCreate (Bundle bundle)
		{
			App.TLog("creating forms app");
			base.OnCreate (bundle);
			global::Xamarin.Forms.Forms.Init (this, bundle);
			LoadApplication (appy);
			App.TLog("forms app is created");
		}
	}
}

