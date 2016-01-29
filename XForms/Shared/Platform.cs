using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace Consonance.XamarinFormsView
{
	class Platform : IPlatform, ITasks
	{
		readonly Action<String, Action> showError;
		public Platform(Action<String, Action> showError)
		{
			this.showError = showError;
			AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;
		}
		void AppDomain_CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
		{
			HandleException (e.ExceptionObject as Exception);
		}
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
			HandleException (t.Exception.InnerException);
		}
		void HandleException(Exception h)
		{
			Debug.WriteLine (h);
			Xamarin.Forms.Device.BeginInvokeOnMainThread (() => 
				showError (h.ToString (), () => Environment.Exit (h.HResult))
			);
		}
		#endregion
		#region IPlatform implementation
		public ITasks TaskOps { get { return this; } }
		#endregion
	}
}

