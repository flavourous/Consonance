using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;
using LibRTP;

namespace Consonance
{
	
	// Models
	public class CalorieDietEatEntry : BaseEatEntry
	{
		public double calories { get; set; } // eaten..33
	}
	public class CalorieDietBurnEntry : BaseBurnEntry
	{
		public double calories { get; set; } // burned...
	}
	// need seperate instance classes, otherwise get confused about which instances belong to which model/API
	public class CalorieDietInstance_Simple : TrackerInstance 
	{
		public double calorieLimit {get;set;}
		public bool trackWeekly {get;set;}
		public bool trackDaily {get;set;}
	}
	public class CalorieDietInstance_Scav : TrackerInstance
	{
		public double strictCalorieLimit {get;set;}
		public double looseCalorieLimit {get;set;}
		public int nStrictDays {get;set;}
		public int nLooseDays {get;set;}
	}

	// helper derrivative
	public class CalHold<T> : SimpleTrackerHolder<T, CalorieDietEatEntry, FoodInfo, double, CalorieDietBurnEntry, FireInfo, TimeSpan>
		where T : TrackerInstance, new()
	{ public CalHold(IExtraRelfectedHelpy<FoodInfo,FireInfo, double, TimeSpan> helpy) : base(helpy) { } }

	// Diet factory methods
	public static class CalorieDiets
	{
		public static readonly CalHold<CalorieDietInstance_Simple> simple = new CalHold<CalorieDietInstance_Simple> (new CalorieDiet_SimpleTemplate ());
		public static readonly CalHold<CalorieDietInstance_Scav> scav = new CalHold<CalorieDietInstance_Scav> (new CalorieDiet_Scavenger ());
	}

	// Implimentations
	public class CalorieDiet_SimpleTemplate : IExtraRelfectedHelpy<FoodInfo, FireInfo, double, TimeSpan>
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
		public InstanceValue [] instanceValueFields { get { return new[] { 
				InstanceValue.FromType (0.0, "Calorie Limit", "calorieLimit", f => f.DoubleRequestor),
				InstanceValue.FromType (true, "Track Daily", "trackDaily", f => f.BoolRequestor),
				InstanceValue.FromType (false, "Track Weekly", "trackWeekly", f => f.BoolRequestor)
			}; } } // creating an instance
		public RecurringAggregatePattern[] Calcluate(object[] fieldValues) 
		{ 
			List<RecurringAggregatePattern> targs = new List<RecurringAggregatePattern> ();
			if ((bool)fieldValues [1]) targs.Add(new RecurringAggregatePattern(0, AggregateRangeType.DaysEitherSide, new[] { 1 }, new[] { (double)fieldValues [0] }));
			if ((bool)fieldValues [2]) targs.Add (new RecurringAggregatePattern(7, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { (double)fieldValues [0]*7 }) );
			return targs.ToArray ();
		}
	}

	public class CalorieDiet_Scavenger : IExtraRelfectedHelpy<FoodInfo, FireInfo, double, TimeSpan>
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
		public InstanceValue[] instanceValueFields { get { return new[] { 
					InstanceValue.FromType(0, v => (int)v[0] > 0, "Loose days", "nLooseDays", f=>f.IntRequestor ), 
					InstanceValue.FromType(0.0, "Calories", "looseCalorieLimit", f=>f.DoubleRequestor ), 
					InstanceValue.FromType(0, v => (int)v[2] > 0, "Strict days", "nStrictDays", f=>f.IntRequestor ), 
					InstanceValue.FromType(0.0, "Calories", "strictCalorieLimit", f=>f.DoubleRequestor ) 
			}; } } // creating an instance
		public RecurringAggregatePattern[] Calcluate(object[] fieldValues) 
		{
			return new[] { 
				new RecurringAggregatePattern (
					// Range Tracking
					0, AggregateRangeType.DaysEitherSide, 

					// Pattern Values
					new int[] { (int)fieldValues [0], (int)fieldValues [2] }, 
					new double[] { (double)fieldValues [1], (double)fieldValues [3] }) 
			};
		}
	}
	
	// entry stuff...
	class CalDiet_HelpyIn : IReflectedHelpyQuants<FoodInfo,double>
	{
		#region IReflectedHelpyQuants implementation
		public String trackedMember { get { return "calories"; } }
		public string Convert (double quant) { return quant.ToString ("F1") + "g"; }
		public double InfoFixedQuantity { get { return 100.0; } }
		public InstanceValue quantifier { get { return InstanceValue.FromType (0.0, "Grams", "grams", f => f.DoubleRequestor); } }
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
		public TimeSpan InfoFixedQuantity { get { return TimeSpan.FromHours(1.0); } }
		public InstanceValue quantifier { get { return InstanceValue.FromType(TimeSpan.Zero,"Duration", "duration",f=>f.TimeSpanRequestor); } }
		public string[] calculation { get { return new[] { "calories" }; } }
		public double Calcluate (TimeSpan amount, double[] values) { return (amount.TotalHours/InfoFixedQuantity.TotalHours) * values [0]; }
		public Func<string, IValueRequest<TimeSpan>> FindRequestor (IValueRequestFactory fact) { return fact.TimeSpanRequestor; }
		public Expression<Func<FireInfo, bool>> InfoComplete { get { return fi => fi.calories != null; } }
		#endregion
	}				
}