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



	#region BASE_MODELS
	public class BaseEntry 
	{
		[PrimaryKey, AutoIncrement]
		public int id{get;set;}
		public int dietinstanceid{get;set;}
		public int? infoinstanceid{get;set;}
		public DateTime entryWhen {get;set;}
	}
	public class BaseEatEntry : BaseEntry {	}
	public class BaseBurnEntry : BaseEntry { }

	// when we're doing a diet here, created by diet class
	public class DietInstance
	{
		[PrimaryKey, AutoIncrement]
		public int id {get;set;}
		public string name{get;set;}
		public DateTime started{get;set;}
		public DateTime? ended{get;set;}
	}

	public class BaseInfo 
	{
		[PrimaryKey, AutoIncrement]
		public int id {get;set;}
		public String name{ get; set; }
	}

	public class FireInfo : BaseInfo { }
	public class FoodInfo : BaseInfo
	{
		// Amount Info
		public double per_hundred_grams { get; set; }

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
	#endregion

	#region DIET_PLAN_INTERFACES
	public interface IDietModel<Te,Tei,Tb,Tbi> : IDietModel 
		where Te  : BaseEatEntry
		where Tei : FoodInfo
		where Tb  : BaseBurnEntry
		where Tbi : FireInfo
	{ }
	/// <summary>
	/// Recommend implimenting explicitly - dreaded diamond.
	/// </summary>
	public interface IDietModel :
		IEntryCreation<BaseEatEntry,FoodInfo>, 
		IEntryCreation<BaseBurnEntry,FireInfo>
	{
		// creator for dietinstance
		DietInstance NewDiet();
	}
	public interface IEntryCreation<EntryType, InfoType>
	{
		// What named fields do I need to fully create an entry (eg eating a banana) - "kcal", "fat"
		String[] CreationFields (); 
		// Here's those values, give me the entry (ww points on eating a bananna) - broker wont attempt to remember a "item / info".
		bool Create (IEnumerable<double> values, out EntryType entry);

		// Ok, I've got info on this food (bananna, per 100g, only kcal info) - I still need "fat" and "grams"
		String[] CalculationFields (InfoType info);
		// right, heres the fat too, give me entry (broker will update that bananna info also)
		bool Calculate(InfoType info, IEnumerable<double> values, out EntryType result);

		// Ok what was the change to the foodinfo from that calculate that completes this food for you? - :here I added in those "fat"
		void CompleteInfo(ref InfoType toComplete, IEnumerable<double> values);
		// So what info you need to correctly create an info on an eg food item from scratch? "fat" "kcal" "per grams" please
		String[] InfoCreationFields();
		// ok make me an info please here's that data.
		InfoType CreateInfo (IEnumerable<double> values);
	}
	#endregion

	#region DIET_MODELS_PRESENTER_HANDLER
	// just for generic polymorphis, intermal, not used by clients creating diets. they make idietmodel
	interface IHandleDietPlanModels<TrackType, InfoType>
	{
		bool Add(InfoType info, IEnumerable<double> values, DietInstance diet);
		void Add(DietInstance diet, TrackType ent);
		bool Update(TrackType ent, InfoType fi, IEnumerable<double> values);
		void Update (TrackType ent);
		void Remove (TrackType tet);
		IEnumerable<TrackType> Get(DietInstance diet, DateTime start, DateTime end);
		int Count();
	}
	interface IDiet
	{
		IDietModel model { get; }
		DietInstance StartNewDiet(DateTime started);
	}

	// NOTE if you wanna compose the IHandleDietPlanModels for more reuse, figure out the generics in a simple case first! its not simple.
	class Diet<EatType,BurnType,EatInfoType,BurnInfoType> : IDiet, IHandleDietPlanModels<EatType,EatInfoType>, IHandleDietPlanModels<BurnType, BurnInfoType>
		where EatType : BaseEatEntry, new()
		where BurnType : BaseBurnEntry, new()
		where EatInfoType : BaseEatEntry, new()
		where BurnInfoType : BaseBurnEntry, new()
	{
		SQLiteConnection conn;
		public IDietModel model { get; private set; }
		public DietInstance StartNewDiet(DateTime started)
		{
			var di = model.NewDiet ();
			di.started = started;
			di.ended = null;
			conn.Insert (di);
			return di;
		}

		public Diet(SQLiteConnection conn, IDietModel<EatType,EatInfoType,BurnType,BurnInfoType> model)
		{
			this.conn = conn;
			this.model = model;
			conn.CreateTable<EatType> (CreateFlags.None);
		}

		#region IHandleDietPlanModels implementation
		// eaties
		bool IHandleDietPlanModels<EatType, EatInfoType>.Add (EatInfoType info, IEnumerable<double> values, DietInstance diet)
		{
			EatType ent = new EatType ();
			if (!((IEntryCreation<EatType, EatInfoType>)model).Calculate (info, values, out ent))
				return false;
			ent.dietinstanceid = diet.id;
			conn.Insert (ent);
			return true;
		}
		void IHandleDietPlanModels<EatType, EatInfoType>.Add (DietInstance diet, EatType ent)
		{
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = null;
			conn.Insert (ent);
		}
		bool IHandleDietPlanModels<EatType, EatInfoType>.Update (EatType ent, EatInfoType fi, IEnumerable<double> values)
		{
			if (!((IEntryCreation<EatType, EatInfoType>)model).Calculate (fi, values, out ent))
				return false;
			ent.infoinstanceid = fi.id;
			conn.Update (ent);
			return true;
		}
		void IHandleDietPlanModels<EatType, EatInfoType>.Update (EatType ent)
		{
			ent.infoinstanceid = null;
			conn.Update (ent);
		}
		void IHandleDietPlanModels<EatType, EatInfoType>.Remove (EatType tet)
		{
			conn.Delete<EatType> (tet.id);
		}
		IEnumerable<EatType> IHandleDietPlanModels<EatType, EatInfoType>.Get (DietInstance diet, DateTime start, DateTime end)
		{
			return conn.Table<EatType> ().Where (d => d.dietinstanceid == diet.id && d.entryWhen >= start && d.entryWhen < end);
		}
		int IHandleDietPlanModels<EatType, EatInfoType>.Count ()
		{
			return conn.Table<EatType> ().Count ();
		}

		// burnies
		bool IHandleDietPlanModels<BurnType, BurnInfoType>.Add (BurnInfoType info, IEnumerable<double> values, DietInstance diet)
		{
			BurnType ent = new BurnType ();
			if (!((IEntryCreation<BurnType, BurnInfoType>)model).Calculate (info, values, out ent))
				return false;
			ent.dietinstanceid = diet.id;
			conn.Insert (ent);
			return true;
		}
		void IHandleDietPlanModels<BurnType, BurnInfoType>.Add (DietInstance diet, BurnType ent)
		{
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = null;
			conn.Insert (ent);
		}
		bool IHandleDietPlanModels<BurnType, BurnInfoType>.Update (BurnType ent, BurnInfoType fi, IEnumerable<double> values)
		{
			if (!((IEntryCreation<BurnType, BurnInfoType>)model).Calculate (fi, values, out ent))
				return false;
			ent.infoinstanceid = fi.id;
			conn.Update (ent);
			return true;
		}
		void IHandleDietPlanModels<BurnType, BurnInfoType>.Update (BurnType ent)
		{
			ent.infoinstanceid = null;
			conn.Update (ent);
		}
		void IHandleDietPlanModels<BurnType, BurnInfoType>.Remove (BurnType tet)
		{
			conn.Delete<BurnType> (tet.id);
		}
		IEnumerable<BurnType> IHandleDietPlanModels<BurnType, BurnInfoType>.Get (DietInstance diet, DateTime start, DateTime end)
		{
			return conn.Table<BurnType> ().Where (d => d.dietinstanceid == diet.id && d.entryWhen >= start && d.entryWhen < end);
		}
		int IHandleDietPlanModels<BurnType, BurnInfoType>.Count ()
		{
			return conn.Table<BurnType> ().Count ();
		}
		#endregion
	}

	// FIXME is there not a way to get the interface to cast to one with T,I ? hmmm
	class PlanModelHandler<T, I, Tuse, Iuse> : IHandleDietPlanModels<T, I> 
	{
		// helpful wrappers
		private bool CalculateWrapper(I info, IEnumerable<double> values, out T result)
		{

			if (!(modelbranch as IEntryCreation<BaseEatEntry,FoodInfo>).Calculate (info, values, out result))
				return false;
			result.infoinstanceid = info.id;
			return true;
		}
		public bool IHandleDietPlanModels<EatType,EatInfoType>.Add(FoodInfo info, IEnumerable<double> values, DietInstance diet)
		{
			BaseEatEntry ent = new EatType ();
			if (!CalculateEatWrapper (info, values, out ent))
				return false;
			ent.dietinstanceid = diet.id;
			conn.Insert (ent);
			return true;
		}
		public bool UpdateEat(BaseEatEntry ent, FoodInfo fi, IEnumerable<Object> values)
		{
			if (!CalculateEatWrapper (fi, values, out ent))
				return false;
			conn.Update (ent);
			return true;
		}
	}
	#endregion

}

