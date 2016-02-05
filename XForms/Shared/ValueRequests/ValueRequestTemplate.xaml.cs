using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;

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
		class TestVM<T> : INotifyPropertyChanged, IValueRequest<T>
		{
			public event PropertyChangedEventHandler PropertyChanged = delegate { };
			public event Action changed = delegate { };
			public void ClearListeners () { }
			public object request { get { return null; } }
			public bool enabled { get { return true; } set { } }
			public bool valid { get { return true; } set { } }
			public bool read_only { get { return false; } set { } }
			public String name { get { return "moo"; } }
			public T value { get { return default(T); } set { } }
		}
		public ValueRequestTemplate ()
		{
			InitializeComponent ();
			#if DEBUG
			if(!testingComplete)
			{
				foreach(var kv in templateSelector)
				{
					Console.WriteLine("testing " + kv.Key.ToString());
					try 
					{ 
						var view = kv.Value(); 
						view.BindingContext = Activator.CreateInstance(typeof(TestVM<>).MakeGenericType(kv.Key));
					}
					catch(Exception e) {
						Console.WriteLine("Creating "+kv.Key.ToString()+" template failed");
						Console.WriteLine(e.ToString());
					}
				}
				Console.WriteLine("testing complete");
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

