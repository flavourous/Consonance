
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Consonance
{
	public class TrackerTrackView : View
	{
		public TrackerTrackView (Context context) :
			base (context)
		{
			Initialize ();
		}

		public TrackerTrackView (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			Initialize ();
		}

		public TrackerTrackView (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			Initialize ();
		}

		void Initialize ()
		{
			LayoutParameters = new ViewGroup.LayoutParams (ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			SetBackgroundColor (Android.Graphics.Color.PaleVioletRed);
		}

		public void SetName(String name)
		{
			
		}
		public void SetTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others)
		{
			List<String> elns = new List<string> ();
			foreach(var ti in current.tracks)
			{
				double? inVal = 0.0;
				if (ti.eatValues == null) inVal = null;
				else foreach (var te in ti.eatValues)
					inVal += te.value;

				double? outVal = 0.0;
				if (ti.burnValues == null) outVal = null;
				else foreach (var te in ti.burnValues)
					outVal += te.value;

				if (!inVal.HasValue)
					elns.Add (ti.valueName + " : " + ti.targetValue);
				else if (!outVal.HasValue)
					elns.Add (ti.valueName + " : " + inVal + "/" + ti.targetValue);
				else 
					elns.Add(String.Format("{0:0} eaten - {1:0} burned = {2:0} of {3:0} {4}",
						inVal, outVal, inVal-outVal, ti.targetValue, ti.valueName));
			}
		}
	}
}

