using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView.PCL
{
	public partial class TimeSpanRequest : ContentView
	{
		public TimeSpanRequest ()
		{
			InitializeComponent ();
		}

        // View bound 2way to these, by x:Reference
        public String Hours { get { return (String)GetValue(HoursProperty); } set { SetValue(HoursProperty, value); } }
        public static readonly BindableProperty HoursProperty = BindableProperty.Create("Hours", typeof(String), typeof(TimeSpanRequest), "0");
        public String Minutes { get { return (String)GetValue(MinutesProperty); } set { SetValue(MinutesProperty, value); } }
        public static readonly BindableProperty MinutesProperty = BindableProperty.Create("Minutes", typeof(String), typeof(TimeSpanRequest), "0");
        public String Seconds { get { return (String)GetValue(SecondsProperty); } set { SetValue(SecondsProperty, value); } }
        public static readonly BindableProperty SecondsProperty = BindableProperty.Create("Seconds", typeof(String), typeof(TimeSpanRequest), "0");

        // But, the real data goes in BindingContext.value (TimeSpan) cant think of a clever binding
        ValueRequestVM<TimeSpan, TimeSpanRequest> current;
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            if (current != null) current.PropertyChanged -= ValueChanged;
            if (BindingContext is ValueRequestVM<TimeSpan, TimeSpanRequest>)
                (current = BindingContext as ValueRequestVM<TimeSpan, TimeSpanRequest>).PropertyChanged += ValueChanged;
            else current = null;
            ValueChanged(null,null);
        }
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == HoursProperty.PropertyName) HMSChanged();
            if (propertyName == MinutesProperty.PropertyName) HMSChanged();
            if (propertyName == SecondsProperty.PropertyName) HMSChanged();
        }

        int IHours { get => double.TryParse(Hours, out double res) ? (int)res : 0; }
        int IMinutes { get => double.TryParse(Minutes, out double res) ? (int)res : 0; }
        int ISeconds { get => double.TryParse(Seconds, out double res) ? (int)res : 0; }

        // Simple conversion        
        void ValueChanged(object sender, PropertyChangedEventArgs pea)
        {
            if ((pea?.PropertyName ?? "value") != "value") return;
            BlockReentrancy(() =>
            {
                // this is when external comes in
                var use = current?.value ?? new TimeSpan(0, 0, 0);
                Hours = ((int)Math.Floor(use.TotalHours)).ToString();
                Minutes = use.Minutes.ToString();
                Seconds = use.Seconds.ToString();
            });
        }
        void HMSChanged()
        {
            BlockReentrancy(() =>
            {
                // this is one of the dudes changing
                var h = IHours;
                var m = IMinutes;
                var s = ISeconds;
                if (s >= 60)
                {
                    m += s / 60;
                    s = s % 60;
                }
                if(m >= 60)
                {
                    h += m / 60;
                    m = m % 60;
                }
                Hours = h.ToString();
                Minutes = m.ToString();
                Seconds = s.ToString();
                // set value
                if (current != null)
                    current.value = new TimeSpan(h, m, s);
            });
        }

        // helpers
        bool doingstuff = false;
        void BlockReentrancy(Action orig)
        {
            // I should install postsharp for [DoingStuff]
            if (doingstuff) return;
            doingstuff = true;
            try { orig(); }
            finally { doingstuff = false; }
        }
    }
    
}

