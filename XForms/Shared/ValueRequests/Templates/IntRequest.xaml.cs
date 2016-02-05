using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class IntRequest : ContentView
	{
		public IntRequest ()
		{
			InitializeComponent ();
		}
	}
	public class dic : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			double ret = value == null ? 0 : (int)value;
			return ret;
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			int val = 0;
			int.TryParse ((value as String) ?? "", out val);
			return val;
		}
		#endregion
	}
}

