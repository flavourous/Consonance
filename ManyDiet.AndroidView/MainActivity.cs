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
	public class MainActivity : Activity, ActionBar.ITabListener, IView, IAddItemView, ISelectInfoView
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

			ReloadItemsAdapterForTab ();

			InvalidateOptionsMenu ();
		}

		int useContext;
		void ReloadItemsAdapterForTab() {
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
		}

		#region IView implementation
		public event Action addburnitemquick = delegate{};
		public event Action addburnitem = delegate{};
		public event Action<EntryLineVM> removeburnitem = delegate{};
		public event Action addeatitemquick = delegate{};
		public event Action addeatitem = delegate{};
		public event Action<EntryLineVM> removeeatitem = delegate{};
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

		class SetRet {
			public LAdapter apt;
			public String name;
		}

		SetRet eatitems, burnitems;
		public void SetEatLines (IEnumerable<EntryLineVM> lineitems)
		{
			eatitems = SetLines (lineitems);
			ReloadItemsAdapterForTab ();
		}
		public void SetBurnLines (IEnumerable<EntryLineVM> lineitems)
		{
			burnitems = SetLines (lineitems);
			ReloadItemsAdapterForTab ();
		}
		SetRet SetLines (IEnumerable<EntryLineVM> lineitems)
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
			return new SetRet () { apt = new LAdapter (this, ll, use), name = use };
		}

		public IAddItemView additemview {get{ return this; }}
		public ISelectInfoView selectinfoview { get { return this; } }
		#endregion

		#region IAddItemView implementation

		public AddedItemVM GetValues (String title, IEnumerable<string> names, AddedItemVMDefaults defaultUse = AddedItemVMDefaults.Name | AddedItemVMDefaults.When)
		{
			List<double> vs = new List<double>();
			foreach (var s in names) vs.Add (42.0);
			return new AddedItemVM (vs.ToArray(), DateTime.Now, "Namey 42 omg");
		}

		#endregion

		#region ISelectFoodView implementation
		public FoodInfo SelectFood (IEnumerable<FoodInfo> foods)
		{
			throw new NotImplementedException ();
		}
		public FireInfo SelectFire (IEnumerable<FireInfo> fires)
		{
			throw new NotImplementedException ();
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


