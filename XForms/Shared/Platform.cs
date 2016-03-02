using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using Xamarin.Forms;
using System.Threading;

namespace Consonance.XamarinFormsView
{
	class Platform : IPlatform, ITasks
	{
		static int uiThread;
		Action<String, Action> showError = (ex,a) => a();
		// Must be constructed on UI thread.
		public Platform()
		{
			uiThread = Thread.CurrentThread.ManagedThreadId;
			AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;
		}
		public static void UIAssert()
		{	
			Debug.Assert (Thread.CurrentThread.ManagedThreadId == uiThread);
		}
		public static Task UIThread(Action method)
		{
			TaskCompletionSource<EventArgs> tc = new TaskCompletionSource<EventArgs> ();
			if (Thread.CurrentThread.ManagedThreadId == uiThread) {
				method(); 
				tc.SetResult(new EventArgs());
			}
			else 
			{
				var callingStack = Environment.StackTrace;
				Device.BeginInvokeOnMainThread(() => {
					try { method(); }
					catch(Exception e) 
					{ 
						tc.SetException(e); 
						Debug.WriteLine("From invoker:\n " + callingStack);
						Debug.WriteLine(e);
						throw;
					}
					tc.SetResult(new EventArgs());
				});
			}
			return tc.Task;
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
			Platform.UIThread (() => showError (h.ToString (), () => { throw h; }));
		}
		#endregion
		#region IPlatform implementation
		public ITasks TaskOps { get { return this; } }
		#endregion
	}
}

