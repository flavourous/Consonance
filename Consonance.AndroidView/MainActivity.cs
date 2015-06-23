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
		readonly AndroidRequestBuilder defaultBuilder;
		public MainActivity()
		{
			defaultBuilder = new AndroidRequestBuilder (this);
		}

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
				pl.Adapter = planAdapter;
				useContext = Resource.Menu.PlanEntryMenu;
				SwitchHiglightDietInstance();
				if (tabchanged) {
					RegisterForContextMenu (pl);
					pl.ItemClick += (sender, e) => _plan.OnSelect (planAdapter [e.Position]);
				}
			}
		}

		#region IView implementation

		public event Action<DateTime> changeday = delegate { };
		readonly LooseRequestCaller<DietInstanceVM> _plan = new LooseRequestCaller<DietInstanceVM>();
		public ICollectionEditorLooseCommands<DietInstanceVM> plan { get { return _plan; } }
		public event Action<InfoManageType> manageInfo = delegate { };
		public void ManageInfos (InfoManageType mt, ChangeTriggerList<InfoLineVM> toManage, Action finished)
		{
			// launch that manage activity...and erm pass it some hooks?? 
			var icom = (mt == InfoManageType.Eat ? planCommands.eatinfo : planCommands.burninfo) as BoundRequestCaller<InfoLineVM>;
			ManageInfoActivity.SetSharedObjects(finished, toManage, icom);
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
			public LAdapter<EntryLineVM> apt;
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
				for (int i = 0; i < planAdapter.Count; i++)
					if (Object.ReferenceEquals (planAdapter [i], _diet)) {
						var pl = FindViewById<ListView> (Resource.Id.planlist);
						pl.SetItemChecked (i, true);
					}
			}
		}
		DAdapter planAdapter;
		public void SetInstances (IEnumerable<DietInstanceVM> instanceitems)
		{
			var itms = new List<DietInstanceVM> (instanceitems);
			planAdapter = new DAdapter (this, itms);
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
			return new SetRet () { apt = new LAdapter<EntryLineVM> (this, ll, vid, (v,vm) => cfg(v,vm,use) ), name = use };
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
			case Resource.Id.manageFoods:
				manageInfo (InfoManageType.Eat);
				break;
			case Resource.Id.manageBurns:
				manageInfo (InfoManageType.Burn);
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
		BoundCommands planCommands = new BoundCommands ();
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
			Presenter.Singleton.PresentTo (this, this, planCommands, defaultBuilder);
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
			}
		}

	}
}


