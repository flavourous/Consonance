using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Reflection;

namespace Consonance.XamarinFormsView
{
	public partial class InfoSelectRequest : ContentView
	{
		public InfoSelectRequest ()
		{
			InitializeComponent ();
		}
		public async void OnChoose(object sender, EventArgs nooopse) // it's an event handler...async void has to be
		{
			var chooseview = new InfoChooseView ();
			var isv = BindingContext.GetType ().GetProperty ("value").GetValue (BindingContext) as InfoSelectValue;
			isv.selected = await chooseview.ChooseFrom (isv.choices);
			OnPropertyChanged("value");
			await Navigation.PushAsync (chooseview);
		}
	}
	class SValConverter : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var isv = (value as InfoSelectValue);
			return isv == null ? null : isv.selected < isv.choices.Count && isv.selected > -1 ? isv.choices [isv.selected].name : "None Selected";
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}

