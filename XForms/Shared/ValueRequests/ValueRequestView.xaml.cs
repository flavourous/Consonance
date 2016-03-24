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
			rowViews.Clear ();
			titleViews.Clear ();
			inputs.RowDefinitions.Clear ();
			inputs.Children.Clear ();
			vlm.ClearListens ();
		}
		List<View> rowViews = new List<View>();
		Dictionary<View,View> titleViews = new  Dictionary<View, View>();
		public void AddRow(View forRow) { InsertRow (rowViews.Count, forRow); }
		public void InsertRow(int idx, View forRow)
		{
			inputs.RowDefinitions.Insert (idx, new RowDefinition { Height = GridLength.Auto });
			var isn = (forRow.BindingContext as IValueRequestVM);

			// title maueb
			if (isn.showName)
				inputs.Children.Add (titleViews [forRow] = RLab (isn.name));

			// view
			int col = isn.showName ? 1 : 0, colspan = isn.showName ? 1 : 2;
			forRow.SetValue (Grid.ColumnProperty, col);
			forRow.SetValue (Grid.ColumnSpanProperty, colspan);
			inputs.Children.Add (forRow);

			// indexing etc
			rowViews.Insert (idx, forRow);
			vlm.ListenForValid ((INotifyPropertyChanged)forRow.BindingContext);

			//process after
			for (int i = 0; i < rowViews.Count; i++) {
				if((int)rowViews[i].GetValue (Grid.RowProperty) != i)
					rowViews[i].SetValue (Grid.RowProperty, i);
				if (titleViews.ContainsKey (rowViews [i]) 
					&& (int)titleViews [rowViews [i]].GetValue (Grid.RowProperty) != i)
					titleViews [rowViews [i]].SetValue (Grid.RowProperty, i);
			}
		}

		View RLab(String name)
		{
			return new Frame { 
				Content = new Label {
					Text = name, 
					HorizontalOptions = LayoutOptions.End, 
					VerticalOptions = LayoutOptions.Center
				}, 
				Padding = new Thickness (5.0, 0, 3.0, 0)
			};
		}
		public void RemoveRow(int row)
		{
			var v = rowViews [row];
			inputs.Children.Remove (v);
			rowViews.Remove (v);
			if (titleViews.ContainsKey (v)) {
				inputs.Children.Remove (titleViews [v]);
				titleViews.Remove (v);
			}
			vlm.RemoveListen (v.BindingContext as INotifyPropertyChanged);
		}
		void Completed(bool suc)
		{
			completed (suc);
			ClearRows ();
		}
		protected override bool OnBackButtonPressed ()
		{
			Completed (false);
			return base.OnBackButtonPressed ();
		}
		public Action<bool> completed = delegate { };
		ValidListenManager vlm = new ValidListenManager ("valid");
		InvalidRedConverter invrc = new InvalidRedConverter ();
		public void OKClick(object sender, EventArgs args) 
		{
			invrc.ignore = false;
			
			if(vlm.Valid) Completed (true); 
			else UserInputWrapper.message("Fix the invalid input first");
		}
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
			currentValidity [obj] = (bool)obj.GetType ().GetProperty (ValidName).GetValue (obj);
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

