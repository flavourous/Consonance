using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;

namespace ManyDiet.AndroidView
{
	public delegate void ViewConfiguror<VMType>(View view, VMType vm); 
	public static class ItemViewConfigs
	{
		public static void Eat(View view, EntryLineVM vm, String useTrack)
		{
			view.FindViewById<TextView> (Resource.Id.eatitemname).Text = vm.name;
			view.FindViewById<TextView> (Resource.Id.eatitemdatetime).Text = vm.start.ToString();
			var find = vm.displayAmounts.FindAll (k => k.Key == useTrack);
			if(find.Count > 0)
				view.FindViewById<TextView> (Resource.Id.eatitemtrack).Text = find[0].Value.ToString("F2");
		}
		public static void Burn(View view, EntryLineVM vm, String useTrack)
		{
			view.FindViewById<TextView> (Resource.Id.burnitemname).Text = vm.name;
			view.FindViewById<TextView> (Resource.Id.burnitemdatetime).Text = vm.start.ToString();
			var find = vm.displayAmounts.FindAll (k => k.Key == useTrack);
			if(find.Count > 0)
				view.FindViewById<TextView> (Resource.Id.burnitemtrack).Text = find[0].Value.ToString("F2");
		}
	}
	class DAdapter : BaseAdapter<DietInstanceVM>{}
	class LAdapter : BaseAdapter<EntryLineVM>
	{
		readonly Activity context;
		readonly List<EntryLineVM> vms;
		readonly int uView;
		readonly ViewConfiguror<EntryLineVM> uConfig;
		public LAdapter(Activity context, List<EntryLineVM> vms, int viewID, ViewConfiguror<EntryLineVM> config)
		{
			this.context = context;
			this.vms = vms;
			uConfig = config;
			uView = viewID;
		}

		#region implemented abstract members of BaseAdapter
		public override long GetItemId (int position) { return position; } 
		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			View view = convertView; // re-use an existing view, if one is available
			if (view == null || view.Id != uView) // otherwise create a new one
				view = context.LayoutInflater.Inflate (uView, null);
			uConfig (view, vms [position]);
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

