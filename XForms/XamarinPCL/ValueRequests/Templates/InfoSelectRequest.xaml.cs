using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Reflection;
using System.Threading.Tasks;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class InfoSelectRequest : ContentView
	{
		public InfoSelectRequest ()
		{
			InitializeComponent ();
		}
        bool block_reentrancy = false;
		public async void OnChoose(object sender, EventArgs nooopse) // it's an event handler...async void has to be
		{
            if (block_reentrancy) return;
			var vm = BindingContext as IValueRequest<InfoSelectValue>;
            block_reentrancy = true;
			await vm.value.OnChoose();
            block_reentrancy = false;
        }
	}
	class InfoSelectRequestConverter : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var sv = value as InfoSelectValue;
			return sv == null || sv.selected == null ? "None" : sv.selected.name;
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}

