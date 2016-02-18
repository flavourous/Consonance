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
			Presenter.PresentTo (view, new CPlat(), input, plancommands, dbuild).Wait();
			// console loop
			consolePager = new ConsolePager(view);
			consolePager.RunLoop ();
		}
	}
	class CPlat : IPlatform, ITasks
	{
		#region IPlatform implementation
		Action<string,Action> serr;
		public void Attach (Action<string, Action> showError)
		{
			this.serr = showError; 
		}
		public ITasks TaskOps { get { return this; } }
		#endregion
		#region ITasks implementation
		public Task RunTask (Func<Task> asyncMethod)
		{
			return Task.Run (asyncMethod);
		}
		public Task RunTask (Action syncMethod)
		{
			return Task.Run (syncMethod);
		}
		public Task<T> RunTask<T> (Func<Task<T>> asyncMethod)
		{
			return Task.Run (asyncMethod);
		}
		public Task<T> RunTask<T> (Func<T> syncMethod)
		{
			return Task.Run (syncMethod);
		}
		#endregion
	}
}
