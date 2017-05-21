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
        public Command ChooseCommand { get; private set; }
		private InfoLineVM mselectedItem;
		public InfoLineVM selectedItem {
			get { return mselectedItem; }
			set {
				var val = value ?? noth;
                foreach (var it in Items) // subvert reference comparison
                    if (OriginatorVM.OriginatorEquals(it, val))
                        mselectedItem = it;
                ChooseCommand.ChangeCanExecute();
				OnPropertyChanged ("selectedItem");
			}
		}
		public InfoLineVM initiallySelectedItem { get; set; }

        public static InfoLineVM noth = new InfoLineVM { name = "Nothing" };
        IList<InfoLineVM> _Items;
		public IList<InfoLineVM> Items
		{
			get { return _Items; }
			set {
                _Items = choice ? new TaggedObservableCollection<InfoLineVM>(noth, value) : value;
                OnPropertyChanged ("Items");
			}
		}
		protected override void OnPropertyChanged ([CallerMemberName] string propertyName = null)
		{
			App.UIThread (() =>
				base.OnPropertyChanged (propertyName)
			);
		}
        // Fixme: generalize into ViewTask IInputResponse as an option
        public class ctr
        {
            public InfoLineVM vm;
            public bool popping;
        }
        bool choice = false;
		public TaskCompletionSource<ctr> completedTask;
		public InfoManageView (bool choice, bool manage)
		{
			InitializeComponent ();
            ChooseCommand = new Command(OnChoose, () => selectedItem != null);
			BindingContext = this;
            this.choice = choice;


            // menuitems and actual itemtemplate
            String res = "dt_none";
            if (choice && manage) res = "dt_both";
            else if (choice) res = "dt_choose";
            else if (manage) res = "dt_manage";
			infoList.ItemTemplate = Resources [res] as DataTemplate;

            // add button
            if (!manage) ToolbarItems.Remove(AddToolbarItem);
            if (!choice)
            {
                ToolbarItems.Remove(ChooseToolbarItem);
                infoList.ItemSelected += InfoList_ItemSelected;
                infoList.RemoveBinding(ListView.SelectedItemProperty);
            }
        }

        private void InfoList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            infoList.SelectedItem = null;
        }

        protected override void OnDisappearing()
        {
            if(!completed) completedTask.SetResult (new ctr { vm = initiallySelectedItem, popping = true }); // signals no selection change
            completed = true;
            base.OnDisappearing();
        }

        bool completed = false;
		void OnChoose() 
		{
            if (completed) return; completed = true;
			completedTask.SetResult(new ctr { vm = selectedItem == noth ? null : selectedItem, popping = false }); 
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