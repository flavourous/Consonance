
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Consonance.XamarinFormsView
{
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

