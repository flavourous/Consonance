using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LibRTP;

namespace Consonance
{
	// Models
	public class SimpleBudgetEatEntry : BaseEntry
	{
		public double amount { get; set; } // eaten..
		public int? quantity { get; set; } // burned...
	}
	public class SimpleBudgetBurnEntry : BaseEntry
	{
		public double amount { get; set; } // burned...
		public int? quantity { get; set; } // burned...
	}
	// need seperate instance classes, otherwise get confused about which instances belong to which model/API
	public class SimpleBudgetInstance_Simple : TrackerInstance 
	{
		public double budget {get;set;} // could easily be zero
	}

	public class ExpenditureInfo : BaseInfo
	{
		public double amount { get; set; } // burned...
		public int? quantity { get; set; } // burned...
	}
	public class IncomeInfo : BaseInfo
	{
		public double amount { get; set; } // burned...
		public int? quantity { get; set; } // burned...		
	}

	public class SBH : SimpleTrackerHolder<SimpleBudgetInstance_Simple, SimpleBudgetEatEntry, IncomeInfo, int, SimpleBudgetBurnEntry, ExpenditureInfo, int>
	{
		public SBH(IExtraRelfectedHelpy<IncomeInfo, ExpenditureInfo, int, int> h) : base(h) {
		}
	}

	// Diet factory methods
	public static class Budgets
	{
		public static SBH simpleBudget = new SBH (new SimpleBudget_SimpleTemplate ());
	}

	// Implimentations
	public class SimpleBudget_SimpleTemplate : IExtraRelfectedHelpy<IncomeInfo, ExpenditureInfo, int, int>
	{
		readonly IReflectedHelpyQuants<IncomeInfo,int> _input = new SimpleBudget_HelpyIn();
		public IReflectedHelpyQuants<IncomeInfo,int> input { get { return _input; } }
		readonly IReflectedHelpyQuants<ExpenditureInfo,int> _output = new SimpleBudget_HelpyOut();
		public IReflectedHelpyQuants<ExpenditureInfo,int> output { get { return _output; } }
		readonly TrackerDetailsVM _TrackerDetails = new TrackerDetailsVM ("Finance budget", "Track spending goals and other finances.", "Finance");
		public TrackerDetailsVM TrackerDetails { get { return _TrackerDetails; } }
		readonly TrackerDialect _TrackerDialect = new TrackerDialect ("Earn", "Spend", "Incomes", "Expenses");
		public TrackerDialect TrackerDialect { get { return _TrackerDialect; } }
		public String name { get { return TrackerDetails.name; } }
		public String typename { get { return "Finance"; } }
		public String trackedname { get { return "amount"; } }
		public VRVConnectedValue [] instanceValueFields { get { return new[] { 
					VRVConnectedValue.FromType(0.0, "Target", "budget", f=>f.DoubleRequestor)
				}; } } // creating an instance
		public RecurringAggregatePattern[] Calcluate(object[] fieldValues) 
		{ 
			List<RecurringAggregatePattern> targs = new List<RecurringAggregatePattern> ();
			targs.Add (new RecurringAggregatePattern(1, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { (double)fieldValues [0] }));
			return targs.ToArray ();
		}
	}

	// entry stuff...
	class SimpleBudget_HelpyIn : IReflectedHelpyQuants<IncomeInfo,int>
	{
		#region IReflectedHelpyQuants implementation
		public InstanceValue<double> tracked { get { return new InstanceValue<double>("Amount", "amount", 0.0); } }
		public string Convert (int quant) { return quant.ToString (); }
		public int InfoFixedQuantity { get { return 1; } }
		public VRVConnectedValue quantifier { get { return VRVConnectedValue.FromType (1, "Quantity", "quantity", f => f.IntRequestor); } }
		public InstanceValue<double>[] calculation { get { return new[] { new InstanceValue<double>("Amount", "amount", 0.0) }; } }
		public double Calcluate (int amount, double[] values) { return amount * values [0] / InfoFixedQuantity; }
		public Func<string, IValueRequest<int>> FindRequestor (IValueRequestFactory fact) { return fact.IntRequestor; }
		public Expression<Func<IncomeInfo, bool>> InfoComplete { get { return fi => true; } }
		#endregion
	}
	class SimpleBudget_HelpyOut : IReflectedHelpyQuants<ExpenditureInfo,int>
	{
        #region IReflectedHelpyQuants implementation
        public InstanceValue<double> tracked { get { return new InstanceValue<double>("Amount", "amount", 0.0); } }
        public string Convert (int quant) { return quant.ToString (); }
		public int InfoFixedQuantity { get { return 1; } }
		public VRVConnectedValue quantifier { get { return VRVConnectedValue.FromType (1, "Quantity", "quantity", f => f.IntRequestor); } }
        public InstanceValue<double>[] calculation { get { return new[] { new InstanceValue<double>("Amount", "amount", 0.0) }; } }
		public double Calcluate (int amount, double[] values) { return amount * values [0]; }
		public Func<string, IValueRequest<int>> FindRequestor (IValueRequestFactory fact) { return fact.IntRequestor; }
		public Expression<Func<ExpenditureInfo, bool>> InfoComplete { get { return fi => true; } }
		#endregion
	}				

}

