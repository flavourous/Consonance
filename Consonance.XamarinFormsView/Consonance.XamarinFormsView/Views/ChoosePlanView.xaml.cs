﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
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

		ObservableCollection<TrackerDetailsVM> mPlanChoices = new ObservableCollection<TrackerDetailsVM> ();
		public ObservableCollection<TrackerDetailsVM> PlanChoices { get { return mPlanChoices; } }
		public TrackerDetailsVM choicey { get; set; }
		public event Action<int> chosen = delegate { };
        public void DoChoose(Object s, EventArgs e) { chosen(mPlanChoices.IndexOf(choicey)); }
		public void DoCancel(Object s, EventArgs e) { Navigation.PopAsync(); }
	}
}
