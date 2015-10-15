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
	public class CalorieDietInstance : SimplyTrackedInst
	{
		// targes get stored in another place.
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

	// Implimentations

	public class CalorieDiet_SimpleTemplate : IReflectedHelpy<FoodInfo, FireInfo, double, TimeSpan>
	{
		readonly IReflectedHelpyQuants<FoodInfo,double> _input = new CalDiet_HelpyIn();
		public IReflectedHelpyQuants<FoodInfo,double> input { get { return _input; } }
		readonly IReflectedHelpyQuants<FireInfo,TimeSpan> _output = new CalDiet_HelpyOut();
		public IReflectedHelpyQuants<FireInfo,TimeSpan> output { get { return _output; } }
		readonly TrackerDetailsVM _TrackerDetails = new TrackerDetailsVM ("Calorie diet", "Simple calorie-control diet with a daily target.", "Diet");
		public TrackerDetailsVM TrackerDetails { get { return _TrackerDetails; } }
		readonly TrackerDialect _TrackerDialect = new TrackerDialect ("Eat", "Burn", "Foods", "Exercises");
		public TrackerDialect TrackerDialect { get { return _TrackerDialect; } }
		public String name { get { return TrackerDetails.name; } }
		public String typename { get { return "Diet"; } }
		public String trackedname { get { return "calories"; } }
		public String[] instanceFields { get { return new[] { "Calorie Limit" }; } } // creating an instance
		public SimpleTargetVal[] Calcluate(double[] fieldValues) 
		{ 
			return new[] { new SimpleTargetVal (1, fieldValues [0]) };
		}
		public SimpleTrackingRange[] AdditionalTrackingRanges { get { return new SimpleTrackingRange[0]; } }
	}

	public class CalorieDiet_Scavenger : IReflectedHelpy<FoodInfo, FireInfo, double, TimeSpan>
	{
		readonly IReflectedHelpyQuants<FoodInfo,double> _input = new CalDiet_HelpyIn();
		public IReflectedHelpyQuants<FoodInfo,double> input { get { return _input; } }
		readonly IReflectedHelpyQuants<FireInfo,TimeSpan> _output = new CalDiet_HelpyOut();
		public IReflectedHelpyQuants<FireInfo,TimeSpan> output { get { return _output; } }
		readonly TrackerDetailsVM _TrackerDetails = new TrackerDetailsVM ("Scavenger calorie diet", "Calorie controlled diet, using periods of looser control followed by periods of stronger control.", "Diet");
		public TrackerDetailsVM TrackerDetails { get { return _TrackerDetails; } }
		readonly TrackerDialect _TrackerDialect = new TrackerDialect ("Eat", "Burn", "Foods", "Exercises");
		public TrackerDialect TrackerDialect { get { return _TrackerDialect; } }
		public String name { get { return TrackerDetails.name; } }
		public String typename { get { return "Diet"; } }
		public String trackedname { get { return "calories"; } }
		public String[] instanceFields { get { return new[] { "Loose days", "Calories", "Strict days", "Calories" }; } } // creating an instance
		public SimpleTargetVal[] Calcluate(double[] fieldValues) 
		{ 
			return new[] { 
				new SimpleTargetVal ((int)fieldValues [0], fieldValues [1]),
				new SimpleTargetVal ((int)fieldValues [2], fieldValues [3])
			};
		}
		public SimpleTrackingRange[] AdditionalTrackingRanges { get { return new SimpleTrackingRange[0]; } }
	}

	public class CalorieDiet_WithWeeklyGoal : IReflectedHelpy<FoodInfo, FireInfo, double, TimeSpan>
	{
		readonly IReflectedHelpyQuants<FoodInfo,double> _input = new CalDiet_HelpyIn();
		public IReflectedHelpyQuants<FoodInfo,double> input { get { return _input; } }
		readonly IReflectedHelpyQuants<FireInfo,TimeSpan> _output = new CalDiet_HelpyOut();
		public IReflectedHelpyQuants<FireInfo,TimeSpan> output { get { return _output; } }
		readonly TrackerDetailsVM _TrackerDetails = new TrackerDetailsVM ("Calorie diet", "Simple calorie-control diet with a daily target.", "Diet");
		public TrackerDetailsVM TrackerDetails { get { return _TrackerDetails; } }
		readonly TrackerDialect _TrackerDialect = new TrackerDialect ("Eat", "Burn", "Foods", "Exercises");
		public TrackerDialect TrackerDialect { get { return _TrackerDialect; } }
		public String name { get { return TrackerDetails.name; } }
		public String typename { get { return "Diet"; } }
		public String trackedname { get { return "calories"; } }
		public String[] instanceFields { get { return new[] { "Calorie Limit" }; } } // creating an instance
		public SimpleTargetVal[] Calcluate(double[] fieldValues) 
		{ 
			return new[] { new SimpleTargetVal (1, fieldValues [0]) };
		}
		public SimpleTrackingRange[] AdditionalTrackingRanges {
			get { 
				DateTime dd = DateTime.Now;
				dd.AddDays (-(int)dd.DayOfWeek); // start monday
				return new SimpleTrackingRange[] { 
					new SimpleTrackingRange (dd, TimeSpan.FromDays (7))
				};
			}
		}

	}

	// entry stuff...
	class CalDiet_HelpyIn : IReflectedHelpyQuants<FoodInfo,double>
	{
		#region IReflectedHelpyQuants implementation
		public String trackedMember { get { return "calories"; } }
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
	class CalDiet_HelpyOut : IReflectedHelpyQuants<FireInfo,TimeSpan>
	{
		#region IReflectedHelpyQuants implementation
		public String trackedMember { get { return "calories"; } }
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


