using System;
using Consonance;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using Consonance.Invention;
using System.Collections.Specialized;
using Consonance.Protocol;

namespace Consonance.ConsoleView
        
{
    static class ChangeyBlap
    {
        public static Action Blap<T>(this IVMList<T> yo, Action assign, IConsolePage cv)
        {
            NotifyCollectionChangedEventHandler cc = (a, r) =>
            {
                assign();
                cv.pageChanged = true;
            };
            cc(null, null);
            yo.CollectionChanged += cc;
            return () => yo.CollectionChanged -= cc;
        }
    }

    class CView : IView, ICollectionEditorSelectableLooseCommands<TrackerInstanceVM>, IConsolePage
    {
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
				pdb.AppendFormat ("Date: {0}\n\n", day.ToShortDateString ());

				int maxRows = Math.Min (10, ManyMax (inlines.Count, outlines.Count, instances.Count, inventions.Count));
				int colWid = 20;
				if (currentTrackerInstance != null)
					pdb.Append (RowString (colWid, "Index", currentTrackerInstance.dialect.InputEntryVerb, currentTrackerInstance.dialect.OutputEntryVerb, "Plan"));
				else
					pdb.Append (RowString (colWid, "Index", "In", "Out", "Plan", "Inventions"));
				pdb.AppendLine ();
				pdb.AppendLine (RowString (colWid, new String ('=', colWid + 4), new String ('=', colWid + 4), new String ('=', colWid + 4), new String ('=', colWid + 4), new String('=', colWid + 4)));
				int i = 0;
				for (; i < maxRows; i++) {
                    pdb.Append(RowString(colWid, i.ToString(),
                        i < inlines.Count ? inlines[i].name : "",
                        i < outlines.Count ? outlines[i].name : "",
                        i < instances.Count ? (OriginatorVM.OriginatorEquals(instances[i], currentTrackerInstance) ? "X " : "") + instances[i].name : "",
                        i < inventions.Count ? inventions[i].name : ""
                        ));
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
									ConsoleWrap.ReadKey ();
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
								ConsoleWrap.ReadKey ();
								break;
							}
						}
					},
					IOA(inName, inlines, (CPlanCommands.CCollectionEditorBoundCommands<EntryLineVM>)pc.eat),
					IOA(outName, outlines, (CPlanCommands.CCollectionEditorBoundCommands<EntryLineVM>)pc.burn),
                    IOA("inventors", inventions, inv.doadd, inv.doremove, inv.doedit),
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
        ConsolePageAction IOA<T>(String name, IReadOnlyList<T> entries, CPlanCommands.CCollectionEditorBoundCommands<T> commands)
        {
            return IOA(name, entries, commands.Add, commands.Remove, commands.Edit);
        }

        ConsolePageAction IOA<T>(String name, IReadOnlyList<T> entries, Action add, Action<T> remove, Action<T> edit)
		{
			return new ConsolePageAction () {
				name =  name + " actions",
				argumentNames = new [] { "mode[a,d,e]", "index" },
				action = d => {
					int idx = -1;
					if (d.Length > 1 &&int.TryParse (d [1], out idx)&& (idx < 0 || idx >= entries.Count)) {
						Console.WriteLine ("Out of range...");
						ConsoleWrap.ReadKey ();
						return;
					}
					switch(d[0])
					{
					case "a": add (); break;
					case "d": remove (entries[idx]); break;
					case "e": edit (entries[idx]); break;
					default:
						Console.WriteLine ("unknown mode...");
						ConsoleWrap.ReadKey ();
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
        IReadOnlyList<EntryLineVM> inlines, outlines;
        public void SetEatLines(IVMList<EntryLineVM> lineitems) { lineitems.Blap(() =>  inlines = new List<EntryLineVM>(lineitems), this); }
		public void SetBurnLines (IVMList<EntryLineVM> lineitems) { lineitems.Blap(() =>  outlines = new List<EntryLineVM>(lineitems), this); }

        // blap to info request
        public void SetEatInfos(IVMList<InfoLineVM> lineitems) { lineitems.Blap(() =>  MainClass.dbuild.crf.OneInfo.ininfos = new List<InfoLineVM>(lineitems), MainClass.dbuild.crf.OneInfo); }
        public void SetBurnInfos(IVMList<InfoLineVM> lineitems) { lineitems.Blap(() => MainClass.dbuild.crf.OneInfo.outinfos = new List<InfoLineVM>(lineitems), MainClass.dbuild.crf.OneInfo); }

        IReadOnlyList<TrackerInstanceVM> instances;
		public void SetInstances (IVMList<TrackerInstanceVM> instanceitems) { instanceitems.Blap(() =>  instances = new List<TrackerInstanceVM>(instanceitems), this); }

        IReadOnlyList<TrackerTracksVM> inTracks, outTracks;
		public void SetEatTrack (IVMList<TrackerTracksVM> others)  {others.Blap(() =>  inTracks = new List<TrackerTracksVM>(others), this);}
        public void SetBurnTrack(IVMList<TrackerTracksVM> others) { others.Blap(() =>  outTracks = new List<TrackerTracksVM>(others), this); }

        IReadOnlyList<InventedTrackerVM> inventions;
        public void SetInventions(IVMList<InventedTrackerVM> inventionitems) { inventionitems.Blap(() => inventions = new  List<InventedTrackerVM>(inventionitems), this); }

        // Some data members
        DateTime mday;
		public DateTime day { get{ return mday; } set { mday = value; pageChanged = true; } }
		TrackerInstanceVM mcti;
		public TrackerInstanceVM currentTrackerInstance { get{ return mcti; } set { mcti = value; pageChanged = true; } }

        class IV : ICollectionEditorLooseCommands<InventedTrackerVM>
        {
            public void doadd() { add(); }
            public void doedit(InventedTrackerVM i) { edit(i); }
            public void doremove(InventedTrackerVM i) { remove(i); }
            public event Action add = delegate { };
            public event Action<InventedTrackerVM> edit = delegate { };
            public event Action<InventedTrackerVM> remove = delegate { };
        }
        IV inv = new IV();
        public ICollectionEditorLooseCommands<InventedTrackerVM> invention{get{return inv;}}
        #endregion
    }
}

