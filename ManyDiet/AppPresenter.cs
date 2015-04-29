using System;
using System.Collections.Generic;
using SQLite;
using System.IO;

namespace ManyDiet
{
	public class Presenter
	{
		// Singleton logic - lazily created
		static Presenter singleton;
		public static Presenter Singleton { get { return singleton ?? (singleton = new Presenter()); } }
		SQLiteConnection conn;
		private Presenter ()
		{
			var datapath = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			if (!Directory.Exists (datapath))
				Directory.CreateDirectory (datapath);
			var maindbpath = Path.Combine (datapath, "manydiet.db");
			conn = new SQLiteConnection (maindbpath);
			conn.CreateTable<DietInstance> (CreateFlags.None);
		}

		// present app logic domain to this view.
		IView view;
		public void PresentTo(IView view)
		{
			this.view = view;
			AddDietPair ( new CalorieDiet (), new CalorieDietPresenter ());
			view.additemquick += AddItemQuick;
			view.additem += Additem;
			view.changeday += ChangeDay;
			conn.TableChanged += HandleTableChanged;
			ChangeDay (DateTime.UtcNow);
		}
		DateTime ds,de;
		void ChangeDay(DateTime to)
		{
			ds = new DateTime (to.Year, to.Month, to.Day, 0, 0, 0);
			de = new DateTime (to.Year, to.Month, to.Day + 1, 0, 0, 0);
			view.day = ds;
			view.SetEatLines (GetLines ((IEnumerable<BaseEatEntry>)currentDietModel.foodhandler.Get(currentDietInstance, ds, de)));
		}

		void Additem ()
		{
			// via a fooood
			var fis = conn.Table<FoodInfo> ().Where (f => currentDietModel.model.foodcreator.IsInfoComplete(f));
			var food = view.selectfoodview.SelectFood(fis);
			AddedItemVM mod=view.additemview.GetValues("Eat Entry", currentDietModel.model.foodcreator.CalculationFields(food));
			BaseEatEntry vm;
			currentDietModel.model.foodcreator.Calculate (food, mod.values, out vm);
			vm.entryWhen = mod.when;
			vm.entryName = mod.name;
			currentDietModel.foodhandler.Add (currentDietInstance, vm);
		}
		void AddItemQuick()
		{
			var mod = view.additemview.GetValues ("Quick Eat Entry", currentDietModel.model.foodcreator.CreationFields());
			BaseEatEntry vm;
			currentDietModel.model.foodcreator.Create (mod.values, out vm);
			vm.entryWhen = mod.when;
			vm.entryName = mod.name;
			currentDietModel.foodhandler.Add (currentDietInstance, vm);
		}

		void HandleTableChanged (object sender, NotifyTableChangedEventArgs e)
		{
			if (typeof(BaseEatEntry).IsAssignableFrom (e.Table.MappedType))
				view.SetEatLines (GetLines ((IEnumerable<BaseEatEntry>)currentDietModel.foodhandler.Get (currentDietInstance, ds, de)));
		}
		IEnumerable<EntryLineVM> GetLines(IEnumerable<BaseEatEntry> ents)
		{
			foreach (var e in ents)
				yield return currentDietPresenter.GetLineRepresentation (e);
		}
		DietInstance currentDietInstance
		{
			get 
			{
				var cdis = conn.Table<DietInstance> ().Where (d => d.started <= DateTime.Now && (d.ended == null || d.ended > DateTime.Now));
				if(cdis.Count() == 0)
				{
					var vals = view.additemview.GetValues ("New Diet", currentDietModel.model.DietCreationFields ());
					currentDietModel.StartNewDiet (DateTime.Now, vals.values);
				}
				return	cdis.First ();
			}
		}

		IDiet currentDietModel;
		IDietPresenter currentDietPresenter;
		void AddDietPair<D,E,Ei,B,Bi>(IDietModel<D,E,Ei,B,Bi> dietModel, DietPresenter<D,E,Ei,B,Bi> dietPresenter)
			where D : DietInstance
			where E  : BaseEatEntry,new() 
			where Ei : FoodInfo,new() 
			where B  : BaseBurnEntry,new() 
			where Bi : FireInfo,new() 
		{
			currentDietModel = new Diet<D,E,Ei,B,Bi> (conn, dietModel);
			currentDietPresenter = dietPresenter;
		}
	}

	/// <summary>
	/// definition on the application view
	/// </summary>
	public interface IView
	{
		void SetEatLines(IEnumerable<EntryLineVM> lineitems);
		event Action<DateTime> changeday;
		DateTime day { set; }
		event Action additem;
		event Action additemquick;
		IAddItemView additemview { get; }
		ISelectFoodView selectfoodview {get;}
	}
	public enum AddedItemVMDefaults { Name =1, When = 2 };
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
	public interface IAddItemView
	{
		AddedItemVM GetValues (String title, IEnumerable<String> names, AddedItemVMDefaults defaultUse = AddedItemVMDefaults.Name | AddedItemVMDefaults.When);
	}
	public interface ISelectFoodView
	{
		FoodInfo SelectFood (IEnumerable<FoodInfo> foods);
	}
}

