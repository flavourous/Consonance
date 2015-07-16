using System;
using System.Collections.Generic;
using System.Text;

namespace Consonance.XamarinFormsView
{
    class Dummy<T> : ICollectionEditorBoundCommands<T>
    {
        public event Action<IValueRequestBuilder> add;
        public event Action<T> remove;
        public event Action<T, IValueRequestBuilder> edit;
    }
    class PlanCommandsWrapper : IPlanCommands
    {
        public ICollectionEditorBoundCommands<EntryLineVM> eat { get { return new Dummy<EntryLineVM>(); } }
        public ICollectionEditorBoundCommands<InfoLineVM> eatinfo { get { return new Dummy<InfoLineVM>(); } }
        public ICollectionEditorBoundCommands<EntryLineVM> burn { get { return new Dummy<EntryLineVM>(); } }
        public ICollectionEditorBoundCommands<InfoLineVM> burninfo { get { return new Dummy<InfoLineVM>(); } }
    }
}
