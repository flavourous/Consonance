using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Reflection;
using System.Threading.Tasks;

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
			var vm = BindingContext as ValueRequestVM<InfoSelectValue>;
			chooseview.name = vm.name;
			await Navigation.PushAsync (chooseview);
			var vv = vm.value;
			vv.selected = await chooseview.ChooseFrom (vv.choices, vv.selected);
			vm.value = vv; // so things fire...
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

