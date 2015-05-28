using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using SQLite;

namespace ManyDiet
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
	// basic workings of presenter and VMs, to be rehomed.
	public class EntryLineVM
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
	public interface IDietPresenter<DietInstType, EatType, EatInfoType, BurnType, BurnInfoType>
		where DietInstType : DietInstance, new()
		where EatType : BaseEatEntry, new()
		where EatInfoType : FoodInfo, new()
		where BurnType : BaseBurnEntry, new()
		where BurnInfoType : FireInfo, new()
	{
		EntryLineVM GetRepresentation (EatType entry, EatInfoType entryInfo);
		EntryLineVM GetRepresentation (BurnType entry, BurnInfoType entryInfo);

		SelectableItemVM GetRepresentation (EatInfoType info);
		SelectableItemVM GetRepresentation (BurnInfoType info);

		DietInstanceVM GetRepresentation (DietInstType entry);
	}

	interface IAbstractedDiet
	{
		String dietName { get; }
		IEnumerable<DietInstanceVM> Instances();
		IEnumerable<EntryLineVM>  EatEntries (DietInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<EntryLineVM>  BurnEntries(DietInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<TrackingInfo> GetEatTracking (DietInstanceVM instance, DateTime start, DateTime end);
		IEnumerable<TrackingInfo> GetBurnTracking (DietInstanceVM instance, DateTime start, DateTime end);
		void StartNewDiet();
		void RemoveDiet (DietInstanceVM dvm);
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
	class DietPresentationAbstractionHandler <DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> : IAbstractedDiet
		where DietInstType : DietInstance, new()
		where EatType : BaseEatEntry, new()
		where EatInfoType : FoodInfo, new()
		where BurnType : BaseBurnEntry, new()
		where BurnInfoType : FireInfo, new()
	{
		readonly IUserInput getInput;
		readonly IDietPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter;
		readonly DietModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> modelHandler;
		readonly MyConn conn;
		public DietPresentationAbstractionHandler(
			IUserInput getInput, 
			MyConn conn,
			IDietModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model,
			IDietPresenter<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> presenter
		)
		{
			// store objects
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
				yield return rep;
			}
		}
		public IEnumerable<EntryLineVM> EatEntries(DietInstanceVM instance, DateTime start, DateTime end)
		{
			var eatModels = modelHandler.foodhandler.Get (instance.originator as DietInstType, start, end);
			return ConvertMtoVM<EntryLineVM, EatType, EatInfoType> (eatModels, presenter.GetRepresentation, ee => GetInfo<EatInfoType>(ee.infoinstanceid));
		}
		public IEnumerable<TrackingInfo> GetEatTracking (DietInstanceVM instance, DateTime start, DateTime end)
		{
			var eatModels = modelHandler.foodhandler.Get (instance.originator as DietInstType, start, end);
			var burnModels = modelHandler.firehandler.Get (instance.originator as DietInstType, start, end);
			return modelHandler.model.DetermineEatTrackingForRange (eatModels, burnModels, start, end);
		}
		public IEnumerable<EntryLineVM> BurnEntries(DietInstanceVM instance, DateTime start, DateTime end)
		{
			var burnModels = modelHandler.firehandler.Get (instance.originator as DietInstType, start, end);
			return ConvertMtoVM<EntryLineVM, BurnType, BurnInfoType> (burnModels, presenter.GetRepresentation, ee => GetInfo<BurnInfoType>(ee.infoinstanceid));
		}
		public IEnumerable<TrackingInfo> GetBurnTracking (DietInstanceVM instance, DateTime start, DateTime end)
		{
			var eatModels = modelHandler.foodhandler.Get (instance.originator as DietInstType, start, end);
			var burnModels = modelHandler.firehandler.Get (instance.originator as DietInstType, start, end);
			return modelHandler.model.DetermineBurnTrackingForRange (eatModels, burnModels, start, end);
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
			getInput.GetValues ("New " + dietName, modelHandler.model.DietCreationFields(), aivm => {
				var di = modelHandler.StartNewDiet(aivm.name, aivm.when, aivm.values);
			}); // defaults to getting name and date.
		}
		public void RemoveDiet (DietInstanceVM dvm)
		{
			modelHandler.RemoveDiet (dvm.originator as DietInstType);
		}
	}
		

	class IndexEnumerator<T> : IEnumerator<T>
	{
		IReadOnlyList<T> items;
		int curr = 0;
		public IndexEnumerator(IReadOnlyList<T> items)
		{
			this.items = items;
		}

		#region IEnumerator implementation
		public bool MoveNext () { return ++curr < items.Count; }
		public void Reset () { curr = 0; }
		object System.Collections.IEnumerator.Current { get { return items [curr]; } }
		#endregion

		#region IDisposable implementation
		public void Dispose () { }
		#endregion

		#region IEnumerator implementation
		T IEnumerator<T>.Current { get { return items [curr]; } }
		#endregion
	}
	delegate SelectableItemVM CreateSelectableVM<T>(T item);
	class SelectVMListDecorator<T> : IReadOnlyList<SelectableItemVM> where T : BaseInfo
	{
		IList<T> items;
		CreateSelectableVM<T> creator;
		public SelectVMListDecorator(IList<T> items, CreateSelectableVM<T> creator)
		{
			this.items = items;
			this.creator = creator;
		}

		#region IEnumerable implementation
		public IEnumerator<SelectableItemVM> GetEnumerator ()
		{
			return new IndexEnumerator<SelectableItemVM> (this);
		}
		#endregion
		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return new IndexEnumerator<SelectableItemVM> (this);
		}
		#endregion
		#region IReadOnlyList implementation
		public SelectableItemVM this [int index] {
			get {
				return creator (items [index]);
			}
		}
		#endregion
		#region IReadOnlyCollection implementation
		public int Count {
			get {
				return items.Count;
			}
		}
		#endregion
	}
}

