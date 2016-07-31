using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Consonance.XamarinFormsView.PCL
{
    public partial class ValueRequestList : ContentView
    {
        public ValidListenManager vlm = new ValidListenManager("valid");
        public ValueRequestList()
        {
            InitializeComponent();
        }

        public void ClearRows()
        {
            while (rowViews.Count > 0)
                RemoveRow(rowViews.Count - 1);
        }
        public List<View> rowViews = new List<View>();
        Dictionary<View, View> titleViews = new Dictionary<View, View>();
        Dictionary<View, View> crossViews = new Dictionary<View, View>();
        public void AddRow(View forRow) { InsertRow(rowViews.Count, forRow); }
        public void InsertRow(int idx, View forRow)
        {
            inputs.RowDefinitions.Insert(idx, new RowDefinition { Height = GridLength.Auto });
            var isn = (forRow.BindingContext as IValueRequestVM);

            // title maueb
            if (isn.showName)
            {
                var tv = titleViews[forRow] = RLab(isn.name);
                tv.BindingContext = isn;
                inputs.Children.Add(tv);
            }

            // crossview
            var cv = crossViews[forRow] = CLab(isn);
            inputs.Children.Add(cv);
            cv.SetValue(Grid.ColumnProperty, 0);
            cv.SetValue(Grid.ColumnSpanProperty, 3);

            // view
            int col = isn.showName ? 1 : 0, colspan = isn.showName ? 1 : 2;
            forRow.SetValue(Grid.ColumnProperty, col);
            forRow.SetValue(Grid.ColumnSpanProperty, colspan);
            inputs.Children.Add(forRow);

            // indexing etc
            isn.ignorevalid = ignorevalidity;
            rowViews.Insert(idx, forRow);
            vlm.ListenForValid(isn);

            // process after
            for (int i = 0; i < rowViews.Count; i++)
            {
                if ((int)rowViews[i].GetValue(Grid.RowProperty) != i)
                    rowViews[i].SetValue(Grid.RowProperty, i);
                if (titleViews.ContainsKey(rowViews[i])
                    && (int)titleViews[rowViews[i]].GetValue(Grid.RowProperty) != i)
                    titleViews[rowViews[i]].SetValue(Grid.RowProperty, i);
                crossViews[rowViews[i]].SetValue(Grid.RowProperty, i);
            }
        }
        public bool ignorevalidity = true;
        InvalidConverter ic = new InvalidConverter(false, true);
        View CLab(Object bc)
        {
            var fr = new Button
            {
                InputTransparent = false,
                BorderWidth = 2.0,
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Red,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                BindingContext = bc
            };
            fr.SetBinding(Frame.IsVisibleProperty, "vvalid", BindingMode.OneWay, ic);
            return fr;
        }
        View RLab(String name)
        {
            var fr = new Frame
            {
                Content = new Label
                {
                    Text = name,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center
                },
                Padding = new Thickness(5.0, 0, 3.0, 0)
            };
            return fr;
        }
        public void RemoveRow(int row)
        {
            var v = rowViews[row];
            var vv = v.BindingContext as IValueRequestVM;
            vv.ClearPropChanged();
            inputs.Children.Remove(v);
            rowViews.Remove(v);
            if (titleViews.ContainsKey(v))
            {
                inputs.Children.Remove(titleViews[v]);
                titleViews.Remove(v);
            }
            inputs.Children.Remove(crossViews[v]);
            crossViews.Remove(v);
            vlm.RemoveListen(vv);
        }
    }

    public class ValidListenManager : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void OnPropertyChanged(String prop)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
        private bool mValid;
        public bool Valid { get { return mValid; } set { mValid = value; OnPropertyChanged("Valid"); } }

        readonly String ValidName;
        public ValidListenManager(String ValidName)
        {
            this.ValidName = ValidName;
        }

        Dictionary<INotifyPropertyChanged, bool> currentValidity = new Dictionary<INotifyPropertyChanged, bool>();
        public void ListenForValid(INotifyPropertyChanged obj)
        {
            currentValidity[obj] = (bool)obj.GetType().GetTypeInfo().GetDeclaredProperty(ValidName).GetValue(obj);
            obj.PropertyChanged += ValidityListener;
            ValidityListener(obj, new PropertyChangedEventArgs(ValidName));
        }
        public void ClearListens()
        {
            foreach (var k in currentValidity.Keys)
                k.PropertyChanged -= ValidityListener;
            Valid = false;
            currentValidity.Clear();
        }
        public void RemoveListen(INotifyPropertyChanged itm)
        {
            currentValidity.Remove(itm);
            itm.PropertyChanged -= ValidityListener;
        }
        void ValidityListener(Object sender, PropertyChangedEventArgs pea)
        {
            // oh hate reflection, but it's in the spirit of things.
            if (pea.PropertyName != ValidName) return;
            var sti = sender.GetType().GetTypeInfo();
            bool isValid = (bool)sti.GetDeclaredProperty(pea.PropertyName).GetValue(sender);
            currentValidity[sender as INotifyPropertyChanged] = isValid;
            bool validCheck = true;
            foreach (var val in currentValidity.Values)
                if (!val) validCheck = false;
            Valid = validCheck;
        }
    }
}
