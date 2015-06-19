
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.ComponentModel;

namespace Consonance.AndroidView
{
	class ManageInfoIntent : Intent
	{
		public readonly Action finished;
		public readonly ChangeTriggerList<InfoLineVM> toShow;
		public readonly BoundRequestCaller<InfoLineVM> icom;
		public ManageInfoIntent(Action finished, ChangeTriggerList<InfoLineVM> l, BoundRequestCaller<InfoLineVM> c, Context cont, Java.Lang.Class cls) : base(cont,cls)
		{
			this.finished = finished;
			this.toShow = l;
			this.icom = c;
		}
	}

	[Activity (Label = "ManageInfoActivity")]			
	public class ManageInfoActivity : Activity
	{
		readonly AndroidRequestBuilder defBuilder;
		public ManageInfoActivity()
		{
			defBuilder = new AndroidRequestBuilder (this);
		}

		ManageInfoIntent itnt;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create your application here
			SetContentView (Resource.Layout.ManageInfoView);

			// cant be clicked when not showing, so leave attached.
			FindViewById<Button>(Resource.Id.add).Click += (sender, e) => itnt.icom.OnAdd(defBuilder);
			FindViewById<Button>(Resource.Id.edit).Click += (sender, e) => itnt.icom.OnEdit(itnt.toShow[sid], defBuilder);
			FindViewById<Button>(Resource.Id.delete).Click += (sender, e) => itnt.icom.OnRemove(itnt.toShow[sid]);
		}
		protected override void OnStart ()
		{
			// grab the intent...ugh
			itnt = Intent as ManageInfoIntent;
			itnt.toShow.Changed += Itnt_toShow_Changed;;
			MakeLAD ();
			base.OnStart ();
		}
		protected override void OnStop ()
		{
			itnt.toShow.Changed -= Itnt_toShow_Changed;;
			itnt.finished ();
			base.OnStop ();
		}

		void Itnt_toShow_Changed ()
		{
			MakeLAD ();
		}
		int sid { get { return FindViewById<ListView> (Resource.Id.infolist).SelectedItemPosition; } }
		void MakeLAD()
		{
			var lv = FindViewById<ListView>(Resource.Id.infolist);
			LAdapter<InfoLineVM> ld= new LAdapter<InfoLineVM>(
				this, 
				itnt.toShow,
				Resource.Layout.ManageInfoLine,
				ConfigLine
			);
			lv.Adapter = ld;
		}
		void ConfigLine(View lv, InfoLineVM vm)
		{
			lv.FindViewById<TextView> (Resource.Id.value).Text = vm.name;
		}
	}
}

