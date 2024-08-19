///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// An inherited event.
/// </summary>
/// <typeparam name="T">Type of the event handler delegate.</typeparam>
[DebuggerDisplay("Declaring Type = {EventInfo.DeclaringType.FullName}, Event Info = {EventInfo}")]
class InheritedEvent<T> : Member, IInheritedEvent<T> where T : Delegate
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InheritedEvent{T}"/> class.
	/// </summary>
	/// <param name="typeDefinition">The type definition the member belongs to.</param>
	/// <param name="eventInfo">Event the type in creation has inherited.</param>
	internal InheritedEvent(TypeDefinition typeDefinition, EventInfo eventInfo) :
		base(typeDefinition)
	{
		EventInfo = eventInfo;
		AddAccessor = new InheritedMethod(typeDefinition, EventInfo.GetAddMethod(true));
		RemoveAccessor = new InheritedMethod(typeDefinition, EventInfo.GetRemoveMethod(true));
	}

	/// <summary>
	/// Gets the class definition the member belongs to.
	/// </summary>
	public new ClassDefinition TypeDefinition => (ClassDefinition)base.TypeDefinition;

	/// <summary>
	/// Gets the name of the event.
	/// </summary>
	public string Name => EventInfo.Name;

	/// <summary>
	/// Gets the type of the event handler.
	/// </summary>
	public Type EventHandlerType => EventInfo.EventHandlerType;

	/// <summary>
	/// Gets the kind of the event.
	/// </summary>
	public EventKind Kind => EventInfo.ToEventKind();

	/// <summary>
	/// Gets the access modifier of the event.
	/// </summary>
	public Visibility Visibility => EventInfo.ToVisibility();

	/// <summary>
	/// Gets the <see cref="System.Reflection.EventInfo"/> associated with the event.
	/// </summary>
	public EventInfo EventInfo { get; }

	/// <summary>
	/// Gets the 'add' accessor method.
	/// </summary>
	IMethod IEvent.AddAccessor => AddAccessor;

	/// <summary>
	/// Gets the 'add' accessor method.
	/// </summary>
	public IInheritedMethod AddAccessor { get; }

	/// <summary>
	/// Gets the 'remove' accessor method.
	/// </summary>
	IMethod IEvent.RemoveAccessor => RemoveAccessor;

	/// <summary>
	/// Gets the 'remove' accessor method.
	/// </summary>
	public IInheritedMethod RemoveAccessor { get; }

	/// <summary>
	/// Adds an override for the event.
	/// </summary>
	/// <param name="implementation">
	/// Implementation strategy that implements the add/remove accessor methods of the event and the event raiser method, if any.
	/// </param>
	/// <returns>The generated event.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedEvent<T> Override(IEventImplementation implementation)
	{
		return TypeDefinition.AddEventOverride(this, implementation);
	}

	/// <summary>
	/// Adds an override for the event.
	/// </summary>
	/// <param name="implementation">
	/// Implementation strategy that implements the add/remove accessor methods of the event and the event raiser method, if any.
	/// </param>
	/// <returns>The generated event.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	IGeneratedEvent IInheritedEvent.Override(IEventImplementation implementation)
	{
		return Override(implementation);
	}

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
	public IGeneratedEvent<T> Override(
		EventAccessorImplementationCallback addAccessorImplementationCallback,
		EventAccessorImplementationCallback removeAccessorImplementationCallback)
	{
		return TypeDefinition.AddEventOverride(
			this,
			addAccessorImplementationCallback,
			removeAccessorImplementationCallback);
	}

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
	IGeneratedEvent IInheritedEvent.Override(
		EventAccessorImplementationCallback addAccessorImplementationCallback,
		EventAccessorImplementationCallback removeAccessorImplementationCallback)
	{
		return Override(addAccessorImplementationCallback, removeAccessorImplementationCallback);
	}
}
