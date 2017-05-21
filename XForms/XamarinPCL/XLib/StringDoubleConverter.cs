using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XLib
{
    public class StringDoubleConverter : IValueConverter
    {
        String last;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            last = value as String;
            double.TryParse(last, out double prs);
            return prs; // def 0
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double use = value is double ? (double)value : 0.0;
            double.TryParse(last, out double lprs);
            if (lprs == use) return last;
            var dc = 0.0.ToString("N1").TrimEnd('0').ToCharArray().Last();
            var trimmed= use.ToString("N3").TrimEnd('0');
            if (trimmed[trimmed.Length - 1] == dc) trimmed += '0';
            return trimmed;
        }
    }
}
