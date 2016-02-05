using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Consonance.XamarinFormsView
{
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

