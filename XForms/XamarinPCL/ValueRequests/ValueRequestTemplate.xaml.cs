using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;
using System.Diagnostics;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class ValueRequestTemplate : ContentView
	{
		public ValueRequestTemplate (View Child)
		{
			InitializeComponent ();
            fc.Content = Child;
		}
	}

}

