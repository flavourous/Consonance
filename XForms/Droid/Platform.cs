using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using Xamarin.Forms;
using System.Threading;
using SQLite.Net.Interop;
using System.IO;

namespace Consonance.XamarinFormsView
{
    class FileSystem : IFSOps
    {
        public string AppData { get; set; }
        public FileSystem()
        {
            AppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        public void Delete(string file)
        {
            File.Delete(file);
        }

        public byte[] ReadFile(string file)
        {
            return File.ReadAllBytes(file);
        }
    }
    class Platform : IPlatform, ITasks
	{
        public PropertyInfo GetPropertyInfo(Type t, String p) { return t.GetProperty(p); }
        readonly FileSystem FF = new FileSystem();
        public IFSOps filesystem { get { return FF; } }
		static int uiThread;
		Action<String, Action> showError = (ex,a) => a();
        public Task UIThread(Action method)
        {
            TaskCompletionSource<EventArgs> tc = new TaskCompletionSource<EventArgs>();
            Device.BeginInvokeOnMainThread(() =>
            {
                method();
                tc.SetResult(new EventArgs());
            });
            return tc.Task;
        }
		public void Attach(Action<String, Action> showError)
		{
			this.showError = showError;
		}
		#region ITasks implementation
		public Task RunTask (Func<Task> asyncMethod)
		{ return FailHandler (Task.Run(asyncMethod)); }
		public Task RunTask (Action syncMethod) 
		{ return FailHandler (Task.Run(syncMethod)); }
		public Task<T> RunTask<T> (Func<Task<T>> asyncMethod)
		{ return FailHandler (Task.Run(asyncMethod)) as Task<T>; }
		public Task<T> RunTask<T> (Func<T> syncMethod) 
		{ return FailHandler (Task.Run(syncMethod)) as Task<T>; }
		Task FailHandler(Task t)
		{
			t.ContinueWith (Failed, TaskContinuationOptions.OnlyOnFaulted); 
			return t;
		}
		void Failed(Task t)
		{
			HandleException (t.Exception.InnerException);
		}
		void HandleException(Exception h)
		{
			Debug.WriteLine (h);
			//UIThread (() => showError (h.ToString (), () => { throw h; }));
            Device.BeginInvokeOnMainThread(() => { throw h; });
            throw h;
        }

        public bool CreateDirectory(string ifdoesntexist)
        {
            if (Directory.Exists(ifdoesntexist))
                return false;
            Directory.CreateDirectory(ifdoesntexist);
            return true;
        }
        #endregion
        #region IPlatform implementation
        public ITasks TaskOps { get { return this; } }

        public ISQLitePlatform sqlite
        {
            get
            {
                return new SQLite.Net.Platform.XamarinAndroid.SQLitePlatformAndroid();
            }
        }
        #endregion
    }
}

