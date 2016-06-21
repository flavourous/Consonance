using System;
using System.Collections.Generic;
using System.Text;

namespace Consonance.XamarinFormsView.PCL
{
    class DefbuilderCommandWrapper<T> : ICollectionEditorBoundCommands<T>
    {
		readonly CommonServices srv;
		public DefbuilderCommandWrapper(CommonServices srv)
		{
            this.srv = srv;
		}
		public void OnAdd() { add (srv.DefaultBuilder); }
		public void OnRemove(T rem) { remove (rem); }
		public void OnEdit(T ed) { edit (ed, srv.DefaultBuilder); }
		public event Action<IValueRequestBuilder> add = delegate { };
		public event Action<T> remove = delegate { };
		public event Action<T, IValueRequestBuilder> edit = delegate { };
    }
    class PlanCommandsWrapper : IPlanCommands
    {
		readonly DefbuilderCommandWrapper<EntryLineVM> inBacker, outBacker;
		readonly DefbuilderCommandWrapper<InfoLineVM> inInfoBacker, outInfoBacker;
		DefbuilderCommandWrapper<InfoLineVM> imtSwitch(InfoManageType mt)
		{
			return mt == InfoManageType.In ? inInfoBacker : outInfoBacker;
		}
		public PlanCommandsWrapper(MainTabs main, CommonServices srv)
		{
			// Ready Backers
			inBacker = new DefbuilderCommandWrapper<EntryLineVM> (srv);
			outBacker = new DefbuilderCommandWrapper<EntryLineVM> (srv);
			inInfoBacker = new DefbuilderCommandWrapper<InfoLineVM> (srv);
			outInfoBacker = new DefbuilderCommandWrapper<InfoLineVM> (srv);

			// hooks on the main tabs
			main.AddIn += inBacker.OnAdd;
			main.AddOut += outBacker.OnAdd;
			main.InItemEdit += inBacker.OnEdit;
			main.OutItemEdit += outBacker.OnEdit;
			main.InItemDelete += inBacker.OnRemove;
			main.OutItemDelete += outBacker.OnRemove;

		}
		public void Attach(InfoManageView iman, InfoManageType mt)
		{
			// hooks on the info manager...
			iman.ItemAdd += () => imtSwitch(mt).OnAdd();
			iman.ItemEdit += m => imtSwitch(mt).OnEdit(m);
			iman.ItemDelete += m => imtSwitch(mt).OnRemove(m);
		}
        public ICollectionEditorBoundCommands<EntryLineVM> eat { get { return inBacker; } }
		public ICollectionEditorBoundCommands<InfoLineVM> eatinfo { get { return inInfoBacker; } }
		public ICollectionEditorBoundCommands<EntryLineVM> burn { get { return outBacker; } }
		public ICollectionEditorBoundCommands<InfoLineVM> burninfo { get { return outInfoBacker;; } }
    }
}
