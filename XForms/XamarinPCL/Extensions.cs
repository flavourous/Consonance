using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Consonance.XamarinFormsView.PCL
{
	public static class ExMethods
	{
		public static T OnCol<T>(this T ret, int col) where T : View
		{
			ret.SetValue (Grid.ColumnProperty, col);
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

