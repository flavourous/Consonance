using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public partial class ChoosePlanView : ContentPage
	{
		public ChoosePlanView ()
		{
			InitializeComponent ();
		}

		public readonly BindingList<TrackerDetailsVM> planItems = new BindingList<TrackerDetailsVM> ();
		TrackerDetailsVM chosen { get; set; }
		public event Action<TrackerDetailsVM> import = delegate { };
		public void DoImport(Object s, EventArgs e) { import (chosen); }
	}
}
