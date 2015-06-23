using System;
using System.ComponentModel;
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
		public TimeSpan entryDur { get; set; }
		public String entryName{ get; set; }
	}
	public class BaseEatEntry : BaseEntry 
	{
		public double? info_grams { get; set; }		
	}
	public class BaseBurnEntry : BaseEntry 
	{ 
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
		IEnumerable<DietWizardPage<T>> DietCreationPages<T>(IValueRequestFactory<T> factory);
		IEnumerable<DietWizardPage<T>> DietEditPages<T>(D editing, IValueRequestFactory<T> factory);
		D NewDiet();
		void EditDiet (D toEdit);
	}
	public class DietWizardPage<T>
	{
		public readonly String title;
		public readonly BindingList<T> valuerequests;
		public DietWizardPage( String title, BindingList<T> req)
		{
			this.title = title;
			valuerequests = req;
		}
	}
	public interface IEntryCreation<EntryType, InfoType> : IInfoCreation<InfoType> where InfoType : class
	{
		// ok you can clear stored data now
		void ResetRequests();

		// What named fields do I need to fully create an entry (eg eating a banana) - "kcal", "fat"
		BindingList<T> CreationFields<T> (IValueRequestFactory<T> factory); 
		// Here's those values, give me the entry (ww points on eating a bananna) - broker wont attempt to remember a "item / info".
		EntryType Create ();

		// Ok, I've got info on this food (bananna, per 100g, only kcal info) - I still need "fat" and "grams"
		BindingList<T> CalculationFields <T>(IValueRequestFactory<T> factory, InfoType info);
		// right, heres the fat too, give me entry (broker will update that bananna info also)
		EntryType Calculate(InfoType info, bool shouldComplete);

		// and again for editing
		BindingList<T> EditFields<T> (EntryType toEdit, IValueRequestFactory<T> factory); 
		EntryType Edit (EntryType toEdit);
		BindingList<T> EditFields <T>(EntryType toEdit, IValueRequestFactory<T> factory, InfoType info);
		EntryType Edit(EntryType toEdit, InfoType info, bool shouldComplete);
	}
	public interface IInfoCreation<InfoType> where InfoType : class
	{
		// So what info you need to correctly create an info on an eg food item from scratch? "fat" "kcal" "per grams" please
		BindingList<T> InfoFields<T>(IValueRequestFactory<T> factory, InfoType willEdit=null);
		// ok make me an info please here's that data.
		InfoType MakeInfo (InfoType toEdit=null);
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
		public DietInstType StartNewDiet()
		{
			var di = model.NewDiet ();
			conn.Insert (di as DietInstType);
			return di;
		}
		public void EditDiet(DietInstType diet)
		{
			List<BaseEntry> orphans = new List<BaseEntry>();
			orphans.AddRange (foodhandler.GetOrphans (diet));
			orphans.AddRange (firehandler.GetOrphans (diet));
			foreach (var e in orphans)
				if (e.entryWhen < diet.started)
					diet.started = e.entryWhen;
			if (diet.ended.HasValue)
				foreach (var e in orphans)
					if (e.entryWhen > diet.ended)
						diet.ended = e.entryWhen;
			conn.Update (diet, typeof(DietInstType)); 
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


	interface IInfoHandler<EntryInfoType> where EntryInfoType : BaseInfo, new()
	{
		void Add ();
		void Edit(EntryInfoType editing);
		void Remove(EntryInfoType removing);
	}

	class EntryHandler<D, EntryType,EntryInfoType> : IInfoHandler<EntryInfoType>
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
		bool ShouldComplete()
		{
			return true;
		}
		void CheckOrphans(D diet, EntryType entry)
		{
			bool updateDiet = false;
			if (diet.started > entry.entryWhen) {
				updateDiet = true;
				diet.started = entry.entryWhen;
			}
			if (diet.ended != null && diet.ended < entry.entryWhen) {
				updateDiet = true;
				diet.ended = entry.entryWhen;
			}
			if (updateDiet)
				conn.Update (diet, typeof(D));
		}
		public void Add (D diet, EntryInfoType info)
		{
			EntryType ent = creator.Calculate (info, ShouldComplete());
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = info.id;
			CheckOrphans (diet, ent);
			conn.Insert (ent as EntryType);
		}
		public void Add (D diet)
		{
			EntryType ent = creator.Create ();
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = null;
			CheckOrphans (diet, ent);
			conn.Insert (ent as EntryType);
		}
		public void Edit(EntryType ent, D diet, EntryInfoType info)
		{
			creator.Edit(ent, info, ShouldComplete());
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = info.id;
			CheckOrphans (diet, ent);
			conn.Update (ent as EntryType);
		}
		public void Edit(EntryType ent, D diet)
		{
			creator.Edit (ent);
			ent.dietinstanceid = diet.id;
			ent.infoinstanceid = null;
			CheckOrphans (diet, ent);
			conn.Update (ent as EntryType);
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
		public IEnumerable<EntryType> GetOrphans(D diet)
		{
			return conn.Table<EntryType> ().Where (d => 
				d.dietinstanceid == diet.id &&  (
					d.entryWhen < diet.started || 
					(diet.ended != null && d.entryWhen > diet.ended)
				)
			);
		}
		public int Count ()
		{
			return conn.Table<EntryType> ().Count ();
		}
		public int Count (D diet)
		{
			return conn.Table<EntryType> ().Where(e=>e.dietinstanceid==diet.id).Count ();
		}

		#endregion

		#region IInfoHandler implementation

		public void Add ()
		{
			var mod = creator.MakeInfo ();
			conn.Insert (mod, typeof(EntryInfoType));
		}

		public void Edit (EntryInfoType editing)
		{
			creator.MakeInfo (editing);
			conn.Update (editing, typeof(EntryInfoType));
		}

		public void Remove (EntryInfoType removing)
		{
			conn.Delete (removing);
		}

		#endregion
	}

	#endregion
}
