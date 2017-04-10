using Consonance.Invention;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Consonance.Protocol;

namespace Consonance.XamarinFormsView
{
    public class InventionCommandManager : ICollectionEditorLooseCommands<InventedTrackerVM>
    {
        public void Add() { add(); }
        public void Edit(InventedTrackerVM v) { edit(v); }
        public void Delete(InventedTrackerVM v) { remove(v); }
        public event Action add = delegate { };
        public event Action<InventedTrackerVM> edit = delegate { };
        public event Action<InventedTrackerVM> remove = delegate { };
    }
}
