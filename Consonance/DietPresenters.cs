using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;
using SQLite;

namespace Consonance
{
	public class OriginatorVM {
		// Dear views, do not modify this object, or I will kill you.
		public Object sender;
		public Object originator;
	}
	public class KVPList<T1,T2> : List<KeyValuePair<T1,T2>>
	{
		public void Add(T1 a, T2 b) 
		{
			Add (new KeyValuePair<T1, T2> (a, b));
		}
	}
	public class DietInstanceVM : OriginatorVM
	{
		public readonly DateTime start;
		public readonly DateTime? end;
		public readonly String name;
		public readonly String desc;
		public readonly KVPList<string,double> displayAmounts;

		public DietInstanceVM(DateTime s, DateTime? e, String n, String d, KVPList<string,double>  t)
		{
			start=s; end = e;
			name=n; desc=d;
			displayAmounts = t;
		}
	}
	public class EntryLineVM : OriginatorVM
	{
		public readonly DateTime start;
		public readonly TimeSpan duration;
		public readonly String name;
		public readonly String desc;
		public readonly KVPList<string,double> displayAmounts;

		public EntryLineVM(DateTime w, TimeSpan l, String n, String d, KVPList<string,double>  t)
		{
			duration = l;
			start=w; name=n; desc=d;
			displayAmounts = t;
		}
	}

	// The default behaviour is something like this:
	// balance value if both not null.  if one is null, treat as a simple target. 
	public class TrackingElementVM
	{
		public String name;
		public double value;
	}
	public class TrackingInfoVM
	{
		public String valueName;
		public TrackingElementVM[] eatValues;
		public TrackingElementVM[] burnValues;
		public double targetValue;
	}
	public class InfoLineVM
	{
		public String name;
	}

	public interface IDietPresenter<DietInstType, EatType, EatInfoType, BurnType, BurnInfoType>
		where DietInstType : DietInstance, new()
		where EatType : BaseEatEntry, new()
		where EatInfoType : FoodInfo, new()
		where BurnType : BaseBurnEntry, new()
		where BurnInfoType : FireInfo, new()
	{
		EntryLineVM GetRepresentation (EatType entry, EatInfoType entryInfo);
		EntryLineVM GetRepresentation (BurnType entry, BurnInfoType entryInfo);

		InfoLineVM GetRepresentation (EatInfoType info);
		InfoLineVM GetRepresentation (BurnInfoType info);

		DietInstanceVM GetRepresentation (DietInstType entry);

		// Deals with goal tracking
		IEnumerable<TrackingInfoVM> DetermineEatTrackingForRange(DietInstType di, IEnumerable<EatType> eats, IEnumerable<BurnType> burns, DateTime startBound,  DateTime endBound);
		IEnumerable<TrackingInfoVM> DetermineBurnTrackingForRange(DietInstType di, IEnumerable<EatType> eats, IEnumerable<BurnType> burns, DateTime startBound,  DateTime endBound);
	}

	interface IAbstractedDiet
	{
		String dietName { get; }
		IEnumerable<DietInstanceVM> Instances();
		IEnumerable<EntryLineVM>  EatEntries (DietInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<EntryLineVM>  BurnEntries(DietInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<TrackingInfoVM> GetEatTracking (DietInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<TrackingInfoVM> GetBurnTracking (DietInstanceVM instance, DateTime start, DateTime end);
		void StartNewDiet();
		void RemoveDiet (DietInstanceVM dvm);
		void EditDiet (DietInstanceVM dvm);

		// entries
		void FullEat(DietInstanceVM to);
		void RemoveEat(EntryLineVM toRemove);
		void EditEat(EntryLineVM toEdit);
		void FullBurn(DietInstanceVM to);
		void RemoveBurn(EntryLineVM toRemove);
		void EditBurn(EntryLineVM toEdit);
	}
	enum DietVMChangeType { None, Instances, EatEntries, BurnEntries };
	class DietVMChangeEventArgs
	{
		public DietVMChangeType changeType;
	}
	delegate void DietVMChangeEventHandler(IAbstractedDiet sender, DietVMChangeEventArgs args);
	/// <summary>
	/// This contains all the code which the app would want to do to get viewmodels out of
	/// implimentations, and retains the generic definitions therefore.
	/// But it does stuff that the IDietPresenter shouldnt have to impliment since it's specific to the 
	/// app logic, not problem logic.
	/// 
	/// As such, it should obey a non-generic contract that the AppPresenter can easily consume and
	/// query for data.
	/// </summary>
	class DietPresentationAbstractionHandler <IRO, DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> : IAbstractedDiet
		where DietInstType : DietInstance, new()
		where EatType : BaseEatEntry, new()
		where EatInfoType : FoodInfo, new()
		where BurnType : BaseBurnEntry, new()
		where BurnInfoType : FireInfo, new()
	{
		readonly IValueRequestBuilder<IRO> getValues;
		readonly IUserInput getInput;
		readonly IDietPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter;
		readonly DietModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> modelHandler;
		readonly MyConn conn;
		public DietPresentationAbstractionHandler(
			IValueRequestBuilder<IRO> getValues, 
			IUserInput getInput,
			MyConn conn,
			IDietModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model,
			IDietPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter
		)
		{
			// store objects
			this.getInput = getInput;
			this.getValues = getValues;
			this.presenter = presenter;
			this.modelHandler = new DietModelAccessLayer<DietInstType, EatType, EatInfoType, BurnType, BurnInfoType>(conn, model);
			this.conn = conn;
			conn.MyTableChanged += HandleMyTableChanged;
		}

		void HandleMyTableChanged (object sender, NotifyTableChangedEventArgs e)
		{
			DietVMChangeType changeType = DietVMChangeType.None;
			if (e.Table.MappedType.Equals (typeof(DietInstType)))
				changeType = DietVMChangeType.Instances;
			if (e.Table.MappedType.Equals (typeof(EatType)))
				changeType = DietVMChangeType.EatEntries;
			if (e.Table.MappedType.Equals (typeof(BurnType)))
				changeType = DietVMChangeType.BurnEntries;
			if (changeType != DietVMChangeType.None)
				ViewModelsChanged (this, new DietVMChangeEventArgs () { changeType = changeType });
		}

		public event DietVMChangeEventHandler ViewModelsChanged = delegate { };
		public String dietName { get { return modelHandler.model.name; } }
		public IEnumerable<DietInstanceVM> Instances()
		{
			foreach (var dt in modelHandler.GetDiets())
			{
				var rep = presenter.GetRepresentation (dt);
				rep.sender = this;
				rep.originator = dt;
				yield return rep;
			}
		}
		public IEnumerable<EntryLineVM> EatEntries(DietInstanceVM instance, DateTime start, DateTime end)
		{
			var eatModels = modelHandler.foodhandler.Get (instance.originator as DietInstType, start, end);
			return ConvertMtoVM<EntryLineVM, EatType, EatInfoType> (
				eatModels, 
				(e,i) => {
					var vm= presenter.GetRepresentation(e,i);
					vm.originator = e;
					return vm;
				}, 
				ee => GetInfo<EatInfoType>(ee.infoinstanceid)
			);
		}
		public IEnumerable<TrackingInfoVM> GetEatTracking (DietInstanceVM instance, DateTime start, DateTime end)
		{
			var eatModels = modelHandler.foodhandler.Get (instance.originator as DietInstType, start, end);
			var burnModels = modelHandler.firehandler.Get (instance.originator as DietInstType, start, end);
			return presenter.DetermineEatTrackingForRange (instance.originator as DietInstType, eatModels, burnModels, start, end);
		}
		public IEnumerable<EntryLineVM> BurnEntries(DietInstanceVM instance, DateTime start, DateTime end)
		{
			var burnModels = modelHandler.firehandler.Get (instance.originator as DietInstType, start, end);
			return ConvertMtoVM<EntryLineVM, BurnType, BurnInfoType> (
				burnModels, 
				(e,i) => {
					var vm= presenter.GetRepresentation(e,i);
					vm.originator = e;
					return vm;
				}, 
				ee => GetInfo<BurnInfoType>(ee.infoinstanceid)
			);
		}
		public IEnumerable<TrackingInfoVM> GetBurnTracking (DietInstanceVM instance, DateTime start, DateTime end)
		{
			var eatModels = modelHandler.foodhandler.Get (instance.originator as DietInstType, start, end);
			var burnModels = modelHandler.firehandler.Get (instance.originator as DietInstType, start, end);
			return presenter.DetermineBurnTrackingForRange (instance.originator as DietInstType, eatModels, burnModels, start, end);
		}
		T GetInfo<T>(int? id) where T : BaseInfo, new()
		{
			if (!id.HasValue) return null;
			return conn.Table<T> ().Where (e => e.id == id.Value).First();
		}
		IEnumerable<O> ConvertMtoVM<O,I1,I2>(IEnumerable<I1> input, Func<I1,I2,O> convert, Func<I1,I2> findSecondInput)
		{
			foreach (I1 i in input)
				yield return convert (i,findSecondInput(i));
		}
		IEnumerable<O> ConvertMtoVM<O,I>(IEnumerable<I> input, Func<I,O> convert)
		{
			foreach (I i in input)
				yield return convert (i);
		}
		public void StartNewDiet()
		{
			PageIt (
				new List<DietWizardPage<IRO>> (modelHandler.model.DietCreationPages<IRO> (getValues.requestFactory)),
				() => {
					var di = modelHandler.StartNewDiet ();
				}
			);
		}
		public void EditDiet (DietInstanceVM dvm)
		{
			PageIt (
				new List<DietWizardPage<IRO>> (modelHandler.model.DietEditPages<IRO> (dvm.originator as DietInstType, getValues.requestFactory)),
				() => {
					modelHandler.EditDiet (dvm.originator as DietInstType);
				}
			);
		}
		void PageIt(List<DietWizardPage<IRO>> pages, Action complete, int page = 0)
		{
			getValues.GetValues(pages[page].title, new BindingList<IRO>(pages[page].valuerequests), b => {
				if(++page < pages.Count) PageIt(pages, complete, page);
				else complete();
			}, page, pages.Count);
		}
		public void RemoveDiet (DietInstanceVM dvm)
		{
			var diet = dvm.originator as DietInstType;
			int ct = 0;
			if ((ct = modelHandler.firehandler.Count () + modelHandler.foodhandler.Count ()) > 0)
				getInput.WarnConfirm (
					"That instance still has "+ct+" entries, they will be removed if you continue.",
					() => modelHandler.RemoveDiet (diet)
				);
		}
			
		public void FullEat (DietInstanceVM to) {
			Full<EatType,EatInfoType> (to.originator as DietInstType, modelHandler.model.foodcreator, modelHandler.foodhandler, "Food", "Eat", presenter.GetRepresentation);
		}
		public void FullBurn (DietInstanceVM to) {
			Full<BurnType,BurnInfoType> (to.originator as DietInstType, modelHandler.model.firecreator, modelHandler.firehandler, "Burn", "Burn", presenter.GetRepresentation);
		}
		delegate InfoLineVM CreateInfoLineVM<T>(T input);
		void Full<T,I>(DietInstType diet, IEntryCreation<T,I> creator, 
			EntryHandler<DietInstType,T,I> handler, String infoName, String entryName,
			CreateInfoLineVM<I> iconv, T editing = null) 
				where T : BaseEntry, new()
				where I : BaseInfo, new()
		{
			// get a request object for infos
			var infoRequest = getValues.requestFactory.InfoLineVMRequestor ("Select " + infoName);
			var dateRequest = getValues.requestFactory.DateRequestor ("When");
			var nameRequest = getValues.requestFactory.StringRequestor("Name");

			// Pull models, generate viewmodels - this can be async wrapped in a new Ilist class
			var fis = new List<I> (conn.Table<I> ().Where (creator.IsInfoComplete));
			var fi_vms = new List<InfoLineVM> ();
			foreach (var m in fis)
				fi_vms.Add (iconv(m));

			// triggers code in factory
			infoRequest.value = new InfoSelectValue () { choices = fi_vms, selected = -1 };
			dateRequest.value = DateTime.Now;

			// Set up for editing
			int selectedInfo = -1;
			Func<IList<IRO>> flds = () => {
				if (selectedInfo < 0)
					return editing == null ?
						creator.CreationFields (getValues.requestFactory) :
						creator.EditFields (editing, getValues.requestFactory);						
				else 
					return editing == null ?
						creator.CalculationFields (getValues.requestFactory, fis[selectedInfo]) :
						creator.EditFields (editing, getValues.requestFactory, fis[selectedInfo]);
			};
			Action editit = () => {
				if (selectedInfo < 0) {
					if (editing == null)
						handler.Add (diet);
					else
						handler.Edit (editing, diet);						
				} else {
					if (editing == null)
						handler.Add (diet, fis [selectedInfo]);
					else
						handler.Edit (editing, diet, fis [selectedInfo]);
				}
			};

			// binfy
			BindingList<IRO> requests = new BindingList<IRO> ();
			requests.Add (infoRequest.request);

			Action checkFields = () => {
				selectedInfo = infoRequest.value ==  null ? -1 : infoRequest.value.selected;
				BindingList<IRO> nrq = new BindingList<IRO>(flds());
				nrq.Insert (0, infoRequest.request);
				CycleRequests(requests, nrq);
			};
			checkFields ();
			infoRequest.changed += checkFields;

			getValues.GetValues (entryName, requests, c => {
				if (c) editit ();
				infoRequest.changed -= checkFields;
			}, 0, 1);
		}

		void CycleRequests(BindingList<IRO> exist, BindingList<IRO> want)
		{
			//remove gone
			for (int i = 0; i < exist.Count; i++)
				if (!want.Contains (exist [i]))
					exist.RemoveAt (i);
			//add new
			for (int i = 0; i < want.Count; i++)
				if (!exist.Contains (want [i]))
					exist.Insert (i, want [i]);
			//should respect ordering - doing all that so we dont remove ones that didnt change
		}

		public void RemoveEat (EntryLineVM toRemove)
		{
			modelHandler.foodhandler.Remove (toRemove.originator as EatType);
		}
		public void RemoveBurn (EntryLineVM toRemove)
		{
			modelHandler.firehandler.Remove (toRemove.originator as BurnType);
		}
			
		DietInstType getit(BaseEntry be)
		{
			var dis = conn.Table<DietInstType> ().Where ( inf => inf.id == be.dietinstanceid);
			return dis.Count() == 0 ? null : dis.First();
		}

		public void EditEat (EntryLineVM ed) {
			var eat = ed.originator as EatType;
			Full<EatType,EatInfoType> (getit(eat), modelHandler.model.foodcreator, modelHandler.foodhandler, "Food", "Eat", presenter.GetRepresentation,eat);
		}
		public void EditBurn (EntryLineVM ed) {
			var burn = ed.originator as BurnType;
			Full<BurnType,BurnInfoType> (getit(burn), modelHandler.model.firecreator, modelHandler.firehandler, "Burn", "Burn", presenter.GetRepresentation,burn);
		}
	}
}

