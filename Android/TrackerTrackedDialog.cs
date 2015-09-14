using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Android.Content;
using System.Collections;
using System.Collections.Generic;

namespace Consonance.AndroidView
{
	public class TrackerTrackedDialog : Dialog
	{
		public TrackerTrackedDialog (Context ctx) : base(ctx)
		{
		}

		protected override void OnCreate (Android.OS.Bundle savedInstanceState)
		{
			RequestWindowFeature ((int)WindowFeatures.NoTitle);
			SetCanceledOnTouchOutside (true);
			SetContentView (Resource.Layout.TrackerInfo_Manage);
			FindViewById<Button>(Resource.Id.btnclose).Click += (sender, e) => Cancel();
			base.OnCreate (savedInstanceState);
		}
		protected override void OnStart ()
		{
			base.OnStart ();
			var lv = FindViewById<ListView> (Resource.Id.managetivm);
			lv.Adapter = lad;
		}

		LAdapter<TrackerInstanceVM> lad;
		Action onClose = delegate { };
		protected override void OnStop ()
		{
			onClose ();
			base.OnStop ();
		}
		public void Show(Activity fromact, IEnumerable<TrackerInstanceVM> manag, TrackerInstanceVM current, Action onClose)
		{
			this.onClose = onClose;
			lad = new LAdapter<TrackerInstanceVM> (
					fromact.LayoutInflater, 
					new List<TrackerInstanceVM> (manag),
					Resource.Layout.TrackerInfo_Manage_Entry,
					(v, vm) => {
					var vcb = v.FindViewById<CheckBox>(Resource.Id.time_cb);
						if(OriginatorVM.OriginatorEquals(current,vm)) 
							vcb.Enabled = false;
						vcb.Checked = vm.tracked;
						vcb.Text = vm.name;
						vcb.CheckedChange += (sender, e) => vm.tracked = vcb.Checked;
					});
			Show ();
		}
	}
}

