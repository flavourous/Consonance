using System;

using Xamarin.Forms;

namespace Consonance.XamarinFormsView
{
	public class ErrorDialog : ContentPage
	{
		public static void Show(String error, INavigation nav, Action a)
		{
			nav.PushModalAsync (new ErrorDialog (error, a));
		}
		Button b;
		private ErrorDialog (String err, Action onclose)
		{
			Content = new StackLayout { 
				Children = {
					new Label { Text = err, VerticalOptions = LayoutOptions.FillAndExpand },
					(b = new Button { Text = "Close" })
				}
			};
			b.Clicked += (sender, e) => { 
				onclose();
				Navigation.PopModalAsync ();
			};
		}
	}
}


