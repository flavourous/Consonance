using System;
using Android.Views;

namespace Consonance
{
	public static class DroidUtils
	{
		public static void PushView(View child, ViewGroup parent)
		{
			PullFromParent (child);
			parent.AddView (child);
		}
		public static void PushView(View child, ViewGroup parent, int insertIndex)
		{
			PullFromParent (child);
			parent.AddView (child, insertIndex);
		}
		static void PullFromParent(View child)
		{
			if (child.Parent is ViewGroup)
				(child.Parent as ViewGroup).RemoveView (child);
		}
	}
}

