using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class MultiRequestList : ContentView
	{
		public MultiRequestList()
		{
			InitializeComponent ();
		}

        IEnumerable<IValueRequestVM> UnwrapViewmodels(IEnumerable views)
        {
            foreach (var v in views)
            {
                var bc1 = (v as View).BindingContext as IValueRequestVM;
                if (bc1 is MultiRequestOptionValue)
                {
                    //could work on interface and recurse. but meh.
                    var rqs = bc1 as MultiRequestOptionValue;
                    foreach (var rq in rqs.IValueRequestOptions)
                        yield return (rq as View).BindingContext as IValueRequestVM;
                }
                else yield return bc1;
            }
        }

        public String[] headers
        {
            get
            {
                if (bc_val == null) return new string[0];
                List<String> ret = new List<string>();
                foreach (var vm in UnwrapViewmodels(bc_val.IValueRequestOptions))
                    ret.Add(vm.name);
                return ret.ToArray();
            }
        }
        public List<String[]> items
        {
            get
            {
                if (bc_val == null) return new List<String[]>();
                var ret = new List<string[]>();
                foreach (var row in bc_val.Items)
                    ret.Add((from o in row select o.ToString()).ToArray());
                return ret;
            }
        }
        public View[] itemrequests
        {
            get
            {
                return new List<View>(bc_val.IValueRequestOptions as IEnumerable<View>).ToArray();
            }
        }

        MultiRequestListValue bc_val;
        INotifyPropertyChanged old;
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            // binding
            var bc = BindingContext as INotifyPropertyChanged;
            if (old != null) old.PropertyChanged -= Bc_PropertyChanged;
            old = bc;
            if (bc != null)
            {
                bc.PropertyChanged += Bc_PropertyChanged;
                Bc_PropertyChanged(bc, new PropertyChangedEventArgs("value"));
            }
        }
        void Bc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "value")
            {
                var bc = BindingContext as IValueRequest<MultiRequestListValue>;
                OnPropertyChanged("items");
                OnPropertyChanged("headers");
                OnPropertyChanged("itemrequests");
            }
        }
    }
}

