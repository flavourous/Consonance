using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Runtime.CompilerServices;
using System.Collections;
using LibSharpHelp;

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
			Platform.UIThread (() =>
				base.OnPropertyChanged (propertyName)
			);
		}
		public TaskCompletionSource<InfoLineVM> completedTask;
		public InfoManageType imt;
		bool choice = false;
		public InfoManageView (bool choice, bool manage)
		{
			InitializeComponent ();
			BindingContext = this;
			this.choice = choice;
			if (manage) infoList.ItemTemplate = Resources ["dt_act"] as DataTemplate;
			else infoList.ItemTemplate = Resources ["dt_noact"] as DataTemplate;

		}
		protected override bool OnBackButtonPressed ()
		{
			completedTask.SetResult (initiallySelectedItem); // signals no selection change
			return base.OnBackButtonPressed ();
		}

		void OnChoose(Object s, EventArgs e) 
		{ 
			completedTask.SetResult (selectedItem == Nothingable.noth ? null : selectedItem); Navigation.PopAsync (); 
		}

		// info hooks
		public event Action<InfoManageType> ItemAdd = delegate { };
		void OnItemAdd() { ItemAdd(imt); }
		public event Action<InfoManageType, InfoLineVM> ItemEdit = delegate { };
		void OnItemEdit(Object s, EventArgs e) {
			var b = (((MenuItem)s).BindingContext as InfoLineVM);
			if (b != Nothingable.noth)
				ItemEdit (imt, b);
		}
		public event Action<InfoManageType, InfoLineVM> ItemDelete = delegate { };
		void OnItemDelete(Object s, EventArgs e) {
			var b = (((MenuItem)s).BindingContext as InfoLineVM);
			if (b != Nothingable.noth)
				ItemDelete (imt, b); 
		}
	}
	public class Nothingable : ObservableCollectionProxy<InfoLineVM, IObservableCollection<InfoLineVM>>
	{
		public Nothingable(IObservableCollection<InfoLineVM> val) : base(val) { }
		public static InfoLineVM noth = new InfoLineVM { name = "Nothing" };
		public override InfoLineVM this[int index] {
			get 
			{
				if( index > 0) return base[index - 1];
				return noth;
			}
			set { if (index > 0) base[index - 1] = value; }
		}
		public override int Count { get { return base.Count + 1; } }
		public override IEnumerator<InfoLineVM> GetEnumerator ()
		{
			return new ListEnumerator<InfoLineVM> (this);
		}
	}
}