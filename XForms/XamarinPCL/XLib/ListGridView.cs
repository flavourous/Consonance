using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView.PCL.XLib
{
    class ListGridView : ContentView
    {
        public IEnumerable Headers { get { return (IEnumerable)GetValue(HeadersProperty); } set { SetValue(HeadersProperty, value); } }
        public static readonly BindableProperty HeadersProperty = BindableProperty.Create("Headers", typeof(IEnumerable), typeof(ListGridView), null, BindingMode.OneWay, null, Reset);

        public IEnumerable<IEnumerable> Items { get { return (IEnumerable<IEnumerable>)GetValue(ItemsProperty); } set { SetValue(ItemsProperty, value); } }
        public static readonly BindableProperty ItemsProperty = BindableProperty.Create("Items", typeof(IEnumerable<IEnumerable>), typeof(ListGridView), null, BindingMode.OneWay, null, Reset);

        // stuff schanges, rebuild the grid. This is lazy first port of call.
        public static void Reset(BindableObject obj, Object oldValue, Object newValue)
        {
            ListGridView sender = obj as ListGridView;
            if(sender.Items != null)
            {
                
            }
        }
        readonly ListView itms;
        public ListGridView()
        {
            Content = itms = new ListView
            {
                ItemTemplate = new DataTemplate()
            };
            BindingContext = this;
            itms.Bind(ListView.ItemsSourceProperty, "Items");
        }
    }
    class lt : ViewCell
    {
        public lt()
        {
            View = new Grid();
        }
    }
}
