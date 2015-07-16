using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Consonance;

namespace Consonance.XamarinFormsView
{
    class ValueRequestBuilder : IValueRequestBuilder
    {
        public void GetValues(string title, BindingList<object> requests, Promise<bool> completed, int page, int pages)
        {
            throw new NotImplementedException();
        }

        public IValueRequestFactory requestFactory
        {
            get { throw new NotImplementedException(); }
        }
    }
}
