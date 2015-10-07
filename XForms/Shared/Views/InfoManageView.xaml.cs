using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class InfoManageView : ContentPage
	{
		private bool mchoiceEnabled;
		public bool choiceEnabled {
			get { return mchoiceEnabled; }
			set {
				mchoiceEnabled = value;
				OnPropertyChanged ("choiceEnabled");
			}
		}
		private bool mmanageEnabled;
		public bool manageEnabled {
			get { return mmanageEnabled; }
			set {
				mmanageEnabled = value;
				if (value) infoList.ItemTemplate = Resources ["dt_noact"] as DataTemplate;
				else infoList.ItemTemplate = Resources ["dt_act"] as DataTemplate;
			}
		}
		private InfoLineVM mselectedItem;
		public InfoLineVM selectedItem {
			get { return mselectedItem; }
			set {
				mselectedItem = value;
				OnPropertyChanged ("selectedItem");
			}
		}
		public InfoLineVM initiallySelectedItem { get; set; }
		ObservableCollection<InfoLineVM> _Items;
		public ObservableCollection<InfoLineVM> Items
		{
			get { return _Items; }
			set {
				_Items = value;
				OnPropertyChanged ("Items");
			}
		}
		public TaskCompletionSource<InfoLineVM> completedTask;
		public InfoManageType imt;
		public InfoManageView ()
		{
			InitializeComponent ();
			BindingContext = this;
		}
		protected override bool OnBackButtonPressed ()
		{
			completedTask.SetResult (initiallySelectedItem); // signals no selection change
			return base.OnBackButtonPressed ();
		}

		void OnChoose(Object sender, EventArgs args) { completedTask.SetResult (selectedItem); Navigation.PopAsync (); }
		void OnNothing(Object sender, EventArgs args) { completedTask.SetResult (null); Navigation.PopAsync (); }

		// info hooks
		public event Action<InfoManageType> ItemAdd = delegate { };
		void OnItemAdd(Object sender, EventArgs args) { ItemAdd(imt); }
		public event Action<InfoManageType, InfoLineVM> ItemEdit = delegate { };
		void OnItemEdit(Object s, EventArgs e) { ItemEdit (imt, (((MenuItem)s).BindingContext as InfoLineVM)); }
		public event Action<InfoManageType, InfoLineVM> ItemDelete = delegate { };
		void OnItemDelete(Object s, EventArgs e) { ItemDelete (imt, (((MenuItem)s).BindingContext as InfoLineVM)); }
	}
}