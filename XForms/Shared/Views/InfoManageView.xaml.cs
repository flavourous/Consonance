using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class InfoManageView : ContentPage
	{
		ObservableCollection<InfoLineVM> _Items;
		public ObservableCollection<InfoLineVM> Items
		{
			get { return _Items; }
			set {
				_Items = value;
				OnPropertyChanged ("Items");
			}
		}
		public TaskCompletionSource<EventArgs> completedTask;
		public InfoManageType imt;
		public InfoManageView ()
		{
			InitializeComponent ();
			BindingContext = this;
		}
		protected override bool OnBackButtonPressed ()
		{
			completedTask.SetResult (new EventArgs ());
			return base.OnBackButtonPressed ();
		}

		// info hooks
		public event Action<InfoManageType> ItemAdd = delegate { };
		void OnItemAdd(Object sender, EventArgs args) { ItemAdd(imt); }
		public event Action<InfoManageType, InfoLineVM> ItemEdit = delegate { };
		void OnItemEdit(Object s, EventArgs e) { ItemEdit (imt, (((MenuItem)s).BindingContext as InfoLineVM)); }
		public event Action<InfoManageType, InfoLineVM> ItemDelete = delegate { };
		void OnItemDelete(Object s, EventArgs e) { ItemDelete (imt, (((MenuItem)s).BindingContext as InfoLineVM)); }
	}
}