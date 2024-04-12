///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// A generated event.
/// </summary>
class GeneratedEvent<T> : Member, IGeneratedEvent<T> where T : Delegate
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedEvent{T}"/> class.
	/// </summary>
	/// <param name="typeDefinition">The type definition the event belongs to.</param>
	/// <param name="kind">Kind of the event determining whether the event is static, abstract, virtual or an override.</param>
	/// <param name="name">Name of the event (<c>null</c> to create a random name).</param>
	/// <param name="visibility">The visibility of the event.</param>
	/// <param name="implementation">
	/// Implementation strategy that implements the add/remove accessor methods of the event and the event raiser method, if any.
	/// Must be <c>null</c>, if <paramref name="kind"/> is <see cref="EventKind.Abstract"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="typeDefinition"/> is <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="kind"/> is not <see cref="EventKind.Abstract"/> and <paramref name="implementation"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="kind"/> is <see cref="EventKind.Abstract"/> and <paramref name="implementation"/> is not <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> is not a valid language independent identifier.
	/// </exception>
	internal GeneratedEvent(
		TypeDefinition       typeDefinition,
		EventKind            kind,
		string               name,
		Visibility           visibility,
		IEventImplementation implementation) :
		base(typeDefinition)
	{
		if (kind == EventKind.Abstract && implementation != null) throw new ArgumentException($"Event kind is '{kind}', an implementation strategy must not be specified.");
		if (kind != EventKind.Abstract && implementation == null) throw new ArgumentNullException(nameof(implementation));
		Implementation = implementation;

		// set the name of the event and check whether it is a valid identifier
		Name = string.IsNullOrWhiteSpace(name) ? "Event_" + Guid.NewGuid().ToString("N") : name;
		EnsureNameIsValidLanguageIndependentIdentifier(Name);

		// ensure that the specified event handler type is public and all nested types are public, too
		// => otherwise the dynamically created assembly is not able to access it
		CodeGenHelpers.EnsureTypeIsTotallyPublic(typeof(T));

		// add 'add' accessor method
		AddAccessor = TypeDefinition.AddMethod(
			kind.ToMethodKind(),
			"add_" + Name,
			typeof(void),
			[EventHandlerType],
			visibility,
			kind == EventKind.Abstract
				? null
				: (_, msilGenerator) => Implementation.ImplementAddAccessorMethod(TypeDefinition, this, msilGenerator),
			MethodAttributes.SpecialName | MethodAttributes.HideBySig);

		// add 'remove' accessor method
		RemoveAccessor = TypeDefinition.AddMethod(
			kind.ToMethodKind(),
			"remove_" + Name,
			typeof(void),
			[EventHandlerType],
			visibility,
			kind == EventKind.Abstract
				? null
				: (_, msilGenerator) => Implementation.ImplementRemoveAccessorMethod(TypeDefinition, this, msilGenerator),
			MethodAttributes.SpecialName | MethodAttributes.HideBySig);

		// add the actual event and wire up the add/remove accessor methods
		EventBuilder = TypeDefinition.TypeBuilder.DefineEvent(Name, EventAttributes.None, EventHandlerType);
		EventBuilder.SetAddOnMethod(AddAccessor.MethodBuilder);
		EventBuilder.SetRemoveOnMethod(RemoveAccessor.MethodBuilder);

		// declare implementation strategy specific members
		Implementation?.Declare(TypeDefinition, this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedEvent{T}"/> class.
	/// </summary>
	/// <param name="typeDefinition">The type definition the event belongs to.</param>
	/// <param name="kind">Kind of the event determining whether the event is static, abstract, virtual or an override.</param>
	/// <param name="name">Name of the event (<c>null</c> to create a random name).</param>
	/// <param name="visibility">The visibility of the event.</param>
	/// <param name="addAccessorImplementationCallback">
	/// A callback that implements the add accessor method of the event.
	/// Must be <c>null</c>, if <paramref name="kind"/> is <see cref="EventKind.Abstract"/>.
	/// </param>
	/// <param name="removeAccessorImplementationCallback">
	/// A callback that implements the remove accessor method of the event.
	/// Must be <c>null</c>, if <paramref name="kind"/> is <see cref="EventKind.Abstract"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="typeDefinition"/> is <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="kind"/> is not <see cref="EventKind.Abstract"/> and <paramref name="addAccessorImplementationCallback"/> or
	/// <paramref name="removeAccessorImplementationCallback"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="kind"/> is <see cref="EventKind.Abstract"/> and <paramref name="addAccessorImplementationCallback"/> or
	/// <paramref name="removeAccessorImplementationCallback"/> is not <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> is not a valid language independent identifier.
	/// </exception>
	internal GeneratedEvent(
		TypeDefinition                      typeDefinition,
		EventKind                           kind,
		string                              name,
		Visibility                          visibility,
		EventAccessorImplementationCallback addAccessorImplementationCallback,
		EventAccessorImplementationCallback removeAccessorImplementationCallback) :
		base(typeDefinition)
	{
		if (kind == EventKind.Abstract && (addAccessorImplementationCallback != null || removeAccessorImplementationCallback != null))
			throw new ArgumentException($"Event kind is '{kind}', an implementation callback must not be specified.");
		if (kind != EventKind.Abstract && addAccessorImplementationCallback == null)
			throw new ArgumentNullException(nameof(addAccessorImplementationCallback));
		if (kind != EventKind.Abstract && removeAccessorImplementationCallback == null)
			throw new ArgumentNullException(nameof(removeAccessorImplementationCallback));

		// set the name of the event and check whether it is a valid identifier
		Name = string.IsNullOrWhiteSpace(name) ? "Event_" + Guid.NewGuid().ToString("N") : name;
		EnsureNameIsValidLanguageIndependentIdentifier(Name);

		// ensure that the specified event handler type is public and all nested types are public, too
		// => otherwise the dynamically created assembly is not able to access it
		CodeGenHelpers.EnsureTypeIsTotallyPublic(typeof(T));

		// add 'add' accessor method if the event is not abstract
		Debug.Assert(addAccessorImplementationCallback != null, nameof(addAccessorImplementationCallback) + " != null");
		AddAccessor = TypeDefinition.AddMethod(
			Kind.ToMethodKind(),
			"add_" + Name,
			typeof(void),
			[EventHandlerType],
			visibility,
			kind == EventKind.Abstract
				? null
				: (_, msilGenerator) => addAccessorImplementationCallback(this, msilGenerator),
			MethodAttributes.SpecialName | MethodAttributes.HideBySig);

		// add 'remove' accessor method if the event is not abstract
		Debug.Assert(removeAccessorImplementationCallback != null, nameof(removeAccessorImplementationCallback) + " != null");
		RemoveAccessor = TypeDefinition.AddMethod(
			Kind.ToMethodKind(),
			"remove_" + Name,
			typeof(void),
			[EventHandlerType],
			visibility,
			kind == EventKind.Abstract
				? null
				: (_, msilGenerator) => removeAccessorImplementationCallback(this, msilGenerator),
			MethodAttributes.SpecialName | MethodAttributes.HideBySig);

		// add the actual event and wire up the add/remove accessor methods
		EventBuilder = TypeDefinition.TypeBuilder.DefineEvent(Name, EventAttributes.None, EventHandlerType);
		EventBuilder.SetAddOnMethod(AddAccessor.MethodBuilder);
		EventBuilder.SetRemoveOnMethod(RemoveAccessor.MethodBuilder);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedEvent{T}"/> class (for overrides).
	/// </summary>
	/// <param name="typeDefinition">The type definition the property belongs to.</param>
	/// <param name="inheritedEvent">Inherited event to override.</param>
	/// <param name="implementation">
	/// Implementation strategy that implements the add/remove accessor methods of the event and the event raiser method, if any.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="typeDefinition"/>, <paramref name="inheritedEvent"/> or <paramref name="implementation"/> is <c>null</c>.
	/// </exception>
	internal GeneratedEvent(
		TypeDefinition       typeDefinition,
		IInheritedEvent<T>   inheritedEvent,
		IEventImplementation implementation) :
		base(typeDefinition)
	{
		if (inheritedEvent == null) throw new ArgumentNullException(nameof(inheritedEvent));

		Implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
		Name = inheritedEvent.Name;

		// add an override for the 'add' accessor and the 'remove' accessor methods
		AddAccessor = TypeDefinition.AddMethodOverride(
			inheritedEvent.AddAccessor,
			(_, msilGenerator) => Implementation.ImplementAddAccessorMethod(TypeDefinition, this, msilGenerator));
		RemoveAccessor = TypeDefinition.AddMethodOverride(
			inheritedEvent.RemoveAccessor,
			(_, msilGenerator) => Implementation.ImplementRemoveAccessorMethod(TypeDefinition, this, msilGenerator));

		// add the actual event and wire up the add/remove accessor
		EventBuilder = TypeDefinition.TypeBuilder.DefineEvent(Name, EventAttributes.None, EventHandlerType);
		EventBuilder.SetAddOnMethod(AddAccessor.MethodBuilder);
		EventBuilder.SetRemoveOnMethod(RemoveAccessor.MethodBuilder);

		// declare implementation strategy specific members
		Implementation?.Declare(TypeDefinition, this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedEvent{T}"/> class (for overrides).
	/// </summary>
	/// <param name="typeDefinition">The type definition the property belongs to.</param>
	/// <param name="inheritedEvent">Inherited event to override.</param>
	/// <param name="addAccessorImplementationCallback">A callback that implements the add accessor method of the event.</param>
	/// <param name="removeAccessorImplementationCallback">A callback that implements the remove accessor method of the event.</param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="typeDefinition"/>, <paramref name="inheritedEvent"/>, <paramref name="addAccessorImplementationCallback"/> or
	/// <paramref name="removeAccessorImplementationCallback"/> is <c>null</c>.
	/// </exception>
	internal GeneratedEvent(
		TypeDefinition                      typeDefinition,
		IInheritedEvent<T>                  inheritedEvent,
		EventAccessorImplementationCallback addAccessorImplementationCallback,
		EventAccessorImplementationCallback removeAccessorImplementationCallback) :
		base(typeDefinition)
	{
		if (inheritedEvent == null) throw new ArgumentNullException(nameof(inheritedEvent));
		if (addAccessorImplementationCallback == null) throw new ArgumentNullException(nameof(addAccessorImplementationCallback));
		if (removeAccessorImplementationCallback == null) throw new ArgumentNullException(nameof(removeAccessorImplementationCallback));

		Name = inheritedEvent.Name;

		// add an override for the 'add' accessor and the 'remove' accessor method
		AddAccessor = TypeDefinition.AddMethodOverride(
			inheritedEvent.AddAccessor,
			(_, msilGenerator) => addAccessorImplementationCallback(this, msilGenerator));
		RemoveAccessor = TypeDefinition.AddMethodOverride(
			inheritedEvent.RemoveAccessor,
			(_, msilGenerator) => removeAccessorImplementationCallback(this, msilGenerator));

		// add the actual event and wire up the add/remove accessor
		EventBuilder = TypeDefinition.TypeBuilder.DefineEvent(Name, EventAttributes.None, EventHandlerType);
		EventBuilder.SetAddOnMethod(AddAccessor.MethodBuilder);
		EventBuilder.SetRemoveOnMethod(RemoveAccessor.MethodBuilder);
	}

	/// <summary>
	/// Gets the name of the event.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the type of the event handler.
	/// </summary>
	public Type EventHandlerType => typeof(T);

	/// <summary>
	/// Gets the kind of the event.
	/// </summary>
	public EventKind Kind => AddAccessor.MethodInfo.ToEventKind();

	/// <summary>
	/// Gets the visibility of the event.
	/// </summary>
	public Visibility Visibility => AddAccessor.Visibility;

	/// <summary>
	/// Gets the implementation strategy used to implement the event
	/// (<c>null</c>, if implementation callbacks are used).
	/// </summary>
	public IEventImplementation Implementation { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.EventBuilder"/> associated with the event.
	/// </summary>
	public EventBuilder EventBuilder { get; }

	/// <summary>
	/// Gets the 'add' accessor method.
	/// </summary>
	public IGeneratedMethod AddAccessor { get; }

	/// <summary>
	/// Gets the 'add' accessor method.
	/// </summary>
	IMethod IEvent.AddAccessor => AddAccessor;

	/// <summary>
	/// Gets the 'remove' accessor method.
	/// </summary>
	public IGeneratedMethod RemoveAccessor { get; }

	/// <summary>
	/// Gets the 'remove' accessor method.
	/// </summary>
	IMethod IEvent.RemoveAccessor => RemoveAccessor;
}
