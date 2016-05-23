using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;
using LibSharpHelp;

namespace Consonance.XamarinFormsView.PCL
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
			if (bc != null) {
				bc.PropertyChanged += Bc_PropertyChanged;
				Bc_PropertyChanged (bc, new PropertyChangedEventArgs ("value"));
			}
		}
		void SIC(object sender, EventArgs args)
		{
			if (brent) return;
			var bc = BindingContext as IValueRequest<OptionGroupValue>;	
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
				var bc = BindingContext as IValueRequest<OptionGroupValue>;	
				brent = true; // this will alter selectedindex, which will alter the bound selectedoption, which we dont want to do.
				psel.Items.Clear (); 
				brent = false;
				// ok, value could be null!
				if (bc.value != null && bc.value.OptionNames != null) {
					psel.Items.AddAll (bc.value.OptionNames);
					psel.SelectedIndex = bc.value.SelectedOption;
				} else psel.SelectedIndex = -1;
			}
		}
	}
}

