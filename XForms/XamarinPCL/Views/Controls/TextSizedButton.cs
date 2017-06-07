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
            b.SetBinding(Button.CommandProperty, new Binding("Command", source: this));
            Content = new Grid
            {
                Children = { b, l },
                RowDefinitions = { (rd=new RowDefinition { Height = GridLength.Auto }) },
                ColumnDefinitions = { (cd=new ColumnDefinition { Width = GridLength.Auto }) }
            };
            MeasureInvalidated += TextSizedButton_MeasureInvalidated;
        }
        public Command Command { get => (Command)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
        public static readonly BindableProperty CommandProperty = BindableProperty.Create("Comand", typeof(Command), typeof(TextSizedButton));
        public String Text { get { return (String)GetValue(TextProperty); } set { SetValue(TextProperty, value); } }
        public static readonly BindableProperty TextProperty = BindableProperty.Create("Text", typeof(String), typeof(TextSizedButton), null, BindingMode.OneWay, null, TextChanged);
        public static void TextChanged(BindableObject obj, Object oldValue, Object newValue)
        {
            TextSizedButton sender = obj as TextSizedButton;
            String oldText = oldValue as String;
            String newText = newValue as String;
            sender.l.Text = newText;
            sender.InvalidateMeasure();
        }

        public double FontSize { get { return l.FontSize; } set { l.FontSize = value; InvalidateMeasure(); } }
        public event EventHandler Clicked { add { b.Clicked += value; } remove { b.Clicked -= value; } }

        private void TextSizedButton_MeasureInvalidated(object sender, EventArgs e)
        {
            var sr = l.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);
            rd.Height = sr.Request.Height + 15;
            cd.Width = sr.Request.Width + 30;
        }
    }
}
