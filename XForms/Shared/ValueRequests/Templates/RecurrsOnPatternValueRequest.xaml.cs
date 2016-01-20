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
			case "yearval": return GetValue(RecurrSpan.Year);
			case "yearbox": return false;
			case "explain-day": return GetExplain (RecurrSpan.Day);
			case "explain-week": return GetExplain (RecurrSpan.Week);
			case "explain-month": return GetExplain (RecurrSpan.Month);
			case "explain-year": return GetExplain (RecurrSpan.Year);
			}
			// We should not be reaching here in a working application
			throw new NotImplementedException ();
		}
		String GetExplain(RecurrSpan flag)
		{
			var f = new List<RecurrSpan> (reference.PatternType.SplitFlags ());
			if (f.Count < 2) return "-";
			int fidx = f.IndexOf (flag);
			if(fidx == -1) return "";
			if(fidx == 0) return "On the " + reference.PatternValues [fidx].WithSuffix () + " " + flag.AsString ();
			if (fidx == f.Count - 1) return "of the " + flag.AsString ();
			return "of the " + reference.PatternValues [fidx].WithSuffix () + " " + flag.AsString ();
		}
		bool GetOnValue (RecurrSpan flag, bool isflag)
		{
			var f = new List<RecurrSpan> (reference.PatternType.SplitFlags ());
			var fi = f.IndexOf (flag);
			return fi != -1 && (isflag || fi < f.Count - 1);
		}
		String GetValue (RecurrSpan flag)
		{
			String ret = "-";
			var f = new List<RecurrSpan> (reference.PatternType.SplitFlags ());
			var fi = f.IndexOf (flag);
			if(fi != -1 && fi < f.Count - 1)
				ret = reference.PatternValues [fi].ToString();
			return ret;
		}
		void SetValue(RecurrSpan flag, Object value)
		{
			if (value.ToString() == "-") value = 0;
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
					var fidx = new List<RecurrSpan> (reference.PatternType.SplitFlags ()).IndexOf (flag);
					if (fidx < vals.Count) vals.Insert (fidx, 1);
					else vals.Add (1);
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

