using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Consonance.Protocol;
using LibRTP;

namespace Consonance
{

    public static class RTPHelp
    {
        public static RecurrsEveryPattern Create(this RecurrsEveryPatternValue @this, DateTime? s, DateTime? e)
        {
            return new RecurrsEveryPattern(@this.PatternFixed, @this.PatternFrequency, (LibRTP.RecurrSpan) @this.PatternType, s, e);
        }
        public static RecurrsOnPattern Create(this RecurrsOnPatternValue @this, DateTime? s, DateTime? e)
        {
            return new RecurrsOnPattern(@this.PatternValues, (LibRTP.RecurrSpan)@this.PatternType, s, e);
        }
    }

    public class DummyFactory : IValueRequestFactory
    {
        public IValueRequest<EventArgs> ActionRequestor(string name) => new DummyValueRequest<EventArgs>();
        public IValueRequest<Barcode> BarcodeRequestor(string name) => new DummyValueRequest<Barcode>();
        public IValueRequest<bool> BoolRequestor(string name) => new DummyValueRequest<bool>();
        public IValueRequest<DateTime> DateRequestor(string name) => new DummyValueRequest<DateTime>();
        public IValueRequest<DateTime> DateTimeRequestor(string name) => new DummyValueRequest<DateTime>();
        public IValueRequest<double> DoubleRequestor(string name) => new DummyValueRequest<double>();
        public IValueRequest<TabularDataRequestValue> GenerateTableRequest() => new DummyValueRequest<TabularDataRequestValue>();
        public IValueRequest<InfoLineVM> InfoLineVMRequestor(string name, InfoManageType imt) => new DummyValueRequest<InfoLineVM>();
        public IValueRequest<int> IntRequestor(string name) => new DummyValueRequest<int>();
        public IValueRequest<MultiRequestOptionValue> IValueRequestOptionGroupRequestor(string name) => new DummyValueRequest<MultiRequestOptionValue>();
        public IValueRequest<DateTime?> nDateRequestor(string name) => new DummyValueRequest<DateTime?>();
        public IValueRequest<OptionGroupValue> OptionGroupRequestor(string name) => new DummyValueRequest<OptionGroupValue>();
        public IValueRequest<RecurrsEveryPatternValue> RecurrEveryRequestor(string name) => new DummyValueRequest<RecurrsEveryPatternValue>();
        public IValueRequest<RecurrsOnPatternValue> RecurrOnRequestor(string name) => new DummyValueRequest<RecurrsOnPatternValue>();
        public IValueRequest<string> StringRequestor(string name) => new DummyValueRequest<string>();
        public IValueRequest<TimeSpan> TimeSpanRequestor(string name) => new DummyValueRequest<TimeSpan>();
    }
    public interface IRequestStorageHelper
	{
		void Reset();
		event Action requestChanged;
		bool requestValid { get; set; }
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
        public bool enabled { get; set; } = true;
		public bool valid { get; set; }
        public bool read_only { get; set; } = false;
		#endregion

	}
	public class RequestStorageHelper<V> : IRequestStorageHelper
	{
		#region IRequestStorageHelper implementation
		public event Action requestChanged { add { request.ValueChanged += value; } remove{ request.ValueChanged -= value; } }
        // the colleasce is cause V might not be nullable.
		public Object requestValue { get { return request.value; } set { request.value = (V)(value ?? defaultValue()); } }
		public object CGet (IValueRequestFactory fact, Func<IValueRequestFactory, Func<String, Object>> FindRequestDelegate)
		{
			return CGet ((Func<String, IValueRequest<V>>)FindRequestDelegate (fact));
		}
		public bool requestValid { get { return request.valid; } set { request.valid = value; } }
		#endregion

		public IValueRequest<V> request { get; private set; }
		readonly String name;
		readonly Func<V> defaultValue = () => default(V);
		readonly Action validate;
        public RequestStorageHelper(String requestName, Func<V,V> defaultValue, Action validate)
        {
            this.validate = validate;
            this.defaultValue = () => defaultValue(request.value);
            name = requestName;
            request = new DummyValueRequest<V> {  };
        }
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
				request.enabled = dum.enabled;
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

