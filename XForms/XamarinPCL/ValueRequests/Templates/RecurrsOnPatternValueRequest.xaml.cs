using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Text;
using Consonance.Protocol;
using LibSharpHelp;
using System.Diagnostics;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class RecurrsOnPatternValueRequest : ContentView
	{
		public RecurrsOnPatternValueRequest ()
		{
			InitializeComponent ();
            //foreach (var c in mg.Children)
            //    c.PropertyChanged += (o, e) => mg.ForceLayout();
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

            switch ((String)parameter)
            {
                case "day": return GetOnValue(RecurrSpan.Day, true);
                case "daybox": return GetOnValue(RecurrSpan.Day, false);
                case "daycolor": return GetOnColor(RecurrSpan.Day);
                case "dayval": return GetValue(RecurrSpan.Day);
                case "week": return GetOnValue(RecurrSpan.Week, true);
                case "weekbox": return GetOnValue(RecurrSpan.Week, false);
                case "weekcolor": return GetOnColor(RecurrSpan.Week);
                case "weekval": return GetValue(RecurrSpan.Week);
                case "month": return GetOnValue(RecurrSpan.Month, true);
                case "monthbox": return GetOnValue(RecurrSpan.Month, false);
                case "monthcolor": return GetOnColor(RecurrSpan.Month);
                case "monthval": return GetValue(RecurrSpan.Month);
                case "year": return GetOnValue(RecurrSpan.Year, true);
                case "yearval": return GetValue(RecurrSpan.Year);
                case "yearbox": return false;
                case "yearcolor": return GetOnColor(RecurrSpan.Year);
                case "prefix-day": return GetPrefix(RecurrSpan.Day);
                case "prefix-week": return GetPrefix(RecurrSpan.Week);
                case "prefix-month": return GetPrefix(RecurrSpan.Month);
                case "prefix-year": return GetPrefix(RecurrSpan.Year);
                case "suffix-day": return GetSuffix(RecurrSpan.Day);
                case "suffix-week": return GetSuffix(RecurrSpan.Week);
                case "suffix-month": return GetSuffix(RecurrSpan.Month);
                case "suffix-year": return GetSuffix(RecurrSpan.Year);
            }
			// We should not be reaching here in a working application
			throw new NotImplementedException ();
		}
        String GetPrefix(RecurrSpan fl)
        {
            var f = new List<RecurrSpan>(((uint)reference.PatternType).SplitFlags<RecurrSpan>());
            int dx = f.IndexOf(fl);
            return dx == -1 ? "" : dx == 0 ? "The" : "of the";
        }
        String GetSuffix(RecurrSpan fl)
        {
            var f = new List<RecurrSpan>(((uint)reference.PatternType).SplitFlags<RecurrSpan>());
            int dx = f.IndexOf(fl);
            var nst = dx == -1 || dx == f.Count - 1 ? "" : reference.PatternValues[dx].Suffix();
            return nst + " " + LibRTP.PublicHelpers.AsString((LibRTP.RecurrSpan)fl);
        }
        Color GetOnColor(RecurrSpan f)
        {
            return GetOnValue(f, true) ? Color.Default : Color.FromRgba(.3, .3, .3, .5);
        }
		bool GetOnValue (RecurrSpan flag, bool isflag)
		{
			var f = new List<RecurrSpan> (((uint)reference.PatternType).SplitFlags<RecurrSpan> ());
			var fi = f.IndexOf (flag);
			return fi != -1 && (isflag || fi < f.Count - 1);
		}
		String GetValue (RecurrSpan flag)
		{
			String ret = " ";
			var f = new List<RecurrSpan> (((uint)reference.PatternType).SplitFlags<RecurrSpan> ());
			var fi = f.IndexOf (flag);
			if(fi != -1 && fi < f.Count - 1)
				ret = reference.PatternValues [fi].ToString();
			return ret;
		}
		void SetValue(RecurrSpan flag, Object value)
		{
			if (value.ToString() == " ") value = "0";
			var f = new List<RecurrSpan> (((uint)reference.PatternType).SplitFlags<RecurrSpan> ());
			var fi = f.IndexOf (flag);
			if (fi != -1 && fi < f.Count - 1)
				int.TryParse ((String)value, out reference.PatternValues [f.IndexOf (flag)]);
		}
		void SetValue(RecurrSpan flag, bool value)
		{
			var f = new List<RecurrSpan> (((uint)reference.PatternType).SplitFlags<RecurrSpan> ());
			var fi = f.IndexOf (flag);
			if ((fi != -1) ^ value) 
			{
				// Remember what we got right now...
				Dictionary<RecurrSpan, int> currvals = new Dictionary<RecurrSpan, int> ();
				for (int i = 0; i < reference.PatternValues.Length; i++)
					currvals [f [i]] = reference.PatternValues [i];

				// new flags
				if (value) reference.PatternType |= flag;
				else reference.PatternType ^= flag;

                // process values
                var newflags = new List<RecurrSpan>(((uint)reference.PatternType).SplitFlags<RecurrSpan>());
                var newvals = new List<int>();
                for (int i = 0; i < newflags.Count - 1; i++)
                {
                    var nf = newflags[i];
                    var have = currvals.ContainsKey(nf);
                    newvals.Add(have ? currvals[nf] : 1);
                }

				reference.PatternValues = newvals.ToArray ();
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

