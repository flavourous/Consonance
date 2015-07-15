
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
		public static void SetSharedObjects(String infoPlural, Action finished, ChangeTriggerList<InfoLineVM> toShow, IFindList<InfoLineVM> finder, BoundRequestCaller<InfoLineVM> icom)
		{
			SO.infoPlural = infoPlural;
			SO.finished = finished;
			SO.toShow = toShow;
			SO.icom = icom;
			SO.externalFindy = finder;
		}
		static class SO
		{
			public static IFindList<InfoLineVM> externalFindy;
			public static String infoPlural;
			public static Action finished;
			public static ChangeTriggerList<InfoLineVM> toShow;
			public static BoundRequestCaller<InfoLineVM> icom;
		}

		AndroidRequestBuilder defBuilder;
		ListView ilv;
		Button b_add,b_edit,b_delete, b_find;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// default builer
			defBuilder = new AndroidRequestBuilder (new DialogRequestBuilder (this), this);

			// Create your application here
			SetContentView (Resource.Layout.ManageInfoView);

			// cant be clicked when not showing, so leave attached.
			(b_add=FindViewById<Button>(Resource.Id.add)).Click += (sender, e) => SO.icom.OnAdd(defBuilder);
			(b_edit=FindViewById<Button>(Resource.Id.edit)).Click += (sender, e) => SO.icom.OnEdit(si, defBuilder);
			(b_delete=FindViewById<Button>(Resource.Id.delete)).Click += (sender, e) => SO.icom.OnRemove(si);
			(b_find = FindViewById<Button> (Resource.Id.find)).Click += (sender, e) => {
				// start the finder dialog
				var fd = new FindMoreInfoDialog(this);
				fd.SetFinder(SO.externalFindy);
				fd.Show();
			}; 

			b_find.Enabled = SO.externalFindy.CanFind;
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
				ILVMConfig.ConfigLine
			);
			lv.Adapter = ld;
		}

	}

	static class ILVMConfig
	{
		public static void ConfigLine(View lv, InfoLineVM vm)
		{
			lv.FindViewById<TextView> (Resource.Id.value).Text = vm.name;
		}
	}

	class FindMoreInfoDialog : Dialog
	{
		IFindList<InfoLineVM> finder;
		LAdapter<InfoLineVM> lads;
		public FindMoreInfoDialog(Context c) : base(c) { }
		public void SetFinder(IFindList<InfoLineVM> finder)
		{
			this.finder = finder;
		}
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			Window.RequestFeature (WindowFeatures.NoTitle);
			SetContentView (Resource.Layout.FindInfoView);

			// set up adapter
			lads = new LAdapter<InfoLineVM> (
				LayoutInflater, 
				new InfoLineVM[0],
				Resource.Layout.ManageInfoLine,
				ILVMConfig.ConfigLine
			);
			var ilv = FindViewById<ListView>(Resource.Id.infolist);
			ilv.Adapter = lads;

			// cant be clicked when not showing, so leave attached.
			var b_import=FindViewById<Button>(Resource.Id.importitem);
			b_import.Click += (sender, e) => {
				var itm = lads[ilv.CheckedItemPosition];
				finder.Import(itm);
				Toast.MakeText(Context ,"Imported " + itm.name, ToastLength.Short).Show();
			};

			// go searcht button
			var b_find = FindViewById<Button>(Resource.Id.filterGo);
			b_find.Click += (sender, e) => {
				lads.SwitchData(finder.Find());
			};

			// state
			b_import.Enabled = false;
			ilv.ItemClick += (sender, e) => {
				if(e.Position > -1)
					ilv.SetItemChecked(e.Position, true); //hacks	
				b_import.Enabled = e.Position > -1;
			};

			// mr factory
			var factory = new ValueRequestFactory (Context);

			// filter change delegate
			Action msSel = delegate { };

			// mode spinner
			var modeSelector = FindViewById<Spinner> (Resource.Id.modeSpinner);
			modeSelector.ItemSelected += (sender, e) => msSel ();
			modeSelector.Adapter = new ArrayAdapter<String> (Context, Android.Resource.Layout.SimpleListItem1, finder.FindModes);

			// filtercontainer
			LinearLayout filterHost = FindViewById<LinearLayout>(Resource.Id.valReqFrame);

			// changing method
			msSel = () => {
				filterHost.RemoveAllViews ();
				foreach (var vr in finder.UseFindMode(finder.FindModes[modeSelector.SelectedItemPosition], factory)) {
					var vrw = vr as ValueRequestWrapper;
					filterHost.AddView (vrw.inputView);
				}
			};
			// fire it now...
			msSel ();	
		
		}

	}
		
}

