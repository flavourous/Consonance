using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using Xamarin.Forms;
using System.Threading;
using SQLite.Net.Interop;
using System.IO;
using System.Reflection.Emit;
using Android.Content;
using Android.OS;

namespace Consonance.XamarinFormsView
{
    class FileSystem : IFSOps
    {
        public string AppData { get; set; }
        public FileSystem()
        {
            AppData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        }
        public void Delete(string file)
        {
            File.Delete(file);
        }

        public byte[] ReadFile(string file)
        {
            return File.ReadAllBytes(file);
        }
        public bool CreateDirectory(String ifdoesntexist)
        {
            var di = new DirectoryInfo(ifdoesntexist);
            if (!di.Exists)
            {
                di.Create();
                return true;
            }
            else return false;
        }
    }
    
    class Platform : IPlatform, ITasks
    {
        public Platform()
        {
        }

        public PropertyInfo GetPropertyInfo(Type t, String p) { return t.GetProperty(p); }
        public MethodInfo GetMethodInfo(Type t, String m) { return t.GetMethod(m); }
        readonly FileSystem FF = new FileSystem();
        public IFSOps filesystem { get { return FF; } }
        static int uiThread;
        Action<String, Action> showError = (ex, a) => a();
        public void Attach(Action<String, Action> showError)
        {
            this.showError = showError;

#if DEBUG
            TaskScheduler.UnobservedTaskException += (a, b) =>
            {
                showError(b.Exception.ToString(), Android.App.Application.Context.MainLooper.QuitSafely); // maye?
            };
#endif
        }
        #region ITasks implementation
        public long CurrentThreadID { get { return Thread.CurrentThread.ManagedThreadId; } }
        void EHandle(Exception e)
        {
#if !DEBUG
            //ensure app dies
            Device.BeginInvokeOnMainThread(() =>
            {
                throw e;
            });
#else
            Debugger.Break();
#endif
        }
        public Task RunTask(Func<Task> asyncMethod)
        {
            return Task.Run(async () => 
            {
                try { await asyncMethod(); }
                catch (Exception e) { EHandle(e); throw; }
            });
        }
        public Task RunTask(Action syncMethod)
        {
            return Task.Run(() =>
            {
                try { syncMethod(); }
                catch (Exception e) { EHandle(e); throw; }
            });
        }
        public Task<T> RunTask<T>(Func<Task<T>> asyncMethod)
        {
            return Task.Run(async () =>
            {
                try { return await asyncMethod(); }
                catch (Exception e) { EHandle(e); throw; }
            });
        }
        public Task<T> RunTask<T>(Func<T> syncMethod)
        {
            return Task.Run(() =>
            {
                try { return syncMethod(); }
                catch (Exception e) { EHandle(e); throw; }
            });
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

        public bool IsMainContext => Looper.MainLooper.Thread == Java.Lang.Thread.CurrentThread();
#endregion
    }
}