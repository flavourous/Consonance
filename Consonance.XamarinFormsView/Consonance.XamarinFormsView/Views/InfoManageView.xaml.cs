using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class InfoManageView : ContentPage
	{
		public Func<Task> finished;
		public IFindList<InfoLineVM> finder;
		public BindingList<InfoLineVM> Items;
		public InfoManageType imt;
		public InfoManageView ()
		{
			InitializeComponent ();
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