using System;
using Consonance;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Consonance.ConsoleView
{
	class MainClass
	{
		public static CValueRequestBuilder dbuild = new CValueRequestBuilder();
		public static CView view;
		public static CInput input;
		public static CPlanCommands plancommands;
		public static ConsolePager consolePager;
		public static void Main (string[] args)
		{
			plancommands = new CPlanCommands(dbuild);
			input = new CInput (plancommands,dbuild.requestFactory);
			view = new CView(plancommands);
			Presenter.PresentTo (view, null, input, plancommands, dbuild).Wait();
			// console loop
			consolePager = new ConsolePager(view);
			consolePager.RunLoop ();
		}
	}
}
