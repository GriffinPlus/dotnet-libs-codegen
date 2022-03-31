///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Collection of predefined event implementation strategies.
	/// </summary>
	public static class EventImplementations
	{
		/// <summary>
		/// Event implementation that simply backs the event with a field storing event handlers.
		/// An event raiser method is not added.
		/// </summary>
		public static EventImplementation_Standard Standard => new EventImplementation_Standard();

		/// <summary>
		/// Event implementation that simply backs the event with a field storing event handlers.
		/// An event raiser method is added and implemented depending on the event type.
		/// If the event type is <see cref="System.EventHandler"/> or <see cref="System.EventHandler{TEventArgs}"/> with
		/// <c>TEventArgs</c> being <see cref="System.EventArgs"/> the event raiser method will have the signature <c>void OnEvent()</c>.
		/// If the event type is <see cref="System.EventHandler{TEventArgs}"/> with more specialized <c>TEventArgs</c> (e.g. <c>SpecializedEventArgs</c>)
		/// the event raiser method will have the signature <c>void OnEvent(SpecializedEventArgs e)</c>. Any other event type will
		/// produce an event raiser method that has the same return type and parameters as the delegate.
		/// </summary>
		/// <param name="name">Name of the event raiser method (<c>null</c> to use 'On' + the name of the event).</param>
		/// <param name="visibility">Visibility of the event raiser method.</param>
		public static EventImplementation_Standard StandardWithEventRaiser(string name = null, Visibility visibility = Visibility.Protected)
		{
			return new EventImplementation_Standard(name, visibility);
		}
	}

}
