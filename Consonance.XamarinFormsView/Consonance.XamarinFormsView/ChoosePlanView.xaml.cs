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
            BindingContext = this;
		}

		BindingList<TrackerDetailsVM> mPlanChoices = new BindingList<TrackerDetailsVM> ();
        public BindingList<TrackerDetailsVM> PlanChoices { get { return mPlanChoices; } }
		public TrackerDetailsVM choicey { get; set; }
		public event Action<int> chosen = delegate { };
        public void DoChoose(Object s, EventArgs e) { chosen(mPlanChoices.IndexOf(choicey)); }
		public void DoCancel(Object s, EventArgs e) { Navigation.PopAsync(); }
	}
}
