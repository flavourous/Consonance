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
		}
		public ObservableCollection<InfoLineVM> Items { get; set; }
		TaskCompletionSource<int> tcs;
		public Task<int> ChooseFrom(IReadOnlyList<InfoLineVM> infos)
		{
			this.tcs = new TaskCompletionSource<int> ();
			Items = new ObservableCollection<InfoLineVM> (infos);
			OnPropertyChanged ("Items");
			return tcs.Task;
		}
		public void OnChosen(object sender, EventArgs nope)
		{
			tcs.SetResult(Items.IndexOf (InfoList.SelectedItem as InfoLineVM));
			Navigation.PopAsync ();
		}
	}
}

