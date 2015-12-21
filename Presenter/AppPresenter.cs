using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using SQLite;
using System.IO;
using System.Threading;
using System.Text;

namespace Consonance
{
	public interface IChangeTrigger
	{
		event Action Changed;
	}
	// this one really used for mangeinfo data supply.
	public class ChangeTriggerList<T> : List<T>, IChangeTrigger
	{
		#region IChangeTrigger implementation
		public event Action Changed = delegate { };
		public void OnChanged() { Changed (); }
		#endregion
	}


	class PlanCommandManager
	{
		IAbstractedTracker cdh;
		TrackerInstanceVM cd;
		readonly Func<TrackerInstanceVM> getCurrent;
		public PlanCommandManager(IPlanCommands commands, Func<TrackerInstanceVM> getCurrent)
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

		void View_addeatitem (IValueRequestBuilder bld) 					{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.AddIn (cd,bld); }); }
		void View_removeeatitem (EntryLineVM vm) 							{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.RemoveIn (vm); });  }
		void View_editeatitem (EntryLineVM vm,IValueRequestBuilder bld) 	{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.EditIn (vm,bld); }); }
		void View_addeatinfo (IValueRequestBuilder bld) 					{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.AddInInfo (bld); }); }
		void View_removeeatinfo (InfoLineVM vm) 							{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.RemoveInInfo (vm); }); }
		void View_editeatinfo (InfoLineVM vm,IValueRequestBuilder bld) 		{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.EditInInfo (vm,bld); }); }
		void View_addburnitem (IValueRequestBuilder bld) 					{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.AddOut (cd,bld); }); }
		void View_removeburnitem (EntryLineVM vm)						 	{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.RemoveOut (vm); }); }
		void View_editburnitem (EntryLineVM vm,IValueRequestBuilder bld) 	{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.EditOut (vm,bld); }); }
		void View_addburninfo (IValueRequestBuilder bld) 					{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.AddOutInfo (bld); }); }
		void View_removeburninfo (InfoLineVM vm)							{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.RemoveOutInfo (vm); }); }
		void View_editburninfo (InfoLineVM vm,IValueRequestBuilder bld) 	{ PTask.Run(() => { if (!VerifyDiet ()) return; cdh.EditOutInfo (vm,bld); }); }

		bool VerifyDiet()
		{
			cd = getCurrent();
			if (cd == null) {
				// ping the view about being stupid.
				cdh = null;
				return false;
			}
			cdh = cd.sender as IAbstractedTracker;
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
	static class PTask
	{
		public static ITasks taskops;
		public static Task Run (Func<Task> asyncMethod){ return taskops.RunTask(asyncMethod); }
		public static Task Run (Action syncMethod){ return taskops.RunTask(syncMethod); }
		public static Task<T> Runk<T> (Func<Task<T>> asyncMethod){ return taskops.RunTask(asyncMethod); }
		public static Task<T> Run<T> (Func<T> syncMethod) { return taskops.RunTask(syncMethod); }
	}
	public class Presenter
	{
		// Singleton logic - lazily created
		static Presenter singleton;
		//public static Presenter Singleton { get { return singleton ?? (singleton = new Presenter()); } }
		public static async Task PresentTo(IView view, IPlatform platform, IUserInput input, IPlanCommands commands, IValueRequestBuilder defBuilder)
		{
			TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs e) => 
			{
				view.UIThread (() => {
					throw e.Exception.InnerException;
				});
			};
			PTask.taskops = platform.TaskOps;

			singleton = new Presenter ();
			await singleton.PresentToImpl (view, platform, input, commands, defBuilder);
		}
		MyConn conn;
		private Presenter ()
		{
			var datapath = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			if (!Directory.Exists (datapath))
				Directory.CreateDirectory (datapath);
			var maindbpath = Path.Combine (datapath, "manydiet.db");
			#if DEBUG
			//File.Delete (maindbpath); // fresh install 
			#endif
			conn = new MyConn(maindbpath);
		}

		// present app logic domain to this view.
		IView view;
		IUserInput input;
		Object pcm_refholder;
		Task PresentToImpl(IView view, IPlatform platform, IUserInput input, IPlanCommands commands, IValueRequestBuilder defBuilder)
		{
			// Start fast!
			return PTask.Run (() => {
				this.view = view;
				this.input = input;
				AddDietPair (CalorieDiets.simple.model, CalorieDiets.simple.presenter, defBuilder);
				AddDietPair (CalorieDiets.scav.model, CalorieDiets.scav.presenter, defBuilder);
				AddDietPair (Budgets.simpleBudget.model, Budgets.simpleBudget.presenter, defBuilder);

				// commanding...
				view.plan.add += Handleadddietinstance;
				view.plan.select += View_trackerinstanceselected;
				view.plan.remove += Handleremovedietinstance;
				view.plan.edit += View_editdietinstance;

				// more commanding...
				view.changeday += ChangeDay;
				view.manageInfo += View_manageInfo;

				pcm_refholder = new PlanCommandManager (commands, () => view.currentTrackerInstance);

				// setup view
				PushDietInstances ();
				ChangeDay (DateTime.UtcNow);
			});
		}

		void View_manageInfo (InfoManageType obj)
		{
			PTask.Run (async () => {
				await Task.Yield ();
				if (view.currentTrackerInstance != null)
					using (var hk = new HookedInfoLines (view.currentTrackerInstance.sender as IAbstractedTracker, obj))
						await input.InfoView(InfoCallType.AllowManage, obj, hk.lines, null);
			});
		}
			
		void View_trackerinstanceselected (TrackerInstanceVM obj)
		{
			PTask.Run (() => {
				// yeah, it was selected....cant stack overflow here...
				//view.currentTrackerInstance = obj;
				PushEatLines ();
				PushBurnLines ();
				PushTracking ();	
			});
		}

		DateTime ds,de;
		void ChangeDay(DateTime to)
		{
			PTask.Run (() => {
				ds = new DateTime (to.Year, to.Month, to.Day, 0, 0, 0);
				de = ds.AddDays (1);
				view.day = ds;
				if (view.currentTrackerInstance != null) {
					PushEatLines ();
					PushBurnLines ();
					PushTracking ();
				}
			});
		}

		void Handleadddietinstance ()
		{
			PTask.Run (async () => {
				List<IAbstractedTracker> saveDiets = new List<IAbstractedTracker> (dietHandlers);
				List<TrackerDetailsVM> dietnames = new List<TrackerDetailsVM> ();
				foreach (var ad in saveDiets)
					dietnames.Add (ad.details);
				var chooseViewTask = input.ChoosePlan ("Select Diet Type", dietnames, -1);
				chooseViewTask.Completed.ContinueWith(async index =>
				{
					var addViewTask = saveDiets [index.Result].StartNewTracker ();
					await addViewTask.Pushed;
					chooseViewTask.Pop();
				});
			});
		}

		void Handleremovedietinstance (TrackerInstanceVM obj)
		{
			PTask.Run (() => (obj.sender as IAbstractedTracker).RemoveTracker (obj));
		}
		void View_editdietinstance (TrackerInstanceVM obj)
		{
			PTask.Run (() => (obj.sender as IAbstractedTracker).EditTracker (obj));
		}

		void PushEatLines()
		{
			var ad = view.currentTrackerInstance.sender as IAbstractedTracker;
			var eatEntries = ad.InEntries (view.currentTrackerInstance, ds, de);
			view.SetEatLines (eatEntries);
		}
		void PushBurnLines()
		{
			var ad = view.currentTrackerInstance.sender as IAbstractedTracker;
			var burnEntries = ad.OutEntries (view.currentTrackerInstance, ds, de);
			view.SetBurnLines (burnEntries);
		}
		void PushTracking()
		{
			SetViewTrackerTracks (ti => (ti.sender as IAbstractedTracker).GetInTracking (ti, ds), view.SetEatTrack);
			SetViewTrackerTracks (ti => (ti.sender as IAbstractedTracker).GetOutTracking (ti, ds), view.SetBurnTrack);
		}
		void SetViewTrackerTracks(Func<TrackerInstanceVM, IEnumerable<TrackingInfoVM>> processor, Action<TrackerTracksVM,IEnumerable<TrackerTracksVM>> viewSetter)
		{
			var v = processor (view.currentTrackerInstance);
			viewSetter (
				new TrackerTracksVM () { 
					tracks = processor (view.currentTrackerInstance), 
					instance = view.currentTrackerInstance
				},
				OtherOnes (view.currentTrackerInstance, lastBuild, processor)
			);
		}
		IEnumerable<TrackerTracksVM> OtherOnes (TrackerInstanceVM curr, List<TrackerInstanceVM> last, Func<TrackerInstanceVM, IEnumerable<TrackingInfoVM>> processor)
		{
			foreach (var d in last)
				if (d != curr && d.tracked)
					yield return new TrackerTracksVM () {
						tracks = processor (d),
						instance = d
					};
		}

		List<TrackerInstanceVM> lastBuild = new  List<TrackerInstanceVM>();
		void PushDietInstances()
		{
			lastBuild.Clear ();	
			bool currentRemoved = view.currentTrackerInstance != null;
			foreach (var dh in dietHandlers)
				foreach (var d in dh.Instances ()) {
					if (currentRemoved && OriginatorVM.OriginatorEquals (d, view.currentTrackerInstance)) // that checks db id and table, if originator is correctly set.
						currentRemoved = false;
					lastBuild.Add (d);
				}
			foreach (var vm in lastBuild)
				vm.trackChanged = v => PushTracking (); // lazy way.
			Debug.WriteLine("Setting Trackers");
			view.SetInstances (lastBuild);
			// change current diet if we have to.
			if (currentRemoved || view.currentTrackerInstance == null) {
				Debug.WriteLine("Attempting to default to a tracker instance, lastbuild has " + lastBuild.Count);
				// select the first one thats open today
				foreach (var d in lastBuild) {
					if (d.start <= DateTime.Now && (d.hasended ? d.end : DateTime.MaxValue) >= DateTime.Now) {
						view.currentTrackerInstance = d;
						Debug.WriteLine("Selecting tracker defaultly");
						PushEatLines ();
						PushBurnLines ();
						PushTracking ();
						break;
					}
				}
			}
		}
			
		List<IAbstractedTracker> dietHandlers = new List<IAbstractedTracker>();
		void AddDietPair<D,E,Ei,B,Bi>(ITrackModel<D,E,Ei,B,Bi> dietModel, ITrackerPresenter<D,E,Ei,B,Bi> dietPresenter, IValueRequestBuilder defBuilder)
			where D : TrackerInstance, new()
			where E  : BaseEntry,new() 
			where Ei : BaseInfo,new() 
			where B  : BaseEntry,new() 
			where Bi : BaseInfo,new() 
		{
			var presentationHandler = new TrackerPresentationAbstractionHandler<D,E,Ei,B,Bi> (defBuilder, input, conn, dietModel, dietPresenter);
			dietHandlers.Add (presentationHandler);
			presentationHandler.ViewModelsChanged += HandleViewModelsChanged;
		}

		void HandleViewModelsChanged (IAbstractedTracker sender, DietVMChangeEventArgs args)
		{
			// we want these from any registered tracker
			if(args.changeType == DietVMChangeType.Instances)
				PushDietInstances ();
			
			// check its from the active tracker
			if (view.currentTrackerInstance == null || Object.ReferenceEquals (view.currentTrackerInstance.sender, sender)) {
				switch (args.changeType) {
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
	public delegate Task Promise();
	public delegate Task Promise<T>(T arg);
	/// <summary>
	/// definition on the application view
	/// </summary>
	public interface IView
	{
		void SetEatTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others);
		void SetBurnTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others);
		void SetEatLines (IEnumerable<EntryLineVM> lineitems);
		void SetBurnLines (IEnumerable<EntryLineVM> lineitems);
		void SetInstances (IEnumerable<TrackerInstanceVM> instanceitems);
		event Action<DateTime> changeday;
		DateTime day { get; set; }
		TrackerInstanceVM currentTrackerInstance { get; set; }
		ICollectionEditorLooseCommands<TrackerInstanceVM> plan { get; }
		event Action<InfoManageType> manageInfo;
		void UIThread (Action a);
	}
	public enum InfoManageType { In, Out };
	public interface IPlatform
	{
		ITasks TaskOps { get; }
	}
	public interface ITasks
	{
		Task RunTask (Func<Task> asyncMethod);
		Task RunTask (Action syncMethod);
		Task<T> RunTask<T> (Func<Task<T>> asyncMethod);
		Task<T> RunTask<T> (Func<T> syncMethod);
	}
	public interface IPlanCommands
	{
		ICollectionEditorBoundCommands<EntryLineVM> eat { get; }
		ICollectionEditorBoundCommands<InfoLineVM> eatinfo { get; }
		ICollectionEditorBoundCommands<EntryLineVM> burn { get; }
		ICollectionEditorBoundCommands<InfoLineVM> burninfo { get; }
	}
	public interface ICollectionEditorBoundCommands<T> 
	{
		event Action<IValueRequestBuilder> add;
		event Action<T> remove;
		event Action<T, IValueRequestBuilder> edit;
	}
	public interface ICollectionEditorLooseCommands<T>
	{
		event Action add;
		event Action<T> remove;
		event Action<T> edit;
		event Action<T> select;
	}
	interface IViewTask 
	{
		Task Completed {get;}
		Task Pushed {get;}
		void Pop();
	}
	public class ViewTask<TResult> : IViewTask
	{
		Task IViewTask.Completed { get { return Completed; } }
		public Task<TResult> Completed {get;private set;}
		public Task Pushed {get;private set;}
		public void Pop() { pop (); }
		readonly Action pop;
		public ViewTask(Action pop, Task pushed, Task<TResult> completed)
		{
			this.Pushed = pushed;
			this.pop = pop;
			this.Completed = completed;
		}
	}
	[Flags]
	public enum InfoCallType { AllowManage = 1, AllowSelect = 2 };
	public interface IUserInput
	{
		// User Input
		Task SelectString (String title, IReadOnlyList<String> strings, int initial, Promise<int> completed);
		ViewTask<int> ChoosePlan (String title, IReadOnlyList<TrackerDetailsVM> choose_from, int initial);
		Task WarnConfirm (String action, Promise confirmed);
		Task<InfoLineVM> InfoView(InfoCallType calltype, InfoManageType imt, ObservableCollection<InfoLineVM> toManage,InfoLineVM initiallySelected); // which ends up calling this one
		Task<InfoLineVM> Choose (IFindList<InfoLineVM> ifnd);
	}
	public interface IValueRequestBuilder
	{
		// get generic set of values on a page thing
		ViewTask<bool> GetValues (IEnumerable<GetValuesPage> requestPages);

		// VRO Factory Method
		IValueRequestFactory requestFactory { get; }
	}
	public class Barcode
	{
		public long value; // I think this works?
	}
	public interface IValueRequestFactory
	{
		IValueRequest<String> StringRequestor(String name);
		IValueRequest<InfoSelectValue> InfoLineVMRequestor(String name);
		IValueRequest<DateTime> DateRequestor(String name);
		IValueRequest<TimeSpan> TimeSpanRequestor(String name);
		IValueRequest<double> DoubleRequestor(String name); 
		IValueRequest<int> IntRequestor(String name); 
		IValueRequest<bool> BoolRequestor(String name);
		IValueRequest<EventArgs> ActionRequestor(String name);
		IValueRequest<Barcode> BarcodeRequestor (String name);
		IValueRequest<OptionGroupValue> OptionGroupRequestor(String name);
	}
	public class OptionGroupValue 
	{
		public readonly IReadOnlyList<String> OptionNames;
		public int SelectedOption { get; set; }
		public OptionGroupValue(IEnumerable<String> options)
		{
			SelectedOption = 0;
			OptionNames = new List<String> (options);
		}
		public static implicit operator int(OptionGroupValue other)
		{
			return other.SelectedOption;
		}
		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder();
			for(int i=0;i<OptionNames.Count;i++)
			{
				if (i == SelectedOption) sb.Append ("[");
				sb.Append (OptionNames [i]);
				if (i == SelectedOption) sb.Append ("]");
				if (i != OptionNames.Count-1) sb.Append (" | ");
			}
			return sb.ToString ();
		}
	}
	public class InfoSelectValue
	{
		public InfoLineVM selected {get;set;}
		public event Action choose = delegate { };
		public void OnChoose() { choose(); }

		public override string ToString ()
		{
			return string.Format ("Selected={0}", selected == null ? "None" : selected.name);
		}
	}
	public interface IValueRequest<V>
	{
		Object request { get; }  // used by view to encapsulate viewbuilding lookups
		V value { get; set; } // set by view when done, and set by view to indicate an initial value.
		event Action changed; // so model domain can change the flags
		void ClearListeners();
		bool enabled { set; } // so the model domain can communicate what fields should be in action (for combining quick and calculate entries)
		bool valid { set; } // if we want to check the value set is ok
		bool read_only { set; } // if we want to check the value set is ok
	}
	public interface IRequestStorageHelper
	{
		void Reset();
		event Action requestChanged;
		bool requestValid {set;}
		Object requestValue { get; set; }
		object CGet (IValueRequestFactory fact, Func<IValueRequestFactory, Func<String, Object>> FindRequestDelegate);
	}
	public class RequestStorageHelper<V> : IRequestStorageHelper
	{
		#region IRequestStorageHelper implementation
		public event Action requestChanged { add { request.changed += value; } remove{ request.changed -= value; } }
		public Object requestValue { get { return request.value; } set { request.value = (V)value; } }
		public object CGet (IValueRequestFactory fact, Func<IValueRequestFactory, Func<String, Object>> FindRequestDelegate)
		{
			return CGet ((Func<String, IValueRequest<V>>)FindRequestDelegate (fact));
		}
		public bool requestValid { set { request.valid = value; } }
		#endregion

		public IValueRequest<V> request { get; private set; }
		readonly String name;
		readonly Func<V> defaultValue = () => default(V);
		readonly Action validate;
		public RequestStorageHelper(String requestName, Func<V> defaultValue, Action validate)
		{
			this.validate = validate;
			this.defaultValue = defaultValue;
			name = requestName;
		}
		bool shouldReset = true; // initiailisation
		public void Reset()
		{
			if (!(shouldReset = request == null)) {
				request.ClearListeners ();
				request.value = defaultValue ();
			} 
		}
		// will return cached instance if possible, but will do defaulting if specified and will
		// always call ClearListeners, so that old registrations to the changed event are no longer called.
		public Object CGet(Func<String,IValueRequest<V>> creator)
		{
			if (request == null)				
				request = creator (name);
			if (shouldReset) Reset ();
			request.ClearListeners ();
			request.changed += validate;
			return request.request;
		}
		public static implicit operator V (RequestStorageHelper<V> me)
		{
			return me.request.value;
		}
	}
}
