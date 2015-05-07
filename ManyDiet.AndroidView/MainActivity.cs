using System;
using System.Collections;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace ManyDiet.AndroidView
{
	class TabDescList : List<TabDesc>
	{
		public void Add(String name, int layout, int menu)
		{
			this.Add(new TabDesc() { name=name, layout=layout, menu=menu });
		}
	}
	class TabDesc { public String name; public int layout; public int menu; }

	[Activity (Label = "ManyDiet", MainLauncher=true, Icon = "@drawable/icon")]
	public class MainActivity : Activity, ActionBar.ITabListener, IView
	{
		TabDescList tabs = new TabDescList () {
			{ "Eat", Resource.Layout.Eat, Resource.Menu.EatMenu },
			{ "Burn", Resource.Layout.Burn, Resource.Menu.BurnMenu },
			{ "Plan", Resource.Layout.Plan, Resource.Menu.PlanMenu },
		};
		public void OnTabReselected (ActionBar.Tab tab, FragmentTransaction ft) { }
		public void OnTabUnselected (ActionBar.Tab tab, FragmentTransaction ft) { }
		public void OnTabSelected (ActionBar.Tab tab, FragmentTransaction ft)
		{
			SetContentView (tabs [ActionBar.SelectedNavigationIndex].layout);

			ReloadLayoutForTab ();

			InvalidateOptionsMenu ();
		}

		int useContext;
		void ReloadLayoutForTab() {
			// FIXME why break pattern :/
			if (ActionBar.SelectedNavigationIndex == 0) {
				FindViewById<ListView> (Resource.Id.eatlist).Adapter = eatitems.apt;
				FindViewById<TextView> (Resource.Id.eatlisttitletrack).Text = eatitems.name;
				RegisterForContextMenu (FindViewById<ListView> (Resource.Id.eatlist));
				useContext = Resource.Menu.EatEntryMenu;
			}
			if (ActionBar.SelectedNavigationIndex == 1) {
				FindViewById<ListView> (Resource.Id.burnlist).Adapter = burnitems.apt;
				FindViewById<TextView> (Resource.Id.burnlisttitletrack).Text = burnitems.name;
				RegisterForContextMenu (FindViewById<ListView> (Resource.Id.burnlist));
				useContext = Resource.Menu.BurnEntryMenu;
			}
			if (ActionBar.SelectedNavigationIndex == 2) {
				var pl = FindViewById<ListView> (Resource.Id.planlist);
				pl.Adapter = plan;
				RegisterForContextMenu (pl);
				useContext = Resource.Menu.PlanEntryMenu;
				HighlightCurrentDietInstance ();
				pl.ItemSelected += (sender, e) => selectdietinstance (plan [pl.SelectedItemPosition]);
			}
		}

		#region IView implementation
		public event Action addburnitemquick = delegate{};
		public event Action addburnitem = delegate{};
		public event Action<EntryLineVM> removeburnitem = delegate{};
		public event Action addeatitemquick = delegate{};
		public event Action addeatitem = delegate{};
		public event Action<EntryLineVM> removeeatitem = delegate{};
		public event Action adddietinstance;
		public event Action<DietInstanceVM> selectdietinstance;
		public event Action<DateTime> changeday = delegate{};
		private DateTime _day;
		public DateTime day 
		{ 
			set
			{ 
				_day = value;
				// FIXME code to set day on view visible somewhere
			}
		}
		private DietInstanceVM _diet;
		public DietInstanceVM currentDiet
		{
			set
			{
				_diet = value;
			}
		}

		class SetRet {
			public LAdapter apt;
			public String name;
		}

		SetRet eatitems, burnitems;
		public void SetEatLines (IEnumerable<EntryLineVM> lineitems)
		{
			eatitems = SetLines (lineitems, Resource.Layout.EatEntryLine, ItemViewConfigs.Eat);
			ReloadLayoutForTab ();
		}
		public void SetBurnLines (IEnumerable<EntryLineVM> lineitems)
		{
			burnitems = SetLines (lineitems, Resource.Layout.BurnEntryLine, ItemViewConfigs.Burn);
			ReloadLayoutForTab ();
		}
		void HighlightCurrentDietInstance()
		{
			for (int i = 0; i < plan.Count; i++)
				if(Object.ReferenceEquals(plan[i], _diet))
					FindViewById<ListView>(Resource.Id.planlist).SetSelection(i);
		}
		DAdapter plan;
		public void SetInstances (IEnumerable<DietInstanceVM> instanceitems)
		{
			var itms = new List<DietInstanceVM> (instanceitems);
			plan = new DAdapter (this, itms);
			ReloadLayoutForTab ();
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
			
		public AddedItemVM GetValues (String title, IEnumerable<string> names, AddedItemVMDefaults defaultUse = AddedItemVMDefaults.Name | AddedItemVMDefaults.When)
		{
			List<double> vs = new List<double>();
			foreach (var s in names) vs.Add (42.0);
			return new AddedItemVM (vs.ToArray(), DateTime.Now, "Namey 42 omg");
		}

		public int SelectInfo (IReadOnlyList<SelectableItemVM> foods)
		{
			return 0;
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
			menu.Clear ();
			MenuInflater.Inflate (tabs [ActionBar.SelectedNavigationIndex].menu, menu);
			return base.OnPrepareOptionsMenu (menu);
		}
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Window.RequestFeature (WindowFeatures.ActionBar);
			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;

			// init with nothing
			SetEatLines (new EntryLineVM[0]);
			SetBurnLines (new EntryLineVM[0]);
			
			foreach (var tab in tabs) {
				ActionBar.Tab t = ActionBar.NewTab ();
				t.SetText (tab.name);
				t.SetTabListener (this);
				ActionBar.AddTab (t);
			}

			Presenter.Singleton.PresentTo (this);
		}
		ListView slv;
		public override void OnCreateContextMenu (IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
		{
			base.OnCreateContextMenu (menu, v, menuInfo);
			MenuInflater.Inflate (useContext, menu);
			slv = v as ListView;
		}
		public override bool OnContextItemSelected (IMenuItem item)
		{
			var pos = (item.MenuInfo as AdapterView.AdapterContextMenuInfo).Position;
			var vm = (slv.Adapter as BaseAdapter<EntryLineVM>)[pos];
			switch (item.ItemId) {
			case Resource.Id.removeEatEntry:
				removeeatitem (vm);
				break;
			case Resource.Id.removeBurnEntry:
				removeburnitem (vm);
				break;
			}
			return base.OnContextItemSelected (item);
		}

	}
}


