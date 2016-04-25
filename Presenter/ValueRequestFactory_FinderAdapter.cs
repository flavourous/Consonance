using LibSharpHelp;
using System;

namespace Consonance
{
	/// <summary>
	/// the purpose of this class is to take IValueRequest objects alreadt created, add an extra "button" request object.
	/// THis when fired will trigger a IFindData search for T, which, if accepted, fill pass back to the creator of the vanilla
	/// request objects that they should fill the request objects with the data from this accepted found T.
	/// 
	/// ofcourse this is stateful, and the model object will expect the last requested request objects to be the pertenant ones.
	/// </summary>
	public class ValueRequestFactory_FinderAdapter<T> where T : class
	{
		readonly IFindList<InfoLineVM> infoFinder;
		readonly IInfoCreation<T> infoCreator;
		readonly IValueRequestFactory factory;
		readonly IUserInput finderView;
		public ValueRequestFactory_FinderAdapter (IFindList<InfoLineVM> infoFinder, IInfoCreation<T> infoCreator, IValueRequestFactory factory, IUserInput fv)
		{
			this.factory=factory;
			this.infoFinder = infoFinder;
			this.infoCreator = infoCreator;
			this.finderView = fv;
		} 
		public BindingList<Object> GetRequestObjects()
		{
			var flds = infoCreator.InfoFields(factory);
            if (infoFinder.CanFind)
            { // dont push in if we dont actually have a finder...
                var ar = factory.ActionRequestor("search");
                ar.valid = true;
                flds.Add(ar.request);
                ar.changed += LaunchFinder;
            }
			return flds;
		}
		async void LaunchFinder() // again, it's event handler.
		{
			finderView.Choose (infoFinder).ContinueWith (t =>
				infoCreator.FillRequestData ((T)t.Result.originator)
			);
		}
	}
}

