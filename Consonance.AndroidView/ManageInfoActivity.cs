
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

		readonly AndroidRequestBuilder defBuilder;
		public ManageInfoActivity()
		{
			defBuilder = new AndroidRequestBuilder (new DialogRequestBuilder (this), this);
		}
		ListView ilv;
		Button b_add,b_edit,b_delete, b_find;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create your application here
			SetContentView (Resource.Layout.ManageInfoView);

			// cant be clicked when not showing, so leave attached.
			(b_add=FindViewById<Button>(Resource.Id.add)).Click += (sender, e) => SO.icom.OnAdd(defBuilder);
			(b_edit=FindViewById<Button>(Resource.Id.edit)).Click += (sender, e) => SO.icom.OnEdit(si, defBuilder);
			(b_delete=FindViewById<Button>(Resource.Id.delete)).Click += (sender, e) => SO.icom.OnRemove(si);
			(b_find = FindViewById<Button> (Resource.Id.find)).Click += (sender, e) => {
				// start the finder dialog
				var fd = new FindMoreInfoDialog(this, SO.externalFindy);
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
		readonly IFindList<InfoLineVM> finder;
		readonly LAdapter<InfoLineVM> lads;
		public FindMoreInfoDialog(Context c, IFindList<InfoLineVM> finder) : base(c)
		{
			this.finder = finder;
			lads = new LAdapter<InfoLineVM> (
				LayoutInflater, 
				new InfoLineVM[0],
				Resource.Layout.ManageInfoLine,
				ILVMConfig.ConfigLine
			);
		}



		protected override void OnCreate (Bundle savedInstanceState)
		{
			// reuse the above layout plus hacks! lol
			base.OnCreate (savedInstanceState);

			SetContentView (Resource.Layout.FindInfoView);

			// create and push in the filter view, we write below.
			var fvContainer = FindViewById<RelativeLayout> (Resource.Id.filterContainer);
			var filter = new FilteringView (Context);
			filter.LayoutParameters = new RelativeLayout.LayoutParams (RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.WrapContent);
			fvContainer.AddView (filter);
			filter.filterChanged += (obj) => lads.SwitchData(finder.Find(obj));

			// cant be clicked when not showing, so leave attached.
			var b_import=FindViewById<Button>(Resource.Id.importitem);
			var ilv = FindViewById<ListView>(Resource.Id.infolist);
			b_import.Click += (sender, e) => {
				var itm = lads[ilv.SelectedItemPosition];
				finder.Import(itm);
				Toast.MakeText(Context ,"Imported " + itm.name, ToastLength.Short);
			};

			// state
			b_import.Enabled = false;
			ilv.ItemClick += (sender, e) => {
				if(e.Position > -1)
					ilv.SetSelection(e.Position); //hacks	
				b_import.Enabled = e.Position > -1;
			};
		
		}

	}

	class FilteringView : LinearLayout
	{
		public event Action<String> filterChanged = delegate{ };
		public FilteringView(Context c) : base(c)
		{
			var tv = new EditText (c);	
			tv.LayoutParameters = new ViewGroup.LayoutParams (ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
			tv.TextChanged += (sender, e) => filterChanged(tv.Text);
		}
	}
}

