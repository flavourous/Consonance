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
			var vm = BindingContext as ValueRequestVM<InfoSelectValue>;
			vm.value.OnChoose();
		}
	}
}

