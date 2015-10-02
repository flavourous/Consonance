using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace Consonance.XamarinFormsView
{
	public partial class InfoChooseView : ContentPage
	{
		public InfoChooseView ()
		{
			InitializeComponent ();
			BindingContext = this;
		}
		public String name { get; set; }
		public ObservableCollection<InfoLineVM> Items { get; set; }
		TaskCompletionSource<int> tcs;
		int isel;
		public Task<int> ChooseFrom(IReadOnlyList<InfoLineVM> infos, int isel)
		{
			this.tcs = new TaskCompletionSource<int> ();
			Items = new ObservableCollection<InfoLineVM> (infos);
			InfoList.SelectedItem = isel > -1 ? Items [isel] : null;
			this.isel = isel;
			OnPropertyChanged ("Items");
			return tcs.Task;
		}
		protected override bool OnBackButtonPressed ()
		{
			tcs.SetResult(isel);
			return base.OnBackButtonPressed ();
		}
		public void OnChosen(object sender, EventArgs nope)
		{
			tcs.SetResult(Items.IndexOf (InfoList.SelectedItem as InfoLineVM));
			Navigation.PopAsync ();
		}
		public void OnNone(object sender, EventArgs nope)
		{
			tcs.SetResult(-1);
			Navigation.PopAsync ();
		}
	}
}

