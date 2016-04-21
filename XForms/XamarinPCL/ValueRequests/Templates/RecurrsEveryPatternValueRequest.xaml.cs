using System;
using System.Collections.Generic;
using Xamarin.Forms;
using LibSharpHelp;
using System.ComponentModel;
using LibRTP;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class RecurrsEveryPatternValueRequest : ContentView
	{
		public RecurrsEveryPatternValueRequest ()
		{
			InitializeComponent ();
			picky.Items.AddAll (new List<String> { "Days", "Weeks", "Months", "Years" });
			convy.changed += parameter => sl.Ival ();
		}
	}
	public class StackLayoutIval : StackLayout
	{
		public void Ival() {
			InvalidateLayout ();
			ForceLayout ();
		}
	}
	public class PickerIval : Picker
	{
		public PickerIval() { SelectedIndexChanged += (sender, e) => InvalidateMeasure(); }
	}
	public class RecurrsEveryPatternValueRequestConverter : IValueConverter
	{
		public event Action<String> changed = delegate { };
		RecurrsEveryPatternValue reference = new RecurrsEveryPatternValue();
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// Keep a reference to the original value here.
			if (!Object.ReferenceEquals(reference,value)) 
				reference = value as RecurrsEveryPatternValue ?? new RecurrsEveryPatternValue();

			switch ((String)parameter) {
				case "date": return reference.PatternFixed;
				case "type": return  Math.Log10 ((int)reference.PatternType) / Math.Log10 (2);
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
				case "date": reference.PatternFixed = (DateTime)value; break;
				case "type": reference.PatternType = (LibRTP.RecurrSpan)(1 << (int)value); break;
				case "freq": 
					int v;
					if (int.TryParse(value as String, out v))
						reference.PatternFrequency = v;
					break;
			}
			changed ((String)parameter);
			return reference;
		}
		#endregion
	}
}

