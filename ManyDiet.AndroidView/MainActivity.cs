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
		public void Add(String name, int layout, int menu, int contextmenu)
		{
			this.Add(new TabDesc() { name=name, layout=layout, menu=menu, contextmenu=contextmenu });
		}
	}
	class TabDesc { public String name; public int layout; public int menu; public int contextmenu; }

	[Activity (Label = "ManyDiet", MainLauncher=true, Icon = "@drawable/icon")]
	public class MainActivity : Activity, ActionBar.ITabListener, IView
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
			}
			if (ActionBar.SelectedNavigationIndex == 1) {
				FindViewById<ListView> (Resource.Id.burnlist).Adapter = burnitems.apt;
				FindViewById<TextView> (Resource.Id.burnlisttitletrack).Text = burnitems.name;
				if(tabchanged) RegisterForContextMenu (FindViewById<ListView> (Resource.Id.burnlist));
				useContext = Resource.Menu.BurnEntryMenu;
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
		public event Action adddietinstance;
		public event Action<DietInstanceVM> selectdietinstance;
		public event Action<DietInstanceVM> removedietinstance;
		public event Action<DateTime> changeday = delegate{};
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
		public void SetEatTrack(IEnumerable<TrackingInfo> trackinfo)
		{
		}
		public void SetBurnLines (IEnumerable<EntryLineVM> lineitems)
		{
			burnitems = SetLines (lineitems, Resource.Layout.BurnEntryLine, ItemViewConfigs.Burn);
			if(ActionBar.SelectedNavigationIndex==1) ReloadLayoutForTab ();
		}
		public void SetBurnTrack(IEnumerable<TrackingInfo> trackinfo)
		{
		}
		void SwitchHiglightDietInstance()
		{
			for (int i = 0; i < plan.Count; i++)
				if (Object.ReferenceEquals (plan [i], _diet)) {
					var pl = FindViewById<ListView> (Resource.Id.planlist);
					pl.SetItemChecked(i,true);
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
			
		Promise<AddedItemVM> GetValuesPromise;
		public void GetValues (String title, IEnumerable<String> names, Promise<AddedItemVM> completed, AddedItemVMDefaults defaultUse = AddedItemVMDefaults.Name | AddedItemVMDefaults.When)
		{
			// init dialog
			Dialog gvDialog = new Dialog (this);
			gvDialog.RequestWindowFeature ((int)WindowFeatures.NoTitle);
			gvDialog.SetCanceledOnTouchOutside (true);
			gvDialog.SetContentView (Resource.Layout.GetValues);

			// get elements
			var ok = gvDialog.FindViewById<Button> (Resource.Id.getvalues_ok);
			var cancel = gvDialog.FindViewById<Button> (Resource.Id.getvalues_cancel);
			var sll = gvDialog.FindViewById<LinearLayout> (Resource.Id.getvalues_scrolllinearlayout);
			var gvtitle = gvDialog.FindViewById<TextView> (Resource.Id.getvalues_titletext);

			// options
			bool useName = (defaultUse & AddedItemVMDefaults.Name) == AddedItemVMDefaults.Name;
			bool useWhen = (defaultUse & AddedItemVMDefaults.When) == AddedItemVMDefaults.When;
			if (!useName)
				gvDialog.FindViewById<LinearLayout> (Resource.Id.getvalues_titlebox).Visibility = ViewStates.Gone;
			if (!useWhen) gvDialog.FindViewById<LinearLayout> (Resource.Id.getvalues_date).Visibility = ViewStates.Gone;

			// set element values
			gvtitle.Text = title;
			List<EditText> vals = new List<EditText> ();
			foreach (var vv in names) {
				var row = gvDialog.LayoutInflater.Inflate (Resource.Layout.GetValueRow, sll);
				vals.Add (row.FindViewById<EditText> (Resource.Id.getvalues_itemvalue));
				row.FindViewById<TextView> (Resource.Id.getvalues_itemtitle).Text = vv;
			}

			// do wrapping layout
			gvDialog.Window.SetLayout (ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

			// hook events
			ok.Click += (sender, e) => {
				List<double> valvals = new List<double>();
				foreach(var v in vals)
					valvals.Add(double.Parse(v.Text == "" ? "0.0" : v.Text));
				gvDialog.Cancel();
				completed(new AddedItemVM(valvals.ToArray(),
					gvDialog.FindViewById<DatePicker> (Resource.Id.getvalues_datepicker).DateTime,
					gvDialog.FindViewById<EditText> (Resource.Id.getvalues_titleboxname).Text));
			};

			cancel.Click += (sender, e) => {
				gvDialog.Cancel(); // just dont complete the promise! ok they are good.
			};

			gvDialog.Show ();
		}

		Promise<int> SelectInfoPromise;
		public void SelectInfo (String title, IReadOnlyList<SelectableItemVM> items, Promise<int> completed)
		{
			List<String> sings = new List<string> ();
			foreach (var si in items)
				sings.Add (si.name);
			SelectString (title, sings, completed);
		}
		Promise<int> SelectStringPromise;
		IReadOnlyList<String> strings = null;
		public void SelectString (String title, IReadOnlyList<String> strings, Promise<int> completed)
		{
			this.strings = strings;
			SelectStringPromise = completed;
			RegisterForContextMenu (FindViewById<RelativeLayout> (Resource.Id.plan));
			OpenContextMenu (FindViewById<RelativeLayout>(Resource.Id.plan));
			UnregisterForContextMenu (FindViewById<RelativeLayout> (Resource.Id.plan));
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
			SetEatTrack (new TrackingInfo[0]);
			SetBurnTrack (new TrackingInfo[0]);
			
			foreach (var tab in tabs) {
				ActionBar.Tab t = ActionBar.NewTab ();
				t.SetText (tab.name);
				t.SetTabListener (this);
				ActionBar.AddTab (t);
			}

			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
			Presenter.Singleton.PresentTo (this);
		}
		ListView slv;
		Action<IMenuItem> selectAction;
		public override void OnCreateContextMenu (IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
		{
			base.OnCreateContextMenu (menu, v, menuInfo);
			if (v.Id == Resource.Id.plan) {
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
			}
		}

	}
}


