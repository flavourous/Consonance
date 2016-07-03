using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LibRTP;

namespace Consonance
{
	// Models
	public class SimpleBudgetEatEntry : HBaseEntry
	{
		public double amount { get; set; } // eaten..
	}
	public class SimpleBudgetBurnEntry : HBaseEntry
	{
		public double amount { get; set; } // burned...
	}
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
		public SBH(IExtraRelfectedHelpy<IncomeInfo, ExpenditureInfo> h) : base(h) {
		}
	}

	// Diet factory methods
	public static class Budgets
	{
		public static SBH simpleBudget = new SBH (new SimpleBudget_SimpleTemplate ());
	}

	// Implimentations
	public class SimpleBudget_SimpleTemplate : IExtraRelfectedHelpy<IncomeInfo, ExpenditureInfo>
	{
		readonly IReflectedHelpyQuants<IncomeInfo> _input = new SimpleBudget_HelpyIn();
		public IReflectedHelpyQuants<IncomeInfo> input { get { return _input; } }
		readonly IReflectedHelpyQuants<ExpenditureInfo> _output = new SimpleBudget_HelpyOut();
		public IReflectedHelpyQuants<ExpenditureInfo> output { get { return _output; } }
		readonly TrackerDetailsVM _TrackerDetails = new TrackerDetailsVM ("Finance budget", "Track spending goals and other finances.", "Finance");
		public TrackerDetailsVM TrackerDetails { get { return _TrackerDetails; } }
		readonly TrackerDialect _TrackerDialect = new TrackerDialect ("Finance", "Earn", "Spend", "Incomes", "Expenses", "Earned", "Spent");
		public TrackerDialect TrackerDialect { get { return _TrackerDialect; } }
		public String trackedname { get { return "amount"; } }
		public VRVConnectedValue [] instanceValueFields { get { return new[] { 
					VRVConnectedValue.FromType(0.0, "Target", "budget", f=>f.DoubleRequestor)
				}; } } // creating an instance
		public SimpleTrackyTarget[] Calcluate(object[] fieldValues) 
		{ 
			List<SimpleTrackyTarget> targs = new List<SimpleTrackyTarget> ();
			targs.Add (new SimpleTrackyTarget("Balance",true,true, 1, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { (double)fieldValues [0] }));
			return targs.ToArray ();
		}
	}

	// entry stuff...
	class SimpleBudget_HelpyIn : IReflectedHelpyQuants<IncomeInfo>
	{
		#region IReflectedHelpyQuants implementation
		public InstanceValue<double> tracked { get { return new InstanceValue<double>("Amount", "amount", 0.0); } }
		public InstanceValue<double>[] calculation { get { return new[] { new InstanceValue<double>("Amount", "amount", 0.0) }; } }
		public double Calcluate (double[] values) { return values [0]; }
		public Expression<Func<IncomeInfo, bool>> InfoComplete { get { return fi => true; } }
        public InfoQuantifier[] quantifier_choices { get { return new[] { InfoQuantifier.Integer("Quantity", 0, 1) }; } }
		#endregion
	}
	class SimpleBudget_HelpyOut : IReflectedHelpyQuants<ExpenditureInfo>
	{
        #region IReflectedHelpyQuants implementation
        public InstanceValue<double> tracked { get { return new InstanceValue<double>("Amount", "amount", 0.0); } }
        public InstanceValue<double>[] calculation { get { return new[] { new InstanceValue<double>("Amount", "amount", 0.0) }; } }
		public double Calcluate (double[] values) { return values [0]; }
		public Expression<Func<ExpenditureInfo, bool>> InfoComplete { get { return fi => true; } }
        public InfoQuantifier[] quantifier_choices { get { return new[] { InfoQuantifier.Integer("Quantity", 0, 1) }; } }
        #endregion
    }				

}

