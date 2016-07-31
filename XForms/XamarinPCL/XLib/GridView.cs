using Consonance.XamarinFormsView.PCL;
using LibSharpHelp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XLib
{
    class GridView : ContentView
    {
        public IEnumerable Headers { get { return (IEnumerable)GetValue(HeadersProperty); } set { SetValue(HeadersProperty, value); } }
        public static readonly BindableProperty HeadersProperty = BindableProperty.Create("Headers", typeof(IEnumerable), typeof(GridView), null, BindingMode.OneWay, null, HeadersChanged);

        public IEnumerable<IEnumerable> Items { get { return (IEnumerable<IEnumerable>)GetValue(ItemsProperty); } set { SetValue(ItemsProperty, value); } }
        public static readonly BindableProperty ItemsProperty = BindableProperty.Create("Items", typeof(IEnumerable<IEnumerable>), typeof(GridView), null, BindingMode.OneWay, null, ItemsChanged);

        public static void HeadersChanged(BindableObject obj, Object oldValue, Object newValue)
        {
            GridView sender = obj as GridView;


            ItemsChanged(obj, null, sender.Items);
        }
        public static void ItemsChanged(BindableObject obj, Object oldValue, Object newValue)
        {
            GridView sender = obj as GridView;

            Dictionary<int, int> cmap = new Dictionary<int, int>();

            // Setup Headers
            sender.basegrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sender.basegrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            int i = 1;
            int j = 0;
            foreach(var h in sender.Headers)
            {
                sender.basegrid.ColumnDefinitions.Add(sender.sizehandler.AddColumn(new ColumnDefinition { Width = GridLength.Auto }));
                cmap[j] = i;
                sender.basegrid.Children.Add(sender.sizehandler.AddCell(new Label { Text = h.ToString() }.OnRow(0).OnCol(i), j));
                sender.basegrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                i += 2; j++;
            }

            // nopes.
            if (sender.Items != null)
            {
                // Setup items
                int r = 1;
                foreach (var row in sender.Items)
                {
                    sender.basegrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    int ic = 0;
                    foreach (var cell in row)
                        sender.basegrid.Children.Add(sender.sizehandler.AddCell(new Label { Text = cell.ToString() }.OnRow(r).OnCol(cmap[ic]), ic++));
                    r++;
                }
            }

            sender.sizehandler.LayoutPass();
        }

        readonly ColWatcher sizehandler;
        readonly Grid basegrid;
        public GridView()
        {
            Content = basegrid = new Grid
            {
                ColumnSpacing = 0,
                RowSpacing =0,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
            };
            sizehandler = new ColWatcher(() => Content.Width);
        }
        
    }
}
