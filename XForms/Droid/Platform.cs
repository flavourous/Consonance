using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using Xamarin.Forms;
using System.Threading;
using SQLite.Net.Interop;
using System.IO;
using System.Reflection.Emit;

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

    class StandardEmission : IEmissionPlatform
    {
        public Type CreateClass(String classname, Type baseclass, String[] propnames, Type[] proptypes)
        {
            TypeBuilder tb = GetTypeBuilder(classname, baseclass);
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            for(int i=0; i< propnames.Length; i++)
                CreateProperty(tb, propnames[i], proptypes[i]);

            Type objectType = tb.CreateType();
            return objectType;
        }

        public bool TypeExists(String typename)
        {
            var moduleBuilder = GetModuleBuilder();
            return moduleBuilder.FindTypes((t, o) => t.Name == typename, null).Length > 0;
        }

        ModuleBuilder GetModuleBuilder()
        {
            var an = new AssemblyName("Consonance.Invention.Emitted");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            return assemblyBuilder.DefineDynamicModule("EmissionPlatform"); // fucks sake VB
        }

        TypeBuilder GetTypeBuilder(String classname, Type baseClass)
        {
            var moduleBuilder = GetModuleBuilder();
            TypeBuilder tb = moduleBuilder.DefineType(classname
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout 
                                , baseClass);
            return tb;
        }

        void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            var modifyProperty = setIl.DefineLabel();
            var exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
    class Platform : IPlatform, ITasks
    {
        public Platform()
        {
            emit = new StandardEmission();
        }

        public PropertyInfo GetPropertyInfo(Type t, String p) { return t.GetProperty(p); }
        public MethodInfo GetMethodInfo(Type t, String m) { return t.GetMethod(m); }
        readonly FileSystem FF = new FileSystem();
        public IFSOps filesystem { get { return FF; } }
        static int uiThread;
        Action<String, Action> showError = (ex, a) => a();
        /// <summary>
        /// WARNING: if you execute something asynchronous in here, any problems are likely to be COMPLETEY lost.
        /// use begininbokeonmainthread, with an async void in that case. You gotta await, wait or continueation that shit. In this context.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
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
        public Task RunTask(Func<Task> asyncMethod)
        { return FailHandler(Task.Run(asyncMethod)); }
        public Task RunTask(Action syncMethod)
        { return FailHandler(Task.Run(syncMethod)); }
        public Task<T> RunTask<T>(Func<Task<T>> asyncMethod)
        { return FailHandler(Task.Run(asyncMethod)) as Task<T>; }
        public Task<T> RunTask<T>(Func<T> syncMethod)
        { return FailHandler(Task.Run(syncMethod)) as Task<T>; }
        Task FailHandler(Task t)
        {
            t.ContinueWith(Failed, TaskContinuationOptions.OnlyOnFaulted);
            return t;
        }
        void Failed(Task t)
        {
            HandleException(t.Exception.InnerException);
        }
        void HandleException(Exception h)
        {
            Debug.WriteLine(h);
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
        public IEmissionPlatform emit { get; private set; }
        #endregion
    }
}