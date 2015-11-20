using System;
using Consonance;
using System.Collections.Generic;
using System.Text;

namespace Consonance.ConsoleView
{
	class CView : IView, ICollectionEditorLooseCommands<TrackerInstanceVM>, IConsolePage
	{
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
			get 
			{
				pdb.Clear ();
				pdb.AppendFormat ("Main Page\n=================================\n");
				pdb.AppendFormat ("Date: {0}\n\n", day.ToShortDateString());

				int maxRows = Math.Min (10, ManyMax (inlines.Count, outlines.Count, instances.Count));
				int colWid = 20;
				if(currentTrackerInstance != null)
					pdb.Append (RowString (colWid, "Index", currentTrackerInstance.dialect.InputEntryVerb, currentTrackerInstance.dialect.OutputEntrytVerb, "Plan"));
				else pdb.Append (RowString (colWid, "Index", "In", "Out", "Plan"));
				pdb.AppendLine ();
				pdb.AppendLine (RowString (colWid, new String ('=', colWid + 4),  new String ('=', colWid + 4), new String ('=', colWid + 4), new String ('=', colWid + 4)));
				int i = 0;
				for (; i < maxRows; i++) {
					pdb.Append (RowString (colWid, i.ToString(),
						i < inlines.Count ? inlines [i].name : "", 
						i < outlines.Count ? outlines [i].name : "", 
						i < instances.Count ? (OriginatorVM.OriginatorEquals(instances [i], currentTrackerInstance) ? "X " : "") + instances [i].name : ""));
					pdb.AppendLine();
				}
				if (i == 0) pdb.AppendLine ("Nothing here!");
				pdb.AppendLine ();
				return pdb.ToString ();
			} 
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
				String outName = currentTrackerInstance == null ? "Out" : currentTrackerInstance.dialect.OutputEntrytVerb;
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
		public ICollectionEditorLooseCommands<TrackerInstanceVM> plan {  get { return this; } }
		#region ICollectionEditorLooseCommands implementation
		public event Action add = delegate { };
		public event Action<TrackerInstanceVM> remove = delegate { };
		public event Action<TrackerInstanceVM> edit = delegate { };
		public event Action<TrackerInstanceVM> select = delegate { };
		#endregion

		// Data incoming
		public void SetEatTrack (TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { }
		public void SetBurnTrack (TrackerTracksVM current, IEnumerable<TrackerTracksVM> others) { }
		IReadOnlyList<EntryLineVM> inlines = new List<EntryLineVM>(), outlines= new List<EntryLineVM>();
		public void SetEatLines (IEnumerable<EntryLineVM> lineitems) { inlines = new List<EntryLineVM> (lineitems);  pageChanged = true;}
		public void SetBurnLines (IEnumerable<EntryLineVM> lineitems) { outlines = new List<EntryLineVM> (lineitems);  pageChanged = true;}
		IReadOnlyList<TrackerInstanceVM> instances= new List<TrackerInstanceVM>();

		public void SetInstances (IEnumerable<TrackerInstanceVM> instanceitems) { instances = new List<TrackerInstanceVM> (instanceitems); pageChanged = true; }
		public void UIThread (Action a) { a (); }

		// Some data members
		DateTime mday;
		public DateTime day { get{ return mday; } set { mday = value; pageChanged = true; } }
		TrackerInstanceVM mcti;
		public TrackerInstanceVM currentTrackerInstance { get{ return mcti; } set { mcti = value; pageChanged = true; } }
		#endregion
	}
}

