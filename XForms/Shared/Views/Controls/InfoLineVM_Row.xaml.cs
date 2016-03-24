using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public class WithChoosableConverter : IValueConverter
	{
		public Predicate Chooseable = () => false;
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value != null && (bool)value && Chooseable();
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
	
	public partial class InfoLineVM_Row : ContentView
	{
		public InfoLineVM_Row ()
		{
			InitializeComponent ();
			(Resources ["withChooseable"] as WithChoosableConverter).Chooseable = () => this.Chooseable;
		}

		// chosen
		public event EventHandler Chosen = delegate { };
		private void OnChosen(Object oo, EventArgs ea) { Chosen (oo, ea); }

		// Choosable
		public static BindableProperty ChooseableProperty = BindableProperty.Create<InfoLineVM_Row, bool>(r=> r.Chooseable, false, BindingMode.OneWay, null, (a,b,c) => (a as InfoLineVM_Row).CheckButtonState ());
		public bool Chooseable { get { return (bool)GetValue (ChooseableProperty); } set { SetValue (ChooseableProperty, value); } }

		void CheckButtonState() 
		{
			OnBindingContextChanged (); 
		}
	}
}

