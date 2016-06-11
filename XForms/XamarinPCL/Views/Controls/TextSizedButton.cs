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
        Label l;
        Button b;
        public TextSizedButton()
        {
            l=new Label { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            b = new Button();
            Content = new Grid { Children = { b, l } };
            MeasureInvalidated += TextSizedButton_MeasureInvalidated;
        }

        public String Text { get { return l.Text; } set { l.Text = value; InvalidateMeasure(); } }
        public event EventHandler Clicked { add { b.Clicked += value; } remove { b.Clicked -= value; } }


        public void IM() { InvalidateMeasure();  }
        public int vhack = 5, hhack = 20;
        private void TextSizedButton_MeasureInvalidated(object sender, EventArgs e)
        {
            var sr = l.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);
            b.HeightRequest = sr.Request.Height + vhack;
            b.WidthRequest = sr.Request.Width + hhack;
        }

    }
}
