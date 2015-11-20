using System;
using Consonance;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Consonance.ConsoleView
{
	class MainClass
	{
		public static CValueRequestBuilder dbuild = new CValueRequestBuilder();
		public static CView view;
		public static CInput input;
		public static CPlanCommands plancommands;
		public static ConsolePager consolePager;
		public static void Main (string[] args)
		{
			plancommands = new CPlanCommands(dbuild);
			input = new CInput (plancommands,dbuild.requestFactory);
			view = new CView(plancommands);
			Presenter.PresentTo (view,new Platform(), input, plancommands, dbuild).Wait();
			// console loop
			consolePager = new ConsolePager(view);
			consolePager.RunLoop ();
		}
	}
	class Platform : IPlatform, ITasks
	{
		#region ITasks implementation
		public Task RunTask (Func<Task> asyncMethod)
		{ return FailHandler (Task.Factory.StartNew(asyncMethod, TaskCreationOptions.AttachedToParent)); }
		public Task RunTask (Action syncMethod) 
		{ return FailHandler (Task.Factory.StartNew(syncMethod, TaskCreationOptions.AttachedToParent)); }
		public Task<T> RunTask<T> (Func<Task<T>> asyncMethod)
		{ return FailHandler (Task.Factory.StartNew(asyncMethod, TaskCreationOptions.AttachedToParent)) as Task<T>; }
		public Task<T> RunTask<T> (Func<T> syncMethod) 
		{ return FailHandler (Task.Factory.StartNew(syncMethod, TaskCreationOptions.AttachedToParent)) as Task<T>; }
		Task FailHandler(Task t)
		{
			t.ContinueWith (Failed, TaskContinuationOptions.OnlyOnFaulted); 
			return t;
		}
		void Failed(Task t)
		{
			Debug.WriteLine (t.Exception.InnerException);
			throw t.Exception.InnerException;
		}
		#endregion
		#region IPlatform implementation
		public ITasks TaskOps { get { return this; } }
		#endregion
	}
}
