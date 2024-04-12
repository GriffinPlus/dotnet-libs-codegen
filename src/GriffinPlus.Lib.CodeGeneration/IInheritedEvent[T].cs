///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Typed interface of an inherited event.
/// </summary>
/// <typeparam name="T">Type of the event handler delegate.</typeparam>
public interface IInheritedEvent<T> : IInheritedEvent where T : Delegate
{
	/// <summary>
	/// Adds an override for the event.
	/// </summary>
	/// <param name="implementation">
	/// Implementation strategy that implements the add/remove accessor methods of the event and the event raiser method, if any.
	/// </param>
	/// <returns>The generated event.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	new IGeneratedEvent<T> Override(IEventImplementation implementation);

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
	new IGeneratedEvent<T> Override(
		EventAccessorImplementationCallback addAccessorImplementationCallback,
		EventAccessorImplementationCallback removeAccessorImplementationCallback);
}
