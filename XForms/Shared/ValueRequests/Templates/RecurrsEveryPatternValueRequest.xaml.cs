using System;
using System.Collections.Generic;
using Xamarin.Forms;
using LibSharpHelp;

namespace Consonance.XamarinFormsView
{
	public partial class RecurrsEveryPatternValueRequest : ContentView
	{
		public RecurrsEveryPatternValueRequest ()
		{
			InitializeComponent ();
			picky.Items.AddAll(new List<String> { "Days", "Weeks", "Months", "Years" });
		}
	}
	public class RecurrsEveryPatternValueRequestConverter : IValueConverter
	{
		RecurrsEveryPatternValue reference = new RecurrsEveryPatternValue();
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// Keep a reference to the original value here.
			if (!Object.ReferenceEquals(reference,value)) 
				reference = value as RecurrsEveryPatternValue ?? new RecurrsEveryPatternValue();

			switch ((String)parameter) {
				case "date": return reference.PatternFixed;
				case "type": return reference.PatternType;
				case "freq": return reference.PatternFrequency;	
			}

			// We should not be reaching here in a working application
			throw new NotImplementedException ();
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// Sorry, we've got nothing to do
			if (reference != null)
			switch ((String)parameter) {
				case "date":
					if (value is DateTime)
						reference.PatternFixed = (DateTime)value;
					break;
				case "type": 
					if (value is int)
						reference.PatternType = (LibRTP.RecurrSpan)(1 << (int)value);
					break;
				case "freq": 
					if (value is int)
						reference.PatternFrequency = (int)value;
					break;
			}
			return reference;
		}
		#endregion
	}
}

