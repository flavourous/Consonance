using System;
using System.Collections.Generic;
using Xamarin.Forms;
using LibRTP;
using System.Text;
using LibSharpHelp;
using System.Diagnostics;

namespace Consonance.XamarinFormsView
{
	public partial class RecurrsOnPatternValueRequest : ContentView
	{
		public RecurrsOnPatternValueRequest ()
		{
			InitializeComponent ();
		}
	}

	public class RecurrsOnPatternValueRequestConverter : IValueConverter
	{
		RecurrsOnPatternValue reference = new RecurrsOnPatternValue(); // null prtoection
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// Keep a reference to the original value here.
			if (!Object.ReferenceEquals(reference,value)) 
				reference = value as RecurrsOnPatternValue ?? new RecurrsOnPatternValue();

			switch ((String)parameter) {
			case "day": return GetOnValue(RecurrSpan.Day, true);
			case "daybox": return GetOnValue(RecurrSpan.Day, false);
			case "dayval": return GetValue(RecurrSpan.Day);
			case "week": return GetOnValue(RecurrSpan.Week, true);
			case "weekbox": return GetOnValue(RecurrSpan.Week, false);
			case "weekval": return GetValue(RecurrSpan.Week);
			case "month": return GetOnValue(RecurrSpan.Month, true);
			case "monthbox": return GetOnValue(RecurrSpan.Month, false);
			case "monthval": return GetValue(RecurrSpan.Month);
			case "year": return GetOnValue(RecurrSpan.Year, true);
			case "explanation":
				var f = new List<RecurrSpan> (reference.PatternType.SplitFlags ());
				if (f.Count < 2) return "You must select at least two time types.";
				StringBuilder ss = new StringBuilder ("Repeat on ");
				for (int i = 0; i < reference.PatternValues.Length; i++)
					ss.AppendFormat ("the {0} {1} of ", reference.PatternValues [i].WithSuffix (), f [i].AsString ());
				ss.AppendFormat ("the {0}", f [f.Count - 1].AsString ());
				return ss.ToString ();
			}

			// We should not be reaching here in a working application
			throw new NotImplementedException ();
		}
		bool GetOnValue (RecurrSpan flag, bool isflag)
		{
			var f = new List<RecurrSpan> (reference.PatternType.SplitFlags ());
			var fi = f.IndexOf (flag);
			return fi != -1 && (isflag || fi < f.Count - 1);
		}
		int GetValue (RecurrSpan flag)
		{
			var f = new List<RecurrSpan> (reference.PatternType.SplitFlags ());
			var fi = f.IndexOf (flag);
			if(fi != -1 && fi < f.Count - 1)
				return reference.PatternValues [fi];
			return 0;
		}
		void SetValue(RecurrSpan flag, Object value)
		{
			var f = new List<RecurrSpan> (reference.PatternType.SplitFlags ());
			var fi = f.IndexOf (flag);
			if (fi != -1 && fi < f.Count - 1)
				reference.PatternValues [f.IndexOf (flag)] = ConvertHelp.ToInt(value);
		}
		void SetValue(RecurrSpan flag, bool value)
		{
			var f = new List<RecurrSpan> (reference.PatternType.SplitFlags ());
			var fi = f.IndexOf (flag);
			if ((fi != -1) ^ value) 
			{
				// Remember what we got right now...
				Dictionary<RecurrSpan, int> currvals = new Dictionary<RecurrSpan, int> ();
				for (int i = 0; i < reference.PatternValues.Length; i++)
					currvals [f [i]] = reference.PatternValues [i];

				// new flags
				var vals = new List<int>(reference.PatternValues);
				if (value) {
					reference.PatternType |= flag;
					vals.Insert (new List<RecurrSpan> (reference.PatternType.SplitFlags ()).IndexOf (flag), 1);
				}
				else {
					reference.PatternType ^= flag;
					if(fi < f.Count - 1) vals.RemoveAt (f.IndexOf (flag));
				}
				reference.PatternValues = vals.ToArray ();
			}
			// otherwise theres nowt to do.
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			Debug.WriteLine ("I am " + value.GetType ());
			if(value != null)
			switch ((String)parameter) 
			{
				case "day": SetValue(RecurrSpan.Day, (bool)value); break;
				case "dayval": SetValue(RecurrSpan.Day, value); break;
				case "week": SetValue(RecurrSpan.Week, (bool)value); break;
				case "weekval": SetValue(RecurrSpan.Week, value); break;
				case "month": SetValue(RecurrSpan.Month, (bool)value); break;
				case "monthval": SetValue(RecurrSpan.Month, value); break;
				case "year": SetValue(RecurrSpan.Year, (bool)value); break;
			}

			return reference;
		}
		#endregion
	}

}

