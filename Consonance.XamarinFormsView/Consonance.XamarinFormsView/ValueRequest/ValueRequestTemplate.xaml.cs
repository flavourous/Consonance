using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class ValueRequestTemplate : ContentView
	{
		public ValueRequestTemplate ()
		{
			InitializeComponent ();
		}
		Dictionary<Type, View> templateSelector = new Dictionary<Type, View> {
			{ typeof(String), new StringRequest() },
			{ typeof(double), new DoubleRequest() },
			{ typeof(TimeSpan), new TimeSpanRequest() },
			{ typeof(DateTime), new DateTimeRequest() },
			{ typeof(bool), new BoolRequest() },
			{ typeof(InfoSelectValue), new InfoSelectRequest() },
		};
		protected override void OnBindingContextChanged ()
		{
			if (BindingContext == null || BindingContext.GetType ().GetGenericArguments ().Length == 0)
				Content = new Frame ();
			else Content = templateSelector [BindingContext.GetType ().GetGenericArguments () [0]];
			base.OnBindingContextChanged ();
		}
	}


}

