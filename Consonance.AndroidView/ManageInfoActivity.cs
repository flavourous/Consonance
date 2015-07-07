
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

	[Activity (Label = "ManageInfoActivity")]			
	public class ManageInfoActivity : Activity
	{
		public static void SetSharedObjects(String infoPlural, Action finished, ChangeTriggerList<InfoLineVM> toShow, BoundRequestCaller<InfoLineVM> icom)
		{
			SO.infoPlural = infoPlural;
			SO.finished = finished;
			SO.toShow = toShow;
			SO.icom = icom;
		}
		static class SO
		{
			public static String infoPlural;
			public static Action finished;
			public static ChangeTriggerList<InfoLineVM> toShow;
			public static BoundRequestCaller<InfoLineVM> icom;
		}

		readonly AndroidRequestBuilder defBuilder;
		public ManageInfoActivity()
		{
			defBuilder = new AndroidRequestBuilder (this);
		}
		ListView ilv;
		Button b_add,b_edit,b_delete;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create your application here
			SetContentView (Resource.Layout.ManageInfoView);

			// cant be clicked when not showing, so leave attached.
			(b_add=FindViewById<Button>(Resource.Id.add)).Click += (sender, e) => SO.icom.OnAdd(defBuilder);
			(b_edit=FindViewById<Button>(Resource.Id.edit)).Click += (sender, e) => SO.icom.OnEdit(si, defBuilder);
			(b_delete=FindViewById<Button>(Resource.Id.delete)).Click += (sender, e) => SO.icom.OnRemove(si);
			b_delete.Enabled = b_edit.Enabled = false;

			// state
			ilv = FindViewById<ListView>(Resource.Id.infolist);
			ilv.ItemClick += Ilv_ItemClick;;
		}

		InfoLineVM si = null;
		void Ilv_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
		{
			si = SO.toShow[e.Position];
			CheckSel ();
		}
		void CheckSel()
		{
			int sidx = SO.toShow.IndexOf (si);
			if (sidx == -1) si = null;
			else if (ilv.SelectedItemPosition != sidx)
				ilv.SetSelection (sidx);
			b_delete.Enabled = b_edit.Enabled = si != null;
		}

		protected override void OnStart ()
		{
			// grab the intent...ugh
			SO.toShow.Changed += Itnt_toShow_Changed;
			Title = "Manage " + SO.infoPlural;
			MakeLAD ();
			base.OnStart ();
		}
		protected override void OnStop ()
		{
			SO.finished ();
			base.OnStop ();
		}

		void Itnt_toShow_Changed ()
		{
			MakeLAD ();
			CheckSel ();
		}
		void MakeLAD()
		{
			var lv = FindViewById<ListView>(Resource.Id.infolist);
			LAdapter<InfoLineVM> ld= new LAdapter<InfoLineVM>(
				this.LayoutInflater, 
				SO.toShow,
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

