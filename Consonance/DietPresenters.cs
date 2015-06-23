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
	public class InfoLineVM : OriginatorVM
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
		IEnumerable<InfoLineVM> EatInfos (bool onlycomplete);
		IEnumerable<InfoLineVM> BurnInfos (bool onlycomplete);
		event DietVMChangeEventHandler ViewModelsChanged;
	}
	interface INotSoAbstractedDiet<IRO>
	{
		// entry ones
		void AddEat (DietInstanceVM diet, IValueRequestBuilder<IRO> bld);
		void RemoveEat (EntryLineVM evm);
		void EditEat (EntryLineVM evm, IValueRequestBuilder<IRO> bld);
		void AddEatInfo (IValueRequestBuilder<IRO> bld);
		void RemoveEatInfo (InfoLineVM ivm);
		void EditEatInfo (InfoLineVM ivm, IValueRequestBuilder<IRO> bld);
		void AddBurn (DietInstanceVM diet, IValueRequestBuilder<IRO> bld);
		void RemoveBurn (EntryLineVM evm);
		void EditBurn (EntryLineVM evm, IValueRequestBuilder<IRO> bld);
		void AddBurnInfo (IValueRequestBuilder<IRO> bld);
		void RemoveBurnInfo (InfoLineVM ivm);
		void EditBurnInfo (InfoLineVM ivm, IValueRequestBuilder<IRO> bld);
	}
	enum DietVMChangeType { None, Instances, EatEntries, BurnEntries, EatInfos, BurnInfos };
	class DietVMChangeEventArgs
	{
		public DietVMChangeType changeType;
	}
	delegate void DietVMChangeEventHandler(IAbstractedDiet sender, DietVMChangeEventArgs args);
	delegate DietInstanceVM DVMPuller();
	/// <summary>
	/// This contains all the code which the app would want to do to get viewmodels out of
	/// implimentations, and retains the generic definitions therefore.
	/// But it does stuff that the IDietPresenter shouldnt have to impliment since it's specific to the 
	/// app logic, not problem logic.
	/// 
	/// As such, it should obey a non-generic contract that the AppPresenter can easily consume and
	/// query for data.
	/// </summary>
	class DietPresentationAbstractionHandler <IRO, DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> : IAbstractedDiet, INotSoAbstractedDiet<IRO>
		where DietInstType : DietInstance, new()
		where EatType : BaseEatEntry, new()
		where EatInfoType : FoodInfo, new()
		where BurnType : BaseBurnEntry, new()
		where BurnInfoType : FireInfo, new()
	{
		readonly IValueRequestBuilder<IRO> instanceBuilder;
		readonly IUserInput getInput;
		readonly IDietPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter;
		readonly DietModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> modelHandler;
		readonly MyConn conn;
		public DietPresentationAbstractionHandler(
			IValueRequestBuilder<IRO> instanceBuilder,
			IUserInput getInput,
			MyConn conn,
			IDietModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model,
			IDietPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter
		)
		{
			// store objects
			this.instanceBuilder = instanceBuilder;
			this.getInput = getInput;
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
			if (e.Table.MappedType.Equals (typeof(EatInfoType)))
				changeType = DietVMChangeType.EatInfos;
			if (e.Table.MappedType.Equals (typeof(BurnInfoType)))
				changeType = DietVMChangeType.BurnInfos;
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
		public IEnumerable<InfoLineVM> EatInfos(bool complete)
		{
			// Pull models, generate viewmodels - this can be async wrapped in a new Ilist class
			var fis = conn.Table<EatInfoType> ();
			if(complete) fis=fis.Where (modelHandler.model.foodcreator.IsInfoComplete);
			foreach (var m in fis)
			{
				var vm = presenter.GetRepresentation (m);
				vm.originator = m;
				yield return vm;
			}
		}
		public IEnumerable<InfoLineVM> BurnInfos(bool complete)
		{
			// Pull models, generate viewmodels - this can be async wrapped in a new Ilist class
			var fis = conn.Table<BurnInfoType> ();
			if (complete) fis = fis.Where (modelHandler.model.firecreator.IsInfoComplete);
			foreach (var m in fis)
			{
				var vm = presenter.GetRepresentation (m);
				vm.originator = m;
				yield return vm;
			}
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
				new List<DietWizardPage<IRO>> (modelHandler.model.DietCreationPages<IRO> (instanceBuilder.requestFactory)),
				() => modelHandler.StartNewDiet()
			);
		}
		public void EditDiet (DietInstanceVM dvm)
		{
			PageIt (
				new List<DietWizardPage<IRO>> (modelHandler.model.DietEditPages<IRO> (dvm.originator as DietInstType, instanceBuilder.requestFactory)),
				() => modelHandler.EditDiet (dvm.originator as DietInstType)
			);
		}
		void PageIt(List<DietWizardPage<IRO>> pages, Action complete, int page = 0)
		{
			instanceBuilder.GetValues(pages[page].title, new BindingList<IRO>(pages[page].valuerequests), b => {
				if(++page < pages.Count) PageIt(pages, complete, page);
				else if(b) complete();
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

		public void AddEat(DietInstanceVM to, IValueRequestBuilder<IRO> bld)
		{
			Full<EatType,EatInfoType> (to.originator as DietInstType, modelHandler.model.foodcreator, modelHandler.foodhandler, "Food", "Eat", EatInfos(false), bld);
		}
		public void AddBurn(DietInstanceVM to, IValueRequestBuilder<IRO> bld)
		{
			Full<BurnType,BurnInfoType> (to.originator as DietInstType, modelHandler.model.firecreator, modelHandler.firehandler, "Burn", "Burn", BurnInfos(false), bld);
		}



		void Full<T,I>(DietInstType diet, IEntryCreation<T,I> creator, 
			EntryHandler<DietInstType,T,I> handler, String infoName, String entryName,
			IEnumerable<InfoLineVM> infos, IValueRequestBuilder<IRO> getValues, T editing = null) 
				where T : BaseEntry, new()
				where I : BaseInfo, new()
		{
			// reset to start
			creator.ResetRequests();

			// get a request object for infos
			var infoRequest = getValues.requestFactory.InfoLineVMRequestor ("Select " + infoName);

			// triggers code in factory
			var fis = new List<InfoLineVM>(infos);
			infoRequest.value = new InfoSelectValue () { choices = fis, selected = -1 };

			// Set up for editing
			int selectedInfo = -1;
			Func<IList<IRO>> flds = () => {
				if (selectedInfo < 0)
					return editing == null ?
						creator.CreationFields (getValues.requestFactory) :
						creator.EditFields (editing, getValues.requestFactory);						
				else 
					return editing == null ?
						creator.CalculationFields (getValues.requestFactory, fis[selectedInfo].originator as I) :
						creator.EditFields (editing, getValues.requestFactory, fis[selectedInfo].originator as I);
			};
			Action editit = () => {
				if (selectedInfo < 0) {
					if (editing == null)
						handler.Add (diet);
					else
						handler.Edit (editing, diet);						
				} else {
					if (editing == null)
						handler.Add (diet, fis[selectedInfo].originator as I);
					else
						handler.Edit (editing, diet, fis[selectedInfo].originator as I);
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

		public void EditEat (EntryLineVM ed,IValueRequestBuilder<IRO> bld) {
			var eat = ed.originator as EatType;
			Full<EatType,EatInfoType> (getit(eat), modelHandler.model.foodcreator, modelHandler.foodhandler, "Food", "Eat", EatInfos(true),bld,eat);
		}
		public void EditBurn (EntryLineVM ed,IValueRequestBuilder<IRO> bld) {
			var burn = ed.originator as BurnType;
			Full<BurnType,BurnInfoType> (getit(burn), modelHandler.model.firecreator, modelHandler.firehandler, "Burn", "Burn", BurnInfos(true), bld,burn);
		}

		public void AddEatInfo(IValueRequestBuilder<IRO> bld)
		{
			DoInfo<EatInfoType> ("Create a Food", modelHandler.model.foodcreator, modelHandler.foodhandler, bld);
		}
		public void EditEatInfo(InfoLineVM ivm, IValueRequestBuilder<IRO> bld)
		{
			DoInfo<EatInfoType> ("Edit Food", modelHandler.model.foodcreator, modelHandler.foodhandler, bld, ivm.originator as EatInfoType);
		}
		public void RemoveEatInfo(InfoLineVM ivm)
		{
			modelHandler.foodhandler.Remove (ivm.originator as EatInfoType);
		}

		void DoInfo<I>(String title, IInfoCreation<I> creator, IInfoHandler<I> handler, IValueRequestBuilder<IRO> builder, I toEdit = null)  where I : BaseInfo, new()
		{
			bool editing = toEdit != null;
			builder.GetValues (
				title,
				creator.InfoFields<IRO>(builder.requestFactory, toEdit),
				success => {
					if(editing) handler.Edit(toEdit);
					else handler.Add();
				},
				0,
				1);
		}

		public void AddBurnInfo(IValueRequestBuilder<IRO> bld)
		{
			DoInfo<BurnInfoType> ("Create a Fire", modelHandler.model.firecreator, modelHandler.firehandler, bld);
		}
		public void EditBurnInfo(InfoLineVM ivm, IValueRequestBuilder<IRO> bld)
		{
			DoInfo<BurnInfoType> ("Edit Food", modelHandler.model.firecreator, modelHandler.firehandler, bld, ivm.originator as BurnInfoType);
		}
		public void RemoveBurnInfo(InfoLineVM ivm)
		{
			modelHandler.firehandler.Remove (ivm.originator as BurnInfoType);
		}
	}
}

