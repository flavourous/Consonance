using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using Xamarin.Forms;

namespace Consonance.XamarinFormsView.PCL
{
    public class TextSizedButton : ContentView
    {
        readonly ColumnDefinition cd;
        readonly RowDefinition rd;
        readonly Label l;
        readonly Button b;
        public TextSizedButton()
        {
            l = new Label { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            b = new Button();
            Content = new Grid
            {
                Children = { b, l },
                RowDefinitions = { (rd=new RowDefinition { Height = GridLength.Auto }) },
                ColumnDefinitions = { (cd=new ColumnDefinition { Width = GridLength.Auto }) }
            };
            MeasureInvalidated += TextSizedButton_MeasureInvalidated;
        }

        public String Text { get { return l.Text; } set { l.Text = value; InvalidateMeasure(); } }
        public event EventHandler Clicked { add { b.Clicked += value; } remove { b.Clicked -= value; } }

        private void TextSizedButton_MeasureInvalidated(object sender, EventArgs e)
        {
            var sr = l.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);
            rd.Height = sr.Request.Height + 5;
            cd.Width = sr.Request.Width + 20;
        }
    }
}
