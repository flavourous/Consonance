using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace Consonance.XamarinFormsView
{
	class Platform : IPlatform, ITasks
	{
		Action<String, Action> showError = (ex,a) => a();
		public Platform()
		{
			AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;
		}
		void AppDomain_CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
		{
			HandleException (e.ExceptionObject as Exception);
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
			Xamarin.Forms.Device.BeginInvokeOnMainThread (() => showError (h.ToString (), () => { throw h; }));
		}
		#endregion
		#region IPlatform implementation
		public ITasks TaskOps { get { return this; } }
		#endregion
	}
}

