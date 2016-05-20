using System;
using Consonance;
using System.Threading.Tasks;
using System.Diagnostics;
using SQLite.Net.Interop;
using System.Reflection;

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
    class Folders : IFSOps
    {
        public string AppData { get; set; }
        public Folders()
        {
            AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
    }
    class CPlat : IPlatform, ITasks
	{
        readonly Folders FF = new Folders();
        public IFSOps filesystem { get { return FF; } }
        #region IPlatform implementation
        Action<string,Action> serr;
		public void Attach (Action<string, Action> showError)
		{
			this.serr = showError; 
		}
		public ITasks TaskOps { get { return this; } }

        public ISQLitePlatform sqlite
        {
            get
            {
                return new SQLite.Net.Platform.Generic.SQLitePlatformGeneric();
            }
        }
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

        public PropertyInfo GetPropertyInfo(Type t, String p) { return t.GetProperty(p); }
        public void UIAssert()
        {
            
        }

        public Task UIThread(Action method)
        {
            TaskCompletionSource<EventArgs> tea = new TaskCompletionSource<EventArgs>();
            method();
            tea.SetResult(new EventArgs());
            return tea.Task;
        }

        public bool CreateDirectory(string ifdoesntexist)
        {
            return System.IO.Directory.CreateDirectory(ifdoesntexist).Exists;
        }
        #endregion
    }
}
