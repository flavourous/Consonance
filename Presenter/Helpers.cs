using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;

namespace Consonance
{
	public interface IObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged,  IList<T> { }
	public class ObservableCollectionProxy<T, ProxyConstraint> : IObservableCollection<T>
		where ProxyConstraint : INotifyCollectionChanged, INotifyPropertyChanged,  IList<T>
	{
		protected readonly ProxyConstraint toProxy;
		public ObservableCollectionProxy(ProxyConstraint toProxy) { this.toProxy = toProxy; }

		// Explicit implimentations with weird overrides
//		int IList.Add (object value) { return IListAdd(value); }
//		public virtual int IListAdd(object value) { return ((IList)toProxy).Add (value); } 
//		bool IList.Contains (object value) { return this.Contains((T)value); }
//		int IList.IndexOf (object value) { return this.IndexOf((T)value); }
//		void IList.Insert (int index, object value) { this.Insert(index, (T)value); }
//		void IList.Remove (object value) { this.Remove((T)value); }
//		void ICollection.CopyTo (Array array, int index) { this.CopyTo((T[])array, index); }
//		Object IList.this [int index] { get { return this[index]; } set { this[index]=(T)value; } }
		IEnumerator IEnumerable.GetEnumerator () { return this.GetEnumerator(); }

		// Impl, virtual for overriding...
		public virtual int IndexOf (T item) { return toProxy.IndexOf(item); }
		public virtual void Insert (int index, T item) { toProxy.Insert(index, item); }
		public virtual void RemoveAt (int index) { ((IList<T>)toProxy).RemoveAt(index); }
		public virtual void Add (T item) { toProxy.Add(item); }
		public virtual void Clear () { ((IList<T>)toProxy).Clear(); }
		public virtual bool Contains (T item) { return toProxy.Contains(item); }
		public virtual void CopyTo (T[] array, int arrayIndex) { toProxy.CopyTo(array, arrayIndex); }
		public virtual bool Remove (T item) { return toProxy.Remove(item); }
		public virtual IEnumerator<T> GetEnumerator () { return toProxy.GetEnumerator(); }

		public virtual bool IsFixedSize { get { return ((IList)toProxy).IsFixedSize; } }
		public virtual object SyncRoot { get { return ((IList)toProxy).SyncRoot; } }
		public virtual bool IsSynchronized { get { return ((IList)toProxy).IsSynchronized; } } 
		public virtual int Count { get { return ((IList<T>)toProxy).Count; } }
		public virtual bool IsReadOnly { get { return ((IList)toProxy).IsReadOnly; } }
		
		public virtual T this [int index] { get { return ((IList<T>)toProxy)[index]; } set { ((IList<T>)toProxy)[index]=value; } }
		public virtual event PropertyChangedEventHandler PropertyChanged { add { ((INotifyPropertyChanged)toProxy).PropertyChanged += value; } remove { ((INotifyPropertyChanged)toProxy).PropertyChanged -= value; } }
		public virtual event NotifyCollectionChangedEventHandler CollectionChanged {  add { toProxy.CollectionChanged += value; } remove { toProxy.CollectionChanged -= value; } } 
	}

	class HookedInfoLines : IDisposable
	{
		readonly InfoManageType imt;
		readonly IAbstractedTracker cdh;
		public readonly ObservableCollectionProxy<InfoLineVM, ObservableCollection<InfoLineVM>> lines;
		public HookedInfoLines(IAbstractedTracker cdh, InfoManageType imt)
		{
			this.imt = imt;
			this.cdh = cdh;
			this.lines = new ObservableCollectionProxy<InfoLineVM, ObservableCollection<InfoLineVM>> (new ObservableCollection<InfoLineVM> ());
			cdh.ViewModelsChanged += Cdh_ViewModelsChanged;;
			PushInLinesAndFire ();
		}

		void Cdh_ViewModelsChanged (IAbstractedTracker sender, DietVMChangeEventArgs args)
		{
			PushInLinesAndFire ();
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			cdh.ViewModelsChanged -= Cdh_ViewModelsChanged;;
		}
		#endregion

		void PushInLinesAndFire()
		{
			lines.Clear ();
			switch (imt) {
			case InfoManageType.In:
				foreach (var ii in cdh.InInfos (false))
					lines.Add (ii);
				break;
			case InfoManageType.Out:
				foreach (var oi in cdh.OutInfos (false))
					lines.Add (oi);
				break;
			}
		}
	}

	public interface IRequestStorageHelper
	{
		void Reset();
		event Action requestChanged;
		bool requestValid {set;}
		Object requestValue { get; set; }
		object CGet (IValueRequestFactory fact, Func<IValueRequestFactory, Func<String, Object>> FindRequestDelegate);
	}
	class DummyValueRequest<T> : IValueRequest<T> 
	{
		#region IValueRequest implementation
		public event Action ValueChanged = delegate { };
		public void ClearListeners () { ValueChanged = delegate { }; }
		public object request { get { return null; } }
		public T value { get; set; }
		public bool enabled { get; set; }
		public bool valid { get; set; }
		public bool read_only { get; set; }
		#endregion

	}
	public class RequestStorageHelper<V> : IRequestStorageHelper
	{
		#region IRequestStorageHelper implementation
		public event Action requestChanged { add { request.ValueChanged += value; } remove{ request.ValueChanged -= value; } }
		public Object requestValue { get { return request.value; } set { request.value = (V)value; } }
		public object CGet (IValueRequestFactory fact, Func<IValueRequestFactory, Func<String, Object>> FindRequestDelegate)
		{
			return CGet ((Func<String, IValueRequest<V>>)FindRequestDelegate (fact));
		}
		public bool requestValid { set { request.valid = value; } }
		#endregion

		public IValueRequest<V> request { get; private set; }
		readonly String name;
		readonly Func<V> defaultValue = () => default(V);
		readonly Action validate;
		public RequestStorageHelper(String requestName, Func<V> defaultValue, Action validate)
		{
			this.validate = validate;
			this.defaultValue = defaultValue;
			name = requestName;
			request = new DummyValueRequest<V> { value = defaultValue() };
		}
		public void Reset()
		{
			request.ClearListeners ();
			request.ValueChanged += validate;
			request.value = defaultValue ();
			validate ();
		}
		// will return cached instance if possible, but will do defaulting if specified and will
		// always call ClearListeners, so that old registrations to the changed event are no longer called.
		public Object CGet(Func<String,IValueRequest<V>> creator)
		{
			// Here, we'd not yet had a factory - but we remember what was done in the dummy.
			if (request is DummyValueRequest<V>) {
				var dum = request as DummyValueRequest<V>;
				request = creator (name);
				request.valid = dum.valid;
				request.value = dum.value;
				request.read_only = dum.read_only;
				request.enabled = dum.read_only;
			}
			Reset ();
			return request.request;
		}
		public static implicit operator V (RequestStorageHelper<V> me)
		{
			return me.request.value;
		}
	}

}

