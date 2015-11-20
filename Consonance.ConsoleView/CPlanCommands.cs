using System;
using Consonance;
using System.Threading.Tasks;

namespace Consonance.ConsoleView
{
	public class CPlanCommands : IPlanCommands
	{
		public CPlanCommands(IValueRequestBuilder builder)
		{
			eat = new CCollectionEditorBoundCommands<EntryLineVM> (builder);
			eatinfo = new CCollectionEditorBoundCommands<InfoLineVM> (builder);
			burn = new CCollectionEditorBoundCommands<EntryLineVM> (builder);
			burninfo = new CCollectionEditorBoundCommands<InfoLineVM> (builder);
		}
		#region IPlanCommands implementation
		public ICollectionEditorBoundCommands<EntryLineVM> eat { get; private set; }
		public ICollectionEditorBoundCommands<InfoLineVM> eatinfo  { get; private set; }
		public ICollectionEditorBoundCommands<EntryLineVM> burn  { get; private set; }
		public ICollectionEditorBoundCommands<InfoLineVM> burninfo  { get; private set; }
		#endregion

		public class CCollectionEditorBoundCommands<T> : ICollectionEditorBoundCommands<T> {
			readonly IValueRequestBuilder builder;
			public CCollectionEditorBoundCommands(IValueRequestBuilder builder) { this.builder = builder; }
			#region ICollectionEditorBoundCommands implementation
			public event Action<IValueRequestBuilder> add = delegate { };
			public event Action<T> remove = delegate { };
			public event Action<T, IValueRequestBuilder> edit = delegate { };
			#endregion
			public void Add() { add(builder); }
			public void Remove(T item) { remove(item); }
			public void Edit(T item) { edit(item, builder); }
		}
	}
}

