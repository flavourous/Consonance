using Consonance.XamarinFormsView.PCL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;


namespace XLib
{
    class GridView : ContentView
    {
        public IEnumerable Headers { get { return (IEnumerable)GetValue(HeadersProperty); } set { SetValue(HeadersProperty, value); } }
        public static readonly BindableProperty HeadersProperty = BindableProperty.Create("Headers", typeof(IEnumerable), typeof(GridView), null, BindingMode.OneWay, null, Reset);

        public IEnumerable<IEnumerable> Items { get { return (IEnumerable<IEnumerable>)GetValue(ItemsProperty); } set { SetValue(ItemsProperty, value); } }
        public static readonly BindableProperty ItemsProperty = BindableProperty.Create("Items", typeof(IEnumerable<IEnumerable>), typeof(GridView), null, BindingMode.OneWay, null, Reset);

        // stuff schanges, rebuild the grid. This is lazy first port of call.
        public static void Reset(BindableObject obj, Object oldValue, Object newValue)
        {
            GridView sender = obj as GridView;
            sender.basegrid.ColumnDefinitions.Clear();
            sender.basegrid.RowDefinitions.Clear();
            sender.basegrid.Children.Clear();

            if (sender.Headers == null) return;

            // do headers columns
            var i = 0;
            sender.basegrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            foreach (var h in sender.Headers)
            {
                View hh = (h as View ?? new Label { Text = h.ToString() }).OnCol(i).OnRow(0);
                hh.HorizontalOptions = LayoutOptions.Center;
                sender.basegrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                sender.basegrid.Children.Add(hh);
                i++;
            }

            // other rows
            int r = 1, c;
            foreach(var row in sender.Items)
            {
                c = 0;
                foreach(var cell in row)
                {
                    View hh = (cell as View ?? new Label { Text = cell.ToString() }).OnCol(c).OnRow(r);
                    hh.HorizontalOptions = LayoutOptions.Center;
                    sender.basegrid.Children.Add(hh);
                    c++;
                    if (c == i) break; // not more columns allowed! :/
                }
                r++;
            }
        }

        readonly Grid basegrid;
        public GridView()
        {
            Content = basegrid = new Grid();
        }
    }
}
