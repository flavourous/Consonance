using System;
using Xamarin.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Consonance.Protocol;
using System.Collections.Specialized;
using LibSharpHelp;

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
        DateTime? current;
        DateTime lastKnown = DateTime.Now;

        //Read
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            // valid.
            // Always store what's just bean read in.
            current = value is DateTime ? (DateTime)value : value as DateTime?;
            lastKnown = current ?? lastKnown;
            // date picker - easy
            if (targetType == typeof(DateTime)) return lastKnown;
            // timepicker 
            else if (targetType == typeof(TimeSpan))
                return new TimeSpan(lastKnown.Hour, lastKnown.Minute, lastKnown.Second);
            // on/pff
            else if (targetType == typeof(bool)) return current.HasValue;
            throw new NotImplementedException("Conversion fallthrough");
        }

        //Write
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //valid.
            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                if (current.HasValue)
                {
                    // from datepicker
                    if (value is DateTime)
                    {
                        var dt = (DateTime)value;
                        current = lastKnown = new DateTime(dt.Year, dt.Month, dt.Day,
                            current.Value.Hour, current.Value.Minute, current.Value.Second);
                    }
                    // from timepikcer
                    else if (value is TimeSpan)
                    {
                        var ts = (TimeSpan)value;
                        if (ts.TotalHours > 24.0) ts = new TimeSpan(0, 0, 0);
                        current = lastKnown = new DateTime(
                            current.Value.Year, current.Value.Month, current.Value.Day,
                            ts.Hours, ts.Minutes, ts.Seconds);
                    }
                }
                if(value is bool)
                {
                    var b = (bool)value;
                    if(current.HasValue != b)
                    {
                        if (b) current = lastKnown;
                        else current = null;
                    }
                }
                return current;
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
				kls.Add (kv.Value + " " + kv.Key);
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
    public class InvalidConverter : IValueConverter
    {
        readonly object yes, no;
        public InvalidConverter(Object yes, Object no)
        {
            this.yes = yes;
            this.no = no;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return (bool)value ? yes : no;
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FirstTrackConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<TrackerTracksVM> v)
                return new FirstTrack(v);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        class FirstTrack : BindableObject
        {
            readonly IEnumerable<TrackerTracksVM> Tracks;
            public FirstTrack(IEnumerable<TrackerTracksVM> Tracks)
            {
                if (Tracks is INotifyCollectionChanged previous)
                {
                    previous.CollectionChanged += Previous_CollectionChanged;
                    this.Tracks = Tracks;
                    LookForFirst();
                }
            }

            private void Previous_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => LookForFirst();
            void LookForFirst()
            {
                if (Tracks == null || Tracks.Count() == 0) return;
                var track = Tracks.First().tracks;
                if (track == null || track.Count() == 0) return;
                var t = track.Take(1); // give enumerable...
                FirstTrackFirstItem = t;
            }

            public IEnumerable<TrackingInfoVM> FirstTrackFirstItem { get => GetValue(FirstTrackFirstItemProperty) as IEnumerable<TrackingInfoVM>; set => SetValue(FirstTrackFirstItemProperty, value); }
            public static BindableProperty FirstTrackFirstItemProperty = BindableProperty.Create("FirstTrackFirstItem", typeof(IEnumerable<TrackingInfoVM>), typeof(FirstTrack));
        }
    }
    
    class DebugBinding : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("(DebugBinding) " + parameter + "=" + value?.ToString() ?? "null");
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class DateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is DateTime d ? String.Format("{0:ddd} {1} {0:MMM yyyy}", d, d.Day.WithSuffix()) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

