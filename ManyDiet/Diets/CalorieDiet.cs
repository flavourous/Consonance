using System;
using System.Collections.Generic;

namespace ManyDiet
{
	public class CalorieDietEntry : BaseDietEntry
	{
		public String myname {get;set;}
		public double kcals { get; set; }
	}

	public class CalorieDiet : IDietModel<CalorieDietEntry>
	{
		#region implemented abstract members of Diet

		public bool CreateEntry (IEnumerable<Object> values, out BaseDietEntry entry)
		{
			List<Object> vals = new List<Object> (values);
			entry = new CalorieDietEntry () { myname = vals[0] as String, kcals = (double)vals [1]};
			return true;
		}

		public String[] EntryCreationFields { get { return new String[] { "name", "kcal" }; } }
		public String[] EntryCalculationFields { get { return new String[] { "name", "kcal" }; } }

		public DietInstance NewDiet ()
		{
			return new DietInstance () {
				name = "Simple calorie diet"
			};
		}
		public bool CalculateEntry (FoodInfo info, IEnumerable<Object> values, out BaseDietEntry result)
		{
			List<Object> vals = new List<Object> (values);
			var cde = new CalorieDietEntry ();
			cde.myname = vals [0] as String;
			cde.kcals = (double)vals [1];
			result = cde;
			return false;
		}
		FoodInfo req = new FoodInfo()
		{
			calcium =null,
			calories=0.0, // needed for a food entry
			carbohydrate=null,
			cholesterol=null,
			fat=null,
			fiber=null,
			iron=null,
			monounsaturated_fat=null,
			polyunsaturated_fat=null,
			potassium=null,
			protein=null,
			saturated_fat=null,
			sodium=null,
			sugar=null,
			trans_fat=null,
			vitamin_a=null,
			vitamin_c=null
		};
		public bool MeetsRequirements(FoodInfo fi)
		{
			return fi.calories.HasValue;
		}

		public FoodInfo CalculationRequirmentsMask { get{ return req; } }
		#endregion
	}

	public class CalorieDietPresenter : IDietPresenter<CalorieDietEntry>
	{
		#region IDietPresenter implementation
		public EatEntryLineVM GetLineRepresentation (BaseDietEntry entry)
		{
			var ent = (entry as CalorieDietEntry);
			var fi = Presenter.Singleton.FindFood (ent.foodinstanceid);
			return new EatEntryLineVM (
				ent.entryWhen, 
				ent.myname, 
				fi == null ? "" : fi.name, 
				new KVPList<string, double> { { "kcal", ent.kcals } }
			);
		}
		#endregion
	}
}

