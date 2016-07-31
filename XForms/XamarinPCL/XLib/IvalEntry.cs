using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using Xamarin.Forms;

namespace XLib
{
    public class IvalEntry : Entry
    {
        public IvalEntry()
        {
            TextChanged += IvalEntry_TextChanged;
        }

        private void IvalEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            InvalidateMeasure();
        }
    }
}
