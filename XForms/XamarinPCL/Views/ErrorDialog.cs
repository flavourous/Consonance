using System;

using Xamarin.Forms;

namespace Consonance.XamarinFormsView.PCL
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
            Content = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition {  Height= new GridLength(1, GridUnitType.Star)},
                    new RowDefinition {Height = GridLength.Auto }
                },
                Children =
                {
                    new Label { Text = err, VerticalOptions = LayoutOptions.StartAndExpand }.OnRow(0),
                    (b = new Button { Text = "Close", VerticalOptions = LayoutOptions.StartAndExpand }.OnRow(1))
                }
            };
			b.Clicked += (sender, e) => { 
				onclose();
				Navigation.PopModalAsync ();
			};
		}
	}
}


