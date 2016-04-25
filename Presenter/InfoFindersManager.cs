using System;
using System.Collections.Generic;
using LibSharpHelp;
using SQLite.Net;

namespace Consonance
{
	static class InfoFindersManager
	{
		static Dictionary<Type,object> repo = new Dictionary<Type, object>();
		static InfoFindersManager()
		{
			//AddFinder (new MockFoodInfoFinder ());
		}
		public static void AddFinder<IType>(IFindData<IType> finder)
		{
			repo [typeof(IType)] = finder;
		}
		public static IFindList<InfoLineVM> GetFinder<IType>(Func<IType, InfoLineVM> creator, SQLiteConnection connection) where IType : BaseInfo
		{
			if(repo.ContainsKey(typeof(IType)))
			{ 
				var finder = repo [typeof(IType)] as IFindData<IType>;
				return new FinderAdapter<IType> (creator, finder, connection);
			}
			return new EmptyFinder ();
		}
	}

	// fuller interface for clients
	public interface IFindList<T> : IFindData<T>
	{
		bool CanFind { get; }
		void Import(T item);
	}

	// Internal interface for making finders
	public interface IFindData<T>
	{
		String[] FindModes { get; }
		// returns IValueRequest Objects, which the view can reinsert.
		Object[] UseFindMode (String mode, IValueRequestFactory factory); // dosent do requests, just pulls data.
		IReadOnlyList<T> Find (); // pulls data from requestbuilder.
	}
		
	class FinderAdapter<IType> : IFindList<InfoLineVM>
	{
		readonly Func<IType, InfoLineVM> creator;
		readonly IFindData<IType> searcher;
		readonly SQLiteConnection conn;
		public FinderAdapter(Func<IType, InfoLineVM> creator, IFindData<IType> searcher, SQLiteConnection conn)
		{
			this.conn = conn;
			this.creator=creator;
			this.searcher=searcher;
		}

		#region IFindList implementation
		public bool CanFind { get { return true; } }
		public IReadOnlyList<InfoLineVM> Find ()
		{
			return new ReadOnlyListConversionAdapter<IType, InfoLineVM> (searcher.Find (), m => {
				var vm = creator (m);
				vm.originator = m;
				return vm;
			});
		}
		public void Import (InfoLineVM item)
		{
			IType model = (IType)item.originator;
			conn.Insert (model, typeof(IType));
		}
		public String[] FindModes { get { return searcher.FindModes; } }
		public Object[] UseFindMode (String mode, IValueRequestFactory factory) { return searcher.UseFindMode (mode, factory); }
		#endregion
	}

	class EmptyFinder : IFindList<InfoLineVM>
	{
		#region IFindList implementation
		public String[] FindModes { get{return new String[0];}}
		public Object[] UseFindMode(String mode, IValueRequestFactory fact) { return new object[0]; }
		public IReadOnlyList<InfoLineVM> Find () { return new List<InfoLineVM>(); }
		public void Import (InfoLineVM item) { }
		public bool CanFind { get { return false; } }
		#endregion
	}

	class MockFoodInfoFinder : IFindData<FoodInfo>
	{
		#region IFindData implementation
		public IReadOnlyList<FoodInfo> Find ()
		{
			Random rd = new Random ();
			List<FoodInfo> ret = new List<FoodInfo> ();
			for (int i = 0; i < 100; i++) {
				ret.Add (new FoodInfo () {
					calories = rd.Next (10, 400),
					name = SomeFilteringDelegate () + " davey food " + i
				});
			}
			return ret;
		}
		Func<String> SomeFilteringDelegate = () => "Unset Filter";
		public String[] FindModes { get { return new String[] { "Text" }; } }
		public Object[] UseFindMode (String mode, IValueRequestFactory factory)
		{
			List<Object> result = new List<object>();
			int idx = new List<String> (FindModes).FindIndex (m => mode == m);
			switch(idx)
			{
				case 0:
					var sr = factory.StringRequestor ("Search Text");
					result.Add (sr.request);
					SomeFilteringDelegate = () => sr.value;
					break;
			}
			return result.ToArray ();
		}
		#endregion
	}

}

