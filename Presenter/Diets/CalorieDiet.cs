using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Consonance
{
	// Shorthands...
	using CalModel = Consonance.ITrackModel<CalorieDietInstance, CalorieDietEatEntry, FoodInfo, CalorieDietBurnEntry, FireInfo>;
	using CalPres = Consonance.ITrackerPresenter<CalorieDietInstance, CalorieDietEatEntry, FoodInfo, CalorieDietBurnEntry, FireInfo>;
	using SCM_Impl = SimpleTrackyHelpy<CalorieDietInstance, CalorieDietEatEntry, FoodInfo, CalorieDietBurnEntry, FireInfo, double,TimeSpan>;
	using SCP_Impl = SimpleTrackyHelpyPresenter<CalorieDietInstance, CalorieDietEatEntry, FoodInfo, CalorieDietBurnEntry, FireInfo, double,TimeSpan>;

	// Models
	public class CalorieDietEatEntry : BaseEatEntry
	{
		public double calories { get; set; } // eaten..
	}
	public class CalorieDietBurnEntry : BaseBurnEntry
	{
		public double calories { get; set; } // burned...
	}
	public class CalorieDietInstance : TrackerInstance
	{
		public double calories { get; set; } // limit...
	}

	// Diet factory methods
	public static class CalorieDiets
	{
		static readonly CalorieDiet_SimpleTemplate CalHelper = new CalorieDiet_SimpleTemplate();
		public static CalModel SimpleCalorieDietModel { 
			get { return new SCM_Impl (CalHelper); } 
		}
		public static CalPres SimpleCalorieDietPresenter { 
			get { return new SCP_Impl (CalHelper.TrackerDetails, CalHelper.TrackerDialect, CalHelper); }
		}
	}

	// Implimentation
	public class CalorieDiet_SimpleTemplate : IReflectedHelpy<FoodInfo, FireInfo, double, TimeSpan>
	{
		public IReflectedHelpyQuants<FoodInfo,double> input { get; private set; }
		public IReflectedHelpyQuants<FireInfo,TimeSpan> output { get; private set; }
		public TrackerDetailsVM TrackerDetails { get;private set;}
		public TrackerDialect TrackerDialect { get; private set; }

		public CalorieDiet_SimpleTemplate()
		{
			// Descriptive parts
			TrackerDetails = new TrackerDetailsVM ("Calorie Diet", "Simple calorie diet", "Diet");
			TrackerDialect = new TrackerDialect ("Eat", "Burn", "Foods", "Exercises");
			//creator things
			input = new HelpyIn();
			output = new HelpyOut ();
		}

		// intance stuff...mainly...
		public String name { get { return TrackerDetails.name; } }
		public String typename { get { return "Diet"; } }
		public String trackedMember { get { return "calories"; } }
		public String[] instanceFields { get { return new[] { "Calorie Limit" }; } } // creating an instance
		public double Calcluate(double[] fieldValues) { return fieldValues [0]; }

		// entry stuff...
		class HelpyIn : IReflectedHelpyQuants<FoodInfo,double>
		{
			#region IReflectedHelpyQuants implementation
			public string Convert (double quant) { return quant.ToString ("F1") + "g"; }
			public double Default { get { return 0.0; } }
			public double InfoFixedQuantity { get { return 100.0; } }
			public string quantifier { get { return "grams"; } }
			public string[] calculation { get { return new[] { "calories" }; } }
			public double Calcluate (double amount, double[] values) { return amount * values [0] / InfoFixedQuantity; }
			public Func<string, IValueRequest<double>> FindRequestor (IValueRequestFactory fact) { return fact.DoubleRequestor; }
			public Expression<Func<FoodInfo, bool>> InfoComplete { get { return fi => fi.calories != null; } }
			#endregion
		}
		class HelpyOut : IReflectedHelpyQuants<FireInfo,TimeSpan>
		{
			#region IReflectedHelpyQuants implementation
			public string Convert (TimeSpan quant) { return quant.TotalHours.ToString("F1") + "h"; }
			public TimeSpan Default { get { return TimeSpan.Zero; } }
			public TimeSpan InfoFixedQuantity { get { return TimeSpan.FromHours(1.0); } }
			public string quantifier { get { return "duration"; } }
			public string[] calculation { get { return new[] { "calories" }; } }
			public double Calcluate (TimeSpan amount, double[] values) { return (amount.TotalHours/InfoFixedQuantity.TotalHours) * values [0]; }
			public Func<string, IValueRequest<TimeSpan>> FindRequestor (IValueRequestFactory fact) { return fact.TimeSpanRequestor; }
			public Expression<Func<FireInfo, bool>> InfoComplete { get { return fi => fi.calories != null; } }
			#endregion
		}
	}
}


