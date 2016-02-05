using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public class GroupedTDVM : ObservableCollection<TrackerDetailsVM>
	{
		public String category { get; set; }
	}
	public class GroupedTDVMCollection
	{
		public ObservableCollection<GroupedTDVM> collection = new ObservableCollection<GroupedTDVM>();
		public Dictionary<String, GroupedTDVM> reffers = new Dictionary<string, GroupedTDVM>();
		public Dictionary<TrackerDetailsVM, int> reffers2 = new Dictionary<TrackerDetailsVM, int>();
		public void Clear() { collection.Clear (); reffers.Clear (); reffers2.Clear (); }
		public void Add(TrackerDetailsVM vm)
		{
			reffers2 [vm] = reffers.Count;
			if (reffers.ContainsKey (vm.category))
				reffers [vm.category].Add (vm);
			else {
				var vvm = new GroupedTDVM { category = vm.category };
				collection.Add (vvm);
				reffers [vm.category] = vvm;
				vvm.Add (vm);
			}
		}
	}
	public partial class ChoosePlanView : ContentPage
	{
		public ChoosePlanView ()
		{
			InitializeComponent ();
            BindingContext = this;
		}

		public GroupedTDVMCollection mPlanChoices = new GroupedTDVMCollection();
		public ObservableCollection<GroupedTDVM> PlanChoices { get { return mPlanChoices.collection; } }
		TrackerDetailsVM _choicey;
		public TrackerDetailsVM choicey {
			get{ return _choicey; }
			set {
				if(_choicey != null) _choicey.selected = false;
				_choicey = value;
				if(_choicey != null) _choicey.selected = true;
				else lv.SelectedItem = null;
			}
		}
		public event Action<int> chosen = delegate { };
		public void DoChoose(Object s, EventArgs e) { chosen(mPlanChoices.reffers2[choicey]); }
	}
}
