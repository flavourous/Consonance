#warning FIXME: Move to XLib
using Consonance.XamarinFormsView.PCL;
using ScnViewGestures.Plugin.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using Xamarin.Forms;

namespace XLib
{
    [ContentProperty("RealContent")]
    public class ViewGesturesMagic : ContentView
    {
        //proxy
        readonly ViewGestures vg = new ViewGestures();
#if DEBUG
        event EventHandler swipeDown;
        event EventHandler swipeUp;
        event EventHandler swipeRight;
        event EventHandler swipeLeft;
        public event EventHandler SwipeDown { add { swipeDown += value; ComputeEvented(); } remove { swipeDown -= value; ComputeEvented(); } }
        public event EventHandler SwipeUp { add { swipeUp += value; ComputeEvented(); } remove { swipeUp -= value; ComputeEvented(); } }
        public event EventHandler SwipeRight { add { swipeRight += value; ComputeEvented(); } remove { swipeRight -= value; ComputeEvented(); } }
        public event EventHandler SwipeLeft { add { swipeLeft += value; ComputeEvented(); } remove { swipeLeft -= value; ComputeEvented(); } }
        View left, right, up, down;

        void ComputeEvented()
        {
            left.IsVisible = swipeLeft != null && swipeLeft.GetInvocationList().Length > 0;
            right.IsVisible = swipeRight != null && swipeRight.GetInvocationList().Length > 0;
            up.IsVisible = swipeUp != null && swipeUp.GetInvocationList().Length > 0;
            down.IsVisible = swipeDown != null && swipeDown.GetInvocationList().Length > 0;
        }
#else
        event EventHandler SwipeDown;
        event EventHandler swipeUp;
        event EventHandler SwipeRight;
        event EventHandler SwipeLeft;
#endif


        public View RealContent { get => GetValue(RealContentProperty) as View; set => SetValue(RealContentProperty, value); }
        public static BindableProperty RealContentProperty = BindableProperty.Create("RealContent", typeof(View), typeof(ViewGesturesMagic));
        public ViewGesturesMagic()
        {
            // proxy
            vg.SwipeDown += (o, e) => swipeDown(o, e);
            vg.SwipeUp += (o, e) => swipeUp(o, e);
            vg.SwipeLeft += (o, e) => swipeLeft(o, e);
            vg.SwipeRight += (o, e) => swipeRight(o, e);

            vg.SetBinding(ContentProperty, new Binding(RealContentProperty.PropertyName, source: this));
#if DEBUG
            Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition{ Width = GridLength.Auto },
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Auto },
                },
                RowDefinitions =
                {
                    new RowDefinition{ Height = GridLength.Auto },
                    new RowDefinition{ Height = GridLength.Star },
                    new RowDefinition{ Height = GridLength.Auto },
                },
                Children =
                {
                    vg.OnCol(0,3).OnRow(0,3),
                    (left=CreateHandle("<",()=>vg.OnSwipeLeft()).OnCol(0).OnRow(1)),
                    (right=CreateHandle(">",()=>vg.OnSwipeRight()).OnCol(2).OnRow(1)),
                    (up=CreateHandle("^",()=>vg.OnSwipeUp()).OnCol(1).OnRow(2)),
                    (down=CreateHandle("v",()=>vg.OnSwipeDown()).OnCol(1).OnRow(0)),
                }
            };
#else
            Content = vg;
#endif
        }
        View CreateHandle(String n, Action hact) => new TextSizedButton
        {
            Text = n,
            Command = new Command(hact),
            Opacity = 0.3,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
        };

    }
}
