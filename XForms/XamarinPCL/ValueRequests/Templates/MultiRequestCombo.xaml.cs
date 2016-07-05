using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;
using LibSharpHelp;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class MultiRequestCombo : ContentView
	{
		public MultiRequestCombo()
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
			var bc = BindingContext as IValueRequest<MultiRequestOptionValue>;	
			bc.value.SelectedRequest = psel.SelectedIndex;
            contv.Content = vrts[psel.SelectedIndex];
			brent = true;
			bc.value =bc.value; // hax lol firing changed method..
			brent=false;
		}
		bool brent = false;
        List<ValueRequestTemplate> vrts = new List<ValueRequestTemplate>();
		void Bc_PropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (brent) return;
			if (e.PropertyName == "value") {
				var bc = BindingContext as IValueRequest<MultiRequestOptionValue>;	
				brent = true; // this will alter selectedindex, which will alter the bound selectedoption, which we dont want to do.
				psel.Items.Clear ();
                vrts.Clear();
				brent = false;
				// ok, value could be null!
				if (bc.value != null && bc.value.IValueRequestOptions != null) {
                    vrts.AddAll(from t in bc.value.IValueRequestOptions.OfType<Func<ValueRequestTemplate>>() select t());
                    foreach (var ivr in vrts)
                        psel.Items.Add((ivr.BindingContext as IValueRequestVM).name);
					psel.SelectedIndex = bc.value.SelectedRequest;
				} else psel.SelectedIndex = -1;
			}
		}
	}
}

