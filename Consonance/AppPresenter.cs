﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using SQLite;
using System.IO;

namespace Consonance
{
	public class ChangeTriggerList<T> : List<T>
	{
		public event Action Changed = delegate { };
		public void OnChanged() { Changed (); }
	}

	class PlanCommandManager<IRO> 
	{
		INotSoAbstractedDiet<IRO> cdh;
		DietInstanceVM cd;
		readonly Func<DietInstanceVM> getCurrent;
		public PlanCommandManager(IPlanCommands<IRO> commands, Func<DietInstanceVM> getCurrent)
		{
			// remember it
			this.getCurrent = getCurrent;

			// commanding for pland
			commands.eat.add += View_addeatitem;
			commands.eat.remove += View_removeeatitem;
			commands.eat.edit += View_editeatitem;
			commands.eatinfo.add += View_addeatinfo;
			commands.eatinfo.remove += View_removeeatinfo;
			commands.eatinfo.edit += View_editeatinfo;
			commands.burn.add += View_addburnitem;
			commands.burn.remove += View_removeburnitem;
			commands.burn.edit += View_editburnitem;
			commands.burninfo.add += View_addburninfo;
			commands.burninfo.remove += View_removeburninfo;
			commands.burninfo.edit += View_editburninfo;

		}

		void View_addeatitem (IValueRequestBuilder<IRO> bld) 					{ if (!VerifyDiet ()) return; cdh.AddEat (cd,bld); }
		void View_removeeatitem (EntryLineVM vm) 								{ if (!VerifyDiet ()) return; cdh.RemoveEat (vm); }
		void View_editeatitem (EntryLineVM vm,IValueRequestBuilder<IRO> bld) 	{ if (!VerifyDiet ()) return; cdh.EditEat (vm,bld); }
		void View_addeatinfo (IValueRequestBuilder<IRO> bld) 					{ if (!VerifyDiet ()) return; cdh.AddEatInfo (bld); }
		void View_removeeatinfo (InfoLineVM vm) 								{ if (!VerifyDiet ()) return; cdh.RemoveEatInfo (vm); }
		void View_editeatinfo (InfoLineVM vm,IValueRequestBuilder<IRO> bld) 	{ if (!VerifyDiet ()) return; cdh.EditEatInfo (vm,bld); }
		void View_addburnitem (IValueRequestBuilder<IRO> bld) 					{ if (!VerifyDiet ()) return; cdh.AddBurn (cd,bld); }
		void View_removeburnitem (EntryLineVM vm)							 	{ if (!VerifyDiet ()) return; cdh.RemoveBurn (vm); }
		void View_editburnitem (EntryLineVM vm,IValueRequestBuilder<IRO> bld) 	{ if (!VerifyDiet ()) return; cdh.EditBurn (vm,bld); }
		void View_addburninfo (IValueRequestBuilder<IRO> bld) 					{ if (!VerifyDiet ()) return; cdh.AddBurnInfo (bld); }
		void View_removeburninfo (InfoLineVM vm)							 	{ if (!VerifyDiet ()) return; cdh.RemoveBurnInfo (vm); }
		void View_editburninfo (InfoLineVM vm,IValueRequestBuilder<IRO> bld) 	{ if (!VerifyDiet ()) return; cdh.EditBurnInfo (vm,bld); }

		bool VerifyDiet()
		{
			cd = getCurrent();
			if (cd == null) {
				// ping the view about being stupid.
				cdh = null;
				return false;
			}
			cdh = cd.sender as INotSoAbstractedDiet<IRO>;
			return true;
		}
	}

	[System.Reflection.Obfuscation(Exclude=true, ApplyToMembers=true)]
	class MyConn : SQLiteConnection
	{
		public MyConn(String dbPath, bool storeDateTimeAsTicks = false) : base(dbPath, storeDateTimeAsTicks)
		{
			TableChanged += (object sender, NotifyTableChangedEventArgs e) => MyTableChanged (sender, e);
		}
		public MyConn(String dbPath, SQLiteOpenFlags openFlags, bool storeDateTimeAsTicks = false) : base(dbPath,openFlags, storeDateTimeAsTicks)
		{
			TableChanged += (object sender, NotifyTableChangedEventArgs e) => MyTableChanged (sender, e);
		}
		public event EventHandler<NotifyTableChangedEventArgs> MyTableChanged = delegate{};
		public void Delete<T>(String whereClause)
		{
			Execute ("DELETE FROM " + typeof(T).Name + " WHERE " + whereClause);
			MyTableChanged(
				this,
				new NotifyTableChangedEventArgs (
					new TableMapping (typeof(T)), 
					NotifyTableChangedAction.Delete
				)
			);
		}
	}
	public class Presenter
	{
		// Singleton logic - lazily created
		static Presenter singleton;
		public static Presenter Singleton { get { return singleton ?? (singleton = new Presenter()); } }
		MyConn conn;
		private Presenter ()
		{
			var datapath = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			if (!Directory.Exists (datapath))
				Directory.CreateDirectory (datapath);
			var maindbpath = Path.Combine (datapath, "manydiet_changetest.db");
			#if DEBUG
			//File.Delete (maindbpath); // fresh install 
			#endif
			conn = new MyConn(maindbpath);
		}

		// present app logic domain to this view.
		IView view;
		IUserInput input;
		Object pcm_refholder;
		public void PresentTo<VRO>(IView view, IUserInput input, IPlanCommands<VRO> commands, IValueRequestBuilder<VRO> defBuilder)
		{
			this.view = view;
			this.input = input;
			AddDietPair ( new CalorieDiet (), new CalorieDietPresenter (), defBuilder);

			// commanding...
			view.plan.add += Handleadddietinstance;
			view.plan.select += View_selectdietinstance;
			view.plan.remove += Handleremovedietinstance;
			view.plan.edit += View_editdietinstance;

			// more commanding...
			view.changeday += ChangeDay;
			view.manageInfo += View_manageInfo;

			pcm_refholder = new PlanCommandManager<VRO> (commands, () => view.currentDiet);

			// setup view
			PushDietInstances ();
			ChangeDay (DateTime.UtcNow);
		}

		void View_manageInfo (InfoManageType obj)
		{
			if (view.currentDiet == null) return;
			var cdh = view.currentDiet.sender as IAbstractedDiet;
			ChangeTriggerList<InfoLineVM> lines = new ChangeTriggerList<InfoLineVM> ();

			DietVMChangeEventHandler cdel = (sender, args) => {
				switch(args.changeType)
				{
				case DietVMChangeType.EatInfos:
					if(obj == InfoManageType.Eat)
						PushInLinesAndFire (obj, lines);
					break;
				case DietVMChangeType.BurnInfos:
					if(obj == InfoManageType.Burn)
						PushInLinesAndFire (obj, lines);
					break;
				}
			};
			Action finished = () => cdh.ViewModelsChanged -= cdel;
			cdh.ViewModelsChanged += cdel;
			PushInLinesAndFire (obj, lines);
			view.ManageInfos(obj, lines, finished);
		}
			
		void PushInLinesAndFire(InfoManageType mt, ChangeTriggerList<InfoLineVM> bl)
		{
			var cdh = view.currentDiet.sender as IAbstractedDiet;
			bl.Clear ();
			switch (mt) {
			case InfoManageType.Eat:
				bl.AddRange (cdh.EatInfos (false));
				break;
			case InfoManageType.Burn:
				bl.AddRange (cdh.BurnInfos (false));
				break;
			}
			bl.OnChanged ();
		}
			
		void View_selectdietinstance (DietInstanceVM obj)
		{
			view.currentDiet = obj;
			PushEatLines();
			PushBurnLines();
			PushTracking();	
		}

		DateTime ds,de;
		void ChangeDay(DateTime to)
		{
			ds = new DateTime (to.Year, to.Month, to.Day, 0, 0, 0);
			de = ds.AddDays (1);
			view.day = ds;
			if (view.currentDiet != null) {
				PushEatLines ();
				PushBurnLines ();
				PushTracking ();
			}
		}

		void Handleadddietinstance ()
		{
			List<IAbstractedDiet> saveDiets = new List<IAbstractedDiet> (dietHandlers);
			List<String> dietnames = new List<string> ();
			foreach (var ad in saveDiets)
				dietnames.Add (ad.dietName);
			input.SelectString ("Select Diet Type", dietnames, -1, si => saveDiets[si].StartNewDiet());
		}

		void Handleremovedietinstance (DietInstanceVM obj)
		{
			(obj.sender as IAbstractedDiet).RemoveDiet (obj);
		}
		void View_editdietinstance (DietInstanceVM obj)
		{
			(obj.sender as IAbstractedDiet).EditDiet (obj);
		}

		void PushEatLines()
		{
			var ad = view.currentDiet.sender as IAbstractedDiet;
			var eatEntries = ad.EatEntries (view.currentDiet, ds, de);
			view.SetEatLines (eatEntries);
		}
		void PushBurnLines()
		{
			var ad = view.currentDiet.sender as IAbstractedDiet;
			var burnEntries = ad.BurnEntries (view.currentDiet, ds, de);
			view.SetBurnLines (burnEntries);
		}
		void PushTracking()
		{
			var ad = view.currentDiet.sender as IAbstractedDiet;
			view.SetEatTrack (ad.GetEatTracking (view.currentDiet, ds, de));
			view.SetBurnTrack (ad.GetBurnTracking (view.currentDiet, ds, de));
		}

		void PushDietInstances()
		{
			List<DietInstanceVM> build = new List<DietInstanceVM> ();
			bool currentRemoved = view.currentDiet != null;
			foreach (var dh in dietHandlers)
				foreach (var d in dh.Instances ()) {
					if (currentRemoved && Object.ReferenceEquals (d, view.currentDiet))
						currentRemoved = false;
					build.Add (d);
				}
			view.SetInstances (build);
			// change current diet if we have to.
			if (currentRemoved || view.currentDiet == null) {
				// select the first one thats open today
				foreach (var d in build) {
					if (d.start <= DateTime.Now && (d.end ?? DateTime.MaxValue) >= DateTime.Now) {
						view.currentDiet = d;
						PushEatLines ();
						PushBurnLines ();
						PushTracking ();
						break;
					}
				}
			}

		}
			
		List<IAbstractedDiet> dietHandlers = new List<IAbstractedDiet>();
		void AddDietPair<VRO, D,E,Ei,B,Bi>(IDietModel<D,E,Ei,B,Bi> dietModel, IDietPresenter<D,E,Ei,B,Bi> dietPresenter, IValueRequestBuilder<VRO> defBuilder)
			where D : DietInstance, new()
			where E  : BaseEatEntry,new() 
			where Ei : FoodInfo,new() 
			where B  : BaseBurnEntry,new() 
			where Bi : FireInfo,new() 
		{
			var presentationHandler = new DietPresentationAbstractionHandler<VRO, D,E,Ei,B,Bi> (defBuilder, input, conn, dietModel, dietPresenter);
			dietHandlers.Add (presentationHandler);
			presentationHandler.ViewModelsChanged += HandleViewModelsChanged;
		}

		void HandleViewModelsChanged (IAbstractedDiet sender, DietVMChangeEventArgs args)
		{
			// check its from the active diet
			if (view.currentDiet == null || Object.ReferenceEquals (view.currentDiet.sender, sender)) {
				switch (args.changeType) {
				case DietVMChangeType.Instances:
					PushDietInstances ();
					break;
				case DietVMChangeType.EatEntries:
					PushEatLines ();
					PushTracking ();
					break;
				case DietVMChangeType.BurnEntries:
					PushBurnLines ();
					PushTracking ();
					break;
				}
			}
		}
	}

	public delegate bool Predicate();
	public delegate void Promise();
	public delegate void Promise<T>(T arg);
	/// <summary>
	/// definition on the application view
	/// </summary>
	public interface IView
	{
		void SetEatTrack(IEnumerable<TrackingInfoVM> trackinfo);
		void SetBurnTrack(IEnumerable<TrackingInfoVM> trackinfo);
		void SetEatLines (IEnumerable<EntryLineVM> lineitems);
		void SetBurnLines (IEnumerable<EntryLineVM> lineitems);
		void SetInstances (IEnumerable<DietInstanceVM> instanceitems);
		event Action<DateTime> changeday;
		DateTime day { get; set; }
		DietInstanceVM currentDiet { get; set; }
		ICollectionEditorLooseCommands<DietInstanceVM> plan { get; }

		event Action<InfoManageType> manageInfo;
		// these fire to trigger managment of eat or burn infos
		void ManageInfos(InfoManageType mt, ChangeTriggerList<InfoLineVM> toManage, Action finished); // which ends up calling this one
		// and the plancommands get called by the view for stuff...
	}
	public enum InfoManageType { Eat, Burn };
	public interface IPlanCommands<IRO>
	{
		ICollectionEditorBoundCommands<EntryLineVM, IRO> eat { get; }
		ICollectionEditorBoundCommands<InfoLineVM, IRO> eatinfo { get; }
		ICollectionEditorBoundCommands<EntryLineVM, IRO> burn { get; }
		ICollectionEditorBoundCommands<InfoLineVM, IRO> burninfo { get; }
	}
	public interface ICollectionEditorBoundCommands<T, IRO> 
	{
		event Action<IValueRequestBuilder<IRO>> add;
		event Action<T> remove;
		event Action<T, IValueRequestBuilder<IRO>> edit;
	}
	public interface ICollectionEditorLooseCommands<T>
	{
		event Action add;
		event Action<T> remove;
		event Action<T> edit;
		event Action<T> select;
	}
	public interface IUserInput
	{
		// User Input
		void SelectString (String title, IReadOnlyList<String> strings, int initial, Promise<int> completed);
		void WarnConfirm (String action, Promise confirmed);
	}
	public interface IValueRequestBuilder<IRO>
	{
		// get generic set of values on a page thing
		void GetValues (String title, BindingList<IRO> requests, Promise<bool> completed, int page, int pages);

		// VRO Factory Method
		IValueRequestFactory<IRO> requestFactory { get; }
	}
	public interface IValueRequestFactory<T>
	{
		IValueRequest<T, String> StringRequestor(String name);
		IValueRequest<T, InfoSelectValue> InfoLineVMRequestor(String name);
		IValueRequest<T, DateTime> DateRequestor(String name);
		IValueRequest<T, TimeSpan> TimeSpanRequestor(String name);
		IValueRequest<T, double> DoubleRequestor(String name);
		IValueRequest<T, bool> BoolRequestor(String name);
	}
	public class InfoSelectValue
	{
		public int selected;
		public IReadOnlyList<InfoLineVM> choices;
	}
	public interface IValueRequest<T,V> : IRequest<V>
	{
		T request { get; }  // used by view to encapsulate viewbuilding lookups
	}
	public interface IRequest<V>
	{
		V value { get; set; } // set by view when done, and set by view to indicate an initial value.
		event Action changed, ended; // so model domain can change the flags
		bool enabled { set; } // so the model domain can communicate what fields should be in action (for combining quick and calculate entries)
		bool valid { set; } // if we want to check the value set is ok
	}
	public class RequestStorageHelper<V>
	{
		public IRequest<V> request { get; private set; }
		readonly String name;
		readonly Func<V> defaultValue = () => default(V);
		readonly bool resetOnCGet = false;
		public RequestStorageHelper(String requestName, Func<V> defaultValue)
		{
			this.resetOnCGet = true;
			this.defaultValue = defaultValue;
			name = requestName;
		}
		public RequestStorageHelper(String requestName)
		{
			name = requestName;
		}
		public T CGet<T>(Func<String,IValueRequest<T,V>> creator)
		{
			if (request == null)
				request = creator (name);
			if (resetOnCGet)
				request.value = defaultValue();
			return (request as IValueRequest<T,V>).request;
		}
		public static implicit operator V (RequestStorageHelper<V> me)
		{
			return me.request.value;
		}
	}
}
