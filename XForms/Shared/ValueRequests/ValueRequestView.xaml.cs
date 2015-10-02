using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class ValueRequestView : ContentPage
	{
		public ValueRequestView ()
		{
			InitializeComponent ();
			okButton.BindingContext = vlm;
		}
		public void ClearRows()
		{
			InputRows.Children.Clear ();
			vlm.ClearListens ();
		}
		public void AddRow(View forRow)
		{
			InputRows.Children.Add (forRow);
			vlm.ListenForValid ((INotifyPropertyChanged)forRow.BindingContext);
		}
		public void InsertRow(int idx, View forRow)
		{
			InputRows.Children.Insert(idx, forRow);
			vlm.ListenForValid ((INotifyPropertyChanged)forRow.BindingContext);
		}
		public void RemoveRow(int row)
		{
			var vm = InputRows.Children [row] as INotifyPropertyChanged;
			InputRows.Children.RemoveAt (row);
			vlm.RemoveListen (vm);
		}

		public Promise<bool> completed = async delegate { };
		ValidListenManager vlm = new ValidListenManager ("valid");
		public void OKClick(object sender, EventArgs args) { completed (true); }
		public void CancelClick(object sender, EventArgs args) { completed (false); }
	}
	class ValidListenManager : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		public void OnPropertyChanged(String prop) {
			PropertyChanged (this, new PropertyChangedEventArgs (prop));
		}
		#endregion
		private bool mValid;
		public bool Valid { get { return mValid; } set { mValid = value; OnPropertyChanged ("Valid"); } }

		readonly String ValidName;
		public ValidListenManager(String ValidName)
		{
			this.ValidName = ValidName;
		}

		Dictionary<INotifyPropertyChanged , bool> currentValidity = new Dictionary<INotifyPropertyChanged , bool>();
		public void ListenForValid(INotifyPropertyChanged obj)
		{
			currentValidity [obj] = false;
			obj.PropertyChanged += ValidityListener;
		}
		public void ClearListens()
		{
			foreach (var k in currentValidity.Keys)
				k.PropertyChanged -= ValidityListener;
			Valid = false;
			currentValidity.Clear ();
		}
		public void RemoveListen(INotifyPropertyChanged itm)
		{
			currentValidity.Remove (itm);
			itm.PropertyChanged -= ValidityListener;
		}
		void ValidityListener(Object sender, PropertyChangedEventArgs pea)
		{
			// oh hate reflection, but it's in the spirit of things.
			if(pea.PropertyName != ValidName) return;
			bool isValid = (bool)sender.GetType ().GetProperty (pea.PropertyName).GetValue (sender); 
			currentValidity [sender as INotifyPropertyChanged] = isValid;
			bool validCheck = true;
			foreach (var val in currentValidity.Values)
				if (!val) validCheck = false;
			Valid = validCheck;
		}
	}
}

