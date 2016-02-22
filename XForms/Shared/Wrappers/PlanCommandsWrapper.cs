using System;
using System.Collections.Generic;
using System.Text;

namespace Consonance.XamarinFormsView
{
    class DefbuilderCommandWrapper<T> : ICollectionEditorBoundCommands<T>
    {
		readonly IValueRequestBuilder bld;
		public DefbuilderCommandWrapper(IValueRequestBuilder bld)
		{
			this.bld = bld;
		}
		public void OnAdd() { add (bld); }
		public void OnRemove(T rem) { remove (rem); }
		public void OnEdit(T ed) { edit (ed, bld); }
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
		public PlanCommandsWrapper(IValueRequestBuilder defBuilder, MainTabs main)
		{
			// Ready Backers
			inBacker = new DefbuilderCommandWrapper<EntryLineVM> (defBuilder);
			outBacker = new DefbuilderCommandWrapper<EntryLineVM> (defBuilder);
			inInfoBacker = new DefbuilderCommandWrapper<InfoLineVM> (defBuilder);
			outInfoBacker = new DefbuilderCommandWrapper<InfoLineVM> (defBuilder);

			// hooks on the main tabs
			main.AddIn += inBacker.OnAdd;
			main.AddOut += outBacker.OnAdd;
			main.InItemEdit += inBacker.OnEdit;
			main.OutItemEdit += outBacker.OnEdit;
			main.InItemDelete += inBacker.OnRemove;
			main.OutItemDelete += outBacker.OnRemove;

		}
		public void Attach(InfoManageView iman)
		{
			// hooks on the info manager...
			iman.ItemAdd += mt => imtSwitch(mt).OnAdd();
			iman.ItemEdit += (mt,m) => imtSwitch(mt).OnEdit(m);
			iman.ItemDelete += (mt,m) => imtSwitch(mt).OnRemove(m);
		}
        public ICollectionEditorBoundCommands<EntryLineVM> eat { get { return inBacker; } }
		public ICollectionEditorBoundCommands<InfoLineVM> eatinfo { get { return inInfoBacker; } }
		public ICollectionEditorBoundCommands<EntryLineVM> burn { get { return outBacker; } }
		public ICollectionEditorBoundCommands<InfoLineVM> burninfo { get { return outInfoBacker;; } }
    }
}
