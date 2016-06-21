using System;
using System.Linq;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Consonance.XamarinFormsView.PCL
{
	public class VStacker : ContentView
	{
        public static Color amnt { get { var rt = Color.Accent; return Color.FromRgba(rt.R / 2, rt.G / 2, rt.B / 2, .5); } }
        public static Color xtra { get { var rt = Color.Accent; return Color.FromRgba(rt.R  / 5, rt.G / 5, rt.B / 5, .5); } }
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
            App.platform.UIThread(() =>
            {
                ms.Children.Clear();
                if (newv != null)
                    foreach (var b in newv as IEnumerable)
                        ms.Children.Add(c(b));
                ItemsChanged();
            });
        }
    }
	public class TTView : VStacker
	{
        TextSizedButton b;
        StackLayout oc;
		public TTView() : base(Generator)
        {
            IsVisible = false;
            Spacing = 3;
            oc = Content as StackLayout;
            var th = new Frame
            {
                Padding = new Thickness(0),
                Content = b = new TextSizedButton()
                {
                    VerticalOptions = LayoutOptions.Start,
                    HorizontalOptions = LayoutOptions.End,
                },
                VerticalOptions = LayoutOptions.Start
            };
            b.Clicked += Cb_Clicked;
            Content = new StackLayout
            {
                Spacing = 0.0,
                Orientation = StackOrientation.Vertical,
                Children =
                {
                    th,
                    new BoxView
                    {
                        BackgroundColor = Color.Accent,
                        HeightRequest = 2,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                    },
                    new ScrollView { Content = oc, VerticalOptions = LayoutOptions.Fill }
                },
                VerticalOptions = LayoutOptions.Fill
            };
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
        private void Cb_Clicked(object sender, EventArgs e)
        {
            expanded = !expanded;
            ProcExp();
        }
        void ProcExp()
        {
            b.vhack = expanded ? 18 : 5;
            b.Text = expanded ? "Less" : "More";
            for (int i = 1; i < oc.Children.Count; i++)
                oc.Children[i].IsVisible = expanded;
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
                        BackgroundColor = Color.Accent,
                        HeightRequest =1,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                    },
                    new TTViewItem
                    {
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
		public TTViewItem () : base(BarInfo.GenerateView) { Spacing = 2; }
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
            
            public static View GenerateView(Object tio)
            {
                TrackingInfoVM ti = tio as TrackingInfoVM;
                var bio = new BarInfo(ti);
                var rl = new RelativeLayout { HorizontalOptions = LayoutOptions.FillAndExpand, VerticalOptions = LayoutOptions.FillAndExpand };
                var rlc = rl.Children as ICollection<View>;
                // I can't make bindings for constraints work! But setters do! I was using a converter - and that was firing, but the constraint was not being evaluated!!!
                //rl.BindingContext = bio;
                rl.Children.Add(Bar(Color.Transparent, amnt), Constraint.Constant(0), Constraint.Constant(bw), Constraint.RelativeToParent(p=>p.Width*bio.amount), Constraint.RelativeToParent(p => p.Height - bw*2));
                rlc.Add(Bar(Color.Accent, Color.Transparent).Rel(bio.target, 0, bio.extras, 1));
                rlc.Add(Bar(Color.Accent, Color.Transparent).Rel(0, 0, bio.target + bio.extras, 1));
                var tl = new Label
                {
                    Text = String.Format(
                        "{0}: {1} / {3} + {4}",
                        ti.targetValueName, bio.InAmount, ti.inValuesName.ToLower(), 
                        bio.TargetAmount, bio.OutAmount, ti.outValuesName.ToLower()
                        ),
                };
                tl.FontSize *= 0.85;
                return new Grid
                {
                    Children =
                    {
                        rl,
                        new Frame
                        {
                            Content = tl,
                            Padding = new Thickness(5,0,0,1),
                            VerticalOptions = LayoutOptions.Center
                        }
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


