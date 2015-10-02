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
		public void OnClicked(object s, EventArgs e)
		{
			// force binding update for action fire.
			(BindingContext as IValueRequest<EventArgs>).value = new EventArgs ();
		}
	}
}

