using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Consonance.AndroidView
{
	class TabDescList : List<TabDesc>
	{
		public void Add(String name, int layout, int menu, int contextmenu)
		{
			this.Add(new TabDesc() { name=name, layout=layout, menu=menu, contextmenu=contextmenu });
		}
	}
	class TabDesc { public String name; public int layout; public int menu; public int contextmenu; }



	[Activity (Label = "Consonance", MainLauncher=true, Icon = "@drawable/icon")]
	public class MainActivity : Activity, ActionBar.ITabListener, IView, IUserInput
	{
		TabDescList tabs = new TabDescList () {
			{ "Eat", Resource.Layout.Eat, Resource.Menu.EatMenu, Resource.Menu.EatEntryMenu },
			{ "Burn", Resource.Layout.Burn, Resource.Menu.BurnMenu, Resource.Menu.BurnEntryMenu },
			{ "Plan", Resource.Layout.Plan, Resource.Menu.PlanMenu, Resource.Menu.PlanEntryMenu },
		};
		public void OnTabReselected (ActionBar.Tab tab, FragmentTransaction ft) { }
		public void OnTabUnselected (ActionBar.Tab tab, FragmentTransaction ft) { }
		public void OnTabSelected (ActionBar.Tab tab, FragmentTransaction ft)
		{
			LoadTab ();
		}
		void LoadTab()
		{
			SetContentView (tabs [ActionBar.SelectedNavigationIndex].layout);
			ReloadLayoutForTab (true);
			InvalidateOptionsMenu ();
		}

		int useContext;
		void ReloadLayoutForTab(bool tabchanged=false) {
			// FIXME why break pattern :/
			if (ActionBar.SelectedNavigationIndex == 0) {
				FindViewById<ListView> (Resource.Id.eatlist).Adapter = eatitems.apt;
				FindViewById<TextView> (Resource.Id.eatlisttitletrack).Text = eatitems.name;
				if(tabchanged) RegisterForContextMenu (FindViewById<ListView> (Resource.Id.eatlist));
				useContext = Resource.Menu.EatEntryMenu;
				FindViewById<TextView> (Resource.Id.eatTrackText).Text = eatTrackText;
			}
			if (ActionBar.SelectedNavigationIndex == 1) {
				FindViewById<ListView> (Resource.Id.burnlist).Adapter = burnitems.apt;
				FindViewById<TextView> (Resource.Id.burnlisttitletrack).Text = burnitems.name;
				if(tabchanged) RegisterForContextMenu (FindViewById<ListView> (Resource.Id.burnlist));
				useContext = Resource.Menu.BurnEntryMenu;
				FindViewById<TextView> (Resource.Id.burnTrackText).Text = burnTrackText;
			}
			if (ActionBar.SelectedNavigationIndex == 2) {
				var pl = FindViewById<ListView> (Resource.Id.planlist);
				pl.Adapter = plan;
				useContext = Resource.Menu.PlanEntryMenu;
				SwitchHiglightDietInstance();
				if (tabchanged) {
					RegisterForContextMenu (pl);
					pl.ItemClick += (sender, e) => selectdietinstance (plan [e.Position]);
				}
			}
		}

		#region IView implementation
		public event Action addburnitemquick = delegate{};
		public event Action addburnitem = delegate{};
		public event Action<EntryLineVM> removeburnitem = delegate{};
		public event Action addeatitemquick = delegate{};
		public event Action addeatitem = delegate{};
		public event Action<EntryLineVM> removeeatitem = delegate{};
		public event Action adddietinstance = delegate { };
		public event Action<DietInstanceVM> selectdietinstance = delegate { };
		public event Action<DietInstanceVM> removedietinstance = delegate { };
		public event Action<DateTime> changeday = delegate{};
		public event Action<EntryLineVM> editeatitem = delegate { };
		public event Action<EntryLineVM> editburnitem = delegate { };
		public event Action<DietInstanceVM> editdietinstance = delegate { };

		private DateTime _day;
		public DateTime day 
		{ 
			set
			{ 
				_day = value;
				// FIXME code to set day on view visible somewhere
			}
			get{ return _day; }
		}
		private DietInstanceVM _diet;
		public DietInstanceVM currentDiet
		{
			set
			{
				_diet = value;
				SwitchHiglightDietInstance();
			}
			get { return _diet; }
		}

		class SetRet {
			public LAdapter apt;
			public String name;
		}

		SetRet eatitems, burnitems;
		public void SetEatLines (IEnumerable<EntryLineVM> lineitems)
		{
			eatitems = SetLines (lineitems, Resource.Layout.EatEntryLine, ItemViewConfigs.Eat);
			if(ActionBar.SelectedNavigationIndex==0) ReloadLayoutForTab ();
		}
		String eatTrackText;
		public void SetEatTrack(IEnumerable<TrackingInfoVM> trackinfo)
		{
			List<String> elns = new List<string> ();
			foreach(var ti in trackinfo)
			{
				double? inVal = 0.0;
				if (ti.eatValues == null) inVal = null;
				else foreach (var te in ti.eatValues)
					inVal += te.value;

				double? outVal = 0.0;
				if (ti.burnValues == null) outVal = null;
				else foreach (var te in ti.burnValues)
					outVal += te.value;

				if (!inVal.HasValue)
					elns.Add (ti.valueName + " : " + ti.targetValue);
				else if (!outVal.HasValue)
					elns.Add (ti.valueName + " : " + inVal + "/" + ti.targetValue);
				else 
					elns.Add(String.Format("{0:0} eaten - {1:0} burned = {2:0} of {3:0} {4}",
						inVal, outVal, inVal-outVal, ti.targetValue, ti.valueName));
			}
			eatTrackText = String.Join ("\n", elns);
			if(ActionBar.SelectedNavigationIndex==0) ReloadLayoutForTab ();
		}
		public void SetBurnLines (IEnumerable<EntryLineVM> lineitems)
		{
			burnitems = SetLines (lineitems, Resource.Layout.BurnEntryLine, ItemViewConfigs.Burn);
			if(ActionBar.SelectedNavigationIndex==1) ReloadLayoutForTab ();
		}
		String burnTrackText;
		public void SetBurnTrack(IEnumerable<TrackingInfoVM> trackinfo)
		{
			List<String> elns = new List<string> ();
			foreach(var ti in trackinfo)
			{
				double? inVal = 0.0;
				if (ti.eatValues == null) inVal = null;
				else foreach (var te in ti.eatValues)
					inVal += te.value;

				double? outVal = 0.0;
				if (ti.burnValues == null) outVal = null;
				else foreach (var te in ti.burnValues)
					outVal += te.value;

				if (!outVal.HasValue)
					elns.Add (ti.valueName + " : " + ti.targetValue);
				else if (!inVal.HasValue)
					elns.Add (ti.valueName + " : " + outVal + "/" + ti.targetValue);
				else 
					elns.Add(String.Format("{0:0} eaten - {1:0} burned = {2:0} of {3:0} {4}",
						inVal, outVal, inVal-outVal, ti.targetValue, ti.valueName));
			}
			burnTrackText = String.Join ("\n", elns);
			if(ActionBar.SelectedNavigationIndex==1) ReloadLayoutForTab ();
		}
		void SwitchHiglightDietInstance()
		{
			if (ActionBar.SelectedNavigationIndex == 2) {
				for (int i = 0; i < plan.Count; i++)
					if (Object.ReferenceEquals (plan [i], _diet)) {
						var pl = FindViewById<ListView> (Resource.Id.planlist);
						pl.SetItemChecked (i, true);
					}
			}
		}
		DAdapter plan;
		public void SetInstances (IEnumerable<DietInstanceVM> instanceitems)
		{
			var itms = new List<DietInstanceVM> (instanceitems);
			plan = new DAdapter (this, itms);
			if(ActionBar.SelectedNavigationIndex==2)ReloadLayoutForTab ();
		}
		delegate void MyViewConfiguror<VMType>(View view, VMType vm, String use);
		SetRet SetLines (IEnumerable<EntryLineVM> lineitems, int vid, MyViewConfiguror<EntryLineVM> cfg)
		{
			var ll = new List<EntryLineVM> (lineitems);
			Dictionary<string,int> test = new Dictionary<string, int> ();
			foreach (var l in ll)
			{
				foreach (var kv in l.displayAmounts)
					if (!test.ContainsKey (kv.Key))
						test [kv.Key] = 1;
					else
						test [kv.Key]++;
			}
			String use=null; int num=0;
			foreach (var tkv in test)
				if (tkv.Value > num) {
					num = tkv.Value;
					use = tkv.Key;
				}
			return new SetRet () { apt = new LAdapter (this, ll, vid, (v,vm) => cfg(v,vm,use) ), name = use };
		}

		#endregion


		#region IUserInput implimentation
		Promise<int> SelectStringPromise;
		IReadOnlyList<String> strings = null;
		public void SelectString (String title, IReadOnlyList<String> strings, int initial, Promise<int> completed)
		{
			int ani = ActionBar.SelectedNavigationIndex;
			int useView = ani == 0 ? Resource.Id.eatpage : ani == 1 ? Resource.Id.burnpage : Resource.Id.plan;
			this.strings = strings;
			SelectStringPromise = completed;
			RegisterForContextMenu (FindViewById<View> (useView));
			OpenContextMenu (FindViewById<View>(useView));
			UnregisterForContextMenu (FindViewById<View> (useView));
		}
		#endregion

		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			switch (item.ItemId) {
			case Resource.Id.addBurned:
				addburnitem ();
				break;
			case Resource.Id.addBurnedQuick:
				addburnitemquick ();
				break;
			case Resource.Id.addEatenQuick:
				addeatitemquick ();
				break;
			case Resource.Id.addEaten:
				addeatitem ();
				break;
			case Resource.Id.addPlan:
				adddietinstance ();
				break;
			default:
				break;
			}
			return base.OnOptionsItemSelected (item);
		}
		public override bool OnPrepareOptionsMenu (IMenu menu)
		{
			if (ActionBar.SelectedNavigationIndex > -1) {
				menu.Clear ();
				MenuInflater.Inflate (tabs [ActionBar.SelectedNavigationIndex].menu, menu);
			}
			return base.OnPrepareOptionsMenu (menu);
		}
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Window.RequestFeature (WindowFeatures.ActionBar);

			// init with nothing
			SetEatLines (new EntryLineVM[0]);
			SetBurnLines (new EntryLineVM[0]);
			SetEatTrack (new TrackingInfoVM[0]);
			SetBurnTrack (new TrackingInfoVM[0]);
			
			foreach (var tab in tabs) {
				ActionBar.Tab t = ActionBar.NewTab ();
				t.SetText (tab.name);
				t.SetTabListener (this);
				ActionBar.AddTab (t);
			}

			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
			Presenter.Singleton.PresentTo (this, this, new AndroidRequestBuilder(this));
		}
		ListView slv;
		Action<IMenuItem> selectAction;
		public override void OnCreateContextMenu (IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
		{
			base.OnCreateContextMenu (menu, v, menuInfo);
			if (v.Id == Resource.Id.plan || v.Id == Resource.Id.eatpage || v.Id == Resource.Id.burnpage) {
				selectAction = SelectStringContextSelected;
				menu.Clear ();
				for (int i = 0; i < strings.Count; i++)
					menu.Add (0, i, i, strings [i]);
			} else {
				MenuInflater.Inflate (useContext, menu);
				selectAction = ListContextSelected;
				slv = v as ListView;
			}
		}
		public override bool OnContextItemSelected (IMenuItem item)
		{
			selectAction (item);
			return base.OnContextItemSelected (item);
		}
		public void SelectStringContextSelected(IMenuItem item)
		{
			SelectStringPromise (item.Order);
		}
		public void ListContextSelected(IMenuItem item)
		{
			EntryLineVM evm = null;
			DietInstanceVM dvm = null;
			var pos = (item.MenuInfo as AdapterView.AdapterContextMenuInfo).Position;
			if(slv.Adapter is BaseAdapter<EntryLineVM>)
				evm = (slv.Adapter as BaseAdapter<EntryLineVM>) [pos];
			else if(slv.Adapter is BaseAdapter<DietInstanceVM>)
				dvm = (slv.Adapter as BaseAdapter<DietInstanceVM>) [pos];
			switch (item.ItemId) {
			case Resource.Id.removeEatEntry:
				removeeatitem (evm);
				break;
			case Resource.Id.removeBurnEntry:
				removeburnitem (evm);
				break;
			case Resource.Id.removePlanEntry:
				removedietinstance (dvm);
				break;
			case Resource.Id.editPlanEntry:
				editdietinstance (dvm);
				break;
			}
		}

	}
}


