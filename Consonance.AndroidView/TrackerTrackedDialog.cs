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

		public void Show(IEnumerable<TrackerInstanceVM> manag)
		{
			SetContentView (Resource.Layout.TrackerInfo_Manage);
			LAdapter<TrackerInstanceVM> lad = 
				new LAdapter<TrackerInstanceVM> (
					Context, 
					new List<TrackerInstanceVM> (manag),
					Android.Resource.Layout.SimpleListItemMultipleChoice,
					(v, vm) => {
						v.FindViewById<CheckBox>(Resource.Id.time_cb).Checked = vm.tracked;
					});
			var lv = FindViewById<ListView> (Resource.Id.managetivm);
			lv.Adapter = lad;
			lv.ItemClick += (object sender, AdapterView.ItemClickEventArgs e) => {
				vm.tracked = !vm.tracked;

				e.View.FindViewById<CheckBox>(Resource.Id.time_cb).Checked = vm.tracked;
			};

		}
	}
}

