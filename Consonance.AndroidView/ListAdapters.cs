using System;
using System.Collections;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;

namespace Consonance.AndroidView
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
	class DAdapter : BaseAdapter<TrackerInstanceVM>, IEnumerable<TrackerInstanceVM>
	{
		readonly Activity context;
		readonly List<TrackerInstanceVM> vms;
		public DAdapter(Activity context, List<TrackerInstanceVM> vms)
		{
			this.context = context;
			this.vms = vms;
		}
		public override long GetItemId (int position) { return position; }
		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			View view = convertView; // re-use an existing view, if one is available
			if (view == null) // otherwise create a new one
				view = context.LayoutInflater.Inflate (Resource.Layout.DietInstanceLine, null);
			var vm = vms [position];
			view.FindViewById<TextView> (Resource.Id.dietitemname).Text = vm.name  + (vm.tracked ? "[Tracked]" : "");
			view.FindViewById<TextView> (Resource.Id.dietitemdatetime).Text = vm.start.ToShortDateString ();
			view.FindViewById<TextView> (Resource.Id.dietitemmetric).Text = vm.displayAmounts [0].Key + ": " + vm.displayAmounts [0].Value.ToString ("F2");
			return view;
		}
		public override int Count {
			get {
				return vms.Count;
			}
		}
		public override TrackerInstanceVM this [int index] { get { return vms [index]; } }

		#region IEnumerable implementation
		public IEnumerator<TrackerInstanceVM> GetEnumerator ()
		{
			return new DEn (this);
		}
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		#endregion
	}
	class DEn : IEnumerator<TrackerInstanceVM>
	{
		readonly DAdapter dd;
		int st = -1;
		public DEn(DAdapter dd)
		{
			this.dd = dd;
		}
		#region IEnumerator implementation
		public bool MoveNext ()
		{
			st++;
			return st < dd.Count;
		}
		public void Reset ()
		{
			st = 0; 
		}
		TrackerInstanceVM current { get { return dd [st]; } }
		object IEnumerator.Current { get { return current; } }
		public TrackerInstanceVM Current { get { return current; } }
		#endregion
		public void Dispose () { }
	}
	class LAdapter<T> : BaseAdapter<T>
	{
		readonly LayoutInflater layoutInflater;
		readonly IReadOnlyList<T> vms;
		readonly int uView;
		readonly ViewConfiguror<T> uConfig;
		public LAdapter(LayoutInflater layoutInflater, IReadOnlyList<T> vms, int viewID, ViewConfiguror<T> config)
		{
			this.layoutInflater = layoutInflater;
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
				view = layoutInflater.Inflate (uView, null);
			uConfig (view, vms [position]);
			return view;
		}

		public override int Count {
			get {
				return vms.Count;
			}
		}
		public override T this [int index] { get { return vms [index]; } }
		#endregion
	}

}

