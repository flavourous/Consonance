using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class DayPagerView : ContentView
	{
		public DayPagerView ()
		{
			InitializeComponent ();
			next.Text = ">>";
			prev.Text = "<<";
		}
		DateTime dayWrapper 
		{
			get { return (BindingContext as IView).day; }
			set { (BindingContext as ViewWrapper).ChangeDay(value); }
		}
		void OnPrev(Object s, EventArgs e) { dayWrapper = dayWrapper.AddDays (-1); }
		void OnNext(Object s, EventArgs e) { dayWrapper = dayWrapper.AddDays (+1); }
	}
	public class DPConv : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null) return "Loading...";
			return ((DateTime)value).ToString("dd-MMM-yyyy dddd");
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
		BoxView b;
	}

}

