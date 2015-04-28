using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;

namespace ManyDiet.AndroidView
{

	class LAdapter : BaseAdapter<EntryLineVM>
	{
		readonly Activity context;
		readonly List<EntryLineVM> vms;
		readonly String useTrack;
		public LAdapter(Activity context, List<EntryLineVM> vms, String useTrack)
		{
			this.context = context;
			this.vms = vms;
			this.useTrack=useTrack;
		}

		#region implemented abstract members of BaseAdapter
		public override long GetItemId (int position) { return position; } 
		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			View view = convertView; // re-use an existing view, if one is available
			if (view == null) // otherwise create a new one
				view = context.LayoutInflater.Inflate (Resource.Layout.EatEntryLine, null);
			view.FindViewById<TextView> (Resource.Id.eatitemname).Text = vms [position].name;
			view.FindViewById<TextView> (Resource.Id.eatitemdatetime).Text = vms [position].when.ToString();
			var find = vms [position].displayAmounts.FindAll (k => k.Key == useTrack);
			if(find.Count > 0)
				view.FindViewById<TextView> (Resource.Id.eatitemtrack).Text = find[0].Value.ToString("F2");
			return view;
		}

		public override int Count {
			get {
				return vms.Count;
			}
		}

		public override EntryLineVM this [int index] { get { return vms [index]; } }
		#endregion
	}


}

