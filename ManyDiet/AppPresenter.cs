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

			// commanding...
			view.addeatitemquick += AddEatItemQuick;
			view.addeatitem += AddEatItem;
			view.removeeatitem += RemEatItem;
			view.addburnitemquick += AddBurnItemQuick;
			view.addburnitem += AddBurnItem;
			view.removeburnitem += RemBurnItem;
			view.changeday += ChangeDay;

			conn.TableChanged += HandleTableChanged;
			ChangeDay (DateTime.UtcNow);
		}
		DateTime ds,de;
		void ChangeDay(DateTime to)
		{
			var dd = GetCurrentDietDomain ();
			ds = new DateTime (to.Year, to.Month, to.Day, 0, 0, 0);
			de = ds.AddDays (1);
			view.day = ds;
			view.SetEatLines (GetLines ((IEnumerable<BaseEatEntry>)dd.broker.foodhandler.Get(dd.instance, ds, de),dd.presenter));
			view.SetBurnLines (GetLines ((IEnumerable<BaseBurnEntry>)dd.broker.firehandler.Get(dd.instance, ds, de),dd.presenter));
		}

		void RemEatItem(EntryLineVM line)
		{
			var dd = GetCurrentDietDomain ();
			dd.broker.foodhandler.Remove (eatRefIndex [line]);
		}
		void RemBurnItem(EntryLineVM line)
		{
			var dd = GetCurrentDietDomain ();
			dd.broker.firehandler.Remove (burnRefIndex [line]);
		}

		void AddEatItem ()
		{
			var dd = GetCurrentDietDomain ();
			var fis = new List<FoodInfo> (conn.Table<FoodInfo> ().Where (f => dd.broker.model.foodcreator.IsInfoComplete (f)));
			var foodidx = view.SelectInfo (new SelectVMListDecorator<FoodInfo> (fis,dd.presenter.GetRepresentation));
			var food = fis [foodidx];
			AddedItemVM mod=view.GetValues("Eat Entry", dd.broker.model.foodcreator.CalculationFields(food));
			dd.broker.foodhandler.Add (dd.instance, mod.values, vm => {
				vm.entryWhen = mod.when;
				vm.entryName = mod.name;
			});
		}
		void AddEatItemQuick()
		{
			var dd = GetCurrentDietDomain ();
			var mod = view.GetValues ("Quick Eat Entry", dd.broker.model.foodcreator.CreationFields());
			dd.broker.foodhandler.Add (dd.instance, mod.values, vm => {
				vm.entryWhen = mod.when;
				vm.entryName = mod.name;
			});
		}
		void AddBurnItem ()
		{
			var dd = GetCurrentDietDomain ();
			var fis = new List<FireInfo> (conn.Table<FireInfo> ().Where (f => dd.broker.model.firecreator.IsInfoComplete (f)));
			var idx = view.SelectInfo (new SelectVMListDecorator<FireInfo> (fis, dd.presenter.GetRepresentation));
			var fire = fis [idx];
			AddedItemVM mod=view.GetValues("Burn Entry", dd.broker.model.firecreator.CalculationFields(fire));
			dd.broker.firehandler.Add (dd.instance, fire, mod.values, vm => {
				vm.entryWhen = mod.when;
				vm.entryName = mod.name;
			});
		}
		void AddBurnItemQuick()
		{
			var dd = GetCurrentDietDomain ();
			var mod = view.GetValues ("Quick Burn Entry", dd.broker.model.firecreator.CreationFields());
			dd.broker.firehandler.Add (dd.instance, mod.values, vm => {
				vm.entryWhen = mod.when;
				vm.entryName = mod.name;
			});
		}

		void HandleTableChanged (object sender, NotifyTableChangedEventArgs e)
		{
			var dd = GetCurrentDietDomain ();
			if (typeof(BaseEatEntry).IsAssignableFrom (e.Table.MappedType))
				view.SetEatLines (GetLines ((IEnumerable<BaseEatEntry>)dd.broker.foodhandler.Get (dd.instance, ds, de),dd.presenter));
			if (typeof(BaseBurnEntry).IsAssignableFrom (e.Table.MappedType))
				view.SetBurnLines (GetLines ((IEnumerable<BaseBurnEntry>)dd.broker.firehandler.Get (dd.instance, ds, de), dd.presenter));
		}
		Dictionary<EntryLineVM, BaseEatEntry> eatRefIndex = new Dictionary<EntryLineVM, BaseEatEntry>();
		IEnumerable<EntryLineVM> GetLines(IEnumerable<BaseEatEntry> ents, IDietPresenter dp)
		{
			eatRefIndex.Clear ();
			foreach (var e in ents) {
				var vm = dp.GetRepresentation (e);
				eatRefIndex[vm] = e;
				yield return vm;
			}
		}
		Dictionary<EntryLineVM,BaseBurnEntry> burnRefIndex = new Dictionary<EntryLineVM, BaseBurnEntry>();
		IEnumerable<EntryLineVM> GetLines(IEnumerable<BaseBurnEntry> ents, IDietPresenter dp)
		{
			burnRefIndex.Clear ();
			foreach (var e in ents) {
				var vm = dp.GetRepresentation (e);
				burnRefIndex[vm] = e;
				yield return vm;
			}
		}
			
		// ** helpers for managing active diets ** //
		class CDIThings 
		{
			public DietInstance instance;
			public IDiet broker;
			public IDietPresenter presenter;
		}
		DietInstance GetCurrentDietInstance()
		{
			var cdis = conn.Table<DietInstance> ().Where (d => d.started <= DateTime.Now && (d.ended == null || d.ended > DateTime.Now));
			if(cdis.Count() == 0) return null;
			return	cdis.First ();
		}
		CDIThings GetDietDomain (DietInstance di)
		{
			var mods = diets.Find (kv => kv.Key.model.IsDietInstance (di));
			return new CDIThings () { instance = di, broker = mods.Key, presenter = mods.Value };
		}
		CDIThings GetCurrentDietDomain()
		{
			return GetDietDomain (GetCurrentDietInstance ());
		}
		// ** ********************************* ** //

		KVPList<IDiet,IDietPresenter> diets = new KVPList<IDiet, IDietPresenter>();
		void AddDietPair<D,E,Ei,B,Bi>(IDietModel<D,E,Ei,B,Bi> dietModel, DietPresenter<D,E,Ei,B,Bi> dietPresenter)
			where D : DietInstance
			where E  : BaseEatEntry,new() 
			where Ei : FoodInfo,new() 
			where B  : BaseBurnEntry,new() 
			where Bi : FireInfo,new() 
		{
			diets.Add (new Diet<D,E,Ei,B,Bi> (conn, dietModel), dietPresenter);
		}
	}

	/// <summary>
	/// definition on the application view
	/// </summary>
	public interface IView
	{
		void SetEatLines(IEnumerable<EntryLineVM> lineitems);
		void SetBurnLines(IEnumerable<EntryLineVM> lineitems);
		event Action<DateTime> changeday;
		DateTime day { set; }

		// eat
		event Action addeatitem;
		event Action addeatitemquick;
		event Action<EntryLineVM> removeeatitem;

		// burn
		event Action addburnitem;
		event Action addburnitemquick;
		event Action<EntryLineVM> removeburnitem;

		// plan (diet managment)

		// User Input
		AddedItemVM GetValues (String title, IEnumerable<String> names, AddedItemVMDefaults defaultUse = AddedItemVMDefaults.Name | AddedItemVMDefaults.When);
		int SelectInfo (IReadOnlyList<SelectableItemVM> foods);
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
	public class SelectableItemVM {
		public String name;
	}
}

