using System;
using Consonance;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using Consonance.Invention;

namespace Consonance.ConsoleView
{
	class CView : IView, ICollectionEditorSelectableLooseCommands<TrackerInstanceVM>, IConsolePage
	{
        enum LoadThings {  BurnItems, EatItems, Instances };
		Dictionary<LoadThings,bool> aloads = new Dictionary<LoadThings, bool> {
			{ LoadThings.BurnItems, false },
			{ LoadThings.EatItems, false },
			{ LoadThings.Instances, false }
		};
		void SetLoadingState (LoadThings thing, bool active)
		{
			aloads [thing] = active;
			pageChanged = true;
		}

		public bool allowDefaultActions { get { return true; } }
		readonly CPlanCommands pc;
		public CView(CPlanCommands pc)
		{
			this.pc = pc;
		}
		#region IConsolePage implementation
		StringBuilder pdb = new StringBuilder ();
		public bool pageChanged { get; set; }
		public string pageData 
		{ 
			get {
				pdb.Clear ();
				pdb.AppendFormat ("Main Page\n=================================\n");
				pdb.AppendFormat ("Loading: In({0}), Out({1}), Plan({2})\n=================================\n", aloads [LoadThings.EatItems], aloads [LoadThings.BurnItems], aloads [LoadThings.Instances]);
				pdb.AppendFormat ("Date: {0}\n\n", day.ToShortDateString ());

				int maxRows = Math.Min (10, ManyMax (inlines.Count, outlines.Count, instances.Count));
				int colWid = 20;
				if (currentTrackerInstance != null)
					pdb.Append (RowString (colWid, "Index", currentTrackerInstance.dialect.InputEntryVerb, currentTrackerInstance.dialect.OutputEntryVerb, "Plan"));
				else
					pdb.Append (RowString (colWid, "Index", "In", "Out", "Plan"));
				pdb.AppendLine ();
				pdb.AppendLine (RowString (colWid, new String ('=', colWid + 4), new String ('=', colWid + 4), new String ('=', colWid + 4), new String ('=', colWid + 4)));
				int i = 0;
				for (; i < maxRows; i++) {
					pdb.Append (RowString (colWid, i.ToString (),
						i < inlines.Count ? inlines [i].name : "", 
						i < outlines.Count ? outlines [i].name : "", 
						i < instances.Count ? (OriginatorVM.OriginatorEquals (instances [i], currentTrackerInstance) ? "X " : "") + instances [i].name : ""));
					pdb.AppendLine ();
				}
				if (i == 0)
					pdb.AppendLine ("Nothing here!");
				pdb.AppendLine ("\n============================================");
				String pad = " == ";
				if (inTracks.Count == 0)
					pdb.AppendLine ("No input tracking");
				else {	
					pdb.AppendLine (" :: input Screen Tracking :: ");
					foreach (var tt in inTracks) {
						pdb.AppendLine (pad + TTS (tt) + pad);
						pad = "";
					}
				}
				if (inTracks.Count == 0)
					pdb.AppendLine ("No output tracking");
				else 
				{
					pdb.AppendLine (" :: output Screen Tracking :: ");
					pad = " == ";
					foreach (var tt in outTracks) {
						pdb.AppendLine (pad + TTS (tt) + pad);
						pad = "";
					}
				}
				return pdb.ToString ();
			}
		}
		String TTS(TrackerTracksVM tt)
		{
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat ("{0} - ", tt.instanceName);
			foreach (var t in tt.tracks) {
				double bal = (from v in t.inValues select v.value).Sum ();
				bal -= (from v in t.outValues select v.value).Sum ();
				sb.AppendFormat ("{0}: {1}/{2}   ", t.targetValueName, bal, t.targetValue);
			}
			return sb.ToString();
		}
		int ManyMax(params int[] vals)
		{
			int max = int.MinValue;
			foreach (var v in vals)
				if (v > max)
					max = v;
			return max;
		}
		String RowString(int colWid, params String[] vals)
		{
			StringBuilder sbnow = new StringBuilder ();
			for (int i = 0; i < vals.Length; i++) {
				var vn = vals [i];
				int useLen = Math.Min (colWid, vn.Length);
				sbnow.Append (vn.Substring (0, useLen));
				sbnow.Append (new String (' ',colWid - useLen));
				if ((i + 1) < vals.Length) sbnow.Append (" || ");
			}
			return sbnow.ToString ();
		}
		public ConsolePageAction[] pageActions {
			get {
				String inName = currentTrackerInstance == null ? "In" : currentTrackerInstance.dialect.InputEntryVerb;
				String outName = currentTrackerInstance == null ? "Out" : currentTrackerInstance.dialect.OutputEntryVerb;
				return new [] {
					new ConsolePageAction () {
						name = "tracker actions",
						argumentNames = new [] { "mode[a,s,d,e]", "index" },
						action = d => {
							int idx = -1;
							if(d.Length > 1) {
								int.TryParse (d [1], out idx);
								if (idx < 0 || idx >= instances.Count) {
									Console.WriteLine ("Out of range...");
									Console.ReadKey ();
									return;
								}
							}
							switch(d[0])
							{
							case "a": add (); break;
							case "s": select (currentTrackerInstance = instances [idx]); break;
							case "d": remove (instances [idx]); break;
							case "e": edit (instances [idx]); break;
							default:
								Console.WriteLine ("unknown mode...");
								Console.ReadKey ();
								break;
							}
						}
					},
					IOA(inName, inlines, (CPlanCommands.CCollectionEditorBoundCommands<EntryLineVM>)pc.eat),
					IOA(outName, outlines, (CPlanCommands.CCollectionEditorBoundCommands<EntryLineVM>)pc.burn),
					ChangeDay("Prev Day",-1),
					ChangeDay("Next Day",1),
				};
			}
		}
		ConsolePageAction ChangeDay(String name, int dc)
		{
			return new ConsolePageAction () {
				name = name,
				argumentNames = new string[0],
				action = _ => changeday (day.AddDays (dc))
			};
		}
		ConsolePageAction IOA(String name, IReadOnlyList<EntryLineVM> entries, CPlanCommands.CCollectionEditorBoundCommands<EntryLineVM> commands)
		{
			return new ConsolePageAction () {
				name =  name + " actions",
				argumentNames = new [] { "mode[a,d,e]", "index" },
				action = d => {
					int idx = -1;
					if (d.Length > 1 &&int.TryParse (d [1], out idx)&& (idx < 0 || idx >= entries.Count)) {
						Console.WriteLine ("Out of range...");
						Console.ReadKey ();
						return;
					}
					switch(d[0])
					{
					case "a": commands.Add (); break;
					case "d": commands.Remove (entries[idx]); break;
					case "e": commands.Edit (entries[idx]); break;
					default:
						Console.WriteLine ("unknown mode...");
						Console.ReadKey ();
						break;
					}

				}
			};
		}
		#endregion

		#region IView implementation
		// Some events on this view.
		public event Action<DateTime> changeday = delegate { };
		public event Action<InfoManageType> manageInfo = delegate { };
		public ICollectionEditorSelectableLooseCommands<TrackerInstanceVM> plan {  get { return this; } }
		#region ICollectionEditorLooseCommands implementation
		public event Action add = delegate { };
		public event Action<TrackerInstanceVM> remove = delegate { };
		public event Action<TrackerInstanceVM> edit = delegate { };
		public event Action<TrackerInstanceVM> select = delegate { };
		#endregion

		// Data incoming
		IReadOnlyList<EntryLineVM> inlines = new List<EntryLineVM>(), outlines= new List<EntryLineVM>();
		public void SetEatLines (IVMList<EntryLineVM> lineitems) { inlines = new List<EntryLineVM> (lineitems);  pageChanged = true;}
		public void SetBurnLines (IVMList<EntryLineVM> lineitems) { outlines = new List<EntryLineVM> (lineitems);  pageChanged = true;}

        IReadOnlyList<InfoLineVM> ininfos = new List<InfoLineVM>(), outinfos = new List<InfoLineVM>();
        public void SetEatInfos(IVMList<InfoLineVM> lineitems) { ininfos = new List<InfoLineVM>(lineitems); pageChanged = true; }
        public void SetBurnInfos(IVMList<InfoLineVM> lineitems) { outinfos = new List<InfoLineVM>(lineitems); pageChanged = true; }

        IReadOnlyList<TrackerInstanceVM> instances= new List<TrackerInstanceVM>();
		public void SetInstances (IVMList<TrackerInstanceVM> instanceitems) { instances = new List<TrackerInstanceVM> (instanceitems); pageChanged = true; }

		IReadOnlyList<TrackerTracksVM> inTracks = new List<TrackerTracksVM>(), outTracks= new List<TrackerTracksVM>();
		public void SetEatTrack (IVMList<TrackerTracksVM> others) { 
			var its = new List<TrackerTracksVM> (others);
			inTracks = its;
			pageChanged = true;
		}
		public void SetBurnTrack (IVMList<TrackerTracksVM> others) { 
			var its = new List<TrackerTracksVM> (others);
			outTracks = its;
			pageChanged = true;
		}

        public void SetInventions(IVMList<InventedTrackerVM> inventionitems)
        {
            throw new NotImplementedException();
        }

        // Some data members
        DateTime mday;
		public DateTime day { get{ return mday; } set { mday = value; pageChanged = true; } }
		TrackerInstanceVM mcti;
		public TrackerInstanceVM currentTrackerInstance { get{ return mcti; } set { mcti = value; pageChanged = true; } }

        public ICollectionEditorLooseCommands<InventedTrackerVM> invention
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}

