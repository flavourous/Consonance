using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class InfoSelectRequest : ContentView
	{
		public InfoSelectRequest ()
		{
			InitializeComponent ();
		}
	}
	class SValConverter : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var isv = (value as InfoSelectValue);
			return isv.selected < isv.choices.Count && isv.selected > -1 ? isv.choices [isv.selected].name : "None Selected";
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
	class ISVConverter : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var ls = new List<String> ();
			foreach (var s in (value as InfoSelectValue).choices)
				ls.Add (s.name);
			return ls;
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}

