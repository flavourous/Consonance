using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;

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
	public interface IReflectedHelpy
	{
		// all
		String trackedMember { get; } // for simples same one across tracker and entries.

		// instance
		String[] instanceFields {get;} // for create/edit/andmemebernames
		double Calcluate(double[] fieldValues); 

		// Entries and infos
		IReflectedHelpyQuants input { get; }
		IReflectedHelpyQuants output { get; }
	}
	public interface IReflectedHelpyQuants
	{
		// these expect type double
		String quantifier { get; }
		// these expect double? cause is optional data.
		String[] calculation { get; }
		double Calcluate (double[] values);
	}
	public class SimpleTrackyHelpy<Inst, In, InInfo, Out, OutInfo> : ITrackModel<Inst, In, InInfo, Out, OutInfo>
		where    Inst : TrackerInstance
		where      In : BaseEntry
		where  InInfo : BaseInfo
		where     Out : BaseEntry
		where OutInfo : BaseInfo
	{
		readonly HelpedCreation<In,InInfo> inc;
		readonly HelpedCreation<Out,OutInfo> ouc;
		readonly IReflectedHelpy helpy; 
		readonly String name;
		readonly IReadOnlyList<Flecter<double>> trackflectors; 
		readonly Flecter<double> trackTargetFlector;
		readonly IReadOnlyList<RequestStorageHelper<double>> flectyRequests;
		readonly DefaultTrackerInstanceRequests defaultTrackerStuff;
		public SimpleTrackyHelpy(String name, String typename, IReflectedHelpy helpy) 
		{
			this.name = name;
			defaultTrackerStuff = new DefaultTrackerInstanceRequests (typename);
			this.helpy = helpy; 
			inc = new HelpedCreation<In, InInfo> (helpy.input);
			ouc = new HelpedCreation<Out, OutInfo> (helpy.output);
			List<Flecter<double>> fls = new List<Flecter<double>> ();
			List<RequestStorageHelper<double>> rqs = new List<RequestStorageHelper<double>> ();
			foreach (var tf in helpy.trackedMember) {
				fls.Add (new Flecter<double> (tf));
				rqs.Add (new NameyStorage (tf));
			}
			this.trackflectors = fls;
			this.flectyRequests = rqs;
			this.trackTargetFlector = new Flecter<double> (helpy.trackedMember);
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
				flectyRequests [i].request.value = trackflectors [i].GetDouble ();
			}
			defaultTrackerStuff.PushInDefaults (editing, rqs, factory);
			yield return new TrackerWizardPage (name, rqs);
		}
		public Inst New ()
		{
			var ti = new Inst ();
			defaultTrackerStuff.Set (ti);
			List<double> vals = new List<double> (flectyRequests);
			trackTargetFlector.SetDouble (ti, helpy.Calcluate (vals.ToArray ()));
		}
		public void Edit (Inst toEdit)
		{
			defaultTrackerStuff.Set (toEdit);
			List<double> vals = new List<double> (flectyRequests);
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
		public double? GetNDouble(Object offof)
		{
			return (double?)pi.GetValue (offof);
		}
		public double GetDouble(Object offof)
		{
			return (double)pi.GetValue (offof);
		}
		public void SetDouble(Object onto, double value)
		{
			pi.SetValue (onto, value);
		}
	}
	class NameyStorage : RequestStorageHelper<double>
	{
		public NameyStorage(String name) : base(name.Replace("_"," "), () => 0.0, () => true)
		{
		}
	}
	class HelpedCreation<E,I> : IEntryCreation<E,I>
	{
		readonly IReflectedHelpyQuants quant;
		public HelpedCreation(IReflectedHelpyQuants quant)
		{
			this.quant = quant;
		}

		#region IEntryCreation implementation

		public void ResetRequests ()
		{
			throw new NotImplementedException ();
		}

		public System.ComponentModel.BindingList<object> CreationFields (IValueRequestFactory factory)
		{
			throw new NotImplementedException ();
		}

		public E Create ()
		{
			throw new NotImplementedException ();
		}

		public System.ComponentModel.BindingList<object> CalculationFields (IValueRequestFactory factory, I info)
		{
			throw new NotImplementedException ();
		}

		public E Calculate (I info, bool shouldComplete)
		{
			throw new NotImplementedException ();
		}

		public System.ComponentModel.BindingList<object> EditFields (E toEdit, IValueRequestFactory factory)
		{
			throw new NotImplementedException ();
		}

		public E Edit (E toEdit)
		{
			throw new NotImplementedException ();
		}

		public System.ComponentModel.BindingList<object> EditFields (E toEdit, IValueRequestFactory factory, I info)
		{
			throw new NotImplementedException ();
		}

		public E Edit (E toEdit, I info, bool shouldComplete)
		{
			throw new NotImplementedException ();
		}

		#endregion

		#region IInfoCreation implementation

		public System.ComponentModel.BindingList<object> InfoFields (IValueRequestFactory factory)
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

		public System.Linq.Expressions.Expression<Func<I, bool>> IsInfoComplete {
			get {
				throw new NotImplementedException ();
			}
		}

		#endregion
	}
}