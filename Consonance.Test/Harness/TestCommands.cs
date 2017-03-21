using System;
using Consonance;
using Consonance.Protocol;

namespace Consonance.Test
{
    public class Loose<T> : ICollectionEditorLooseCommands<T>
    {
        public event Action add;
        public event Action<T> edit;
        public event Action<T> remove;
        public void Add() => add?.Invoke();
        public void Edit(T i) => edit?.Invoke(i);
        public void Remove(T i) => remove?.Invoke(i);
    }
    public class SelectableLoose<T> : Loose<T>, ICollectionEditorSelectableLooseCommands<T>
    {
        public event Action<T> select;
        public void Select(T i) => select?.Invoke(i);
    }
    public class Bound<T> : ICollectionEditorBoundCommands<T>
    {
        public event Action<IValueRequestBuilder> add;
        public event Action<T, IValueRequestBuilder> edit;
        public event Action<T> remove;
        public void Add(IValueRequestBuilder b) => add?.Invoke(b);
        public void Edit(T i, IValueRequestBuilder b) => edit?.Invoke(i,b);
        public void Remove(T i) => remove?.Invoke(i);
    }

    public class PlanCommands : IPlanCommands
    {
        public ICollectionEditorBoundCommands<EntryLineVM> burn { get { return _burn; } }
        public Bound<EntryLineVM> _burn = new Bound<EntryLineVM>();
        public ICollectionEditorBoundCommands<InfoLineVM> burninfo { get { return _burninfo; } }
        public Bound<InfoLineVM> _burninfo = new Bound<InfoLineVM>();
        public ICollectionEditorBoundCommands<EntryLineVM> eat { get { return _eat; } }
        public Bound<EntryLineVM> _eat = new Bound<EntryLineVM>();
        public ICollectionEditorBoundCommands<InfoLineVM> eatinfo { get { return _eatinfo; } }
        public Bound<InfoLineVM> _eatinfo = new Bound<InfoLineVM>();
    }
}
