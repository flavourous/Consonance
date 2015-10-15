using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class ValueRequestTemplate : ContentView
	{
		readonly Dictionary<Type, View> templateSelector;
		public ValueRequestTemplate ()
		{
			InitializeComponent ();
			templateSelector = new Dictionary<Type, View> {
				{ typeof(String), new StringRequest () },
				{ typeof(double), new DoubleRequest () },
				{ typeof(TimeSpan), new TimeSpanRequest () },
				{ typeof(DateTime), new DateTimeRequest () },
				{ typeof(bool), new BoolRequest () },
				{ typeof(InfoSelectValue), new InfoSelectRequest () },
				{ typeof(EventArgs), new ActionRequest () },
				{ typeof(Barcode), new BarcodeRequest () }
			};
		}
		protected override void OnBindingContextChanged ()
		{
			if (BindingContext == null || BindingContext.GetType ().GetGenericArguments ().Length == 0)
				Content = new Frame ();
			else Content = templateSelector [BindingContext.GetType ().GetGenericArguments () [0]];
			base.OnBindingContextChanged ();
		}
	}
	class XorParam : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var invert = ((String)parameter) == "true";
			var visible = (bool)value;
			return (invert ^ visible);
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}

}

