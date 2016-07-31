using Consonance.XamarinFormsView.PCL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Xamarin.Forms;

namespace XLib
{
    public class EquallyDistributedLayout : ContentView
    {
        public IEnumerable Items { get { return (IEnumerable)GetValue(ItemsProperty); } set { SetValue(ItemsProperty, value); } }
        public static readonly BindableProperty ItemsProperty = BindableProperty.Create("Items", typeof(IEnumerable), typeof(BindingStackLayout), null, BindingMode.OneWay, null, ResetItems);

        readonly Grid bg;
        public EquallyDistributedLayout()
        {
            Content = bg = new Grid { RowDefinitions = { new RowDefinition { Height = GridLength.Auto } } };
        }

        static void ResetItems(BindableObject bsl, Object oldvalue, Object newvalue)
        {
            var v = (bsl as EquallyDistributedLayout);
            v.bg.Children.Clear();
            v.bg.ColumnDefinitions.Clear();
            v.deregistrations();
            v.deregistrations = delegate { };
            if (v.Items == null) return;
            foreach (var vm in v.Items)
                v.AddNextItem(vm as View);
        }
        event Action deregistrations = delegate { };
        void AddNextItem(View v)
        {
            int ncol = bg.ColumnDefinitions.Count;
            ColumnDefinition col = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
            bg.ColumnDefinitions.Add(col);
            v.MeasureInvalidated += ProcCols;
            deregistrations += () => v.MeasureInvalidated -= ProcCols;
            if(v is Entry)
            {
                var e = v as Entry;
                e.TextChanged += ProcCols;
                deregistrations += () => e.TextChanged -= ProcCols;
            }
            bg.Children.Add(v.OnCol(ncol));
            ProcCols(v, new EventArgs()); // initialisation
        }
        void ProcCols(Object sender, EventArgs args)
        {
            for(int i=0;i<bg.ColumnDefinitions.Count;i++)
            {
                var v = bg.Children[i];
                var c = bg.ColumnDefinitions[i];
                var vsr = v.GetSizeRequest(double.PositiveInfinity, 0.0);
                c.Width = new GridLength(Math.Max(0, vsr.Minimum.Width), GridUnitType.Star);
            }
        }
    }
}
