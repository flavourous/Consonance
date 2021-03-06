﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Xamarin.Forms;
using Consonance.Protocol;

namespace Consonance.XamarinFormsView.PCL
{
    public partial class ValueRequestView : ContentPage
    {
        // keeps the state between ctor and first ok press. and can be reset from outside.
        public bool ignorevalidity = true;

        public readonly ValueRequestList vlist;
        public ValueRequestView()
        {
            this.ToolbarItems.Add(new ToolbarItem("OK", null, OKClick));
            App.RegisterPoppedCallback(this, () => Completed(false));
            Content = new ScrollView { Content = vlist = new ValueRequestList() };
        }

        void Completed(bool suc)
        {
            // apparently this becomes null after deregistration, not delegate{}.
            completed?.Invoke(suc);
        }

        public Action<bool> completed = delegate { };
        public void OKClick()
        {
            ignorevalidity = false;
            foreach (var vv in vlist.rowViews)
                ((IValueRequestVM)vv.BindingContext).ignorevalid = false;

            if (vlist.vlm.Valid) Completed(true);
            else UserInputWrapper.message("Fix the invalid input first");
        }
    }
}

