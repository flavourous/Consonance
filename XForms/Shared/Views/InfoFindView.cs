using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace Consonance.XamarinFormsView
{
	public partial class InfoFindView : ContentPage
	{
		public InfoFindView ()
		{
			InitializeComponent ();
		}
		public ObservableCollection<String> SearchModes {get;set;}
		public ObservableCollection<InfoLineVM> Items { get; set; }
		ValueRequestFactory myFactory = new ValueRequestFactory();
		TaskCompletionSource<InfoLineVM> tcs;
		IFindList<InfoLineVM> finder;
		public Task<InfoLineVM> Choose(IFindList<InfoLineVM> ifnd)
		{
			this.finder = ifnd;
			this.tcs = new TaskCompletionSource<InfoLineVM> ();
			SearchModes = new ObservableCollection<String> (ifnd.FindModes);
			OnPropertyChanged ("SearchModes");
			return tcs.Task;
		}
		void UseMode(Object sender, EventArgs args)
		{
			String mode = SearchModes [smodes.SelectedIndex];
			requestStack.Children.Clear ();
			foreach (Object rview in finder.UseFindMode (mode, myFactory))
				requestStack.Children.Add (rview as View);	
		}
		void DoFind(object sender, EventArgs nope)
		{
			Items = new ObservableCollection<InfoLineVM> (finder.Find ());
			OnPropertyChanged ("Items");
		}
		public void OnChosen(object sender, EventArgs nope)
		{
			tcs.SetResult(InfoList.SelectedItem as InfoLineVM);
			Navigation.PopAsync ();
		}
	}
}

