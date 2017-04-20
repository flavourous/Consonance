using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Consonance.Protocol;

namespace Consonance.XamarinFormsView.PCL
{
	public class GroupedTDVM : ObservableCollection<ItemDescriptionVM>
	{
		public String category { get; set; }
	}
	public class GroupedTDVMCollection
	{
		public ObservableCollection<GroupedTDVM> collection = new ObservableCollection<GroupedTDVM>();
		public Dictionary<String, GroupedTDVM> reffers = new Dictionary<string, GroupedTDVM>();
		public Dictionary<ItemDescriptionVM, int> reffers2 = new Dictionary<ItemDescriptionVM, int>();
        int iidx = 0;
		public void Clear() { iidx = 0; collection.Clear (); reffers.Clear (); reffers2.Clear (); }
		public void Add(ItemDescriptionVM vm)
		{
			reffers2 [vm] = iidx++;
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
        public Command okCommand { get; private set; }
		public ChoosePlanView ()
		{
			InitializeComponent ();
            this.okCommand = new Command(DoChoose, CanChoose);
            BindingContext = this;

		}

		public GroupedTDVMCollection mPlanChoices = new GroupedTDVMCollection();
		public ObservableCollection<GroupedTDVM> PlanChoices { get { return mPlanChoices.collection; } }
		ItemDescriptionVM _choicey;
		public ItemDescriptionVM choicey {
			get{ return _choicey; }
			set {
				_choicey = value;
                okCommand.ChangeCanExecute();
				if(_choicey == null) lv.SelectedItem = null;
			}
		}
		public event Action<int> chosen = delegate { };
		public void DoChoose() { chosen(mPlanChoices.reffers2[choicey]); }
        public bool CanChoose() { return choicey != null; }
	}
}
