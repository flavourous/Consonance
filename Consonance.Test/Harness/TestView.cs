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

        public void SetBurnInfos(IVMList<InfoLineVM> lineitems) { BurnInfos.val = lineitems; }
        public void SetBurnLines(IVMList<EntryLineVM> lineitems) { BurnLines.val = lineitems; }
        public void SetBurnTrack(IVMList<TrackerTracksVM> tracks_current_first) { BurnTrack.val = tracks_current_first; }
        public void SetEatInfos(IVMList<InfoLineVM> lineitems) { EatInfos.val = lineitems; }
        public void SetEatLines(IVMList<EntryLineVM> lineitems) { EatLines.val = lineitems; }
        public void SetEatTrack(IVMList<TrackerTracksVM> tracks_current_first) { EatTrack.val = tracks_current_first; }
        public void SetInstances(IVMList<TrackerInstanceVM> instanceitems) { Instances.val = instanceitems; }
        public void SetInventions(IVMList<InventedTrackerVM> inventionitems) { Inventions.val = inventionitems; }

        // Should just get set once, can test that after present.
        public IVMListStore<InfoLineVM> BurnInfos = new IVMListStore<InfoLineVM>();
        public IVMListStore<EntryLineVM> BurnLines = new IVMListStore<EntryLineVM>();
        public IVMListStore<TrackerTracksVM> BurnTrack = new IVMListStore<TrackerTracksVM>();
        public IVMListStore<InfoLineVM> EatInfos = new IVMListStore<InfoLineVM>();
        public IVMListStore<EntryLineVM> EatLines = new IVMListStore<EntryLineVM>();
        public IVMListStore<TrackerTracksVM> EatTrack = new IVMListStore<TrackerTracksVM>();
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
            sr(BurnInfos.id, BurnInfos.Results());
            sr(BurnLines.id, BurnLines.Results());
            sr(BurnTrack.id, BurnTrack.Results());
            sr(EatInfos.id, EatInfos.Results());
            sr(EatLines.id, EatLines.Results());
            sr(EatTrack.id, EatTrack.Results());
            sr(Instances.id, Instances.Results());
            sr(Inventions.id, Inventions.Results());
            Assert.IsTrue(ok, sb.ToString());
        }
    }
    
}
