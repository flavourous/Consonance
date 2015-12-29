using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Diagnostics;

namespace Consonance
{
	class HookedInfoLines : IDisposable
	{
		readonly InfoManageType imt;
		readonly IAbstractedTracker cdh;
		public readonly ObservableCollection<InfoLineVM> lines;
		public HookedInfoLines(IAbstractedTracker cdh, InfoManageType imt)
		{
			this.imt = imt;
			this.cdh = cdh;
			this.lines = new ObservableCollection<InfoLineVM> ();
			cdh.ViewModelsChanged += Cdh_ViewModelsChanged;;
			PushInLinesAndFire ();
		}

		void Cdh_ViewModelsChanged (IAbstractedTracker sender, DietVMChangeEventArgs args)
		{
			PushInLinesAndFire ();
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			cdh.ViewModelsChanged -= Cdh_ViewModelsChanged;;
		}
		#endregion

		void PushInLinesAndFire()
		{
			lines.Clear ();
			switch (imt) {
			case InfoManageType.In:
				foreach (var ii in cdh.InInfos (false))
					lines.Add (ii);
				break;
			case InfoManageType.Out:
				foreach (var oi in cdh.OutInfos (false))
					lines.Add (oi);
				break;
			}
		}
	}



}

