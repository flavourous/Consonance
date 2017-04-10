using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Reflection;
using Consonance.Protocol;
using System.Threading.Tasks;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class InfoSelectRequest : ContentView
	{
        public String manage_title { get; set; }
        public InfoManageType mt { get; set; }
        public Func<InfoLineVM,IInputResponse<InfoLineVM>> requestit { get; set; }
        public InfoSelectRequest()
        {
            InitializeComponent();
        }
        bool block_reentrancy = false;
		public async void OnChoose(object sender, EventArgs nooopse) // it's an event handler...async void has to be
		{
            if (block_reentrancy) return;
            block_reentrancy = true;

			var vm = BindingContext as IValueRequest<InfoLineVM>;
            var vr = requestit(vm.value);
            await vr.Opened;
            var ivm = await vr.Result;
            vm.value = ivm == InfoManageView.noth ? null : ivm;
            await vr.Close();
            block_reentrancy = false;
        }
	}
	class InfoSelectRequestConverter : IValueConverter
	{
		#region IValueConverter implementation
		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var sv = value as InfoLineVM;
			return sv == null ? "None" : sv.name;
		}
		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}

