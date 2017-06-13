using System;
using System.Linq;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Collections.Specialized;
using Consonance.Protocol;
using System.Runtime.CompilerServices;
using ScnViewGestures.Plugin.Forms;
using XLib;

namespace Consonance.XamarinFormsView.PCL
{
	public class VStacker : ContentView
	{
        readonly StackLayout ms;
		public delegate View Creator(Object val);
		readonly Creator c;
		public VStacker(Creator c)
		{
			this.c = c;
			Content = ms = new StackLayout { Orientation = StackOrientation.Vertical };
		}
        public IEnumerable Items
        {
            get { return (IEnumerable)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }
        public double Spacing { get { return ms.Spacing; } set { ms.Spacing = value; } }
        protected event Action ItemsChanged = delegate { };
        public static readonly BindableProperty ItemsProperty = BindableProperty.Create("Items", typeof(IEnumerable), typeof(VStacker), null, BindingMode.OneWay, null, (bo, oldv, newv) =>
           {
               var vs = bo as VStacker;
               vs.SetItems(newv as IEnumerable);
               var nv = newv as INotifyCollectionChanged;
               if (nv != null) nv.CollectionChanged += vs.ch;
               var ov = oldv as INotifyCollectionChanged;
               if (ov != null) ov.CollectionChanged -= vs.ch;
           });
        void ch(object o, NotifyCollectionChangedEventArgs e)
        {
            SetItems(o as IEnumerable);
        }
        void SetItems(IEnumerable newv)
        {
            App.UIThread(() =>
            {
                bool attempting = true;
                List<Object> vms = new List<object>();
                while(attempting)
                {
                    try
                    {
                        vms.Clear();
                        foreach (var v in newv as IEnumerable)
                            vms.Add(v);
                    }
                    catch { continue; }
                    attempting = false;
                }

                ms.Children.Clear();
                if (newv != null)
                    foreach (var b in vms)
                        ms.Children.Add(c(b));

                ItemsChanged();
            });
        }
    }
	public class TTView : VStacker
	{
        public bool Expanded { get => (bool)GetValue(ExpandedProperty); set => SetValue(ExpandedProperty, value); }
        public static readonly BindableProperty ExpandedProperty = BindableProperty.Create("Expanded", typeof(bool), typeof(TTView), false);

        StackLayout oc;
		public TTView() : base(Generator)
        {
            IsVisible = false;
            Spacing = 5;
            oc = Content as StackLayout;
            Content = new ScrollView { Content = oc, VerticalOptions = LayoutOptions.Fill };
            ItemsChanged += TTView_ItemsChanged;
        }

        private void TTView_ItemsChanged()
        {
            bool v = false;
            foreach (var i in Items) { v = true; break; }
            IsVisible = v;
        }

        static View Generator(Object vmo)
		{
			TrackerTracksVM vm = vmo as TrackerTracksVM;
            if (vm == null) return new Frame { Padding = new Thickness(0) };

            var tl = new Label
            {
                Text = vm.instanceName,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Start
            };
            var tl2 = new Label
            {
                Text = vm.modelName,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.End
            };
            var ti = new TTViewItem
            {
                Spacing = 2,
                Padding = new Thickness(0, 2, 0, 0),
                Items = vm.tracks,
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            return new Grid
            {
                Padding=  new Thickness(0),
                RowSpacing = 0,
                ColumnSpacing = 0,
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition()
                },
                Children = { tl, tl2, ti.OnRow(1,1) },
            };
		}
	}

    [ContentProperty("RealContent")]
    public class TTViewLauncher : ContentView
    {
        public View RealContent { get => (View)GetValue(RealContentProperty); set => SetValue(RealContentProperty, value); }
        public static readonly BindableProperty RealContentProperty = BindableProperty.Create("RealContent", typeof(View), typeof(TTViewLauncher));

        public TTViewLauncher()
        {
            var vg = new ContentView { InputTransparent = true };
            vg.SetBinding(ContentProperty, new Binding("RealContent", source: this));
            var cb = new BoxView
            {
                WidthRequest = 1,
                HeightRequest = 1,
                BackgroundColor = Color.Transparent,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
            };
            cb.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(VT) });
            Content = new Grid
            {                
                Children = { vg, cb },
            };
        }

        private void VT()
        {
            var tv = new TTView();
            tv.SetBinding(TTView.ItemsProperty, new Binding("Tracks"));
            var cv = new ScrollView { Margin = new Thickness(10), Content = tv };
            cv.SetBinding(BindingContextProperty, new Binding("BindingContext", source: this));
            Navigation.PushAsync(new ContentPage
            {
                Title = "Details",
                Content = cv
            });
        }
    }


    public class TTViewItem : VStacker
	{
		public TTViewItem () : base(BarInfo.GenerateView) { }
        class BarInfo
        {
            static View Txt(String txt, Color f)
            {
                var lab = new Label
                {
                    Text = txt,
                };
                lab.TextColor = f;
                lab.FontSize *= 0.85;
                return new ContentView
                {
                    IsClippedToBounds = false,
                    Padding = new Thickness(5, 0, 0, 1),
                    VerticalOptions = LayoutOptions.Center,
                    Content = lab                    
                };
            }

            public static View GenerateView(Object tio)
            {
                TrackingInfoVM ti = tio as TrackingInfoVM;
                var bio = new BarInfo(ti);
                var rl = new RelativeLayout { HorizontalOptions = LayoutOptions.FillAndExpand, VerticalOptions = LayoutOptions.FillAndExpand };
                var rlc = rl.Children as ICollection<View>;
                // I can't make bindings for constraints work! But setters do! I was using a converter - and that was firing, but the constraint was not being evaluated!!!
                //rl.BindingContext = bio;
                var txt = String.Format(
                        "{0}: {1} / {3} + {4}",
                        ti.targetValueName, bio.InAmount, ti.inValuesName.ToLower(),
                        bio.TargetAmount, bio.OutAmount, ti.outValuesName.ToLower()
                        );

                var fgLabel = Txt(txt, Color.Default);
                return new Grid
                {
                    Children =
                    {
                        GridBar(bio, Color.FromRgba(1, 1, 1, .1),Color.FromRgba(1, 1, 1, .2),Color.FromRgba(1, .3, .3, .1)),
                        fgLabel
                    }
                };
            }
            static ColumnDefinition StarCol(double frac)
            {
                return new ColumnDefinition { Width = new GridLength(frac, frac > 0 ? GridUnitType.Star : GridUnitType.Absolute) };
            }
            static View GridBar(BarInfo bin, Color bg, Color fg, Color bad)
            {
                var ret = new Grid
                {
                    HeightRequest=0,
                    Padding = new Thickness(0),
                    ColumnSpacing = 0,
                    RowSpacing = 0,
                    RowDefinitions =
                    {
                        new RowDefinition { Height=1 },
                        new RowDefinition { Height=GridLength.Star },
                        new RowDefinition { Height=1 },
                    },
                };
                
                // First Vertical
                ret.ColumnDefinitions.Add(new ColumnDefinition { Width = 1 });
                ret.Children.Add(new BoxView { BackgroundColor = fg }.OnRow(1, 1).OnCol(0, 1));

                Debug.WriteLine("{0},{1},{2}", bin.amount, bin.target, bin.extras);

                // The fill is either one or two columns
                if(bin.amount < bin.target)
                {
                    // |-------   |
                    //  amount^   ^target

                    // fill (amount)
                    ret.ColumnDefinitions.Add(StarCol(bin.amount));
                    ret.Children.Add(new BoxView { BackgroundColor = bg }.OnRow(1, 1).OnCol(1, 1));

                    // gap (target)
                    ret.ColumnDefinitions.Add(StarCol(bin.target - bin.amount));

                    // vertical (target marker)
                    ret.ColumnDefinitions.Add(new ColumnDefinition { Width = 1 });
                    ret.Children.Add(new BoxView { BackgroundColor = fg }.OnRow(0, 3).OnCol(3, 1));
                       
                    if (bin.extras > 0)
                    {
                        // |-------   |       |
                        //  amount^   ^target ^extras+target

                        // gap (extras)
                        ret.ColumnDefinitions.Add(StarCol(bin.extras));

                        // vertical (end marker)
                        ret.ColumnDefinitions.Add(new ColumnDefinition { Width = 1 });
                        ret.Children.Add(new BoxView { BackgroundColor = fg }.OnRow(1, 1).OnCol(5, 1));
                    }
                }
                else if(bin.amount < bin.target+bin.extras)
                {
                    // |--------|---       |
                    //    target^  ^amount ^extras+target

                    ret.ColumnDefinitions.Add(StarCol(bin.target));
                    ret.ColumnDefinitions.Add(new ColumnDefinition { Width = 1 });
                    ret.ColumnDefinitions.Add(StarCol(bin.amount - bin.target));
                    ret.ColumnDefinitions.Add(StarCol(bin.amount - bin.target + bin.extras));
                    ret.ColumnDefinitions.Add(new ColumnDefinition { Width = 1 });

                    // fill (amount)
                    ret.Children.Add(new BoxView { BackgroundColor = bg }.OnRow(0, 3).OnCol(1, 3));

                    // vertical (target marker)
                    ret.Children.Add(new BoxView { BackgroundColor = fg }.OnRow(1, 1).OnCol(2, 1));

                    // vertical (end)
                    ret.Children.Add(new BoxView { BackgroundColor = fg }.OnRow(1, 1).OnCol(5, 1));
                }
                else // bin.amount > bin.target + bin.extras
                {
                    var hex = bin.extras > 0;
                    // |--------|-------------
                    //    target^            ^amount
                    ret.ColumnDefinitions.Add(StarCol(bin.target));
                    ret.ColumnDefinitions.Add(new ColumnDefinition { Width = 1 });
                    if (hex)
                    {
                        // |--------|------|--------------------
                        //    target^      ^extras+target      ^amount
                        ret.ColumnDefinitions.Add(StarCol(bin.extras));
                        ret.ColumnDefinitions.Add(new ColumnDefinition { Width = 1 });
                    }
                    ret.ColumnDefinitions.Add(StarCol(bin.amount-bin.target-bin.extras));

                    // fill (amount)
                    ret.Children.Add(new BoxView { BackgroundColor = bg }.OnRow(0, 3).OnCol(1, hex ? 4 : 2));
                    ret.Children.Add(new BoxView { BackgroundColor = bad }.OnRow(0, 3).OnCol(hex ? 5 : 3, 1));

                    // vertical (target marker)
                    ret.Children.Add(new BoxView { BackgroundColor = fg }.OnRow(0, 3).OnCol(2, 1));

                    if (hex)
                    {
                        // vertical (extra marker)
                        ret.Children.Add(new BoxView { BackgroundColor = fg }.OnRow(1, 1).OnCol(4, 1));
                    }
                }

                // horizontals
                ret.Children.Add(new BoxView { BackgroundColor = fg }.OnRow(0, 1).OnCol(0, ret.ColumnDefinitions.Count - 1));
                ret.Children.Add(new BoxView { BackgroundColor = fg }.OnRow(2, 1).OnCol(0, ret.ColumnDefinitions.Count - 1));

                return ret;
            }

            private BarInfo(TrackingInfoVM ti)
            {
                AmountName = ti.targetValueName;
                InAmount = (from f in ti.inValues select f.value).Sum();
                OutAmount = (from f in ti.outValues select f.value).Sum();
                TargetAmount = ti.targetValue;

                // get the biggest one:
                var lrg = Math.Max(OutAmount + TargetAmount, InAmount);

                if (lrg > 0)
                {
                    // col widths
                    target = TargetAmount / lrg;
                    extras = OutAmount / lrg;
                    amount = InAmount / lrg;
                }
                else
                {
                    target = 1;
                    extras = amount = 0;
                }
            }
            public String AmountName { get; private set; }
            public double InAmount { get; private set; }
            public double OutAmount { get; private set; }
            public double TargetAmount { get; private set; }

            public double target { get; private set; }
            public double extras { get; private set; }
            public double amount { get; private set; }
        }
	}
}
