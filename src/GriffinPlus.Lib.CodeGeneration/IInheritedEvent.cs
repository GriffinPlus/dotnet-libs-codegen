///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Untyped interface of an inherited event.
	/// </summary>
	public interface IInheritedEvent : IEvent
	{
		/// <summary>
		/// Gets the 'add' accessor method.
		/// </summary>
		new IInheritedMethod AddAccessor { get; }

		/// <summary>
		/// Gets the 'remove' accessor method.
		/// </summary>
		new IInheritedMethod RemoveAccessor { get; }

		/// <summary>
		/// Gets the <see cref="System.Reflection.EventInfo"/> associated with the event.
		/// </summary>
		EventInfo EventInfo { get; }

		/// <summary>
		/// Adds an override for the event.
		/// </summary>
		/// <param name="implementation">
		/// Implementation strategy that implements the add/remove accessor methods of the event and the event raiser method, if any.
		/// </param>
		/// <returns>The generated event.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
		IGeneratedEvent Override(IEventImplementation implementation);

		/// <summary>
		/// Adds an override for the event.
		/// </summary>
		/// <param name="addAccessorImplementationCallback">A callback that implements the add accessor method of the event.</param>
		/// <param name="removeAccessorImplementationCallback">A callback that implements the remove accessor method of the event.</param>
		/// <returns>The added event overriding the specified inherited event.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="addAccessorImplementationCallback"/> or <paramref name="removeAccessorImplementationCallback"/> is <c>null</c>.
		/// </exception>
		/// <returns>The generated event.</returns>
		IGeneratedEvent Override(
			EventAccessorImplementationCallback addAccessorImplementationCallback,
			EventAccessorImplementationCallback removeAccessorImplementationCallback);
	}

}
