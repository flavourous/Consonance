using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using System.IO;
using System.Reflection;

namespace Consonance.Test
{
    class TestPlatform : IPlatform
    {
        public TestPlatform(string id)
        {
            filesystem = new FSOps(id);
        }
        class FSOps : IFSOps
        {
            public FSOps(string id)
            {
                AppData = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/TestAppData-" + id + "/";
                foreach (var f in Directory.CreateDirectory(AppData).EnumerateFiles())
                    f.Delete();
            }
            public string AppData { get; }
            public void Delete(string file) => File.Delete(file);
            public byte[] ReadFile(string file) => File.ReadAllBytes(file);
            public bool CreateDirectory(string ifdoesntexist)
            {
                bool ex = Directory.Exists(ifdoesntexist);
                Directory.CreateDirectory(ifdoesntexist);
                return ex;
            }
        }
        public IFSOps filesystem { get; }
        public ISQLitePlatform sqlite { get; } = new SQLitePlatformGeneric();
        class TestTaskOps : ITasks
        {
            Task FromAction(Action a) { a(); return Task.FromResult(true); }
            public Task RunTask(Action syncMethod) => FromAction(syncMethod);
            public Task RunTask(Func<Task> asyncMethod) => FromAction(asyncMethod().Wait);
            public Task<T> RunTask<T>(Func<T> syncMethod) => Task.FromResult(syncMethod());
            public Task<T> RunTask<T>(Func<Task<T>> asyncMethod) => Task.FromResult(asyncMethod().Result);
        }
        public ITasks TaskOps { get; } = new TestTaskOps();
        public void Attach(Action<string, Action> showError)
        {
            // nothing to do
        }
    }
}
