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
		public void Add(String name, int layout, int menu, int contextmenu, MenuOverrideList mol)
		{
			this.Add(new TabDesc() { name=name, layout=layout, menu=menu, contextmenu=contextmenu, menuOverrides = mol });
		}
	}
	class TabDesc { public String name; public int layout; public int menu; public int contextmenu; public List<MenuOverride> menuOverrides; }
	class MenuOverrideList : List<MenuOverride> {
		public void Add(int index, String name)
		{
			this.Add (new MenuOverride () { index = index, name = name });
		}
	}
	class MenuOverride { public int index; public String name; }


	class BoundCommands : IPlanCommands<ValueRequestWrapper>
	{
		#region IPlanCommands implementation
		public readonly BoundRequestCaller<EntryLineVM> _eat = new BoundRequestCaller<EntryLineVM>();
		public ICollectionEditorBoundCommands<EntryLineVM, ValueRequestWrapper> eat { get { return _eat; } }

		public readonly BoundRequestCaller<InfoLineVM> _eatinfo = new BoundRequestCaller<InfoLineVM>();
		public ICollectionEditorBoundCommands<InfoLineVM, ValueRequestWrapper> eatinfo { get { return _eatinfo; } }

		public readonly BoundRequestCaller<EntryLineVM> _burn = new BoundRequestCaller<EntryLineVM>();
		public ICollectionEditorBoundCommands<EntryLineVM, ValueRequestWrapper> burn { get { return _burn; } }

		public readonly BoundRequestCaller<InfoLineVM> _burninfo = new BoundRequestCaller<InfoLineVM>();
		public ICollectionEditorBoundCommands<InfoLineVM, ValueRequestWrapper> burninfo { get { return _burninfo; } }
		#endregion
	}

	class LooseRequestCaller<T> : ICollectionEditorLooseCommands<T>
	{
		#region ICollectionEditorWeakCommands implementation
		public event Action add = delegate { };
		public event Action<T> remove= delegate { };
		public event Action<T> edit= delegate { };
		public event Action<T> select= delegate { };
		#endregion
		public void OnAdd() { add (); }
		public void OnRemove(T t) { remove (t); }
		public void OnEdit(T t) { edit (t); }
		public void OnSelect(T t) { select (t); }
	}
	public class BoundRequestCaller<T> : ICollectionEditorBoundCommands<T, ValueRequestWrapper>
	{
		#region ICollectionEditorBoundCommands implementation
		public event Action<IValueRequestBuilder<ValueRequestWrapper>> add;
		public event Action<T> remove;
		public event Action<T, IValueRequestBuilder<ValueRequestWrapper>> edit;
		#endregion
		public void OnAdd(IValueRequestBuilder<ValueRequestWrapper> builder) { add (builder); }
		public void OnRemove(T t) { remove (t); }
		public void OnEdit(T t,IValueRequestBuilder<ValueRequestWrapper> builder) { edit (t, builder); }
	}

	[Activity (Label = "Consonance", MainLauncher=true, Icon = "@drawable/icon")]
	public class MainActivity : Activity, ActionBar.ITabListener, IView, IUserInput
	{
		TrackerPlanChooser tpc;
		public void ChoosePlan (string title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial, Promise<int> completed)
		{
			if (tpc == null)
				tpc = new TrackerPlanChooser (this);
			tpc.Show ();
			tpc.ChoosePlan (title, choose_from, initial, completed);
		}

		TrackerTrackedDialog ttd;
		BoundCommands planCommands = new BoundCommands ();
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// stuff
			ttd = new TrackerTrackedDialog(this);
			defaultBuilder = new AndroidRequestBuilder (new DialogRequestBuilder (this), this);
			eatTracker = new TrackerTrackView (this);
			burnTracker = new TrackerTrackView (this);

			Window.RequestFeature (WindowFeatures.ActionBar);
			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;

			// init with nothing
			SetEatLines (new EntryLineVM[0]);
			SetBurnLines (new EntryLineVM[0]);
			SetEatTrack (null, new TrackerTracksVM[0]);
			SetBurnTrack (null, new TrackerTracksVM[0]);

			foreach (var tab in tabs) {
				ActionBar.Tab t = ActionBar.NewTab ();
				t.SetText (tab.name);
				t.SetTabListener (this);
				ActionBar.AddTab (t);
			}

			Presenter.Singleton.PresentTo (this, this, planCommands, defaultBuilder);
		}

		TrackerTrackView eatTracker, burnTracker;
		AndroidRequestBuilder defaultBuilder;

		public void SetEatTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { eatTracker.SetTrack (current, others); }
		public void SetBurnTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { burnTracker.SetTrack (current, others); }

		List<TabDesc> tabs = new TabDescList
		{
			{ "In", Resource.Layout.Eat, Resource.Menu.EatMenu, Resource.Menu.EatEntryMenu, new MenuOverrideList { { 1, "In" } } },
			{ "Out", Resource.Layout.Burn, Resource.Menu.BurnMenu, Resource.Menu.BurnEntryMenu, new MenuOverrideList { { 1, "Out" } } },
			{ "Plan", Resource.Layout.Plan, Resource.Menu.PlanMenu, Resource.Menu.PlanEntryMenu, new MenuOverrideList() },
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
				if (tabchanged) {
					RegisterForContextMenu (FindViewById<ListView> (Resource.Id.eatlist));
					DroidUtils.PushView (eatTracker, FindViewById<FrameLayout> (Resource.Id.eatTrackContainer));
				}
				useContext = Resource.Menu.EatEntryMenu;
			}
			if (ActionBar.SelectedNavigationIndex == 1) {
				FindViewById<ListView> (Resource.Id.burnlist).Adapter = burnitems.apt;
				FindViewById<TextView> (Resource.Id.burnlisttitletrack).Text = burnitems.name;
				if (tabchanged) {
					RegisterForContextMenu (FindViewById<ListView> (Resource.Id.burnlist));
					DroidUtils.PushView (burnTracker, FindViewById<FrameLayout> (Resource.Id.burnTrackContainer));
				}
				useContext = Resource.Menu.BurnEntryMenu;
			}
			if (ActionBar.SelectedNavigationIndex == 2) {
				var pl = FindViewById<ListView> (Resource.Id.planlist);
				pl.Adapter = planAdapter;
				useContext = Resource.Menu.PlanEntryMenu;
				SwitchHiglightDietInstance();
				if (tabchanged) {
					RegisterForContextMenu (pl);
					pl.ItemClick += (sender, e) => {
						var itm = planAdapter [e.Position];
						_plan.OnSelect (itm);
					};
				}
			}
		}
			

		#region IView implementation

		public event Action<DateTime> changeday = delegate { };
		readonly LooseRequestCaller<TrackerInstanceVM> _plan = new LooseRequestCaller<TrackerInstanceVM>();
		public ICollectionEditorLooseCommands<TrackerInstanceVM> plan { get { return _plan; } }
		public event Action<InfoManageType> manageInfo = delegate { };
		public void ManageInfos (InfoManageType mt, ChangeTriggerList<InfoLineVM> toManage, IFindList<InfoLineVM> finder, Action finished)
		{
			// launch that manage activity...and erm pass it some hooks?? 
			var icom = (mt == InfoManageType.In ? planCommands.eatinfo : planCommands.burninfo) as BoundRequestCaller<InfoLineVM>;
			var ipl = mt == InfoManageType.In ? currentDiet.dialect.InputInfoPlural : currentDiet.dialect.OutputInfoPlural;
			ManageInfoActivity.SetSharedObjects(ipl, finished, toManage, finder, icom);
			StartActivity (new Intent(this, typeof(ManageInfoActivity)));
		}

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
		private TrackerInstanceVM _diet;
		public TrackerInstanceVM currentDiet
		{
			set
			{
				_diet = value;
				SwitchHiglightDietInstance();
				ChangeNames ();
			}
			get { return _diet; }
		}

		class SetRet {
			public LAdapter<EntryLineVM> apt;
			public String name;
		}

		SetRet eatitems, burnitems;
		public void SetEatLines (IEnumerable<EntryLineVM> lineitems)
		{
			eatitems = SetLines (lineitems, Resource.Layout.EatEntryLine, ItemViewConfigs.Eat);
			if(ActionBar.SelectedNavigationIndex==0) ReloadLayoutForTab ();
		}
		public void SetBurnLines (IEnumerable<EntryLineVM> lineitems)
		{
			burnitems = SetLines (lineitems, Resource.Layout.BurnEntryLine, ItemViewConfigs.Burn);
			if(ActionBar.SelectedNavigationIndex==1) ReloadLayoutForTab ();
		}
		void ChangeNames()
		{
			ActionBar.GetTabAt (0).SetText (currentDiet.dialect.InputEntryVerb);
			ActionBar.GetTabAt (1).SetText(currentDiet.dialect.OutputEntrytVerb);
			tabs[0].menuOverrides[0].name = currentDiet.dialect.InputInfoPlural;
			tabs[1].menuOverrides[0].name = currentDiet.dialect.OutputInfoPlural;
			InvalidateOptionsMenu ();
		}
		void SwitchHiglightDietInstance()
		{
			if (ActionBar.SelectedNavigationIndex == 2) {
				for (int i = 0; i < planAdapter.Count; i++)
					if (OriginatorVM.OriginatorEquals(planAdapter [i], _diet)) {
						var pl = FindViewById<ListView> (Resource.Id.planlist);
						pl.SetItemChecked (i, true);
					}
			}
		}
		DAdapter planAdapter;
		public void SetInstances (IEnumerable<TrackerInstanceVM> instanceitems)
		{
			var itms = new List<TrackerInstanceVM> (instanceitems);
			planAdapter = new DAdapter (this, itms);
			if(ActionBar.SelectedNavigationIndex==2) ReloadLayoutForTab ();
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
			return new SetRet () { apt = new LAdapter<EntryLineVM> (this.LayoutInflater, ll, vid, (v,vm) => cfg(v,vm,use) ), name = use };
		}

		#endregion


		#region IUserInput implimentation

		public void WarnConfirm (string action, Promise confirmed)
		{
			new AlertDialog.Builder (this)
				.SetNegativeButton("Cancel", (o,a) => {})
				.SetPositiveButton("OK", (o,a) => confirmed ())
				.SetTitle ("Warning")
				.SetMessage (action)
				.Create().Show ();
		}
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
				planCommands._burn.OnAdd (defaultBuilder);
				break;
			case Resource.Id.addEaten:
				planCommands._eat.OnAdd (defaultBuilder);
				break;
			case Resource.Id.addPlan:
				_plan.OnAdd ();
				break;
			case Resource.Id.managePlans:
				ttd.Show (this, new List<TrackerInstanceVM> (planAdapter), currentDiet, ()=>planAdapter.NotifyDataSetChanged());
				break;
			case Resource.Id.manageFoods:
				manageInfo (InfoManageType.In);
				break;
			case Resource.Id.manageBurns:
				manageInfo (InfoManageType.Out);
				break;
			default:
				break;
			}
			return base.OnOptionsItemSelected (item);
		}
		public override bool OnPrepareOptionsMenu (IMenu menu)
		{
			if (ActionBar.SelectedNavigationIndex > -1) {
				var st = tabs [ActionBar.SelectedNavigationIndex];
				menu.Clear ();
				MenuInflater.Inflate (st.menu, menu);
				foreach(var ovrd in st.menuOverrides)
					menu.GetItem (ovrd.index).SetTitle (ovrd.name);
			}
			return base.OnPrepareOptionsMenu (menu);
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
			TrackerInstanceVM dvm = null;
			var pos = (item.MenuInfo as AdapterView.AdapterContextMenuInfo).Position;
			if(slv.Adapter is BaseAdapter<EntryLineVM>)
				evm = (slv.Adapter as BaseAdapter<EntryLineVM>) [pos];
			else if(slv.Adapter is BaseAdapter<TrackerInstanceVM>)
				dvm = (slv.Adapter as BaseAdapter<TrackerInstanceVM>) [pos];
			switch (item.ItemId) {
			case Resource.Id.removeEatEntry:
				planCommands._eat.OnRemove (evm);
				break;
			case Resource.Id.removeBurnEntry:
				planCommands._burn.OnRemove (evm);
				break;
			case Resource.Id.removePlanEntry:
				_plan.OnRemove (dvm);
				break;
			case Resource.Id.editPlanEntry:
				_plan.OnEdit (dvm);
				break;
			case Resource.Id.editEatEntry:
				planCommands._eat.OnEdit (evm, defaultBuilder);
				break;
			case Resource.Id.editBurnEntry:
				planCommands._burn.OnEdit (evm, defaultBuilder);
				break;
			}
		}

	}
}


