using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;

namespace Consonance.XamarinFormsView
{
	public partial class OptionGroupValueRequest : ContentView
	{
		public OptionGroupValueRequest ()
		{
			InitializeComponent ();
		}
		INotifyPropertyChanged old;
		protected override void OnBindingContextChanged ()
		{
			base.OnBindingContextChanged ();

			// binding
			var bc = BindingContext as INotifyPropertyChanged;
			if(old != null) old.PropertyChanged -= Bc_PropertyChanged;
			old = bc;
			bc.PropertyChanged += Bc_PropertyChanged;
			Bc_PropertyChanged (bc, new PropertyChangedEventArgs ("value"));
		}
		void SIC(object sender, EventArgs args)
		{
			var bc = BindingContext as ValueRequestVM<OptionGroupValue>;	
			bc.value.SelectedOption = psel.SelectedIndex;
			brent = true;
			bc.value =bc.value; // hax lol firing changed method..
			brent=false;
		}
		bool brent = false;
		void Bc_PropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (brent) return;
			if (e.PropertyName == "value") {
				psel.Items.Clear ();
				var bc = BindingContext as ValueRequestVM<OptionGroupValue>;	
				// ok, value could be null!
				if (bc.value != null && bc.value.OptionNames != null) {
					psel.Items.AddAll (bc.value.OptionNames);
					psel.SelectedIndex = bc.value.SelectedOption;
				}
			}
		}
	}
}

