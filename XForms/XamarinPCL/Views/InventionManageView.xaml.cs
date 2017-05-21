using Consonance.Invention;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

using Consonance.Protocol;
using System.Collections.ObjectModel;

namespace Consonance.XamarinFormsView
{
    public partial class InventionManageView : ContentPage
    {
        public InventionCommandManager icm;
        public InventionManageView()
        {
            InitializeComponent();
            BindingContext = this;
        }

        IList<InventedTrackerVM> _Items;
        public IList<InventedTrackerVM> Items
        {
            get { return _Items; }
            set
            {
                _Items = value;
                OnPropertyChanged("Items");
            }
        }

        void OnItemAdd(Object s, EventArgs ea) { icm.Add(); }
        void OnItemEdit(Object s, EventArgs e)
        {
            var b = (((MenuItem)s).BindingContext as InventedTrackerVM);
            icm.Edit(b);
        }
        void OnItemDelete(Object s, EventArgs e)
        {
            var b = (((MenuItem)s).BindingContext as InventedTrackerVM);
            icm.Delete(b);
        }
    }
}
