using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace Consonance.XamarinFormsView.PCL
{
	public static class ExMethods
	{
		public static T OnCol<T>(this T ret, int col, int span = 1) where T : View
		{
			ret.SetValue (Grid.ColumnProperty, col);
            if (span > 1) ret.SetValue(Grid.ColumnSpanProperty, span);
			return ret;
		}
        public static T OnRow<T>(this T ret, int row, int span = 1) where T : View
        {
            ret.SetValue(Grid.RowProperty, row);
            if (span > 1) ret.SetValue(Grid.RowSpanProperty, span);
            return ret;
        }
        public static T Bind<T>(this T ret, BindableProperty bp, String path) where T : View
        {
            ret.SetBinding(bp, path);
            return ret;
        }
        public static T Rel<T>(this T ret, double? fx, double? fy, double? fw, double? fh) where T : View
        {
            if (fx.HasValue)
                ret.SetValue(RelativeLayout.XConstraintProperty, Constraint.RelativeToParent(p => p.Width * fx.Value));
            if (fy.HasValue)
                ret.SetValue(RelativeLayout.YConstraintProperty, Constraint.RelativeToParent(p => p.Height * fy.Value));
            if (fw.HasValue)
                ret.SetValue(RelativeLayout.WidthConstraintProperty, Constraint.RelativeToParent(p => p.Width * fw.Value));
            if (fh.HasValue)
                ret.SetValue(RelativeLayout.HeightConstraintProperty, Constraint.RelativeToParent(p => p.Height * fh.Value));
            return ret;
        }
    }
	[ContentProperty("Source")]
	class EmbededImageExtension : IMarkupExtension
	{
		String Source {get;set;}
		#region IMarkupExtension implementation
		public object ProvideValue (IServiceProvider serviceProvider)
		{
			return Source == null ? null : ImageSource.FromResource (Source);
		}
		#endregion
	}	
}

