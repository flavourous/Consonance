using System;
using System.Collections.Generic;
using SQLite;

namespace ManyDiet
{
	// \Overview\
	// ``````````
	// We want to support different ways of dieting. This might be a calorie control diet with a daily limit, or a weekly limit,
	// or with different limits on each day of the week etc.  It might also not track calories, and instead track some other index
	// like the fiberousness of the food, and aim for a target on that.
	//
	// Strategy is to have a central repository of food items with full nutrient info.  Other data, specific to other diets would
	// exist on foreign tables.  The prime table would have nullable columns, indicating a lack of data.  Diet models could calculate
	// thier index from the prime table or insist manual assignment.
	public class BaseDietEntry
	{
		[PrimaryKey, AutoIncrement]
		public int id{get;set;}
		public int dietinstanceid{get;set;}
		public int? foodinstanceid{get;set;}
		public DateTime entryWhen {get;set;}
	}

	public interface IDietModel<T> : IDietModel where T : BaseDietEntry { }
	public interface IDietModel
	{
		DietInstance NewDiet();
		FoodInfo CalculationRequirmentsMask { get; }
		bool MeetsRequirements (FoodInfo fi);
		String[] EntryCreationFields { get; }
		bool CreateEntry (IEnumerable<Object> values, out BaseDietEntry entry);
		String[] EntryCalculationFields {get;}
		bool CalculateEntry(FoodInfo info, IEnumerable<Object> values, out BaseDietEntry result);
	}

	// just for generic polymorphism
	interface IDiet
	{
		IDietModel model {get;}
		DietInstance StartNewDiet(DateTime started);
		bool AddEntry(FoodInfo info, IEnumerable<Object> values, DietInstance diet);
		bool UpdateEntry(BaseDietEntry ent, FoodInfo fi, IEnumerable<Object> values);
		void AddEntry(DietInstance diet, BaseDietEntry ent);
		void UpdateEntry (BaseDietEntry ent);
		void RemoveEntry (BaseDietEntry tet);
		IEnumerable<BaseDietEntry> GetEntries(DietInstance diet);
		int CountEntries();
	}

	class Diet<TrackingEntryType> : IDiet where TrackingEntryType : BaseDietEntry, new()
	{
		SQLiteConnection conn;
		public IDietModel model{get;private set;}
		public Diet(SQLiteConnection conn, IDietModel model)
		{
			this.conn = conn;
			this.model = model;
			conn.CreateTable<TrackingEntryType> (CreateFlags.None);
		}

		// Get entries
		public IEnumerable<BaseDietEntry> GetEntries(DietInstance diet)
		{
			return conn.Table<TrackingEntryType> ().Where (d => d.dietinstanceid == diet.id);
		}

		// Diet instancing
		public DietInstance StartNewDiet(DateTime started)
		{
			var di = model.NewDiet ();
			di.started = started;
			di.ended = null;
			conn.Insert (di);
			return di;
		}

		//////////////////////////////////////////////////////////////////////
		// entry calculation via diet implimentation and food nutrient info //
		//////////////////////////////////////////////////////////////////////

		// return foodinfo with nulls on unrequired nutrient info. return all null when no calculation possible in any situation.
		// use foodinfo to calculate a entry.  return false if you cant due to insufficient nutrient data.
		private bool CalculateEntryWrapper(FoodInfo info, IEnumerable<Object> values, out BaseDietEntry result)
		{
			if (!model.CalculateEntry (info, values, out result))
				return false;
			result.foodinstanceid = info.id;
			return true;
		}

		////////////////////////////////////////////////////////////////////////
		// For managing entries that come from a diet specific calculation... //
		////////////////////////////////////////////////////////////////////////

		public bool AddEntry(FoodInfo info, IEnumerable<Object> values, DietInstance diet)
		{
			BaseDietEntry ent = new TrackingEntryType ();
			if (!CalculateEntryWrapper (info, values, out ent))
				return false;
			ent.dietinstanceid = diet.id;
			conn.Insert (ent);
			return true;
		}
		public bool UpdateEntry(BaseDietEntry ent, FoodInfo fi, IEnumerable<Object> values)
		{
			if (!CalculateEntryWrapper (fi, values, out ent))
				return false;
			conn.Update (ent);
			return true;
		}

		//////////////////////////////////////////////////////////////////////////////////////////////////////////
		// Creating entries without FoodInfo objects (CreateEntry might call into here when it has no equation) //
		//////////////////////////////////////////////////////////////////////////////////////////////////////////

		public void AddEntry(DietInstance diet, BaseDietEntry ent)
		{
			ent.dietinstanceid = diet.id;
			ent.foodinstanceid = null;
			conn.Insert (ent);
		}
		public void UpdateEntry(BaseDietEntry ent)
		{
			ent.foodinstanceid =  null;
			conn.Update (ent);
		}

		// Removing entries
		public void RemoveEntry(BaseDietEntry tet)
		{
			conn.Delete(tet);
		}

		// Count entries
		public int CountEntries()
		{
			return conn.Table<TrackingEntryType> ().Count ();
		}

		// return 
	}

	// when we're doing a diet here, created by diet class
	public class DietInstance
	{
		[PrimaryKey, AutoIncrement]
		public int id {get;set;}
		public string name{get;set;}
		public DateTime started{get;set;}
		public DateTime? ended{get;set;}
	}

	// implicitly per 100g of edible food.
	public class FoodInfo
	{
		// Base info
		[PrimaryKey, AutoIncrement]
		public int id {get;set;}
		public String name{ get; set; }

		// Nutrient Info
		public double? calories { get; set; }
		public double? carbohydrate { get; set; }
		public double? protein { get; set; }
		public double? fat { get; set; }
		public double? saturated_fat { get; set; }
		public double? polyunsaturated_fat { get; set; }
		public double? monounsaturated_fat { get; set; }
		public double? trans_fat { get; set; }
		public double? cholesterol { get; set; }
		public double? sodium { get; set; }
		public double? potassium { get; set; }
		public double? fiber { get; set; }
		public double? sugar { get; set; }
		public double? vitamin_a { get; set; }
		public double? vitamin_c { get; set; }
		public double? calcium { get; set; }
		public double? iron { get; set; }
	}
}

