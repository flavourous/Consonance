using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Runtime.CompilerServices;

namespace Consonance.XamarinFormsView
{
	public partial class InfoManageView : ContentPage
	{
		
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
		protected override void OnPropertyChanged ([CallerMemberName] string propertyName = null)
		{
			Device.BeginInvokeOnMainThread (() =>
				base.OnPropertyChanged (propertyName));
		}
		public TaskCompletionSource<InfoLineVM> completedTask;
		public InfoManageType imt;
		public InfoManageView (bool choice, bool manage)
		{
			InitializeComponent ();
			BindingContext = this;

			if (choice) {
				ToolbarItems.Add (new ToolbarItem ("Nothing", null, OnNothing));
				ToolbarItems.Add (new ToolbarItem ("Choose", null, OnChoose));
			}
			if (manage) {
				infoList.ItemTemplate = Resources ["dt_act"] as DataTemplate;
				ToolbarItems.Add (new ToolbarItem ("Add", null, OnItemAdd));
			} 
			else infoList.ItemTemplate = Resources ["dt_noact"] as DataTemplate;

		}
		protected override bool OnBackButtonPressed ()
		{
			completedTask.SetResult (initiallySelectedItem); // signals no selection change
			return base.OnBackButtonPressed ();
		}

		void OnChoose() { completedTask.SetResult (selectedItem); Navigation.PopAsync (); }
		void OnNothing() { completedTask.SetResult (null); Navigation.PopAsync (); }

		// info hooks
		public event Action<InfoManageType> ItemAdd = delegate { };
		void OnItemAdd() { ItemAdd(imt); }
		public event Action<InfoManageType, InfoLineVM> ItemEdit = delegate { };
		void OnItemEdit(Object s, EventArgs e) { ItemEdit (imt, (((MenuItem)s).BindingContext as InfoLineVM)); }
		public event Action<InfoManageType, InfoLineVM> ItemDelete = delegate { };
		void OnItemDelete(Object s, EventArgs e) { ItemDelete (imt, (((MenuItem)s).BindingContext as InfoLineVM)); }
	}
}