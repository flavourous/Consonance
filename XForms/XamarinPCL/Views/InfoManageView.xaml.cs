using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Runtime.CompilerServices;
using System.Collections;
using LibSharpHelp;
using System.Collections.Specialized;
using System.Diagnostics;
using Consonance.Protocol;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class InfoManageView : ContentPage
	{
		private InfoLineVM mselectedItem;
		public InfoLineVM selectedItem {
			get { return mselectedItem; }
			set {
				var val = value ?? noth;
                foreach (var it in Items) // subvert reference comparison
                    if (OriginatorVM.OriginatorEquals(it, val))
                        mselectedItem = it;
				OnPropertyChanged ("selectedItem");
			}
		}
		public InfoLineVM initiallySelectedItem { get; set; }

        public static InfoLineVM noth = new InfoLineVM { name = "Nothing" };
        TaggedObservableCollection<InfoLineVM> _Items;
		public IList<InfoLineVM> Items
		{
			get { return _Items; }
			set {
				_Items = new TaggedObservableCollection<InfoLineVM>(noth, value);
				OnPropertyChanged ("Items");
			}
		}
		protected override void OnPropertyChanged ([CallerMemberName] string propertyName = null)
		{
			App.UIThread (() =>
				base.OnPropertyChanged (propertyName)
			);
		}
		public TaskCompletionSource<InfoLineVM> completedTask;
		public InfoManageView (bool choice, bool manage)
		{
			InitializeComponent ();
			BindingContext = this;

            // menuitems and actual itemtemplate
            String res = "dt_none";
            if (choice && manage) res = "dt_both";
            else if (choice) res = "dt_choose";
            else if (manage) res = "dt_manage";
			infoList.ItemTemplate = Resources [res] as DataTemplate;

            // add button
            if (!manage) ToolbarItems.Remove(AddToolbarItem);
        }
		protected override bool OnBackButtonPressed ()
		{
			completedTask.SetResult (initiallySelectedItem); // signals no selection change
			return base.OnBackButtonPressed ();
		}

        bool completed = false;
		async void OnChoose(Object s, EventArgs e) 
		{
            if (completed) return; completed = true;
			await Navigation.PopAsync (); // pop furst AVOID BUG?
			completedTask.SetResult (selectedItem == noth ? null : selectedItem); 
		}

		// info hooks
		public event Action ItemAdd = delegate { };
		void OnItemAdd(Object s, EventArgs ea) { ItemAdd(); }
		public event Action<InfoLineVM> ItemEdit = delegate { };
		void OnItemEdit(Object s, EventArgs e) {
			var b = (((MenuItem)s).BindingContext as InfoLineVM);
			if (b != noth)
				ItemEdit (b);
		}
		public event Action<InfoLineVM> ItemDelete = delegate { };
		void OnItemDelete(Object s, EventArgs e) {
			var b = (((MenuItem)s).BindingContext as InfoLineVM);
			if (b != noth)
				ItemDelete (b); 
		}
	}
}