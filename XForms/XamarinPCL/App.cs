﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Consonance;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using Consonance.Protocol;
using LibSharpHelp;
using System.Threading;

namespace Consonance.XamarinFormsView.PCL
{
    public class ViewTask : IInputResponse
    {
        readonly Func<Task> FClose;
        public ViewTask(Task Completed) : this(() => Task.FromResult(0), Task.FromResult(0), Completed) { }
        public ViewTask(Func<Task> Close, Task Opened, Task Completed)
        {
            this.Opened = Opened;
            this.Result = Completed;
            this.FClose = Close;
        }
        public Task Opened { get; private set; }
        public Task Result { get; private set; }
        public Task Close() => FClose();
    }
    public class ViewTask<T> : ViewTask, IInputResponse<T>
    {
        readonly Task<T> TResult;
        public ViewTask(Task<T> Completed) : this(() => Task.FromResult(0), Task.FromResult(0), Completed) { }
        public ViewTask(Func<Task> Close, Task Opened, Task<T> Completed) : base(Close,Opened,Completed)
        {
            this.TResult = Completed;
        }
        Task<T> IInputResponse<T>.Result { get { return TResult; } }
    }

    public static class Extensions
	{
        static object mutex = new object();
        /// <summary>
        /// same spec as PopAsync
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static Task RemoveOrPopAsync(this INavigation me, Page page)
        {
            
            var tea = new TaskCompletionSource<EventArgs>();
            Task moreAwait = Task.FromResult(0);
            // Dont trust
            Action work = () =>
            {
                // not even a litle
                lock (mutex)
                {
                    // multi-context thread? idk, probabbly sgi.
                    if (me.NavigationStack.Contains(page))
                    {
                        if (me.NavigationStack[me.NavigationStack.Count - 1] == page)
                        {
                            Debug.WriteLine("Navigation Extension: Popping {0}", page.Title);
                            me.PopAsync().ContinueWith(d=>
                            {
                                if (d.IsFaulted) tea.SetException(d.Exception);
                                else tea.SetResult(new EventArgs());
                            });
                        }
                        else
                        {
                            Debug.WriteLine("Navigation Extension: Removing {0}", page.Title);
                            me.RemovePage(page);
                            tea.SetResult(new EventArgs());
                        }
                        Debug.WriteLine("Navigation Extension: Ok, done.");
                    }
                }
            };
            Device.BeginInvokeOnMainThread(work);
            return tea.Task;
        }
	}

    
        public class App : Application
    {
        public static Task UIThread(Action work)
        {
            var tea = new TaskCompletionSource<EventArgs>();
            Device.BeginInvokeOnMainThread(() =>
            {
                try { work(); }
                catch(Exception e) { tea.SetException(e); }
                tea.TrySetResult(new EventArgs());
            });
            return tea.Task;
        }

        

        public static IValueRequestBuilder bld;
		readonly ViewWrapper viewWrapper;
		readonly PlanCommandsWrapper planCommandWrapper;
		readonly ValueRequestBuilder defaultBuilder;
		readonly UserInputWrapper userInputWrapper;
        public static IPlatform platform { get; private set; }
		public Task Presentation(IPlatform plat)
		{
            plat.Attach ((err, a) => App.UIThread ErrorDialog.Show (err, MainPage.Navigation, a));
            App.platform = plat;
			return Presenter.PresentTo(viewWrapper, plat, userInputWrapper, planCommandWrapper, defaultBuilder);
		}

        static Dictionary<Page, Queue<Action>> cb = new Dictionary<Page, Queue<Action>>();
        public static void RegisterPoppedCallback(Page page, Action callback)
        {
            if (!cb.ContainsKey(page))
                cb[page] = new Queue<Action>();
            cb[page].Enqueue(callback);
        }

        private void Navigator_Popped(object sender, NavigationEventArgs e)
        {
            if (cb.ContainsKey(e.Page))
            {
                while (cb[e.Page].Count > 0)
                    cb[e.Page].Dequeue()();
                cb.Remove(e.Page);
            }
        }

        public App()
        {
            // some pages.
            var main = new MainTabs();
            var navigator = new NavigationPage(main);

            // The root page of your application
            MainPage = navigator;
            navigator.Popped += Navigator_Popped;

            // Common services DI container
            CommonServices.Attacher Attach_Services;
            CommonServices services = new CommonServices(out Attach_Services);

            // instantiate wrappers
            userInputWrapper = new UserInputWrapper(services);
            bld = defaultBuilder = new ValueRequestBuilder(services);
            planCommandWrapper = new PlanCommandsWrapper(main, services);
            var inventCommandWrapper = new InventionCommandManager();
            viewWrapper = new ViewWrapper(main, services);
            viewWrapper.invention = inventCommandWrapper;

            // Initialise services
            Attach_Services(navigator, userInputWrapper, viewWrapper, defaultBuilder, planCommandWrapper, inventCommandWrapper);

        }

    }

    // In lieu of resolving the coupling of those Wrappers, I'll do this.
    class CommonServices
    {
        UserInputWrapper u;
        ViewWrapper v;
        ValueRequestBuilder def_b;
        PlanCommandsWrapper c;
        InventionCommandManager ic;
        NavigationPage nroot;

        public delegate void Attacher(NavigationPage nroot, UserInputWrapper u, ViewWrapper v, ValueRequestBuilder def_b, PlanCommandsWrapper c, InventionCommandManager ic);
        public CommonServices(out Attacher atch) { atch = Attach; }

        void Attach(NavigationPage nroot, UserInputWrapper u, ViewWrapper v, ValueRequestBuilder def_b, PlanCommandsWrapper c, InventionCommandManager ic)
        {
            this.nroot = nroot;
            this.u = u;
            this.v = v;
            this.def_b = def_b;
            this.c = c;
            this.ic = ic;
        }

        public IAbstractedTracker Current { get { return v.currentTrackerInstance?.sender as IAbstractedTracker; } }
        public IValueRequestBuilder DefaultBuilder { get { return def_b; } }
        public IValueRequestBuilder CreateNewBuilder() { return new ValueRequestBuilder(this); }
        public void AttachToCommander(InfoManageView mv, InfoManageType mt) { c.Attach(mv, mt); }
        public void AttachToCommander(InventionManageView mv) { mv.icm = ic; }
        public INavigation nav { get { return nroot.Navigation; } }
        public Page root { get { return nroot; } }
        public IInputResponse<InfoLineVM> U_InfoView(bool choose, bool manage, InfoManageType mt, InfoLineVM initially_selected)
        {
            return u.InfoView(choose, manage, mt,
                mt == InfoManageType.In ? v.main.viewmodel.InInfos : v.main.viewmodel.OutInfos
                , initially_selected);
        }
        public IInputResponse<EventArgs> Invent()
        {
            return u.ManageInvention(v.main.viewmodel.InventedPlans);
        }
    }
}
