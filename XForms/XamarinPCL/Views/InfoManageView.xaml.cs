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

namespace Consonance.XamarinFormsView.PCL
{
	public partial class InfoManageView : ContentPage
	{
		private InfoLineVM mselectedItem;
		public InfoLineVM selectedItem {
			get { return mselectedItem; }
			set {
				if(mselectedItem != null) mselectedItem.selected = false;
				var val = value ?? Nothingable.noth;
                foreach (var it in Items) // subvert reference comparison
                    if (OriginatorVM.OriginatorEquals(it, val))
                        mselectedItem = it;
				if(mselectedItem != null) mselectedItem.selected = true;
				OnPropertyChanged ("selectedItem");
			}
		}
		public InfoLineVM initiallySelectedItem { get; set; }

		Nothingable _Items;
		public IObservableCollection<InfoLineVM> Items
		{
			get { return _Items; }
			set {
				_Items = new Nothingable(value);
				OnPropertyChanged ("Items");
			}
		}
		protected override void OnPropertyChanged ([CallerMemberName] string propertyName = null)
		{
			App.platform.UIThread (() =>
				base.OnPropertyChanged (propertyName)
			);
		}
		public TaskCompletionSource<InfoLineVM> completedTask;
		public InfoManageView (bool choice, bool manage)
		{
			InitializeComponent ();
			BindingContext = this;

            String res = "dt_none";
            if (choice && manage) res = "dt_both";
            else if (choice) res = "dt_choose";
            else if (manage) res = "dt_manage";
			infoList.ItemTemplate = Resources [res] as DataTemplate;
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
			completedTask.SetResult (selectedItem == Nothingable.noth ? null : selectedItem); 
		}

		// info hooks
		public event Action ItemAdd = delegate { };
		void OnItemAdd(Object s, EventArgs ea) { ItemAdd(); }
		public event Action<InfoLineVM> ItemEdit = delegate { };
		void OnItemEdit(Object s, EventArgs e) {
			var b = (((MenuItem)s).BindingContext as InfoLineVM);
			if (b != Nothingable.noth)
				ItemEdit (b);
		}
		public event Action<InfoLineVM> ItemDelete = delegate { };
		void OnItemDelete(Object s, EventArgs e) {
			var b = (((MenuItem)s).BindingContext as InfoLineVM);
			if (b != Nothingable.noth)
				ItemDelete (b); 
		}
	}
	public class Nothingable : ObservableCollectionProxy<InfoLineVM, IObservableCollection<InfoLineVM>>
	{
        public static readonly InfoLineVM noth = new InfoLineVM { name = "Nothing" };
		public Nothingable(IObservableCollection<InfoLineVM> val) : base(val)
        {
            val.CollectionChanged += Val_CollectionChanged;
            Val_CollectionChanged(null, null);
        }

        // stop the proxying, do self. just reset.  cant recreate the event args reliably.
        Object raiselock = new object();
        bool fix_in_progress = false;
        public override event NotifyCollectionChangedEventHandler CollectionChanged = delegate { };
        private void Val_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Interlocking checking for nothing item.
            lock (raiselock)
            {
                // Do the thing you were going to do...
                if (sender != null) CollectionChanged(sender, e);
                bool is_ok = Count > 0 && this[0] == noth;
                if(!is_ok && !fix_in_progress)
                {
                    fix_in_progress = true;
                    Remove(noth); // might be in wrong order...
                    Insert(0, noth);
                    fix_in_progress = false;
                }
            }
        }
    }
}