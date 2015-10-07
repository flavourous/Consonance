using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
	public interface IReflectedHelpy<InInfo,OutInfo,QuantityIn,QuantityOut>
		where  InInfo : BaseInfo, new()
		where  OutInfo : BaseInfo, new()
	{
		//Textual
		String name {get;}
		String typename {get;}
		String trackedname {get;}

		// instance
		String[] instanceFields {get;} // for create/edit/andmemebernames
		SimpleTargetVal[] Calcluate(double[] fieldValues); 

		// Entries and infos
		IReflectedHelpyQuants<InInfo,QuantityIn> input { get; }
		IReflectedHelpyQuants<OutInfo,QuantityOut> output { get; }
	}
	public class SimpleTargetVal
	{
		public readonly int dayspan;
		public readonly double target;
		public SimpleTargetVal(int dayspan, double target)
		{
			this.dayspan = dayspan;
			this.target = target;
		}
	}
	public static class SerialiserFactory
	{
		static BinaryFormatter bf = new BinaryFormatter ();
		public static byte[] Serialize(Object graph)
		{
			MemoryStream ms = new MemoryStream ();
			bf.Serialize (ms, graph);
			return ms.ToArray ();
		}
		public static T Deserialize<T>(byte[] data)
		{
			return (T)bf.Deserialize (new MemoryStream (data));
		}
	}
	public class SimplyTrackedInst : TrackerInstance
	{
		public byte[] blob_daypattern { get; set; }
		public byte[] blob_targets { get; set; }
		// sqlite doesnt understand arrays - have to blob this.
		[SQLite.Ignore]
		public int[] daypattern { 
			get { return SerialiserFactory.Deserialize<int[]> (blob_daypattern); } 
			set { blob_daypattern = SerialiserFactory.Serialize (value);}
		}
		[SQLite.Ignore]
		public double[] targets {
			get { return SerialiserFactory.Deserialize<double[]> (blob_targets); } 
			set { blob_targets = SerialiserFactory.Serialize (value); }
		}
	}
	public interface IReflectedHelpyQuants<I,MT>
	{
		// these expect type double
		String trackedMember { get; }
		String quantifier { get; } // this is the field that defines the quantity eg grams or minutes
		// these expect double? cause is optional data.
		String[] calculation { get; } // these are the fields we want to take from/store to the info
		double Calcluate (MT amount, double[] values); // obvs
		Func<String, IValueRequest<MT>> FindRequestor(IValueRequestFactory fact);
		MT Default { get; }
		MT InfoFixedQuantity { get; }
		String Convert (MT quant);
		Expression<Func<I, bool>> InfoComplete { get; }
	}
	public class SimpleTrackyHelpy<Inst, In, InInfo, Out, OutInfo, MIn, MOut> : ITrackModel<Inst, In, InInfo, Out, OutInfo>
		where    Inst : SimplyTrackedInst, new()
		where      In : BaseEntry, new()
		where  InInfo : BaseInfo, new()
		where     Out : BaseEntry, new()
		where OutInfo : BaseInfo, new()
	{
		readonly HelpedCreation<In,InInfo,MIn> inc;
		readonly HelpedCreation<Out,OutInfo,MOut> ouc;
		readonly IReflectedHelpy<InInfo,OutInfo, MIn,MOut> helpy; 
		readonly String name;
		readonly IReadOnlyList<Flecter<Inst, double>> trackflectors; 
		readonly IReadOnlyList<RequestStorageHelper<double>> flectyRequests;
		readonly DefaultTrackerInstanceRequests defaultTrackerStuff;
		public SimpleTrackyHelpy(IReflectedHelpy<InInfo,OutInfo, MIn,MOut> helpy) 
		{
			this.name = helpy.name;
			defaultTrackerStuff = new DefaultTrackerInstanceRequests (helpy.typename);
			this.helpy = helpy; 
			inc = new HelpedCreation<In, InInfo, MIn> (helpy.input);
			ouc = new HelpedCreation<Out, OutInfo, MOut> (helpy.output);
			List<Flecter<Inst,double>> fls = new List<Flecter<Inst,double>> ();
			List<RequestStorageHelper<double>> rqs = new List<RequestStorageHelper<double>> ();
			foreach (var tf in helpy.instanceFields) {
				fls.Add (new Flecter<Inst,double> (tf));
				rqs.Add (new NameyStorage<double> (tf,()=>0.0, Validate));
			}
			this.trackflectors = fls;
			this.flectyRequests = rqs;
		}
		void Validate()
		{
			foreach (var r in flectyRequests)
				r.request.valid = true;
		}
		#region ITrackModel implementation
		public IEntryCreation<In, InInfo> increator {get{ return inc; }}
		public IEntryCreation<Out, OutInfo> outcreator { get { return ouc; }}
		public IEnumerable<GetValuesPage> CreationPages (IValueRequestFactory factory)
		{
			var rqs = new BindingList<object> ();
			for (int i = 0; i < trackflectors.Count; i++)
				rqs.Add (flectyRequests [i].CGet (factory.DoubleRequestor));
			defaultTrackerStuff.PushInDefaults (null, rqs, factory);
			yield return new GetValuesPage (name, rqs);
		}
		public IEnumerable<GetValuesPage> EditPages (Inst editing, IValueRequestFactory factory)
		{
			var rqs = new BindingList<object> ();
			for (int i = 0; i < trackflectors.Count; i++) {
				rqs.Add (flectyRequests [i].CGet (factory.DoubleRequestor));
				flectyRequests [i].request.value = trackflectors [i].Get (editing);
			}
			defaultTrackerStuff.PushInDefaults (editing, rqs, factory);
			yield return new GetValuesPage (name, rqs);
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
			List<int> dp = new List<int> ();
			List<double> tg = new List<double> ();
			foreach (var tpat in helpy.Calcluate (vals.ToArray ())) {
				dp.Add (tpat.dayspan);
				tg.Add (tpat.target);
			}
			toEdit.daypattern = dp.ToArray ();
			toEdit.targets = tg.ToArray ();
		}
		#endregion
	}
	class Flecter<T,FT> where T : class
	{
		readonly PropertyInfo pi;
		public String name { get { return pi.Name; } }
		public Flecter(String name)
		{
			pi = typeof(T).GetProperty (name);
		}
		public FT Get(T offof)
		{
			return (FT)pi.GetValue (offof);
		}
		public void Set(T onto, FT value)
		{
			pi.SetValue (onto, value);
		}
	}
	class NameyStorage<T> : RequestStorageHelper<T>
	{
		public static String VariableMutation (String varName)
		{
			return varName.Replace ("_", " ");
		}
		public NameyStorage(String name, Func<T> dval, Action Validate) : base(VariableMutation(name), dval, Validate)
		{
		}
		public NameyStorage(String name, String perQuant, Func<T> dval, Action Validate) : base(VariableMutation(name) +perQuant, dval, Validate)
		{
		}
	}
	class HelpedCreation<E,I,MT> : IEntryCreation<E,I>
		where E : BaseEntry, new()
		where I : BaseInfo, new()
	{
		// for no-info requests
		readonly RequestStorageHelper<double> trackedQuantity;
		readonly Flecter<E, double> trackedQuantityFlecter;
		// for info requests
		readonly Flecter<I, MT> measureQuantityFlecter;
		readonly RequestStorageHelper<MT> measureQuantity;
		readonly IReadOnlyList<RequestStorageHelper<double>> forMissingInfoQuantities; // pull these as needed
		readonly IReadOnlyList<Flecter<I, double?>> requiredInfoFlecters;
		readonly IReflectedHelpyQuants<I,MT> quant;
		readonly DefaultEntryRequests defaulter = new DefaultEntryRequests ();
		RequestStorageHelper<String> infoNameRequest;
		public HelpedCreation(IReflectedHelpyQuants<I,MT> quant)
		{
			this.IsInfoComplete = quant.InfoComplete;
			this.quant = quant;
			// we need a direct request for no info creation - and posssssibly one of each for the ones on info that might not exist.
			// also need one for info quantity.
			trackedQuantity = new NameyStorage<double>(quant.trackedMember,()=>0.0,() => trackedQuantity.request.valid = true); // it's same on tinfo and entries
			trackedQuantityFlecter = new Flecter<E,double>(quant.trackedMember);
			measureQuantityFlecter = new Flecter<I,MT> (quant.quantifier);
			measureQuantity = new NameyStorage<MT>(quant.quantifier, () => quant.Default, ()=>measureQuantity.request.valid=true);
			var l = new List<RequestStorageHelper<double>> ();
			var f = new List<Flecter<I,double?>> ();
			foreach (var q in quant.calculation) {
				NameyStorage<double> ns = null;
				ns = new NameyStorage<double> (q, " per " + quant.Convert (quant.InfoFixedQuantity), () => 0.0, () => ns.request.valid = true);
				l.Add (ns);
				f.Add (new Flecter<I,double?> (q));
			}
			forMissingInfoQuantities = l;
			requiredInfoFlecters = f;
			infoNameRequest = new RequestStorageHelper<string> ("Name", () => "", () => infoNameRequest.request.valid = true);
		}
		#region IEntryCreation implementation

		public void ResetRequests ()
		{
			trackedQuantity.Reset ();
			measureQuantity.Reset ();
			foreach (var mi in forMissingInfoQuantities)
				mi.Reset ();
			defaulter.ResetRequests ();
		}

		public BindingList<object> CreationFields (IValueRequestFactory factory)
		{
			// we just need the amount...we can get the name and when etc from a defaulter...
			var rp = new BindingList<object> () { trackedQuantity.CGet(factory.DoubleRequestor) };
			defaulter.PushInDefaults (null, rp, factory);
			return rp;
		}

		public E Create ()
		{
			var rv = new E ();
			trackedQuantityFlecter.Set (rv, trackedQuantity);
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
				var getit = forMissingInfoQuantities [i].CGet (factory.DoubleRequestor); // make sure cgot it.
				var ival = requiredInfoFlecters [i].Get (info);
				if (ival.HasValue)
					forMissingInfoQuantities [i].request.value = ival.Value; // ok got it, set requestor for easy sames
				else
					requestoutput.Add (getit); // ash need this so adddyyy.
			}
		}

		public E Calculate (I info, bool shouldComplete)
		{
			var rv = new E ();
			var amount = measureQuantity.request.value;
			List<double> vals = new List<double> ();
			foreach (var tv in forMissingInfoQuantities) 
				vals.Add (tv);
			double res = quant.Calcluate (amount, vals.ToArray ());
			trackedQuantityFlecter.Set(rv, res);
			defaulter.Set (rv);
			return rv;
		}

		public BindingList<object> EditFields (E toEdit, IValueRequestFactory factory)
		{
			// request objects
			var rp = new BindingList<object> () { trackedQuantity.CGet(factory.DoubleRequestor) };
			defaulter.PushInDefaults (toEdit, rp, factory);

			// init request objects - defaulter did both
			trackedQuantity.request.value = trackedQuantityFlecter.Get(toEdit);

			// give it back
			return rp;
		}

		public E Edit (E toEdit)
		{
			// commit edit...simple one
			defaulter.Set(toEdit);
			trackedQuantityFlecter.Set (toEdit, trackedQuantity);
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
			trackedQuantityFlecter.Set (toEdit, res);
			return toEdit;
		}

		#endregion

		#region IInfoCreation implementation	
		public BindingList<object> InfoFields (IValueRequestFactory factory)
		{
			var rv = new BindingList<Object> () { infoNameRequest.CGet (factory.StringRequestor) };
			foreach (var iv in forMissingInfoQuantities)
				rv.Add (iv.CGet (factory.DoubleRequestor));
			return rv;
		}

		public void FillRequestData (I item)
		{
			// put the data in item into the requests please.
			for (int i = 0; i < forMissingInfoQuantities.Count; i++) 
				forMissingInfoQuantities [i].request.value = requiredInfoFlecters [i].Get (item).Value;
			infoNameRequest.request.value = item.name;
		}

		public I MakeInfo (I toEdit = default(I))
		{
			// yeah...but a default for a reference type is null...
			var ret = toEdit ?? new I();
			ret.name = infoNameRequest;
			measureQuantityFlecter.Set (ret, quant.InfoFixedQuantity);
			for (int i = 0; i < forMissingInfoQuantities.Count; i++)
				requiredInfoFlecters [i].Set (ret, forMissingInfoQuantities [i]);
			return ret;
		}

		public Expression<Func<I, bool>> IsInfoComplete { get; private set; }

		#endregion
	}

	public class SimpleTrackyHelpyPresenter<Inst, In, InInfo, Out, OutInfo, MIn, MOut> : ITrackerPresenter<Inst, In, InInfo, Out, OutInfo>
		where    Inst : SimplyTrackedInst, new()
		where      In : BaseEntry, new()
		where  InInfo : BaseInfo, new()
		where     Out : BaseEntry, new()
		where OutInfo : BaseInfo, new()
	{
		public TrackerDetailsVM details { get; private set; }
		public TrackerDialect dialect { get; private set; }
		readonly IReflectedHelpy<InInfo,OutInfo,MIn,MOut> helpy;

		Flecter<InInfo, double> InQuant;
		Flecter<OutInfo, double> OutQuant;
		Flecter<In, double> InTrack;
		Flecter<Out, double> OutTrack;
		readonly String inQuantUnits, outQuantUnits;
		public SimpleTrackyHelpyPresenter(TrackerDetailsVM details, TrackerDialect dialect, IReflectedHelpy<InInfo,OutInfo,MIn,MOut> helpy)
		{
			this.helpy = helpy;
			this.details = details;
			this.dialect = dialect;

			InQuant = new Flecter<InInfo, double> (helpy.input.quantifier);
			OutQuant = new Flecter<OutInfo, double> (helpy.output.quantifier);
			InTrack = new Flecter<In, double> (helpy.input.trackedMember);
			OutTrack = new Flecter<Out, double> (helpy.output.trackedMember);


			inQuantUnits = NameyStorage<EventArgs>.VariableMutation (helpy.input.quantifier);
			outQuantUnits = NameyStorage<EventArgs>.VariableMutation (helpy.output.quantifier);
		}

		#region IDietPresenter implementation
		String QuantyGet(double amt, String unit, String name)
		{
			return amt.ToString ("F1") + " " + unit + " of " + name;
		}
		public EntryLineVM GetRepresentation (In entry, InInfo info)
		{
			return new EntryLineVM (
				entry.entryWhen,
				TimeSpan.Zero,
				entry.entryName, 
				info == null ? "" : QuantyGet (InQuant.Get (info), inQuantUnits, info.name), 
				new KVPList<string, double> { { helpy.trackedname, InTrack.Get (entry) } }
			);
		}
		public EntryLineVM GetRepresentation (Out entry, OutInfo info)
		{
			return new EntryLineVM (
				entry.entryWhen,
				TimeSpan.Zero,
				entry.entryName, 
				info == null ? "" : QuantyGet (OutQuant.Get (info), outQuantUnits, info.name), 
				new KVPList<string, double> { { helpy.trackedname, OutTrack.Get (entry) } }
			);
		}
		public TrackerInstanceVM GetRepresentation (Inst entry)
		{
			var kl = new KVPList<string, double> ();
			for(int i=0;i<entry.daypattern.Length;i++) 
				kl.Add(
					helpy.trackedname + " over " + entry.daypattern[i] + " day" + (entry.daypattern[i] > 1 ? "s" : ""),
					entry.targets[i]
				);
			return new TrackerInstanceVM(
				dialect,
				entry.started, 
				entry.hasEnded,
				entry.ended,
				entry.name,
				"",
				kl
			);
		}
		public InfoLineVM GetRepresentation (InInfo info)
		{
			return new InfoLineVM () { name = info.name };
		}
		public InfoLineVM GetRepresentation (OutInfo info)
		{
			return new InfoLineVM () { name = info.name };
		}


		public IEnumerable<TrackingInfoVM> DetermineInTrackingForRange(Inst di, IEnumerable<In> inEntries, IEnumerable<Out> outEntries, DateTime startBound,  DateTime endBound)
		{
			// We will pull one here.  requests will span days at least, and in our helpy model here in this space, we specify day based patterns.
			// Lets just figure out which part of the pattern we are on.
			TrackingInfoVM ti = new TrackingInfoVM () {
				valueName = helpy.trackedname + " balance",
				//targetValue= InstTrack.Get(di)
			};

			// Hmm, index exception here surely? I think it works.
			var daysSinceStart = (DateTime.Now - di.started).TotalDays;
			int totalPatternLength = 0;
			foreach (var dp in di.daypattern)
				totalPatternLength += dp;
			var daysSincePatternStarted = daysSinceStart % totalPatternLength;
			int day_sum = 0, day_index;
			for (day_index = 0; day_sum + di.daypattern [day_index] < daysSincePatternStarted; day_index++)
				day_sum += di.daypattern [day_index];
			ti.targetValue = di.targets [day_index];

			double tot = 0.0;
			List<TrackingElementVM> vin = new List<TrackingElementVM> (), vout = new List<TrackingElementVM> ();
			foreach (var ient in inEntries) {
				var v = InTrack.Get (ient);
				vin.Add (new TrackingElementVM () { value = v , name = ient.entryName });
				tot += v;
			}
			foreach (var oent in outEntries) {
				var v = OutTrack.Get (oent);
				vout.Add (new TrackingElementVM () { value = v, name = oent.entryName });
				tot -= v;
			}

			ti.inValues = vin.ToArray ();
			ti.outValues = vout.ToArray ();
			yield return ti;
		}

		public IEnumerable<TrackingInfoVM> DetermineOutTrackingForRange(Inst di, IEnumerable<In> eats, IEnumerable<Out> burns, DateTime startBound,  DateTime endBound)
		{
			return DetermineInTrackingForRange (di, eats, burns, startBound, endBound);
		}
		#endregion
	}

}