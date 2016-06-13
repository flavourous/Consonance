using System;

namespace Consonance
{
    public abstract class BaseEatEntry : BaseEntry 
	{
		public double? grams { get; set; }		
	}
	public abstract class BaseBurnEntry : BaseEntry 
	{ 
		public TimeSpan? duration { get; set; }
	}
	public class FireInfo : BaseInfo 
	{
		public TimeSpan duration {get;set;}
		public double? calories {get;set;}
	}
	public class FoodInfo : BaseInfo
	{
		// Amount Info
		public double grams { get; set; }

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

}

