///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Xunit;

// ReSharper disable MoveLocalFunctionAfterJumpStatement

namespace GriffinPlus.Lib.CodeGeneration.Tests;

using static Helpers;

/// <summary>
/// Common tests around the <see cref="ClassDefinition"/> class.
/// </summary>
public abstract class ClassDefinitionTests_ConcreteBaseClass_WithBaseClass<TInheritedTestClass> : ClassDefinitionTests_ConcreteBaseClass_Base
	where TInheritedTestClass : class
{
	#region Adding Events

	#region Test Data

	/// <summary>
	/// Test data for tests targeting <see cref="ClassDefinition.AddEventOverride{T}(IInheritedEvent{T},IEventImplementation)"/> method.
	/// </summary>
	public static IEnumerable<object[]> AddEventOverrideTestData
	{
		get
		{
			// get all events of the test base class
			EventInfo[] events = typeof(TInheritedTestClass).GetEvents(ExactDeclaredOnlyBindingFlags);

			foreach (EventInfo @event in events)
			{
				if (IsOverrideable(@event.AddMethod) || IsOverrideable(@event.RemoveMethod))
				{
					// the event is abstract, virtual or override
					// => it can be overridden
					foreach (bool addRaiser in new[] { false, true })
					{
						// all events in the test class are EventHandler<EventArgs>
						// => the generated event raiser always has the signature void OnEvent()
						yield return
						[
							@event.Name,                       // name of the event
							@event.ToVisibility(),             // visibility of the event
							@event.EventHandlerType,           // Type of the event handler
							addRaiser,                         // determines whether to add an event raiser method
							addRaiser ? typeof(void) : null,   // expected return type of the generated event raiser method
							addRaiser ? Type.EmptyTypes : null // expected parameter types of the generated event raiser method
						];
					}
				}
			}
			yield break;

			static bool IsOverrideable(MethodInfo method) => method != null && method.IsVirtual && !method.IsFinal && (method.IsPublic || method.IsFamily);
		}
	}

	#endregion

	#region AddEventOverride<T>(IInheritedEvent<T> eventToOverride, IEventImplementation<T> implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddEventOverride{T}(IInheritedEvent{T},IEventImplementation)"/> method
	/// using <see cref="TestEventImplementation"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to override.</param>
	/// <param name="expectedVisibility">Expected visibility of the overridden event.</param>
	/// <param name="expectedEventHandlerType">Expected type of the overridden event.</param>
	/// <param name="addEventRaiserMethod">
	/// <c>true</c> to add the event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="expectedEventRaiserReturnType">Expected return type of the generated event raiser method.</param>
	/// <param name="expectedEventRaiserParameterTypes">Expected parameter types of the generated event raiser method.</param>
	[Theory]
	[MemberData(nameof(AddEventOverrideTestData))]
	public void AddEventOverride_WithImplementationStrategy_Standard(
		string     name,
		Visibility expectedVisibility,
		Type       expectedEventHandlerType,
		bool       addEventRaiserMethod,
		Type       expectedEventRaiserReturnType,
		Type[]     expectedEventRaiserParameterTypes)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition();

		// create an instance of the implementation strategy
		const string eventRaiserName = "FireMyEvent";
		const Visibility eventRaiserVisibility = Visibility.Public;
		IEventImplementation implementation = addEventRaiserMethod
			                                      ? new TestEventImplementation(eventRaiserName, eventRaiserVisibility)
			                                      : new TestEventImplementation();

		// get the inherited event to override by its name
		IInheritedEvent eventToOverride = definition.InheritedEvents.SingleOrDefault(x => x.Name == name);
		Assert.NotNull(eventToOverride);

		// get the AddEventOverride(...) method to test
		MethodInfo addEventMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddEventOverride))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(eventToOverride.EventHandlerType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual(
					[
						typeof(IInheritedEvent<>).MakeGenericType(eventToOverride.EventHandlerType),
						typeof(IEventImplementation)
					]));

		// invoke the method to add the event to the type definition
		var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(definition, [eventToOverride, implementation]);
		Assert.NotNull(addedEvent);
		Assert.Equal(EventKind.Override, addedEvent.Kind);
		Assert.Equal(expectedVisibility, addedEvent.Visibility);
		Assert.Equal(expectedEventHandlerType, addedEvent.EventHandlerType);
		Assert.Same(implementation, addedEvent.Implementation);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the implementation of the event
		TestEventImplementation_Standard(
			definition,
			instance,
			eventToOverride.Name,
			EventKind.Override,
			expectedVisibility,
			eventToOverride.EventHandlerType,
			addEventRaiserMethod,
			eventRaiserName,
			expectedEventRaiserReturnType,
			expectedEventRaiserParameterTypes);
	}

	#endregion

	#region AddEventOverride<T>(IInheritedEvent<T>, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback) --- TODO!

	/// <summary>
	/// Tests the
	/// <see cref="ClassDefinition.AddEventOverride{T}(IInheritedEvent{T},EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/> method.
	/// </summary>
	/// <param name="name">Name of the event to override.</param>
	/// <param name="expectedVisibility">Expected visibility of the overridden event.</param>
	/// <param name="expectedEventHandlerType">Expected type of the overridden event.</param>
	/// <param name="addEventRaiserMethod">
	/// <c>true</c> to add the event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="expectedEventRaiserReturnType">Expected return type of the generated event raiser method.</param>
	/// <param name="expectedEventRaiserParameterTypes">Expected parameter types of the generated event raiser method.</param>
	[Theory]
	[MemberData(nameof(AddEventOverrideTestData))]
	public void AddEventOverride_WithImplementationCallbacks(
		string     name,
		Visibility expectedVisibility,
		Type       expectedEventHandlerType,
		bool       addEventRaiserMethod,
		Type       expectedEventRaiserReturnType,
		Type[]     expectedEventRaiserParameterTypes)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition();

		// add a backing field for the event's multicast delegate
		// (always of type EventHandler<EventArgs>, no need to test other types as this affects raising the event only)
		IGeneratedField backingField = definition.AddField(expectedEventHandlerType, name: null, Visibility.Public);

		// get the inherited event to override by its name
		IInheritedEvent eventToOverride = definition.InheritedEvents.SingleOrDefault(x => x.Name == name);
		Assert.NotNull(eventToOverride);

		// get the AddEventOverride(...) method to test
		MethodInfo addEventMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddEventOverride))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(eventToOverride.EventHandlerType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual(
					[
						typeof(IInheritedEvent<>).MakeGenericType(eventToOverride.EventHandlerType),
						typeof(EventAccessorImplementationCallback),
						typeof(EventAccessorImplementationCallback)
					]));

		// invoke the method to add the event to the type definition
		void ImplementGetAccessor(IGeneratedEvent @event, ILGenerator msilGenerator) => ImplementEventAccessor(@event, true, backingField.FieldBuilder, msilGenerator);
		void ImplementSetAccessor(IGeneratedEvent @event, ILGenerator msilGenerator) => ImplementEventAccessor(@event, false, backingField.FieldBuilder, msilGenerator);
		var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
			definition,
			[
				eventToOverride,
				(EventAccessorImplementationCallback)ImplementGetAccessor,
				(EventAccessorImplementationCallback)ImplementSetAccessor
			]);
		Assert.NotNull(addedEvent);
		Assert.Equal(EventKind.Override, addedEvent.Kind);
		Assert.Equal(expectedVisibility, addedEvent.Visibility);
		Assert.Equal(expectedEventHandlerType, addedEvent.EventHandlerType);
		Assert.Null(addedEvent.Implementation);

		// add an event raiser method to the type definition
		// (should always just be: public void FireMyEvent();
		const MethodKind kind = MethodKind.Normal;
		const string eventRaiserName = "FireMyEvent";
		Type eventRaiserReturnType = typeof(void);
		Type[] eventRaiserParameterTypes = [];
		const Visibility eventRaiserVisibility = Visibility.Public;
		if (addEventRaiserMethod)
		{
			definition.AddMethod(
				kind,
				eventRaiserName,
				eventRaiserReturnType,
				eventRaiserParameterTypes,
				eventRaiserVisibility,
				(method, msilGenerator) => ImplementEventRaiserMethod(
					method,
					backingField.FieldBuilder,
					addedEvent,
					msilGenerator));
		}

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the implementation of the event
		// (the implemented event should behave the same as a standard event implementation, use the same test code...)
		TestEventImplementation_Standard(
			definition,
			instance,
			eventToOverride.Name,
			EventKind.Override,
			expectedVisibility,
			eventToOverride.EventHandlerType,
			addEventRaiserMethod,
			addEventRaiserMethod ? eventRaiserName : null,
			addEventRaiserMethod ? expectedEventRaiserReturnType : null,
			addEventRaiserMethod ? expectedEventRaiserParameterTypes : null);
	}

	#endregion

	#endregion

	#region Adding Properties (TODO, override test cases missing)

	#region AddPropertyOverride<T>(IInheritedProperty<T> property, IPropertyImplementation implementation) --- TODO!

	#endregion

	#region AddPropertyOverride(IInheritedProperty property, IPropertyImplementation implementation) --- TODO!

	#endregion

	#region AddPropertyOverride<T>(IInheritedProperty<T> property, PropertyAccessorImplementationCallback getAccessorImplementationCallback, PropertyAccessorImplementationCallback setAccessorImplementationCallback) --- TODO!

	#endregion

	#region AddPropertyOverride(IInheritedProperty property, PropertyAccessorImplementationCallback getAccessorImplementationCallback, PropertyAccessorImplementationCallback setAccessorImplementationCallback) --- TODO!

	#endregion

	#endregion
}
