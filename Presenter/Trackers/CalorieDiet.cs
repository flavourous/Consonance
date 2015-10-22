using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Consonance
{
	// Models
	public class CalorieDietEatEntry : BaseEatEntry
	{
		public double calories { get; set; } // eaten..
	}
	public class CalorieDietBurnEntry : BaseBurnEntry
	{
		public double calories { get; set; } // burned...
	}
	// need seperate instance classes, otherwise get confused about which instances belong to which model/API
	public class CalorieDietInstance_Simple : TrackerInstance { }
	public class CalorieDietInstance_Scav : TrackerInstance{ }

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
		public String[] instanceFields { get { return new[] { "Calorie Limit" }; } } // creating an instance
		public HelpyOption [] instanceOptions { get { return new [] {
				new HelpyOption () { defaultValue = true, optionName = "Track Daily" },
				new HelpyOption () { defaultValue = false, optionName = "Track Weekly" }
			}; } }
		public SimpleTrackingTarget[] Calcluate(double[] fieldValues, bool[] options) 
		{ 
			List<int> trange = new List<int> ();
			List<SimpleTrackingTarget.RangeType> trt = new List<SimpleTrackingTarget.RangeType> ();
			if (options [0]) { trange.Add (0); trt.Add (SimpleTrackingTarget.RangeType.DaysEitherSide); }
			if (options [1]) { trange.Add (7); trt.Add (SimpleTrackingTarget.RangeType.DaysFromStart); }
			return new[] { new SimpleTrackingTarget (new[] { 0 }, new[] { SimpleTrackingTarget.RangeType.DaysEitherSide }, new[] { 1 }, new[] { fieldValues [0] }) };
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
		public String[] instanceFields { get { return new[] { "Loose days", "Calories", "Strict days", "Calories" }; } } // creating an instance
		public HelpyOption [] instanceOptions { get{ return new HelpyOption[0]; } }
		public SimpleTrackingTarget[] Calcluate(double[] fieldValues, bool[] options) 
		{
			return new[] { 
				new SimpleTrackingTarget (

					// Range Tracking
					new int[] { 0 }, 
					new[] { SimpleTrackingTarget.RangeType.DaysEitherSide }, 

					// Pattern Values
					new int[] { (int)fieldValues [0], (int)fieldValues [2] }, 
					new double[] { fieldValues [1], fieldValues [3] }) 
			};
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
				
								


