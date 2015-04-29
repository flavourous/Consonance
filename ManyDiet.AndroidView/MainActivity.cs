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
	public class MainActivity : Activity, ActionBar.ITabListener, IView, IAddItemView, ISelectFoodView
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

			// FIXME why break pattern :/
			if(ActionBar.SelectedNavigationIndex == 0)
				FindViewById<ListView> (Resource.Id.eatlist).Adapter = items;

			InvalidateOptionsMenu ();
		}

		#region IView implementation

		public event Action additemquick = delegate{};
		public event Action additem = delegate{};
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

		LAdapter items;
		public void SetEatLines (IEnumerable<EntryLineVM> lineitems)
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
			items = new LAdapter (this, ll, use);
			FindViewById<ListView> (Resource.Id.eatlist).Adapter = items;
			FindViewById<TextView> (Resource.Id.eatlisttitletrack).Text = use;
		}

		public IAddItemView additemview {get{ return this; }}
		public ISelectFoodView selectfoodview { get { return this; } }
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

		#endregion

		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			switch (item.ItemId) {
			case Resource.Id.addEaten:
				additemquick ();
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
			// Set our view from the "main" layout resource

			Window.RequestFeature (WindowFeatures.ActionBar);
			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;

			foreach (var tab in tabs) {
				ActionBar.Tab t = ActionBar.NewTab ();
				t.SetText (tab.name);
				t.SetTabListener (this);
				ActionBar.AddTab (t);
			}

			Presenter.Singleton.PresentTo (this);
		}


	}
//	class Fragmentor : Fragment
//	{
//		int layout;
//		public Fragmentor(int id)
//		{
//			layout=id;
//		}
//		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
//		{
//			base.OnCreateView (inflater, container, savedInstanceState);
//			return inflater.Inflate (layout, container, false);
//		}
//	}
}


