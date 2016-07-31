using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Xamarin.Forms;

namespace XLib
{
    public class BindingStackLayout : ContentView
    {
        public IEnumerable Items { get { return (IEnumerable)GetValue(ItemsProperty); } set { SetValue(ItemsProperty, value); } }
        public static readonly BindableProperty ItemsProperty = BindableProperty.Create("Items", typeof(IEnumerable), typeof(BindingStackLayout), null, BindingMode.OneWay, null, ResetItems);

        readonly StackLayout bstack;
        public BindingStackLayout()
        {
            Content = bstack = new StackLayout { Orientation = StackOrientation.Horizontal };
        }

        static void ResetItems(BindableObject bsl, Object oldvalue, Object newvalue)
        {
            var v = (bsl as BindingStackLayout);
            v.bstack.Children.Clear();
            if (v.Items == null ) return;
            foreach (var vm in v.Items)
            {
                var vv = vm as View;
                vv.HorizontalOptions = LayoutOptions.StartAndExpand;
                v.bstack.Children.Add(vv);
            }
        }
    }
}
