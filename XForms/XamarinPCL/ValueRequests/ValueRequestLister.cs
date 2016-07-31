using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using XLib;

namespace Consonance.XamarinFormsView.PCL
{
    public class AViewCell : ViewCell
    {
        readonly Action dol;
        public AViewCell(Action dol) { this.dol= dol; }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // at this point GetSizeRequest is functioning....but actually running code here makes bads!
            // setting columndefinitionwidths is "irregularlly" respected.  Is this the middle of a layout pass?
            // also it ges spammed on remove. al re-apperar i think.
            Task.Run(() => Device.BeginInvokeOnMainThread(dol));
            // also handlez when appearing from offscreen non-measurable context.
        }
    }
    public class ValueRequestLister : BindableObject
    {
        public View UseHeader { set { toprequests.Content = value; } }

        public Object UseSource { get { return (Object)GetValue(UseSourceProperty); } set { SetValue(UseSourceProperty, value); } }
        public static readonly BindableProperty UseSourceProperty = BindableProperty.Create("UseSource", typeof(Object), typeof(ValueRequestLister), null);

        readonly ColWatcher cw;
        readonly IValueRequest<TabularDataRequestValue> td;
        public ValueRequestLister(ListView hook, IValueRequest<TabularDataRequestValue> tabdata)
        {
            realheader = new ContentView { Content = new Frame() };
            toprequests = new ContentView { Content = new Frame() };

            hook.Header = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Children = { toprequests, realheader }
            };

            hook.ItemTemplate = new DataTemplate(() =>
            {
                var mirm = new MenuItem { Text = "Remove" };
                mirm.Clicked += (o, e) =>
                {
                    Object[] c = (o as BindableObject).BindingContext as object[];
                    (UseSource as IList).Remove(c);
                };
                var rcell = new AViewCell(() =>
                {
                    if (needsLayout)
                    {
                        needsLayout = false;
                        cw.LayoutPass();
                    }
                })
                {
                    View = new Frame(),
                    ContextActions = { mirm }
                };
                rcell.BindingContextChanged += (o, e) =>
                {
                    if (rcell.BindingContext is Object[])
                        rcell.View = rowviews[rcell.BindingContext as Object[]];
                    else rcell.View = new Frame();
                };
                return rcell;
            });

            hook.SetBinding(ListView.ItemsSourceProperty, new Binding("UseSource") { Source = this });

            this.td = tabdata;
            this.cw = new ColWatcher(() => hook.Width - cpad * (headers.Count + 1) / 2);
            hook.SizeChanged += (o, e) => cw.LayoutPass(true);
            tabdata.ValueChanged += Tabdata_ValueChanged;
            Tabdata_ValueChanged();
        }
        readonly double cpad = 5.0;

        readonly ContentView toprequests, realheader;

        View GenerateRow(Object[] cells)
        {
            Grid rg = new Grid { ColumnSpacing = 0 };
            for (int i = 0; i < headers.Count; i++)
            {
                // includes padding headers on even indexes.
                int ci = (i - 1) / 2;
                rg.ColumnDefinitions.Add(headers[i]);
                if ((i - 1) % 2 == 0 && ci < cells.Length)
                {
                    var cv = new Label { Text = cells[ci].ToString() };
                    rg.Children.Add(cv.OnCol(i));
                    cw.AddCell(cv, ci);
                }
            }
            return rg;
        }

        TabularDataRequestValue old;
        List<ColumnDefinition> headers = new List<ColumnDefinition>();
        private void Tabdata_ValueChanged()
        {
            // set up datasource and header again
            cw.Clear();
            headers.Clear();
            var header_items = new List<String>();
            headers.Add(new ColumnDefinition { Width = cpad });
            foreach (var h in td.value?.Headers ?? new String[0])
            {
                var cd = new ColumnDefinition { Width = 0 };
                headers.Add(cd);
                header_items.Add(h);
                cw.AddColumn(cd);
                headers.Add(new ColumnDefinition { Width = cpad });
            }
            realheader.Content = new StackLayout
            {
                Children =
                {
                    GenerateRow(header_items.ToArray()),
                    new BoxView { HeightRequest = 1, HorizontalOptions = LayoutOptions.FillAndExpand, BackgroundColor = Color.Accent }
                },
                Orientation = StackOrientation.Vertical,
                Spacing = 0
            };

            // manage event listner
            if (old != null)
            {
                old.Items.CollectionChanged -= Items_CollectionChanged;
                old.Items.CollectionChanged -= Items_CollectionChanged1;
            }
            old = td.value;

            // replace itemssource
            if (td.value != null) td.value.Items.CollectionChanged += Items_CollectionChanged;
            UseSource = td.value?.Items;
            if (td.value != null) td.value.Items.CollectionChanged += Items_CollectionChanged1;
        }

        // i dont want to recycle.
        Dictionary<Object[], View> rowviews = new Dictionary<object[], View>();

        bool needsLayout = true;
        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            needsLayout = true;
            // before...
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var ni = e.NewItems[0] as Object[];
                rowviews[ni] = GenerateRow(ni as Object[]);
            }
            else if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var oi = e.OldItems[0] as Object[];
                foreach (var c in (rowviews[oi] as Grid).Children)
                    cw.RemoveCell(c);
                rowviews.Remove(oi);
            }
        }

        private void Items_CollectionChanged1(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // after... 
        }

    }
}
