using System;
using Xamarin.Forms;
using System.Collections.Generic;

namespace Consonance.XamarinFormsView
{
	public class KVPListConverter : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			List<String> kls = new List<string> ();
			var kl = value as KVPList<String,double>;
			foreach (var kv in kl)
				kls.Add (kv.Key + ": " + kv.Value);
			return String.Join ("\n", kls.ToArray ());
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}

