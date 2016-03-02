using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class InfoLineVM_Row : ContentView
	{
		public InfoLineVM_Row ()
		{
			InitializeComponent ();
		}

		// chosen
		public event EventHandler Chosen = delegate { };
		private void OnChosen(Object oo, EventArgs ea) { Chosen (oo, ea); }

		// Choosable
		public static BindableProperty ChooseableProperty = BindableProperty.Create<InfoLineVM_Row, bool>(r=> r.Chooseable, false, BindingMode.OneWay, null, (a,b,c) => (a as InfoLineVM_Row).CheckButtonState ());
		public bool Chooseable { get { return (bool)GetValue (ChooseableProperty); } set { SetValue (ChooseableProperty, value); } }

		// Currenytly selceted vm from list
		public static BindableProperty CurrentlySelectedProperty = BindableProperty.Create<InfoLineVM_Row, InfoLineVM>(r=>r.CurrentlySelected, null,BindingMode.OneWay,null,(a,b,c) => (a as InfoLineVM_Row).CheckButtonState());
		public InfoLineVM CurrentlySelected { get { return (InfoLineVM)GetValue (CurrentlySelectedProperty); } set { SetValue (CurrentlySelectedProperty, value); } }

		// when choose can show.
		void CheckButtonState() 
		{
			cb.IsVisible = Chooseable && CurrentlySelected == BindingContext && BindingContext != null;
		}
	}
}

