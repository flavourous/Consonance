using System;
using System.Linq;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Collections;

namespace Consonance.XamarinFormsView
{
	public class VStacker : ContentView
	{
		readonly StackLayout ms;
		public delegate View Creator(Object val);
		readonly Creator c;
		public VStacker(Creator c)
		{
			this.c = c;
			Content = ms = new StackLayout { Orientation = StackOrientation.Vertical };
		}
		public IEnumerable Items { get { return GetValue (ItemsProperty) as IEnumerable; } set { SetValue (ItemsProperty, value); } }
		public static readonly BindableProperty ItemsProperty = BindableProperty.Create<VStacker, IEnumerable>
			(f => f.Items, null, BindingMode.Default, null, (snd, n, o) => (snd as VStacker).SetItems ());
		void SetItems()
		{
			ms.Children.Clear ();
			if (Items != null)
				foreach (var b in Items) 
					ms.Children.Add (c (b));
		}
	}
	public class TTView : VStacker
	{
		public TTView() : base(Generator) { }
		static View Generator(Object vmo)
		{
			TrackerTracksVM vm = vmo as TrackerTracksVM;
			return new StackLayout {
				Children = { 
					new Label { Text = vm.instanceName },
					new TTViewItem { BindingContext = vm.tracks }
				}
			};
		}
	}
	public class TTViewItem : VStacker
	{
		public TTViewItem () : base(BarInfo.GenerateView) { }
		class BarInfo
		{
			static ColumnDefinition CBound(String wname)
			{
				var c = new ColumnDefinition ();
				c.SetBinding (ColumnDefinition.WidthProperty, wname);
				return c;
			}

			public static View GenerateView(Object tio)
			{
				TrackingInfoVM ti = tio as TrackingInfoVM;
				return new Grid 
				{
					BindingContext = new BarInfo(ti),
					RowDefinitions = new RowDefinitionCollection 
					{ 
						new RowDefinition { Height = GridLength.Auto },  
						new RowDefinition { Height = GridLength.Auto } 
					},
					ColumnDefinitions = new ColumnDefinitionCollection
					{
						CBound("InAmount"), 
						CBound("OutAmount"), 
						CBound("TargetAmount"),
					},
					Children = 
					{
						new BoxView { Color = Color.Red }.OnCol (0),
						new BoxView { Color = Color.Green }.OnCol (1),
						new BoxView { Color = Color.Blue }.OnCol (2)
					}
				};
			}
			private BarInfo(TrackingInfoVM ti)
			{
				AmountName = ti.valueName;
				TargetAmount=ti.targetValue;
				InAmount= (from f in ti.inValues select f.value).Sum();
				OutAmount= (from f in ti.outValues select f.value).Sum();
			}
			public String AmountName { get; private set; }
			public double InAmount {get;private set;}
			public double OutAmount {get;private set;}
			public double TargetAmount {get;private set;}
		}
	}
}


