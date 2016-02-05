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
using LibRTP;

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
		readonly Func<TrackerInstanceVM> getCurrent;
		readonly Func<String,Task> message;
		public PlanCommandManager(IPlanCommands commands, Func<TrackerInstanceVM> getCurrent, Func<String,Task> message)
		{
			// remember it
			this.getCurrent = getCurrent;
			this.message = message;

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

		void View_addeatitem (IValueRequestBuilder bld) 					{ VerifyDiet((cdh, cd) => cdh.AddIn (cd,bld)); }
		void View_removeeatitem (EntryLineVM vm) 							{ VerifyDiet((cdh, cd) => cdh.RemoveIn (vm));  }
		void View_editeatitem (EntryLineVM vm,IValueRequestBuilder bld) 	{ VerifyDiet((cdh, cd) => cdh.EditIn (vm,bld)); }
		void View_addeatinfo (IValueRequestBuilder bld) 					{ VerifyDiet((cdh, cd) => cdh.AddInInfo (bld)); }
		void View_removeeatinfo (InfoLineVM vm) 							{ VerifyDiet((cdh, cd) => cdh.RemoveInInfo (vm)); }
		void View_editeatinfo (InfoLineVM vm,IValueRequestBuilder bld) 		{ VerifyDiet((cdh, cd) => cdh.EditInInfo (vm,bld)); }
		void View_addburnitem (IValueRequestBuilder bld) 					{ VerifyDiet((cdh, cd) => cdh.AddOut (cd,bld)); }
		void View_removeburnitem (EntryLineVM vm)						 	{ VerifyDiet((cdh, cd) => cdh.RemoveOut (vm)); }
		void View_editburnitem (EntryLineVM vm,IValueRequestBuilder bld) 	{ VerifyDiet((cdh, cd) => cdh.EditOut (vm,bld)); }
		void View_addburninfo (IValueRequestBuilder bld) 					{ VerifyDiet((cdh, cd) => cdh.AddOutInfo (bld)); }
		void View_removeburninfo (InfoLineVM vm)							{ VerifyDiet((cdh, cd) => cdh.RemoveOutInfo (vm)); }
		void View_editburninfo (InfoLineVM vm,IValueRequestBuilder bld) 	{ VerifyDiet((cdh, cd) => cdh.EditOutInfo (vm,bld)); }

		public void VerifyDiet(Action<IAbstractedTracker, TrackerInstanceVM> acty)
		{
			TaskCompletionSource<bool> tcs_dummy = new TaskCompletionSource<bool> ();
			tcs_dummy.SetResult (true);
			VerifyDiet((cdh,cd) => { acty(cdh,cd); return tcs_dummy.Task; });
		}
		public void VerifyDiet(Func<IAbstractedTracker, TrackerInstanceVM, Task> acty)
		{
			var cd = getCurrent();
			if (cd == null) // ping the view about being stupid.
				message ("You need to create a tracker before you can do that");
			else acty (cd.sender as IAbstractedTracker, cd); // i dont think these need threading either...
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
		public static Task<T> Run<T> (Func<Task<T>> asyncMethod){ return taskops.RunTask(asyncMethod); }
		public static Task<T> Run<T> (Func<T> syncMethod) { return taskops.RunTask(syncMethod); }
	}
	public class Presenter
	{
		// Singleton logic - lazily created
		static Presenter singleton;
		//public static Presenter Singleton { get { return singleton ?? (singleton = new Presenter()); } }
		public static async Task PresentTo(IView view, IPlatform platform, IUserInput input, IPlanCommands commands, IValueRequestBuilder defBuilder)
		{
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
		PlanCommandManager pcm_refholder;
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

				pcm_refholder = new PlanCommandManager (commands, () => view.currentTrackerInstance, input.Message);

				// setup view
				ChangeDay (DateTime.UtcNow);
				PushDietInstances ();
			});
		}

		void View_manageInfo (InfoManageType obj)
		{
			pcm_refholder.VerifyDiet ((cdh, cd) => {
				using (var hk = new HookedInfoLines (cdh, obj))
					return input.InfoView (InfoCallType.AllowManage, obj, hk.lines, null);
			});
				
		}
			
		void View_trackerinstanceselected (TrackerInstanceVM obj)
		{
			PTask.Run (() => {
				// yeah, it was selected....cant stack overflow here...
				//view.currentTrackerInstance = obj;
				PushEatLines (obj);
				PushBurnLines (obj);
				PushTracking (obj);	
			});
		}

		DateTime ds,de;
		void ChangeDay(DateTime to)
		{
			PTask.Run (() => {
				ds = to.StartOfDay();
				de = ds.AddDays (1);
				view.day = ds;
				var ti = view.currentTrackerInstance;
				if (ti != null) {
					PushEatLines (ti);
					PushBurnLines (ti);
					PushTracking (ti);
				}
			});
		}

		// There is no reason to thread these - we want to block UI while view transitions are made, and not much work is done. //
		void Handleadddietinstance ()
		{
			List<IAbstractedTracker> saveDiets = new List<IAbstractedTracker> (dietHandlers);
			List<TrackerDetailsVM> dietnames = new List<TrackerDetailsVM> ();
			foreach (var ad in saveDiets) dietnames.Add (ad.details);
			var chooseViewTask = input.ChoosePlan ("Select Diet Type", dietnames, -1);
			chooseViewTask.Completed.ContinueWith (async index => {
				var addViewTask = saveDiets [index.Result].StartNewTracker ();
				await addViewTask;
				chooseViewTask.Pop ();
			});
		}
		void Handleremovedietinstance (TrackerInstanceVM obj)
		{
			(obj.sender as IAbstractedTracker).RemoveTracker (obj);
		}
		void View_editdietinstance (TrackerInstanceVM obj)
		{
			(obj.sender as IAbstractedTracker).EditTracker (obj);
		}
		// // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // //

		void PushEatLines(TrackerInstanceVM ti)
		{
			view.SetLoadingState (LoadThings.EatItems, true);
			var ad = ti.sender as IAbstractedTracker;
			var eatEntries = ad.InEntries (ti, ds, de);
			view.SetEatLines (eatEntries);
			view.SetLoadingState (LoadThings.EatItems, false);
		}
		void PushBurnLines(TrackerInstanceVM ti)
		{
			view.SetLoadingState (LoadThings.BurnItems, true);
			var ad = ti.sender as IAbstractedTracker;
			var burnEntries = ad.OutEntries (ti, ds, de);
			view.SetBurnLines (burnEntries);
			view.SetLoadingState (LoadThings.BurnItems, false);
		}
		void PushTracking(TrackerInstanceVM tii)
		{
			SetViewTrackerTracks (tii, ti => (ti.sender as IAbstractedTracker).GetInTracking (ti, ds), view.SetEatTrack);
			SetViewTrackerTracks (tii, ti => (ti.sender as IAbstractedTracker).GetOutTracking (ti, ds), view.SetBurnTrack);
		}
		void SetViewTrackerTracks(TrackerInstanceVM current, Func<TrackerInstanceVM, IEnumerable<TrackingInfoVM>> processor, Action<TrackerTracksVM,IEnumerable<TrackerTracksVM>> viewSetter)
		{
			var v = processor (current);
			viewSetter (
				new TrackerTracksVM () { 
					tracks = processor (current), 
					instance = current
				},
				OtherOnes (current, lastBuild, processor)
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
			view.SetLoadingState (LoadThings.Instances, true);
			lastBuild.Clear ();	
			bool currentRemoved = view.currentTrackerInstance != null;
			foreach (var dh in dietHandlers)
				foreach (var d in dh.Instances ()) {
					if (currentRemoved && OriginatorVM.OriginatorEquals (d, view.currentTrackerInstance)) // that checks db id and table, if originator is correctly set.
						currentRemoved = false;
					lastBuild.Add (d);
				}
			foreach (var vm in lastBuild)
			{
				var lvm = vm;
				vm.trackChanged = v => PushTracking (lvm); // lazy way.
			}
			Debug.WriteLine ("Setting Trackers");
			view.SetInstances (lastBuild);
			// change current diet if we have to.
			if (currentRemoved || view.currentTrackerInstance == null) {
				if (lastBuild.Count > 0) {
					view.currentTrackerInstance = lastBuild [0];
					PushEatLines (lastBuild [0]);
					PushBurnLines (lastBuild [0]);
					PushTracking (lastBuild [0]);
				} else {
					view.SetEatLines (new EntryLineVM[0]);
					view.SetBurnLines (new EntryLineVM[0]);
					view.SetEatTrack (null, new TrackerTracksVM[0]);
					view.SetBurnTrack (null, new TrackerTracksVM[0]);
				}
			}
			view.SetLoadingState (LoadThings.Instances, false);
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
			if (args.changeType == DietVMChangeType.Instances)
				PushDietInstances ();
			
			// check its from the active tracker]
			var ti = view.currentTrackerInstance;
			if (ti != null && Object.ReferenceEquals (ti.sender, sender)) {
				switch (args.changeType) {
					case DietVMChangeType.EatEntries: PushEatLines (ti); break;
					case DietVMChangeType.BurnEntries: PushBurnLines (ti); break;
				}
				PushTracking (ti);
			}
		}
	}

	public delegate bool Predicate();
	public delegate Task Promise();
	public delegate Task Promise<T>(T arg);
	public enum LoadThings { EatItems, BurnItems, Instances };
	/// <summary>
	/// definition on the application view
	/// </summary>
	public interface IView
	{
		void SetLoadingState(LoadThings thing, bool active);
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
		void BeginUIThread (Action a);
	}
	public enum InfoManageType { In, Out };
	public interface IPlatform
	{
		ITasks TaskOps { get; }
		void Attach (Action<String, Action> showError);
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
		Task Message(String msg);
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
		IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor(String name);
		IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor(String name);
	}
	public class RecurrsEveryPatternValue
	{
		public DateTime PatternFixed;
		public RecurrSpan PatternType;
		public int PatternFrequency;
		public RecurrsEveryPatternValue(DateTime date, RecurrSpan pt, int freq)
		{
			PatternFixed = date;
			PatternType = pt;
			PatternFrequency = freq;
		}
		public RecurrsEveryPatternValue() : this(DateTime.Now, RecurrSpan.Day, 1) {
		}
		public bool IsValid 
		{ 
			get 
			{  
				return PatternType == RecurrSpan.Day ||
				       PatternType == RecurrSpan.Month ||
				       PatternType == RecurrSpan.Year ||
				       PatternType == RecurrSpan.Week;	
			}
		}
		public RecurrsEveryPattern Create(DateTime? s, DateTime? e)
		{
			return new RecurrsEveryPattern (PatternFixed, PatternFrequency, PatternType, s, e);
		}
	}
	public class RecurrsOnPatternValue
	{
		public RecurrSpan PatternType;
		public int[] PatternValues;
		public RecurrsOnPatternValue(RecurrSpan pat, int[] vals)
		{
			PatternType = pat;
			PatternValues = vals;
		}
		public RecurrsOnPatternValue():this(RecurrSpan.Day | RecurrSpan.Month, new[] { 1 }){
		}
		public bool IsValid 
		{
			get 
			{ 
				int pc = 0;
				foreach (var pt in PatternType.SplitFlags())
					pc++;
				bool s1 = PatternValues.Length > 1 && pc == PatternValues.Length;
				if (s1) 
				{
					try{ new RecurrsOnPattern(PatternValues, PatternType,null,null); }
					catch{ s1 = false; }
				}
				return s1;
			}
		}
		public RecurrsOnPattern Create(DateTime? s, DateTime? e)
		{
			return new RecurrsOnPattern (PatternValues, PatternType, s, e);
		}
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
}
