﻿using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using SQLite;

namespace Consonance
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
		public int id{ get;set; }
		public int dietinstanceid{get;set;}
		public int? infoinstanceid{get;set;}
		public DateTime entryWhen {get;set;}
		public TimeSpan entryDur {get;set;}
		public String entryName{ get; set; }
	}
	public class BaseEatEntry : BaseEntry 
	{
		public double? info_grams { get; set; }		
	}
	public class BaseBurnEntry : BaseEntry 
	{ 
		public double? info_hours { get; set; }		
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
	public interface IDietModel<D,Te,Tei,Tb,Tbi>
		where D : DietInstance
		where Te  : BaseEatEntry
		where Tei : FoodInfo
		where Tb  : BaseBurnEntry
		where Tbi : FireInfo
	{
		// creates items
		IEntryCreation<Te,Tei> foodcreator { get; }
		IEntryCreation<Tb,Tbi> firecreator { get; }

		// creator for dietinstance
		String name { get; }
		String[] DietCreationFields();
		D NewDiet(double[] values);
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
		Expression<Func<InfoType,bool>> IsInfoComplete { get; }
	}
	#endregion

	#region DIET_MODELS_PRESENTER_HANDLER
	// just for generic polymorphis, intermal, not used by clients creating diets. they make idietmodel
	delegate void EntryCallback(BaseEntry entry);

	class DietModelAccessLayer<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType>
		where DietInstType : DietInstance, new()
		where EatType : BaseEatEntry, new()
		where EatInfoType : FoodInfo, new()
		where BurnType : BaseBurnEntry, new()
		where BurnInfoType : FireInfo, new()
	{
		readonly MyConn conn;
		public readonly IDietModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model;
		public readonly EntryHandler<DietInstType, EatType, EatInfoType> foodhandler;
		public readonly EntryHandler<DietInstType, BurnType, BurnInfoType> firehandler;
		public DietModelAccessLayer(MyConn conn, IDietModel<DietInstType, EatType,EatInfoType,BurnType,BurnInfoType> model)
		{
			this.conn = conn;
			this.model = model;
			conn.CreateTable<DietInstType> ();
			foodhandler = new EntryHandler<DietInstType, EatType, EatInfoType> (conn, model.foodcreator);
			firehandler = new EntryHandler<DietInstType, BurnType, BurnInfoType> (conn, model.firecreator);
		}
		public DietInstType StartNewDiet(String name, DateTime started, double[] values)
		{
			var di = model.NewDiet (values);
			di.started = started;
			di.ended = null;
			di.name = name;
			conn.Insert (di as DietInstType);
			return di;
		}
		public void RemoveDiet(DietInstType rem)
		{
			conn.Delete<EatType>("dietinstanceid = " + rem.id);
			conn.Delete<BurnType>("dietinstanceid = " + rem.id);
			conn.Delete<DietInstType> (rem.id);
		}
		public IEnumerable<DietInstType> GetDiets()
		{
			var tab = conn.Table<DietInstType> ();
			return tab;
		}
		public IEnumerable<DietInstType> GetDiets(DateTime st, DateTime en)
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

	}

	class EntryHandler<D, EntryType,EntryInfoType>
		where D : DietInstance, new()
		where EntryType : BaseEntry, new()
		where EntryInfoType : BaseInfo, new()
	{
		readonly SQLiteConnection conn;
		readonly IEntryCreation<EntryType, EntryInfoType> creator;
		public EntryHandler(SQLiteConnection conn, IEntryCreation<EntryType, EntryInfoType> creator)
		{
			this.conn = conn;
			this.creator = creator;

			// ensure tables are there.
			conn.CreateTable<EntryType> ();
			conn.CreateTable<EntryInfoType> ();
		}

		#region IHandleDietPlanModels implementation
		// eaties
		public bool Add (D diet, EntryInfoType info, IList<double> values, EntryCallback beforeInsert)
		{
			EntryType ent;
			if (creator.Calculate (info, values, out ent))
				return false;
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = info.id;
			beforeInsert (ent);
			conn.Insert (ent as EntryType);
			return true;
		}
		public bool Add (D diet, IList<double> values, EntryCallback beforeInsert )
		{
			EntryType ent;
			if (!creator.Create (values, out ent))
				return false;
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = null;
			beforeInsert (ent);
			conn.Insert (ent as EntryType);
			return true;
		}
		public void Remove (params EntryType[] tets)
		{
			// FIXME drop where?
			foreach(var tet in tets)
				conn.Delete<EntryType> (tet.id);
		}
		public IEnumerable<EntryType> Get (D diet, DateTime start, DateTime end)
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