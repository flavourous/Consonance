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
        public IEnumerable ItemsSource { get { return (IEnumerable)GetValue(ItemsSourceProperty); } set { SetValue(ItemsSourceProperty, value); } }
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create("ItemsSource", typeof(IEnumerable), typeof(BindingStackLayout), null, BindingMode.OneWay, null, ResetItems);

        public DataTemplate ItemTemplate { get { return (DataTemplate)GetValue(ItemTemplateProperty); } set { SetValue(ItemTemplateProperty, value); } }
        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create("ItemTemplate", typeof(DataTemplate), typeof(BindingStackLayout), null, BindingMode.OneWay, null, ResetItems);

        readonly StackLayout bstack;
        public BindingStackLayout()
        {
            Content = bstack = new StackLayout { Orientation = StackOrientation.Horizontal };
        }

        static void ResetItems(BindableObject bsl, Object newvalue, Object oldvalue)
        {
            var v = (bsl as BindingStackLayout);
            v.bstack.Children.Clear();
            if (v.ItemsSource == null || v.ItemTemplate == null) return;
            foreach(var vm in v.ItemsSource)
            {
                var vc = v.ItemTemplate.CreateContent() as View;
                if (vc == null) return;

                vc.BindingContext = vm;
                v.bstack.Children.Add(vc);
            }
        }
    }
}
