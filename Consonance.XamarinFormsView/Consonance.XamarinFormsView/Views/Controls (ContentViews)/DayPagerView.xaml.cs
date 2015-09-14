using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class DayPagerView : ContentView
	{
		public DayPagerView ()
		{
			InitializeComponent ();
		}
		DateTime dayWrapper 
		{
			get { return (BindingContext as IView).day; }
			set { (BindingContext as IView).day = value; }
		}
		void OnPrev() { dayWrapper = dayWrapper.AddDays (-1); }
		void OnNext() { dayWrapper = dayWrapper.AddDays (+1); }
	}
}

