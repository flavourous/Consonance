
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
using Android.Graphics;
using Android.Graphics.Drawables;

namespace Consonance
{
	public static class Paintys
	{
		public static Paint paint_backbar_s = GetPaint (Color.Argb(255,177,177,177));
		public static Paint paint_backbar_f = GetPaint (Color.Argb(255,177,177,199));
		public static Paint paint_left = GetPaint (Color.Argb(180,177,255,177));
		public static Paint paint_right = GetPaint (Color.Argb(180,177,77,77));
		public static Paint paint_target = GetPaint (Color.Argb(180,77,190,120));
		public static Paint paint_name = GetPaint (Color.Argb(230,200,200,200), 12);
		public static Paint GetPaint(Color color)
		{
			return new Paint (PaintFlags.AntiAlias) {
				Color = color,
			};
		}
		static Paint GetPaint(Color color, float stroke)
		{
			var pt = GetPaint (color);
			pt.StrokeWidth = stroke;
			return pt;
		}
		static Paint GetPaint(Color color, int textSize)
		{
			var pt = GetPaint (color);
			pt.TextSize = textSize;
			return pt;
		}
	}
	public class TrackerBlocks : View
	{
		readonly TrackingInfoVM track;
		readonly Rect namebounds = new Rect();
		public TrackerBlocks(Context ctx, TrackingInfoVM track) : base(ctx)
		{
			this.track = track;
			Paintys.paint_name.GetTextBounds (track.valueName, 0, track.valueName.Length, namebounds);
			vin = vout = 0;
			foreach (var d in track.inValues) vin += d.value;
			foreach (var d in track.outValues) vout += d.value;
			DetPix (Width,Height);
		}
		readonly double vin, vout;

		protected override void OnDraw (Canvas canvas)
		{
			// # is target coloring, ~ is out coloring, + is kinda blank coloring, >> is in coloring, ^^ is targetliner
			// when in more than out+target
			//------------------------------------------------------//
			//#########from target###########~~~~from out~~~~~++++++//
			//>>>>>>>>>>>>>>>>>>>>>>>from in>>>>>>>>>>>>>>>>>>>>>>^^//
			//------------------------------------------------------//
			// otherwise
			//------------------------------------------------------//
			//#########from target###################~~~~from out~~~//
			//>>>>>>>>>>>>>>>>>>>>>>>from in>>>>^^###~~~~~~~~~~~~~~~//
			//------------------------------------------------------//

			RectF obar = new RectF (0, 0, Width, Height);
			RectF ibar = new RectF (2, 2, Width-2, Height-2);
			float rr = 4;
			// 1) overdraw the border bar bit

			canvas.DrawRoundRect(obar, rr,rr, Paintys.paint_backbar_s);
			canvas.DrawRoundRect(ibar, rr,rr, Paintys.paint_backbar_f);
			// 2) overdraw target and out by filling again, but setting a rect crop. fill top and bottom halfs
			canvas.Save();
			canvas.ClipRect(new Rect(0,0,pixTargetAmount,Height));
			canvas.DrawRoundRect(ibar, rr,rr, Paintys.paint_target);
			canvas.Restore ();
			canvas.Save();
			canvas.ClipRect(new Rect(0,pixTargetAmount,pixTargetAmount+pixExtraOutAmount,Height));
			canvas.DrawRoundRect(ibar, rr,rr, Paintys.paint_right);
			canvas.Restore ();

			// 3) overdraw in and arrowmarker, set a crop rect again and flll whole rect.
			canvas.Save();
			canvas.ClipRect(new Rect(0,Height/2,pixInAmount,Height));
			canvas.DrawRoundRect(ibar, rr,rr, Paintys.paint_left);
			canvas.Restore ();

//			int h2  = Height/2;
//			canvas.DrawRect(new Rect(0,0,pixTargetAmount,h2),paint_target);
//			canvas.DrawRect(new Rect(pixTargetAmount,0,pixTargetAmount+pixExtraOutAmount,h2),paint_right);
//			canvas.DrawRect(new Rect(0,h2,pixInAmount,Height),paint_left);
//			canvas.DrawText (track.valueName, 0, (Height - namebounds.Height()) / 2, paint_name);

			//base.OnDraw (canvas);
		}
		int pixTargetAmount, pixExtraOutAmount, pixInAmount;
		void DetPix(int w, int h)
		{
			// two bars, target+out vs in
			double biggestTotal = Math.Max (vout + track.targetValue, vin);

			pixTargetAmount = (int)((track.targetValue / biggestTotal) * w);
			pixExtraOutAmount = (int)((vout / biggestTotal) * w);
			pixInAmount = (int)((vin / biggestTotal) * w);

		}
		protected override void OnSizeChanged (int w, int h, int oldw, int oldh)
		{
			DetPix (w,h);	
			base.OnSizeChanged (w, h, oldw, oldh);
		}
	}

	public class TrackerTrackView : LinearLayout
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
			Orientation = Orientation.Vertical;
		}

		int match = LinearLayout.LayoutParams.MatchParent;
		int wrap = LinearLayout.LayoutParams.WrapContent;
		public void SetTrack(TrackerTracksVM current, IEnumerable<TrackerTracksVM> others)
		{
			// start agin...
			RemoveAllViews ();

			if (current != null) {
				// do current...
				PushHeader (
					"Currently Selected: " + current.instanceName + "(" + current.modelName + ")",
					true, 0, this
				);
				PushTrackerTracks (current, 9);
			}

			// others...
			Dictionary<String, List<TrackerTracksVM>> grouped = new Dictionary<string, List<TrackerTracksVM>>();
			foreach (var tt in others) 
			{
				if (!grouped.ContainsKey (tt.modelName))
					grouped [tt.modelName] = new List<TrackerTracksVM> ();
				grouped [tt.modelName].Add (tt);
			}
			foreach(var group in grouped)
			{
				PushHeader(group.Key, true, 0, this);
				foreach(var tt in group.Value)
				{
					PushHeader(tt.instanceName + "(" + tt.modelName + ")", true, 3, this);
					PushTrackerTracks(tt, 9);
				}
			}
		}

		void PushHeader(String text, bool line, int indent, ViewGroup addto)
		{
			// add title of the like trackerinstance thing
			TextView tv = new TextView (Context);
			tv.LayoutParameters = new ViewGroup.LayoutParams (match, wrap);
			tv.SetPadding (indent, 0, 0, 0);
			tv.Text = text;
			addto.AddView (tv);

			if (line) {
				// add a breaker
				View v = new View (Context);
				v.SetBackgroundColor (Paintys.paint_backbar_s.Color);
				v.LayoutParameters = new ViewGroup.LayoutParams (match, 2);
				v.SetPadding (indent, 2, 2, 2);
				addto.AddView (v);
			}
		}
			
		void PushTrackerTracks(TrackerTracksVM track, int indent)
		{
			List<TrackingInfoVM> tiv = new List<TrackingInfoVM> (track.tracks);
			LinearLayout ll = new LinearLayout (Context);
			ll.Orientation = Orientation.Vertical;
			ll.SetPadding (indent, 0, 0, 0);
			ll.LayoutParameters = new LinearLayout.LayoutParams (match, wrap);
			AddTracksToView (tiv, ll);
			AddView (ll);
		}

		void AddTracksToView(List<TrackingInfoVM> tiv, LinearLayout addto)
		{
			for (int i = 0; i < tiv.Count; i++) {
				TrackingInfoVM ti = tiv [i];

				// add title 
				PushHeader (ti.valueName, false, 0, addto);

				// add indicator
				View ttv = new TrackerBlocks (Context, ti);
				ttv.LayoutParameters = new LinearLayout.LayoutParams (
					LinearLayout.LayoutParams.MatchParent, 
					16);
				addto.AddView (ttv);
			}
		}
	}
}

