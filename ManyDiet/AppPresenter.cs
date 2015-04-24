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

		public FoodInfo FindFood(int? id)
		{
			if (!id.HasValue)
				return null;
			var res = new List<FoodInfo> (conn.Table<FoodInfo> ().Where (fi => fi.id == id));
			if (res.Count == 0)
				return null;
			return res [0];
		}

		// present app logic domain to this view.
		IView view;
		public void PresentTo(IView view)
		{
			this.view = view;
			AddDietPair ( new CalorieDiet (), new CalorieDietPresenter ());
			view.additemquick += AddItemQuick;
			view.additem += Additem;
			view.SetLineEntries (GetLines (currentDietModel.GetEntries (currentDietInstance)));
			conn.TableChanged += HandleTableChanged;
		}

		void Additem ()
		{
			// via a fooood
			var fis = conn.Table<FoodInfo> ().Where (f => currentDietModel.model.MeetsRequirements (f));
			var food = view.selectfoodview.SelectFood(fis);
			AddedItemVM mod=view.additemview.GetValues(currentDietModel.model.EntryCalculationFields);
			BaseDietEntry vm;
			currentDietModel.model.CalculateEntry (food, mod.values, out vm);
			vm.entryWhen = mod.when;
			currentDietModel.AddEntry (currentDietInstance, vm);
		}
		void AddItemQuick()
		{
			var mod = view.additemview.GetValues (currentDietModel.model.EntryCreationFields);
			BaseDietEntry vm;
			currentDietModel.model.CreateEntry (mod.values, out vm);
			vm.entryWhen = mod.when;
			currentDietModel.AddEntry (currentDietInstance, vm);
		}

		void HandleTableChanged (object sender, NotifyTableChangedEventArgs e)
		{
			if (typeof(BaseDietEntry).IsAssignableFrom (e.Table.MappedType))
				view.SetLineEntries (GetLines (currentDietModel.GetEntries (currentDietInstance)));
		}
		IEnumerable<EatEntryLineVM> GetLines(IEnumerable<BaseDietEntry> ents)
		{
			foreach (var e in ents)
				yield return currentDietPresenter.GetLineRepresentation (e);
		}
		DietInstance currentDietInstance
		{
			get 
			{
				var cdis = conn.Table<DietInstance> ().Where (d => d.started <= DateTime.Now && (d.ended == null || d.ended > DateTime.Now));
				return cdis.Count() == 0 ?
					currentDietModel.StartNewDiet (DateTime.Now) :
					cdis.First ();
			}
		}

		IDiet currentDietModel;
		IDietPresenter currentDietPresenter;
		void AddDietPair<T>(IDietModel<T> dietModel, IDietPresenter<T> dietPresenter) where T : BaseDietEntry, new()
		{
			currentDietModel = new Diet<T> (conn, dietModel);
			currentDietPresenter = dietPresenter;
		}
	}

	/// <summary>
	/// definition on the application view
	/// </summary>
	public interface IView
	{
		void SetLineEntries(IEnumerable<EatEntryLineVM> lineitems);
		event Action additem;
		event Action additemquick;
		IAddItemView additemview { get; }
		ISelectFoodView selectfoodview {get;}
	}
	public class AddedItemVM
	{
		public readonly DateTime when;
		public readonly IEnumerable<Object> values;
		public AddedItemVM(IEnumerable<Object> values, DateTime when)
		{
			this.values = values;
			this.when = when;
		}
	}
	public interface IAddItemView
	{
		AddedItemVM GetValues(IEnumerable<String> names);
	}
	public interface ISelectFoodView
	{
		FoodInfo SelectFood (IEnumerable<FoodInfo> foods);
	}
}

