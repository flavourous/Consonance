using System;
using System.Collections.Generic;
using Xamarin.Forms;
namespace Consonance.XamarinFormsView.PCL
{
	public partial class MyStepper : ContentView
	{
		public MyStepper () { InitializeComponent (); ValuePropertyChanged (this, null, ValueProperty.DefaultValue); }
		public int Value { get { return (int)GetValue (ValueProperty); } set { SetValue (ValueProperty, value); } }
		public static readonly BindableProperty ValueProperty = BindableProperty.Create ("Value", typeof(int), typeof(MyStepper), 0, BindingMode.TwoWay, null, ValuePropertyChanged);
		static void ValuePropertyChanged(BindableObject source, object oldValue, object newValue)
		{
			var ss = (source as MyStepper);
			ss.displayLabel.Text = ((newValue is int ? (int)newValue : ValueProperty.DefaultValue) + " " + ss.Units).Trim ();
		}
		public String Units { get { return(String)GetValue (UnitsProperty); } set { SetValue (UnitsProperty, value); } }
		public static readonly BindableProperty UnitsProperty = BindableProperty.Create ("Units", typeof(String), typeof(MyStepper), "", BindingMode.OneWay, null, ValuePropertyChanged);
		public void OnNext(Object s, EventArgs e) { Value++; }
		public void OnPrev(Object s, EventArgs e) { Value--; }
	}
}