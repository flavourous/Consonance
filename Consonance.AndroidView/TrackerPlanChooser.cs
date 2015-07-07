
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Consonance.AndroidView
{
	public class TrackerPlanChooser : Dialog
	{
		public TrackerPlanChooser (Context context) :
			base (context)
		{
			Initialize ();
		}

		void Initialize ()
		{
			SetContentView (Resource.Layout.ChooseTrackerPlanView);
			FindViewById<Button> (Resource.Id.ok).Click += (sender, e) => {
				completed (FindViewById<ListView> (Resource.Id.list).SelectedItemPosition);
				Cancel ();
			};
			FindViewById<Button> (Resource.Id.cancel).Click += (sender, e) => Cancel ();
		}

		void initItem(View v,TrackerDetailsVM vm)
		{
			var tv= v.FindViewById<TextView>(Resource.Id.value);
			tv.Text = vm.name;
			tv.Click += (sender, e) => {
				v.FindViewById<TextView>(Resource.Id.description).Text = vm.description;
			};
		}
		Promise<int> completed = delegate { };
		public void ChoosePlan (string title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial, Promise<int> completed)
		{
			this.completed = completed;
			LAdapter<TrackerDetailsVM> lad = new LAdapter<TrackerDetailsVM>
				(
					Context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater, // try it...
					choose_from,
					Resource.Layout.ChooseTrackerPlanItem,
					initItem
				);
			var lv = FindViewById<ListView> (Resource.Id.list);
			lv.Adapter = lad;
			lv.SetSelection (initial);
		}
	}
}

