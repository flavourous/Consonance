using System;
using Consonance;
using Consonance.Invention;
using Consonance.Protocol;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Consonance.Test
{
    public class TestView : IView
    {
        public TrackerInstanceVM currentTrackerInstance { get; set; }
        public DateTime day { get; set; }
        public event Action<DateTime> changeday = delegate { };
        public void ChangeDay(DateTime to)
        {
            changeday(to);
        }

        public ICollectionEditorLooseCommands<InventedTrackerVM> invention { get { return _invention; } }
        public Loose<InventedTrackerVM> _invention = new Loose<InventedTrackerVM>();
        public ICollectionEditorSelectableLooseCommands<TrackerInstanceVM> plan { get { return _plan; } }
        public SelectableLoose<TrackerInstanceVM> _plan = new SelectableLoose<TrackerInstanceVM>();

        public void SetBurnInfos(IVMList<InfoLineVM> lineitems) { OutInfos.val = lineitems; }
        public void SetBurnLines(IVMList<EntryLineVM> lineitems) { OutEntries.val = lineitems; }
        public void SetBurnTrack(IVMList<TrackerTracksVM> tracks_current_first) { OutTrack.val = tracks_current_first; }
        public void SetEatInfos(IVMList<InfoLineVM> lineitems) { InInfos.val = lineitems; }
        public void SetEatLines(IVMList<EntryLineVM> lineitems) { InEntries.val = lineitems; }
        public void SetEatTrack(IVMList<TrackerTracksVM> tracks_current_first) { InTrack.val = tracks_current_first; }
        public void SetInstances(IVMList<TrackerInstanceVM> instanceitems) { Instances.val = instanceitems; }
        public void SetInventions(IVMList<InventedTrackerVM> inventionitems) { Inventions.val = inventionitems; }

        // Should just get set once, can test that after present.
        public IVMListStore<InfoLineVM> OutInfos = new IVMListStore<InfoLineVM>();
        public IVMListStore<EntryLineVM> OutEntries = new IVMListStore<EntryLineVM>();
        public IVMListStore<TrackerTracksVM> OutTrack = new IVMListStore<TrackerTracksVM>();
        public IVMListStore<InfoLineVM> InInfos = new IVMListStore<InfoLineVM>();
        public IVMListStore<EntryLineVM> InEntries = new IVMListStore<EntryLineVM>();
        public IVMListStore<TrackerTracksVM> InTrack = new IVMListStore<TrackerTracksVM>();
        public IVMListStore<TrackerInstanceVM> Instances = new IVMListStore<TrackerInstanceVM>();
        public IVMListStore<InventedTrackerVM> Inventions = new IVMListStore<InventedTrackerVM>();
        public void AssertNoFailsFromExtraLoads()
        {
            bool ok = true;
            StringBuilder sb = new StringBuilder();
            Action<String, IEnumerable<SMR>> sr = (id, res) =>
                sb.AppendLine(id + ": " + String.Join(",",
                    res.Select(s => { ok = ok && s.hit; return String.Format("{0}{1}{0}", s.hit ? "" : "|", s.val); }))
                );
            sr(OutInfos.id, OutInfos.Results());
            sr(OutEntries.id, OutEntries.Results());
            sr(OutTrack.id, OutTrack.Results());
            sr(InInfos.id, InInfos.Results());
            sr(InEntries.id, InEntries.Results());
            sr(InTrack.id, InTrack.Results());
            sr(Instances.id, Instances.Results());
            sr(Inventions.id, Inventions.Results());
            Assert.IsTrue(ok, sb.ToString());
        }
    }
    
}
