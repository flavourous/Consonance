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
		public TimeSpan entryDur {get;set;}
		public String entryName{ get; set; }
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

	public class FireInfo : BaseInfo 
	{
		public double per_hour {get;set;}
		public double? calories {get;set;}
	}
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
	public interface IDietModel<D,Te,Tei,Tb,Tbi> : IDietModel 
		where D : DietInstance
		where Te  : BaseEatEntry
		where Tei : FoodInfo
		where Tb  : BaseBurnEntry
		where Tbi : FireInfo
	{ }
	/// <summary>
	/// Recommend implimenting explicitly - dreaded diamond.
	/// </summary>
	public interface IDietModel 
	{
		// creator for dietinstance
		String name { get; }
		String[] DietCreationFields();
		DietInstance NewDiet(double[] values);
		bool IsDietInstance (DietInstance di);
		IEntryCreation<BaseEatEntry,FoodInfo> foodcreator { get; }
		IEntryCreation<BaseBurnEntry,FireInfo> firecreator { get; }
	}
	public interface IEntryCreation<EntryType, InfoType>
	{
		// What named fields do I need to fully create an entry (eg eating a banana) - "kcal", "fat"
		String[] CreationFields (); 
		// Here's those values, give me the entry (ww points on eating a bananna) - broker wont attempt to remember a "item / info".
		bool Create (IList<double> values, out EntryType entry);

		// Ok, I've got info on this food (bananna, per 100g, only kcal info) - I still need "fat" and "grams"
		String[] CalculationFields (InfoType info);
		// right, heres the fat too, give me entry (broker will update that bananna info also)
		bool Calculate(InfoType info, IList<double> values, out EntryType result);

		// Ok what was the change to the foodinfo from that calculate that completes this food for you? - :here I added in those "fat"
		void CompleteInfo(ref InfoType toComplete, IList<double> values);
		// So what info you need to correctly create an info on an eg food item from scratch? "fat" "kcal" "per grams" please
		String[] InfoCreationFields();
		// ok make me an info please here's that data.
		InfoType CreateInfo (IList<double> values);
		// ok is this info like complete for your diety? yes. ffs.
		bool IsInfoComplete (InfoType info);
	}
	#endregion

	#region DIET_MODELS_PRESENTER_HANDLER
	// just for generic polymorphis, intermal, not used by clients creating diets. they make idietmodel
	delegate void EntryCallback(BaseEntry entry);
	interface IHandleDietPlanModels
	{
		bool Add(DietInstance diet, BaseInfo info, IList<double> values, EntryCallback beforeInsert);
		bool Add(DietInstance diet, IList<double> values, EntryCallback beforeInsert);
		void Remove (BaseEntry tet);
		IEnumerable<BaseEntry> Get(DietInstance diet, DateTime start, DateTime end);
		int Count();
	}
	interface IDiet
	{
		IDietModel model { get; }
		DietInstance StartNewDiet(DateTime started, double[] values);
		IEnumerable<DietInstance> GetDiets (DateTime st, DateTime en);
		IEnumerable<DietInstance> GetDiets ();
		IHandleDietPlanModels foodhandler { get; }
		IHandleDietPlanModels firehandler { get; }
	}

	class Diet<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> : IDiet
		where DietInstType : DietInstance, new()
		where EatType : BaseEatEntry, new()
		where EatInfoType : FoodInfo, new()
		where BurnType : BaseBurnEntry, new()
		where BurnInfoType : FireInfo, new()
	{
		readonly SQLiteConnection conn;
		public IDietModel model { get; private set; }
		public DietInstance StartNewDiet(DateTime started, double[] values)
		{
			var di = model.NewDiet (values);
			di.started = started;
			di.ended = null;
			conn.Insert (di);
			return di;
		}
		public IEnumerable<DietInstance> GetDiets()
		{
			var tab = conn.Table<DietInstType> ();
			return tab;
		}
		public IEnumerable<DietInstance> GetDiets(DateTime st, DateTime en)
		{
			//  | is i1/i2 \ is st/en		i1>st	i2>en	i1>en	i2>st		WANTED
			// |    \    \     |		= 	false	true	false	true		true		
			// \  |    \   |			=	true	true	false	true		true
			// |    \    |   \ 			=	false	false	false	true		true
			// \    |  |       \		=	true	false	true	true		true
			// \  \   |   |				=	true	true	true	true		false
			// |  |   \   \				=	false	false	false	false		false

			// what works is (i1>st ^ i2 > en) | (i1>en ^ i2>st)

			return conn.Table<DietInstType> ().Where (
				d => ((d.ended == null && (st >= d.started || en >= d.started))
					|| ((d.started >= st ^ d.ended >= en) || (d.started >= en ^ d.ended >= st))));
		}
		public IHandleDietPlanModels foodhandler { get; private set; }
		public IHandleDietPlanModels firehandler { get; private set; }
		public Diet(SQLiteConnection conn, IDietModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model)
		{
			this.conn = conn;
			this.model = model;
			conn.CreateTable<DietInstType> ();

			// Bloody diamond...
			var eatcreator = (IEntryCreation<BaseEntry, BaseInfo>)(model as IEntryCreation<BaseEatEntry, FoodInfo>);
			var burncreator = (IEntryCreation<BaseEntry, BaseInfo>)(model as IEntryCreation<BaseBurnEntry, FireInfo>);

			foodhandler = new EntryHandler<EatType, EatInfoType> (conn, eatcreator);
			firehandler = new EntryHandler<BurnType, BurnInfoType> (conn, burncreator);
		}
	}

	class EntryHandler<EntryType,EntryInfoType> : IHandleDietPlanModels
		where EntryType : BaseEntry, new()
		where EntryInfoType : BaseInfo, new()
	{
		readonly SQLiteConnection conn;
		readonly IEntryCreation<BaseEntry, BaseInfo> creator;
		public EntryHandler(SQLiteConnection conn, IEntryCreation<BaseEntry, BaseInfo> creator)
		{
			this.conn = conn;
			this.creator = creator;

			// ensure tables are there.
			conn.CreateTable<EntryType> ();
			conn.CreateTable<EntryInfoType> ();
		}

		#region IHandleDietPlanModels implementation
		// eaties
		public bool Add (DietInstance diet, BaseInfo info, IList<double> values, EntryCallback beforeInsert)
		{
			EntryType ent = new EntryType ();
			BaseEntry ent_cast = ent;
			if (creator.Calculate (info, values, out ent_cast))
				return false;
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = info.id;
			beforeInsert (ent);
			conn.Insert (ent);
			return true;
		}
		public bool Add (DietInstance diet, IList<double> values, EntryCallback beforeInsert )
		{
			EntryType ent = new EntryType ();
			BaseEntry ent_cast = ent;
			if (!creator.Create (values, out ent_cast))
				return false;
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = null;
			beforeInsert (ent);
			conn.Insert (ent);
			return true;
		}
		public void Remove (BaseEntry tet)
		{
			conn.Delete<EntryType> (tet.id);
		}
		public IEnumerable<BaseEntry> Get (DietInstance diet, DateTime start, DateTime end)
		{
			return conn.Table<EntryType> ().Where (d => d.dietinstanceid == diet.id && d.entryWhen >= start && d.entryWhen < end);
		}
		public int Count ()
		{
			return conn.Table<EntryType> ().Count ();
		}

		#endregion
	}
	#endregion
}