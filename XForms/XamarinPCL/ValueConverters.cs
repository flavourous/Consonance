using System;
using Xamarin.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Consonance.XamarinFormsView.PCL
{

    class XorParam : IValueConverter
    {
        #region IValueConverter implementation
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var invert = ((String)parameter) == "true";
            var visible = (bool)value;
            return (invert ^ visible);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    // one of these is attached to a date picker and a time picker
    public class DateTimeStateConverter : IValueConverter
    {
        DateTime current;

        //Read
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // null override
            if (value == null) return null;

            // valid.
            if(value is DateTime)
            {
                // Always store what's just bean read in.
                current = (DateTime)value;
                // date picker - easy
                if (targetType == typeof(DateTime))
                {
                    Debug.WriteLine("Converting " + value + " to datetime, which is itself " + current);
                    return current;
                }
                // timepicker 
                else if (targetType == typeof(TimeSpan))
                {
                    var ts = new TimeSpan(current.Hour, current.Minute, current.Second);
                    Debug.WriteLine("Converting " + value + " to timespan " + ts);
                    return ts;
                }
                throw new NotImplementedException("invalid target");
            }
            throw new NotImplementedException("invalid value type");
        }

        //Write
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // null override
            if (value == null) return null;

            //valid.
            if (targetType == typeof(DateTime))
            {
                // from datepicker
                if (value is DateTime)
                {
                    var dt = (DateTime)value;
                    var ndt = new DateTime(dt.Year, dt.Month, dt.Day,
                        current.Hour, current.Minute, current.Second);
                    Debug.WriteLine("Incoming datetime from datepicker is " + dt + ", we had: " + current + " which becomes: " + ndt);
                    return current = ndt;
                }
                // from timepikcer
                else if (value is TimeSpan)
                {
                    var ts = (TimeSpan)value;
                    if (ts.TotalHours > 24.0) ts = new TimeSpan(0, 0, 0);
                    var ndt = new DateTime(current.Year, current.Month, current.Day,
                        ts.Hours, ts.Minutes, ts.Seconds);
                    Debug.WriteLine("Incoming timespan from timepicker is " + ts + ", we had: " + current + " which becomes: " + ndt);
                    return current = ndt;
                }
                throw new NotImplementedException("invalid value type");
            }
            throw new NotImplementedException("invalid target type!");
        }
    }

    public class TimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime)
            {
                return String.Format("{0:HH:mm}",(DateTime)value);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class KVPListConverter : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			List<String> kls = new List<string> ();
			var kl = value as KVPList<String,double>;
			foreach (var kv in kl ?? new KVPList<string, double>())
				kls.Add (kv.Key + ": " + kv.Value);
			return String.Join ("\n", kls.ToArray ());
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
	public class IntToStringConverter : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value == null ? "" : value.ToString ();
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			int val = 0;
			int.TryParse ((value as String) ?? "", out val);
			return val;
		}
		#endregion
	}
	public class IntToDoubleConverter : IValueConverter
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
    public class InvalidRedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && targetType == typeof(Color))
                return (bool)value ? Color.Transparent : Color.Red;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

