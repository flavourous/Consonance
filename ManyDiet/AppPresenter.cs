using System;
using System.Diagnostics;
using System.Collections.Generic;
using SQLite;
using System.IO;

namespace ManyDiet
{
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
			var maindbpath = Path.Combine (datapath, "manydiet.db");
			#if DEBUG
			File.Delete (maindbpath); // fresh install 
			#endif
			conn = new MyConn(maindbpath);
		}

		// present app logic domain to this view.
		IView view;
		public void PresentTo(IView view)
		{
			this.view = view;
			AddDietPair ( new CalorieDiet (), new CalorieDietPresenter ());

			// commanding...
//			view.addeatitemquick += AddEatItemQuick;
//			view.addeatitem += AddEatItem;
//			view.removeeatitem += RemEatItem;
//			view.addburnitemquick += AddBurnItemQuick;
//			view.addburnitem += AddBurnItem;
//			view.removeburnitem += RemBurnItem;
			view.changeday += ChangeDay;
			view.adddietinstance += Handleadddietinstance;
			view.selectdietinstance += (obj) => { 
				view.currentDiet = obj;
				PushEatLines();
				PushBurnLines();
				PushTracking();
			};
			view.removedietinstance += Handleremovedietinstance;;

			// setup view
			PushDietInstances ();
			ChangeDay (DateTime.UtcNow);
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
			view.SelectString ("Select Diet Type", dietnames, si => saveDiets[si].StartNewDiet());
		}

		void Handleremovedietinstance (DietInstanceVM obj)
		{
			(obj.sender as IAbstractedDiet).RemoveDiet (obj);
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
					if (d.start >= DateTime.Now && (d.end ?? DateTime.MaxValue) <= DateTime.Now) {
						view.currentDiet = d;
						break;
					}
				}
			}

		}
			
		List<IAbstractedDiet> dietHandlers = new List<IAbstractedDiet>();
		void AddDietPair<D,E,Ei,B,Bi>(IDietModel<D,E,Ei,B,Bi> dietModel, IDietPresenter<D,E,Ei,B,Bi> dietPresenter)
			where D : DietInstance, new()
			where E  : BaseEatEntry,new() 
			where Ei : FoodInfo,new() 
			where B  : BaseBurnEntry,new() 
			where Bi : FireInfo,new() 
		{
			var presentationHandler = new DietPresentationAbstractionHandler<D,E,Ei,B,Bi> (view, conn, dietModel, dietPresenter);
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
					break;
				case DietVMChangeType.BurnEntries:
					break;
				}
			}
		}
	}

	public delegate void Promise<T>(T arg);
	/// <summary>
	/// definition on the application view
	/// </summary>
	public interface IView : IUserInput
	{
		void SetEatTrack(IEnumerable<TrackingInfo> trackinfo);
		void SetBurnTrack(IEnumerable<TrackingInfo> trackinfo);
		void SetEatLines (IEnumerable<EntryLineVM> lineitems);
		void SetBurnLines (IEnumerable<EntryLineVM> lineitems);
		void SetInstances (IEnumerable<DietInstanceVM> instanceitems);
		event Action<DateTime> changeday;
		DateTime day { get; set; }
		DietInstanceVM currentDiet { get; set; }

		// eat
		event Action addeatitem;
		event Action addeatitemquick;
		event Action<EntryLineVM> removeeatitem;

		// burn
		event Action addburnitem;
		event Action addburnitemquick;
		event Action<EntryLineVM> removeburnitem;

		// plan (diet managment)
		event Action adddietinstance;
		event Action<DietInstanceVM> selectdietinstance;
		event Action<DietInstanceVM> removedietinstance;
	}
	public interface IUserInput
	{
		// User Input
		void GetValues (String title, IEnumerable<String> names, Promise<AddedItemVM> completed, AddedItemVMDefaults defaultUse = AddedItemVMDefaults.Name | AddedItemVMDefaults.When);
		void SelectInfo (String title, IReadOnlyList<SelectableItemVM> foods, Promise<int> completed);
		void SelectString (String title, IReadOnlyList<String> strings, Promise<int> completed);
	}
	[Flags]
	public enum AddedItemVMDefaults { None =0, Name =1, When = 2 };
	public class AddedItemVM
	{
		public readonly String name;
		public readonly DateTime when;
		public readonly double[] values;
		public AddedItemVM(double[] values, DateTime when, String name)
		{
			this.values = values;
			this.when = when;
			this.name = name;
		}
	}
	public class SelectableItemVM {
		public String name;
	}
}

