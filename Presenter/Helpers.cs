using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Diagnostics;

namespace Consonance
{
	class HookedInfoLines : IDisposable
	{
		readonly InfoManageType imt;
		readonly IAbstractedTracker cdh;
		public readonly ObservableCollection<InfoLineVM> lines;
		public HookedInfoLines(IAbstractedTracker cdh, InfoManageType imt)
		{
			this.imt = imt;
			this.cdh = cdh;
			this.lines = new ObservableCollection<InfoLineVM> ();
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
		public event Action changed = delegate { };
		public void ClearListeners () { changed = delegate { }; }
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
		public event Action requestChanged { add { request.changed += value; } remove{ request.changed -= value; } }
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
			request.changed += validate;
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

