using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;
using LibRTP;

namespace Consonance
{
	
	// Models
	public abstract class CalorieDietEatEntry : BaseEatEntry
	{
		public double calories { get; set; } // eaten.
	}
	public abstract class CalorieDietBurnEntry : BaseBurnEntry
	{
		public double calories { get; set; } // burned...
	}
	// need seperate instance classes, otherwise get confused about which instances belong to which model/API
	public class CalorieDietInstance_Simple : TrackerInstance 
	{
		public double calorieLimit { get; set; }
		public bool trackWeekly { get; set; }
		public bool trackDaily { get; set; }
	}
	public class CalorieDietInstance_Scav : TrackerInstance
	{
		public double strictCalorieLimit {get;set;}
		public double looseCalorieLimit {get;set;}
		public int nStrictDays {get;set;}
		public int nLooseDays {get;set;}
	}

	// helper derrivative
	public class CalHold<T,E,B> : SimpleTrackerHolder<T, E, FoodInfo, B, FireInfo>
		where T : TrackerInstance, new()
        where E : CalorieDietEatEntry, new()
        where B : CalorieDietBurnEntry, new()
	{ public CalHold(IExtraRelfectedHelpy<FoodInfo,FireInfo> helpy) : base(helpy) { } }

	// Diet factory methods
    
	public static class CalorieDiets
	{
		public static readonly CalHold<CalorieDietInstance_Simple, CalorieDiet_Simple_EatEntry, CalorieDiet_Simple_BurnEntry> simple 
            = new CalHold<CalorieDietInstance_Simple, CalorieDiet_Simple_EatEntry, CalorieDiet_Simple_BurnEntry>
            (new CalorieDiet_SimpleTemplate ());
		public static readonly CalHold<CalorieDietInstance_Scav, CalorieDiet_Scav_EatEntry, CalorieDiet_Scav_BurnEntry> scav 
            = new CalHold<CalorieDietInstance_Scav, CalorieDiet_Scav_EatEntry, CalorieDiet_Scav_BurnEntry>
            (new CalorieDiet_Scavenger ());
	}

    // entry tables
    public class CalorieDiet_Simple_EatEntry : CalorieDietEatEntry { }
    public class CalorieDiet_Simple_BurnEntry : CalorieDietBurnEntry { }
    public class CalorieDiet_Scav_EatEntry : CalorieDietEatEntry { }
    public class CalorieDiet_Scav_BurnEntry : CalorieDietBurnEntry { }

    // Implimentations
    public class CalorieDiet_SimpleTemplate : IExtraRelfectedHelpy<FoodInfo, FireInfo>
	{
		readonly IReflectedHelpyQuants<FoodInfo> _input = new CalDiet_HelpyIn();
		public IReflectedHelpyQuants<FoodInfo> input { get { return _input; } }
		readonly IReflectedHelpyQuants<FireInfo> _output = new CalDiet_HelpyOut();
		public IReflectedHelpyQuants<FireInfo> output { get { return _output; } }
		readonly TrackerDetailsVM _TrackerDetails = new TrackerDetailsVM ("Calorie diet", "Simple calorie-control diet with a daily target.  If enabled, weekly tracking starts from the start date of the diet.", "Diet");
		public TrackerDetailsVM TrackerDetails { get { return _TrackerDetails; } }
		readonly TrackerDialect _TrackerDialect = new TrackerDialect ("Diet", "Eat", "Burn", "Foods", "Exercises", "Eaten", "Burned");
		public TrackerDialect TrackerDialect { get { return _TrackerDialect; } }
		public String trackedname { get { return "calories"; } }
		public VRVConnectedValue [] instanceValueFields { get { return new[] { 
				VRVConnectedValue.FromType (0.0, "Calorie Limit", "calorieLimit", f => f.DoubleRequestor),
				VRVConnectedValue.FromType (true, "Track Daily", "trackDaily", f => f.BoolRequestor),
				VRVConnectedValue.FromType (false, "Track Weekly", "trackWeekly", f => f.BoolRequestor)
			}; } } // creating an instance
        public SimpleTrackyTarget[] Calcluate(object[] fieldValues)
        {
            List<SimpleTrackyTarget> targs = new List<SimpleTrackyTarget>();
            targs.Add(new SimpleTrackyTarget("Calories", (bool)fieldValues[1], true, 0, AggregateRangeType.DaysEitherSide, new[] { 1 }, new[] { (double)fieldValues[0] }));
            targs.Add(new SimpleTrackyTarget("Calories", (bool)fieldValues[2], false, 7, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { (double)fieldValues[0] * 7 }));
            return targs.ToArray();
        }
	}

	public class CalorieDiet_Scavenger : IExtraRelfectedHelpy<FoodInfo, FireInfo>
	{
		readonly IReflectedHelpyQuants<FoodInfo> _input = new CalDiet_HelpyIn();
		public IReflectedHelpyQuants<FoodInfo> input { get { return _input; } }
		readonly IReflectedHelpyQuants<FireInfo> _output = new CalDiet_HelpyOut();
		public IReflectedHelpyQuants<FireInfo> output { get { return _output; } }
		readonly TrackerDetailsVM _TrackerDetails = new TrackerDetailsVM ("Scavenger calorie diet", "Calorie controlled diet, using periods of looser control followed by periods of stronger control.", "Diet");
		public TrackerDetailsVM TrackerDetails { get { return _TrackerDetails; } }
		readonly TrackerDialect _TrackerDialect = new TrackerDialect ("Diet", "Eat", "Burn", "Foods", "Exercises", "Eaten", "Burned");
		public TrackerDialect TrackerDialect { get { return _TrackerDialect; } }
		public String trackedname { get { return "calories"; } }
		public VRVConnectedValue[] instanceValueFields { get { return new[] { 
					VRVConnectedValue.FromType(0, v => (int)v[0] > 0, "Loose days", "nLooseDays", f=>f.IntRequestor ), 
					VRVConnectedValue.FromType(0.0, "Calories", "looseCalorieLimit", f=>f.DoubleRequestor ), 
					VRVConnectedValue.FromType(0, v => (int)v[2] > 0, "Strict days", "nStrictDays", f=>f.IntRequestor ), 
					VRVConnectedValue.FromType(0.0, "Calories", "strictCalorieLimit", f=>f.DoubleRequestor ) 
			}; } } // creating an instance
		public SimpleTrackyTarget[] Calcluate(object[] fieldValues) 
		{
			return new[] { 
				new SimpleTrackyTarget ("Calories", true, true,
					// Range Tracking
					0, AggregateRangeType.DaysEitherSide, 

					// Pattern Values
					new int[] { (int)fieldValues [0], (int)fieldValues [2] }, 
					new double[] { (double)fieldValues [1], (double)fieldValues [3] }) 
			};
		}
	}
	
	// entry stuff...
	class CalDiet_HelpyIn : IReflectedHelpyQuants<FoodInfo>
	{
		#region IReflectedHelpyQuants implementation
        public InstanceValue<double> tracked { get { return new InstanceValue<double>("Calories", "calories", 0.0); } }
        public InstanceValue<double>[] calculation { get { return new[] { new InstanceValue<double>("Calories", "calories", 0.0) }; } }
        public InfoQuantifier[] quantifier_choices { get { return new[] { InfoQuantifier.Double("Grams", 0, 100) }; } }
        public double Calcluate (double[] values) { return values [0]; }
		public Expression<Func<FoodInfo, bool>> InfoComplete { get { return fi => fi.calories != null; } }
		#endregion
	}
	class CalDiet_HelpyOut : IReflectedHelpyQuants<FireInfo>
	{
		#region IReflectedHelpyQuants implementation
		public InstanceValue<double> tracked { get { return new InstanceValue<double>("Calories", "calories", 0.0); } }
		public InstanceValue<double>[] calculation { get { return new[] { new InstanceValue<double>("Calories", "calories", 0.0) }; } }
        public InfoQuantifier[] quantifier_choices { get { return new[] { InfoQuantifier.Duration("Duration", 0, TimeSpan.Zero) }; } }
        public double Calcluate (double[] values) { return values [0]; }
		public Expression<Func<FireInfo, bool>> InfoComplete { get { return fi => fi.calories != null; } }
		#endregion
	}				
}