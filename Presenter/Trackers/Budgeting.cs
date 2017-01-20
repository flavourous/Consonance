using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LibRTP;

namespace Consonance
{
    // Models
    public class SimpleBudgetEntry : HBaseEntry
    {
        public double amount { get; set; } // eaten..
    }
    public class SimpleBudgetEatEntry : SimpleBudgetEntry { }
    public class SimpleBudgetBurnEntry : SimpleBudgetEntry { }
	// need seperate instance classes, otherwise get confused about which instances belong to which model/API
	public class SimpleBudgetInstance_Simple : TrackerInstance 
	{
		public double budget {get;set;} // could easily be zero
	}

	public class ExpenditureInfo : HBaseInfo
	{
		public double amount { get; set; } // burned...
	}
	public class IncomeInfo : HBaseInfo
	{
		public double amount { get; set; } // burned...
	}

	public class SBH : SimpleTrackerHolder<SimpleBudgetInstance_Simple, SimpleBudgetEatEntry, IncomeInfo, SimpleBudgetBurnEntry, ExpenditureInfo>
	{
		public SBH(IExtraRelfectedHelpy<SimpleBudgetInstance_Simple, IncomeInfo, ExpenditureInfo> h) : base(h) {
		}
	}

	// Diet factory methods
	public static class Budgets
	{
		public static SBH simpleBudget = new SBH (new SimpleBudget_SimpleTemplate ());
	}

	// Implimentations
	public class SimpleBudget_SimpleTemplate : IExtraRelfectedHelpy<SimpleBudgetInstance_Simple, IncomeInfo, ExpenditureInfo>
	{
        readonly IReflectedHelpyQuants<IncomeInfo> _input = new SimpleBudget_HelpyIn();
		public IReflectedHelpyQuants<IncomeInfo> input { get { return _input; } }
		readonly IReflectedHelpyQuants<ExpenditureInfo> _output = new SimpleBudget_HelpyOut();
		public IReflectedHelpyQuants<ExpenditureInfo> output { get { return _output; } }
		readonly TrackerDetailsVM _TrackerDetails = new TrackerDetailsVM ("Finance budget", "Track spending goals and other finances.", "Finance");
		public TrackerDetailsVM TrackerDetails { get { return _TrackerDetails; } }
		readonly TrackerDialect _TrackerDialect = new TrackerDialect ( "Earn", "Spend", "Incomes", "Expenses", "Earned", "Spent");
		public TrackerDialect TrackerDialect { get { return _TrackerDialect; } }
		public VRVConnectedValue [] instanceValueFields { get { return new[] { 
					VRVConnectedValue.FromType(0.0, "Target", 
                    o=>((SimpleBudgetInstance_Simple)o).budget,
                    (o,v)=>((SimpleBudgetInstance_Simple)o).budget = (double)v,
                    f=>f.DoubleRequestor)
				}; } } // creating an instance
		public SimpleTrackyTarget[] Calcluate(object[] fieldValues) 
		{ 
			List<SimpleTrackyTarget> targs = new List<SimpleTrackyTarget> ();
			targs.Add (new SimpleTrackyTarget("Balance","balance",true,true, 1, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { (double)fieldValues [0] }));
			return targs.ToArray ();
		}
	}

	// entry stuff...
	class SimpleBudget_HelpyIn : IReflectedHelpyQuants<IncomeInfo>
	{
        #region IReflectedHelpyQuants implementation
        public InstanceValue<double>[] calculation { get; } = new[] { new InstanceValue<double>("Amount", o => ((IncomeInfo)o).amount, (o, v) => ((IncomeInfo)o).amount = v, 0.0) };
        public IReflectedHelpyCalc[] calculators { get; } = new[] { new ICalc() };
		public Expression<Func<IncomeInfo, bool>> InfoComplete { get; } = fi => true;
        public InfoQuantifier[] quantifier_choices { get; } = new[] { HelpyInfoQuantifier.FromType(InfoQuantifier.InfoQuantifierTypes.Integer, "Quantity", 0, 1.0) };
        #endregion
        class ICalc : IReflectedHelpyCalc
        {
            public InstanceValue<double> direct { get { return new InstanceValue<double>("Amount", o => ((SimpleBudgetEatEntry)o).amount, (o, v) => ((SimpleBudgetEatEntry)o).amount = v, 0.0); } }
            public string TargetID { get { return "balance"; } }
            public double Calculate(double[] values) { return values[0]; }
        }
    }
    class SimpleBudget_HelpyOut : IReflectedHelpyQuants<ExpenditureInfo>
    {
        #region IReflectedHelpyQuants implementation
        public InstanceValue<double>[] calculation { get; } = new[] { new InstanceValue<double>("Amount", o => ((ExpenditureInfo)o).amount, (o, v) => ((ExpenditureInfo)o).amount = v, 0.0) };
        public IReflectedHelpyCalc[] calculators { get; } = new[] { new ICalc() };
        public Expression<Func<ExpenditureInfo, bool>> InfoComplete { get; }  = fi => true; 
        public InfoQuantifier[] quantifier_choices { get; } = new[] { HelpyInfoQuantifier.FromType(InfoQuantifier.InfoQuantifierTypes.Integer, "Quantity", 0, 1.0) };
        #endregion
        class ICalc : IReflectedHelpyCalc
        {
            public InstanceValue<double> direct { get { return new InstanceValue<double>("Amount", o => ((SimpleBudgetBurnEntry)o).amount, (o, v) => ((SimpleBudgetBurnEntry)o).amount = v, 0.0); } }
            public string TargetID { get { return "balance"; } }
            public double Calculate(double[] values) { return values[0]; }
        }
    }				

}

