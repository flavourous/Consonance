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
			App.TLog ("native droid onstart - init xamarin.forms inside onstart");
			global::Xamarin.Forms.Forms.Init (this, meloady);
			App.TLog ("forms task started, creating forms App+forms init on droid activity, using xamarinforms bgininvokeonmaintherad to return to loop and displayh loading");
			MainActivity.appy = new App ();
			App.TLog ("presenting to forms App");
			var pres = MainActivity.appy.Presentation (new Platform ());
			pres.ContinueWith (t => {
				App.TLog ("starting forms activity");
				StartActivity (typeof(MainActivity));
			}, TaskContinuationOptions.OnlyOnRanToCompletion);
			pres.ContinueWith (t => {
				throw t.Exception;
			}, TaskContinuationOptions.OnlyOnFaulted);
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

