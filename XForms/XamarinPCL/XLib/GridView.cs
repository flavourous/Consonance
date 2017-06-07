using Consonance.XamarinFormsView.PCL;
using LibSharpHelp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
            ProcGrid(obj as GridView);
        }

        public void nf(object sender, NotifyCollectionChangedEventArgs e)
        {
            ProcGrid(this);
        }

        public static void ItemsChanged(BindableObject obj, Object oldValue, Object newValue)
        {
            var g = obj as GridView;
            oldValue.CastAct<INotifyCollectionChanged>(c => c.CollectionChanged -= g.nf);
            newValue.CastAct<INotifyCollectionChanged>(c => c.CollectionChanged += g.nf);
            ProcGrid(g);
        }
        public static void ProcGrid(GridView sender)
        {
            sender.basegrid.Children.Clear();
            sender.basegrid.RowDefinitions.Clear();
            sender.basegrid.ColumnDefinitions.Clear();
            sender.sizehandler.Clear();

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
                sender.basegrid.Children.Add(sender.sizehandler.AddCell(new Label { Text = h.ToString(), FontAttributes = FontAttributes.Bold }.OnRow(0).OnCol(i), j));
                sender.basegrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                i += 2; j++;
            }

            // col for delete
            sender.basegrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            sender.basegrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

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
                    {
                        View use = (cell as View ?? new Label { Text = cell.ToString() }).OnRow(r).OnCol(cmap[ic]);
                        sender.basegrid.Children.Add(sender.sizehandler.AddCell(use, ic++));
                    }
                    if (sender.Items is IList)
                    {
                        var sil = sender.Items as IList;
                        var delete = new Label { Text = "x", TextColor = Color.Red }.OnRow(r).OnCol(i);
                        sender.basegrid.Children.Add(delete);
                        var lr = r;
                        var tgen = new TapGestureRecognizer();
                        delete.GestureRecognizers.Add(tgen);
                        tgen.Command = new Command(() =>
                       {
                           sil.RemoveAt(lr - 1);
                           if (!(sil is INotifyCollectionChanged))
                               ProcGrid(sender);
                       });
                        sender.exampleDelete = delete;
                    }
                    r++;
                }
            }

            sender.sizehandler.LayoutPass();
        }

        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            sizehandler.LayoutPass();
            base.LayoutChildren(x, y, width, height);
        }

        View exampleDelete = null;
        readonly ColWatcher sizehandler;
        readonly Grid basegrid;
        public GridView()
        {
            Content = basegrid = new Grid
            {
                ColumnSpacing = 3,
                RowSpacing =3,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
            };
            sizehandler = new ColWatcher(() =>
            {
                var cw = Content.Width;
                var cgap = (basegrid.ColumnDefinitions.Count - 1) * 3;
                var delcol = exampleDelete?.Measure(double.PositiveInfinity, double.PositiveInfinity).Request.Width ?? 0.0;
                return cw - cgap - delcol;
            });
        }
        
    }
}
