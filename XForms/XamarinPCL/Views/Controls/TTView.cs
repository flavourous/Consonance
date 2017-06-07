using System;
using System.Linq;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Collections.Specialized;
using Consonance.Protocol;
using System.Runtime.CompilerServices;

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
            Spacing = 3;
            oc = Content as StackLayout;
            Content = new ScrollView { Content = oc, VerticalOptions = LayoutOptions.Fill };
            ItemsChanged += TTView_ItemsChanged;
        }

        bool expanded = false;
        private void TTView_ItemsChanged()
        {
            expanded = false;
            ProcExp();
            bool v = false;
            foreach (var i in Items) { v = true; break; }
            IsVisible = v;
        }
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == "Expanded") ProcExp();
        }
        void ProcExp()
        {
            if (!expanded) Content = oc.Children.FirstOrDefault();
            else Content = oc;
            //for (int i = 1; i < oc.Children.Count; i++)
            //    oc.Children[i].IsVisible = expanded;
        }

        static View Generator(Object vmo)
		{
			TrackerTracksVM vm = vmo as TrackerTracksVM;
            if (vm == null) return new Frame { Padding = new Thickness(0) };

            var tl = new Label
            {
                Text = vm.instanceName + " - " + vm.modelName,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            tl.FontSize *= 0.8;
            return new StackLayout
            {
                Spacing = 0,
                Children =
                {                    
                    tl,
                    new BoxView
                    {
                        BackgroundColor = tl.TextColor,
                        HeightRequest =1,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                    },
                    new TTViewItem
                    {
                        Spacing = 2,
                        Padding = new Thickness(0,2,0,0),
                        Items = vm.tracks,
                        VerticalOptions = LayoutOptions.Start,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    }
                },
			};
		}
	}
	public class TTViewItem : VStacker
	{
		public TTViewItem () : base(BarInfo.GenerateView) { }
        class BarInfo
        {
            static View Bar(Color f, Color b)
            {
                return new Button
                {
                    InputTransparent = true,
                    BorderWidth = bw,
                    BackgroundColor = b,
                    BorderColor = f,
                };
            }
            static double bw = 2.0;

            static View Txt(String txt, Color? f=null, Color? b = null)
            {
                var lab = new Label { Text = txt };
                if (f.HasValue) lab.TextColor = f.Value;
                if (b.HasValue) lab.BackgroundColor = b.Value;
                lab.FontSize *= 0.85;
                return new Frame
                {
                    IsClippedToBounds = true,
                    Padding = new Thickness(5, 0, 0, 1),
                    VerticalOptions = LayoutOptions.Center,
                    Content = lab                    
                };
            }

            Color Flip(Color c)
            {
                return new Color(1.0 - c.R, 1.0 - c.G, 1.0 - c.B, c.A);
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

                var fgLabel = Txt(txt);
                View filled = Bar(Color.Transparent, (Color)Label.TextProperty.DefaultValue);
                rl.Children.Add(filled, Constraint.Constant(0), Constraint.Constant(0), Constraint.RelativeToParent(p=>p.Width*bio.amount), Constraint.RelativeToParent(p => p.Height));
                rlc.Add(Bar(Color.Blue, Color.Transparent).Rel(bio.target, 0, bio.extras, 1));
                rlc.Add(Bar(Color.Blue, Color.Transparent).Rel(0, 0, bio.target + bio.extras, 1));

                return new Grid
                {
                    Children =
                    {
                        rl,
                        fgLabel,
                    }
                };
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


