using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;
using System.Diagnostics;

namespace Consonance.XamarinFormsView.PCL
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
			#if DEBUG
			try {
			#endif
			InitializeComponent ();
			#if DEBUG
			} catch(Exception e) {
				System.Diagnostics.Debugger.Break();
				throw;
			}
			#endif

			#if DEBUG
			if(!testingComplete)
			{
				foreach(var kv in templateSelector)
				{

                    Debug.WriteLine("testing " + kv.Key.ToString());
					try 
					{ 
						var view = kv.Value(); 
						view.BindingContext = Activator.CreateInstance(typeof(TestVM<>).MakeGenericType(kv.Key));
					}
					catch(Exception e) {
						Debug.WriteLine("Creating "+kv.Key.ToString()+" template failed");
                        Debug.WriteLine(e.ToString());
					}
				}
                Debug.WriteLine("testing complete");
				testingComplete = true;
			}
			#endif
		}
		INotifyPropertyChanged lastContext = null;
        public Frame pfc { get { return fc; } }
		protected override void OnBindingContextChanged ()
		{
            // select template
            App.platform.UIThread (() => {
				if (BindingContext != null && BindingContext.GetType ().GenericTypeArguments.Length > 0)
					fc.Content = templateSelector [BindingContext.GetType ().GenericTypeArguments [0]] ();
				else
					fc.Content = new Frame ();
			});

			base.OnBindingContextChanged ();
		}
	}

}

