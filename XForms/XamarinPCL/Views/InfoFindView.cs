using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using System.Threading.Tasks;
using Consonance.Protocol;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class InfoFindView : ContentPage
	{
		public InfoFindView (IValueRequestFactory bfact)
		{
            myFactory = bfact;
			InitializeComponent ();
			BindingContext = this;
		}
		public ObservableCollection<InfoLineVM> Items { get; set; }
		readonly IValueRequestFactory myFactory;
		TaskCompletionSource<InfoLineVM> tcs;
		IFindList<InfoLineVM> finder;
		public Task<InfoLineVM> Choose(IFindList<InfoLineVM> ifnd)
		{
			throw new NotImplementedException ();
			this.finder = ifnd;
			this.tcs = new TaskCompletionSource<InfoLineVM> ();
			smodes.Items.Clear ();
			foreach (String s in ifnd.FindModes)
				smodes.Items.Add (s);
			return tcs.Task;
		}
		void UseMode(Object sender, EventArgs args)
		{
			String mode = smodes.Items [smodes.SelectedIndex];
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

