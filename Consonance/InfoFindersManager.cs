using System;
using System.Collections.Generic;

namespace Consonance
{
	delegate InfoLineVM InfoPresenter<IType>(IType info);
	static class InfoFindersManager
	{
		static Dictionary<Type,object> repo = new Dictionary<Type, object>();
		static InfoFindersManager()
		{
			AddFinder (new MockFoodInfoManager ());
		}
		public static void AddFinder<IType>(IFindData<IType> finder)
		{
			repo [typeof(IType)] = finder;
		}
		public static IFindList<InfoLineVM> GetFinder<IType>(InfoPresenter<IType> creator, MyConn connection) where IType : BaseInfo
		{
			if(repo.ContainsKey(typeof(IType)))
			{ 
				var finder = repo [typeof(IType)] as IFindData<IType>;
				return new FinderAdapter<IType> (creator, finder, connection);
			}
			return new EmptyFinder ();
		}
	}

	class FinderAdapter<IType> : IFindList<InfoLineVM>
	{
		readonly InfoPresenter<IType> creator;
		readonly IFindData<IType> searcher;
		readonly MyConn conn;
		public FinderAdapter(InfoPresenter<IType> creator, IFindData<IType> searcher, MyConn conn)
		{
			this.conn = conn;
			this.creator=creator;
			this.searcher=searcher;
		}

		#region IFindList implementation
		public bool CanFind { get { return true; } }
		public IEnumerable<InfoLineVM> Find (string filter)
		{
			// enumerate from the search gradually, so that it can page it or whatever it wants...
			foreach (var sf in searcher.BeginSearch(filter)) {
				var vm = creator (sf);
				vm.originator = sf;
				yield return vm;
			}
		}
		public void Import (InfoLineVM item)
		{
			IType model = (IType)item.originator;
			conn.Insert (model, typeof(IType));
		}
		#endregion
	}

	interface IFindData<out T>
	{
		IEnumerable<T> BeginSearch (String filter);
	}

	class EmptyFinder : IFindList<InfoLineVM>
	{
		#region IFindList implementation
		public IEnumerable<InfoLineVM> Find (string filter) { yield break; }
		public void Import (InfoLineVM item) { }
		public bool CanFind { get { return false; } }
		#endregion
	}

	class MockFoodInfoManager : IFindData<FoodInfo>
	{
		#region IFindData implementation
		public IEnumerable<FoodInfo> BeginSearch (string filter)
		{
			Random rd = new Random ();
			for (int i = 0; i < 100; i++) {
				yield return new FoodInfo () {
					calories = rd.Next(10,400),
					name = filter + " davey food " + i
				};
			}
		}
		#endregion
	}

}

