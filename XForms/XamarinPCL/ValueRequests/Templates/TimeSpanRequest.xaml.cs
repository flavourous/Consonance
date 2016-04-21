using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class TimeSpanRequest : ContentView
	{
		public TimeSpanRequest ()
		{
			InitializeComponent ();
		}
	}
	// can only be used for one set of controls!!
	class TimeSpanValueConverter_Stateful : IValueConverter
	{
		#region IValueConverter implementation
		TimeSpan ts;
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			ts = (TimeSpan)value;
			switch ((String)parameter) 
			{
			case "h": return ts.Hours;
			case "m": return ts.Minutes;
			case "s": return ts.Seconds;
			default: return 0;
			}			
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			int? val = null;
			int tval;
			if (int.TryParse ((String)value, out tval))
				val = tval;
			switch ((String)parameter) 
			{
			// if we didnt get anything, force a set picked up by the inbound bindings.
			case "h": SetHour(ref ts, val ?? ts.Hours); break;
			case "m": SetMinute(ref ts, val ?? ts.Minutes); break;
			case "s": SetSecond (ref ts, val ?? ts.Seconds); break;
			}
			return ts;
		}
		void SetHour(ref TimeSpan ts, int h) { ts = new TimeSpan (h, ts.Minutes, ts.Seconds); }
		void SetMinute(ref TimeSpan ts, int m) { ts = new TimeSpan (ts.Hours, m, ts.Seconds); }
		void SetSecond(ref TimeSpan ts, int s) { ts = new TimeSpan (ts.Hours, ts.Minutes, s); }

		#endregion
	}
}

