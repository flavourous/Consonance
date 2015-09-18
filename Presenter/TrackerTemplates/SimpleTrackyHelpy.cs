using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Consonance
{
	// & how to get it from entrymodels = use reflection provide string.
	// Alright, from the top, what would you ideally wanna specify? for a simple situation...
	//  - unit of measure to track (e.g. points) and it's probabbly gotta be a double. 
	//  - unit of amountage for input (e.g. grams) 
	//  - unit of amountage for output (e.g. minutes) 
	//  - list of items needed from respective infos in order to calc (also helps create infos)
	//  - expression involving those units to creeate entries (creating without an info is simple)
	//  - how to determine target (for creation etc)

	/// <summary>
	/// I helpy.  strings are used for reflection 
	/// </summary>
	public interface IReflectedHelpy<MIn,MOut>
	{
		// all
		String trackedMember { get; } // for simples same one across tracker and entries.

		// instance
		String[] instanceFields {get;} // for create/edit/andmemebernames
		double Calcluate(double[] fieldValues); 

		// Entries and infos
		IReflectedHelpyQuants<MIn> input { get; }
		IReflectedHelpyQuants<MOut> output { get; }
	}
	public interface IReflectedHelpyQuants<MT>
	{
		// these expect type double
		String quantifier { get; } // this is the field that defines the quantity eg grams or minutes
		// these expect double? cause is optional data.
		String[] calculation { get; } // these are the fields we want to take from/store to the info
		double Calcluate (MT amount, double[] values); // obvs
		Func<String, IValueRequest<MT>> FindRequestor(IValueRequestFactory fact);
		MT GetDefault();
	}
	public class SimpleTrackyHelpy<Inst, In, InInfo, Out, OutInfo, MIn, MOut> : ITrackModel<Inst, In, InInfo, Out, OutInfo>
		where    Inst : TrackerInstance, new()
		where      In : BaseEntry, new()
		where  InInfo : BaseInfo, new()
		where     Out : BaseEntry, new()
		where OutInfo : BaseInfo, new()
	{
		readonly HelpedCreation<In,InInfo,MIn> inc;
		readonly HelpedCreation<Out,OutInfo,MOut> ouc;
		readonly IReflectedHelpy<MIn,MOut> helpy; 
		readonly String name;
		readonly IReadOnlyList<Flecter<Inst>> trackflectors; 
		readonly Flecter<Inst> trackTargetFlector;
		readonly IReadOnlyList<RequestStorageHelper<double>> flectyRequests;
		readonly DefaultTrackerInstanceRequests defaultTrackerStuff;
		public SimpleTrackyHelpy(String name, String typename, IReflectedHelpy<MIn,MOut> helpy) 
		{
			this.name = name;
			defaultTrackerStuff = new DefaultTrackerInstanceRequests (typename);
			this.helpy = helpy; 
			inc = new HelpedCreation<In, InInfo, MIn> (helpy.trackedMember, helpy.input);
			ouc = new HelpedCreation<Out, OutInfo, MOut> (helpy.trackedMember, helpy.output);
			List<Flecter<Inst>> fls = new List<Flecter<Inst>> ();
			List<RequestStorageHelper<double>> rqs = new List<RequestStorageHelper<double>> ();
			foreach (var tf in helpy.instanceFields) {
				fls.Add (new Flecter<Inst> (tf));
				rqs.Add (new NameyStorage<double> (tf,()=>0.0, Validate));
			}
			this.trackflectors = fls;
			this.flectyRequests = rqs;
			this.trackTargetFlector = new Flecter<Inst> (helpy.trackedMember);
		}
		void Validate()
		{
			foreach (var r in flectyRequests)
				r.request.valid = true;
		}
		#region ITrackModel implementation
		public IEntryCreation<In, InInfo> increator {get{ return inc; }}
		public IEntryCreation<Out, OutInfo> outcreator { get { return ouc; }}
		public IEnumerable<TrackerWizardPage> CreationPages (IValueRequestFactory factory)
		{
			var rqs = new BindingList<object> ();
			for (int i = 0; i < trackflectors.Count; i++)
				rqs.Add (flectyRequests [i].CGet (factory.DoubleRequestor));
			defaultTrackerStuff.PushInDefaults (null, rqs, factory);
			yield return new TrackerWizardPage (name, rqs);
		}
		public IEnumerable<TrackerWizardPage> EditPages (Inst editing, IValueRequestFactory factory)
		{
			var rqs = new BindingList<object> ();
			for (int i = 0; i < trackflectors.Count; i++) {
				rqs.Add (flectyRequests [i].CGet (factory.DoubleRequestor));
				flectyRequests [i].request.value = trackflectors [i].GetDouble (editing);
			}
			defaultTrackerStuff.PushInDefaults (editing, rqs, factory);
			yield return new TrackerWizardPage (name, rqs);
		}
		public Inst New ()
		{
			var ti = new Inst ();
			Edit (ti);
			return ti;
		}
		public void Edit (Inst toEdit)
		{
			defaultTrackerStuff.Set (toEdit);
			List<double> vals = new List<double> ();
			foreach(var fr in flectyRequests) vals.Add (fr);
			trackTargetFlector.SetDouble (toEdit, helpy.Calcluate (vals.ToArray ()));
		}
		#endregion
	}
	class Flecter<T> where T : class
	{
		readonly PropertyInfo pi;
		public String name { get { return pi.Name; } }
		public Flecter(String name)
		{
			pi = typeof(T).GetProperty (name);
		}
		public double? GetNDouble(T offof)
		{
			return (double?)pi.GetValue (offof);
		}
		public double GetDouble(T offof)
		{
			return (double)pi.GetValue (offof);
		}
		public void SetDouble(T onto, double value)
		{
			pi.SetValue (onto, value);
		}
	}
	class NameyStorage<T> : RequestStorageHelper<T>
	{
		public NameyStorage(String name, Func<T> dval, Action Validate) : base(name.Replace("_"," "), dval, Validate)
		{
		}
	}
	class HelpedCreation<E,I,MT> : IEntryCreation<E,I>
		where E : BaseEntry, new()
		where I : BaseInfo, new()
	{
		// for no-info requests
		readonly RequestStorageHelper<double> trackedQuantity;
		readonly Flecter<E> trackedQuantityFlecter;
		// for info requests
		readonly RequestStorageHelper<MT> measureQuantity;
		readonly IReadOnlyList<RequestStorageHelper<double>> forMissingInfoQuantities; // pull these as needed
		readonly IReadOnlyList<Flecter<I>> requiredInfoFlecters;
		readonly IReflectedHelpyQuants<MT> quant;
		readonly DefaultEntryRequests defaulter = new DefaultEntryRequests ();
		public HelpedCreation(String trackedQuantityName, IReflectedHelpyQuants<MT> quant)
		{
			this.quant = quant;
			// we need a direct request for no info creation - and posssssibly one of each for the ones on info that might not exist.
			// also need one for info quantity.
			trackedQuantity = new NameyStorage<double>(trackedQuantityName,()=>0.0,Validate); // it's same on tinfo and entries
			trackedQuantityFlecter = new Flecter<E>(trackedQuantityName);
			measureQuantity = new NameyStorage<MT>(quant.quantifier, quant.GetDefault, Validate);
			List<RequestStorageHelper<double>> l = new List<RequestStorageHelper<double>> ();
			List<Flecter<I>> f = new List<Flecter<I>> ();
			foreach (var q in quant.calculation) {
				l.Add (new NameyStorage<double> (q,()=>0.0,Validate));
				f.Add (new Flecter<I> (q));
			}
			forMissingInfoQuantities = l;
			requiredInfoFlecters = f;
		}
		void Validate()
		{
			foreach (var rq in forMissingInfoQuantities)
				rq.request.valid = true;
			measureQuantity.request.valid = true;
		}
		#region IEntryCreation implementation

		public void ResetRequests ()
		{
			trackedQuantity.Reset ();
			measureQuantity.Reset ();
			foreach (var mi in forMissingInfoQuantities)
				mi.Reset ();
		}

		public BindingList<object> CreationFields (IValueRequestFactory factory)
		{
			// we just need the amount...we can get the name and when etc from a defaulter...
			var rp = new BindingList<object> () { trackedQuantity };
			defaulter.PushInDefaults (null, rp, factory);
			return rp;
		}

		public E Create ()
		{
			var rv = new E ();
			trackedQuantityFlecter.SetDouble (rv, trackedQuantity);
			defaulter.Set (rv);
			return rv;
		}

		public BindingList<object> CalculationFields (IValueRequestFactory factory, I info)
		{
			var blo = new BindingList<Object> () { measureQuantity.CGet(quant.FindRequestor(factory)) };
			ProcessRequestsForInfo (blo, factory, info);
			defaulter.PushInDefaults (null, blo, factory);
			return blo;
		}

		// I know these will change when info changes...but...the buisness modelling will re-call us in those cases.
		void ProcessRequestsForInfo(BindingList<Object> requestoutput, IValueRequestFactory factory, I info)
		{
			for (int i = 0; i < forMissingInfoQuantities.Count; i++) {
				var ival = requiredInfoFlecters [i].GetNDouble (info);
				if (ival.HasValue)
					forMissingInfoQuantities [i].request.value = ival.Value; // ok got it, set requestor for easy sames
				else
					requestoutput.Add (forMissingInfoQuantities [i].CGet (factory.DoubleRequestor)); // ash need this so adddyyy.
			}
		}

		public E Calculate (I info, bool shouldComplete)
		{
			var rv = new E ();
			var amount = measureQuantity.request.value;
			List<double> vals = new List<double> ();
			foreach (var tv in forMissingInfoQuantities) vals.Add (tv);
			double res = quant.Calcluate (amount, vals.ToArray ());
			trackedQuantityFlecter.SetDouble (rv, res);
			return rv;
		}

		public BindingList<object> EditFields (E toEdit, IValueRequestFactory factory)
		{
			// request objects
			var rp = new BindingList<object> () { trackedQuantity };
			defaulter.PushInDefaults (toEdit, rp, factory);

			// init request objects - defaulter did both
			trackedQuantity.request.value = trackedQuantityFlecter.GetDouble(toEdit);

			// give it back
			return rp;
		}

		public E Edit (E toEdit)
		{
			// commit edit...simple one
			defaulter.Set(toEdit);
			trackedQuantityFlecter.SetDouble (toEdit, trackedQuantity);
			return toEdit;
		}

		public BindingList<object> EditFields (E toEdit, IValueRequestFactory factory, I info)
		{
			// requests for editing a calced one....hmmouch?
			var blo = new BindingList<Object> () { measureQuantity.CGet(quant.FindRequestor(factory)) };
			ProcessRequestsForInfo (blo, factory, info); // initially, no, we'll add none...but maybe subsequently.
			defaulter.PushInDefaults (toEdit, blo, factory);
			return blo;
		}

		public E Edit (E toEdit, I info, bool shouldComplete)
		{
			defaulter.Set (toEdit);
			var amount = measureQuantity.request.value;
			List<double> vals = new List<double> ();
			foreach (var tv in forMissingInfoQuantities) vals.Add (tv);
			double res = quant.Calcluate (amount, vals.ToArray ());
			trackedQuantityFlecter.SetDouble (toEdit, res);
			return toEdit;
		}

		#endregion

		#region IInfoCreation implementation

		public BindingList<object> InfoFields (IValueRequestFactory factory)
		{
			throw new NotImplementedException ();
		}

		public void FillRequestData (I item)
		{
			throw new NotImplementedException ();
		}

		public I MakeInfo (I toEdit = default(I))
		{
			throw new NotImplementedException ();
		}

		public Expression<Func<I, bool>> IsInfoComplete {
			get {
				throw new NotImplementedException ();
			}
		}

		#endregion
	}
}