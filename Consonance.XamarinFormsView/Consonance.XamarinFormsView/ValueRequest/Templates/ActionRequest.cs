using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class ActionRequest : ContentView
	{
		public ActionRequest ()
		{
			InitializeComponent ();
		}
		public void OnClicked()
		{
			// force binding update for action fire.
			(BindingContext as ValueRequestVM<EventArgs>).value = new EventArgs ();
		}
	}
}

