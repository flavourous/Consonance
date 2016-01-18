using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class ValueRequestTemplate : ContentView
	{
		#if DEBUG
		static bool testingComplete = false;
		#endif
		static readonly Dictionary<Type, Func<View>> templateSelector= new Dictionary<Type, Func<View>> {
			{ typeof(String), () => new StringRequest () },
			{ typeof(double), () => new DoubleRequest () },
			{ typeof(TimeSpan), () => new TimeSpanRequest () },
			{ typeof(DateTime), () => new DateTimeRequest () },
			{ typeof(bool), () => new BoolRequest () },
			{ typeof(InfoSelectValue), () => new InfoSelectRequest () },
			{ typeof(EventArgs), () => new ActionRequest () },
			{ typeof(Barcode), () => new BarcodeRequest () },
			{ typeof(int), () => new IntRequest () },
			{ typeof(OptionGroupValue), () => new OptionGroupValueRequest () },
			{ typeof(RecurrsEveryPatternValue), () => new RecurrsEveryPatternValueRequest() },
			{ typeof(RecurrsOnPatternValue), () => new RecurrsOnPatternValueRequest() }
		};
		public ValueRequestTemplate ()
		{
			InitializeComponent ();
			#if DEBUG
			if(!testingComplete)
			{
				foreach(var kv in templateSelector)
				{
					try { var view = kv.Value(); }
					catch(Exception e) {
						Console.WriteLine("Creating "+kv.Key.ToString()+" template failed");
						Console.WriteLine(e.ToString());
					}
				}
				testingComplete = true;
			}
			#endif
		}
		protected override void OnBindingContextChanged ()
		{
			if (BindingContext == null || BindingContext.GetType ().GetGenericArguments ().Length == 0)
				Content = new Frame ();
			else Content = templateSelector [BindingContext.GetType ().GetGenericArguments () [0]]();
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

