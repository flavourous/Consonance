using LibSharpHelp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Consonance.XamarinFormsView.PCL
{
    public partial class BusyList : ContentView
    {
        public bool HasUnevenRows { get { return (bool)GetValue(HasUnevenRowsProperty); } set { SetValue(HasUnevenRowsProperty, value); } }
        public static readonly BindableProperty HasUnevenRowsProperty = BindableProperty.Create("HasUnevenRows", typeof(bool), typeof(BusyList), false);

        public IEnumerable ItemsSource { get { return (IEnumerable)GetValue(ItemsSourceProperty); } set { SetValue(ItemsSourceProperty, value); } }
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create("ItemsSource", typeof(IEnumerable), typeof(BusyList), null);

        public object SelectedItem { get { return (object)GetValue(SelectedItemProperty); } set { SetValue(SelectedItemProperty, value); } }
        public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create("SelectedItem", typeof(object), typeof(BusyList), null, BindingMode.TwoWay);

        public DataTemplate ItemTemplate { get { return (DataTemplate)GetValue(ItemTemplateProperty); } set { SetValue(ItemTemplateProperty, value); } }
        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create("ItemTemplate", typeof(DataTemplate), typeof(BusyList), null);

        Object taskOrderingLock = new object();
        Task lastAnim;

        public bool IsLoading { get { return (bool)GetValue(IsLoadingProperty); } set { SetValue(IsLoadingProperty, value); } }
        public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create("IsLoading", typeof(bool), typeof(BusyList), true, BindingMode.OneWay, null, ActivateLoader);

        static void ActivateLoader(BindableObject busyList, object oldvalue, object newValue)
        {
            var bl = busyList as BusyList;
            var blf = bl.LoadFrame as Frame;

            if ((bool)oldvalue == (bool)newValue) return;

            lock (bl.taskOrderingLock)
            {
                bl.lastAnim = bl.lastAnim.ContinueAfter(async () =>
                    {
                        TaskCompletionSource<bool> acom = new TaskCompletionSource<bool>();
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            if ((bool)newValue)
                            {
                                blf.IsVisible = true;
                                blf.InputTransparent = false;
                                blf.Opacity = 0.0;
                                blf.FadeTo(1.0).ContinueAfter(async () =>
                                {
                                    await Task.Delay(600);
                                    acom.SetResult(true);
                                });
                            }
                            else
                            {
                                blf.Opacity = 1.0;
                                blf.FadeTo(0.0).ContinueAfter(async () =>
                                {
                                    await Task.Yield();
                                    Device.BeginInvokeOnMainThread(() =>
                                    {
                                        blf.IsVisible = false;
                                        blf.InputTransparent = true;
                                        acom.SetResult(false);
                                    });
                                });
                            }
                        });
                        await acom.Task;
                    });
            }
        }



        public BusyList()
        {
            var ts = new TaskCompletionSource<Task>();
            ts.SetResult(null);
            lastAnim = ts.Task;
            InitializeComponent();
        }

        
    }
    
}
