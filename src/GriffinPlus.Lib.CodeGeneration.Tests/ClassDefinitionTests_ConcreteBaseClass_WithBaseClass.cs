///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// Test data for tests targeting event overrides.
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
	public void AddEventOverrideT_WithImplementationStrategy(
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
		MethodInfo addEventOverrideMethod = typeof(ClassDefinition)
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
		var addedEvent = (IGeneratedEvent)addEventOverrideMethod.Invoke(definition, [eventToOverride, implementation]);
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
		TestEventImplementation(
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

	#region AddEventOverride(IInheritedEvent eventToOverride, IEventImplementation implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddEventOverride(IInheritedEvent,IEventImplementation)"/> method
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
	public void AddEventOverride_WithImplementationStrategy(
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
		MethodInfo addEventOverrideMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddEventOverride))
			.Where(method => method.GetGenericArguments().Length == 0)
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual(
					[
						typeof(IInheritedEvent),
						typeof(IEventImplementation)
					]));

		// invoke the method to add the event to the type definition
		var addedEvent = (IGeneratedEvent)addEventOverrideMethod.Invoke(definition, [eventToOverride, implementation]);
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
		TestEventImplementation(
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

	#region AddEventOverride<T>(IInheritedEvent<T> eventToOverride, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)

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
	public void AddEventOverrideT_WithImplementationCallbacks(
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
		// (it is not used as a whole, but parts of it => eliminates duplicate test code)
		var implementation = new TestEventImplementation();

		// get the inherited event to override by its name
		IInheritedEvent eventToOverride = definition.InheritedEvents.SingleOrDefault(x => x.Name == name);
		Assert.NotNull(eventToOverride);

		// get the AddEventOverride(...) method to test
		MethodInfo addEventOverrideMethod = typeof(ClassDefinition)
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
		void ImplementGetAccessor(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementAddAccessorMethod(definition, @event, msilGenerator);
		void ImplementSetAccessor(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementRemoveAccessorMethod(definition, @event, msilGenerator);
		var addedEvent = (IGeneratedEvent)addEventOverrideMethod.Invoke(
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

		// declare the backing field
		implementation.Declare(definition, addedEvent);

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
				(_, msilGenerator) => implementation.ImplementRaiserMethod(definition, addedEvent, msilGenerator));
		}

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the implementation of the event
		// (the implemented event should behave the same as a standard event implementation, use the same test code...)
		TestEventImplementation(
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

	#region AddEventOverride(IInheritedEvent eventToOverride, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)

	/// <summary>
	/// Tests the
	/// <see cref="ClassDefinition.AddEventOverride(IInheritedEvent,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/> method.
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

		// create an instance of the implementation strategy
		// (it is not used as a whole, but parts of it => eliminates duplicate test code)
		var implementation = new TestEventImplementation();

		// get the inherited event to override by its name
		IInheritedEvent eventToOverride = definition.InheritedEvents.SingleOrDefault(x => x.Name == name);
		Assert.NotNull(eventToOverride);

		// get the AddEventOverride(...) method to test
		MethodInfo addEventOverrideMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddEventOverride))
			.Where(method => method.GetGenericArguments().Length == 0)
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual(
					[
						typeof(IInheritedEvent),
						typeof(EventAccessorImplementationCallback),
						typeof(EventAccessorImplementationCallback)
					]));

		// invoke the method to add the event to the type definition
		void ImplementGetAccessor(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementAddAccessorMethod(definition, @event, msilGenerator);
		void ImplementSetAccessor(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementRemoveAccessorMethod(definition, @event, msilGenerator);
		var addedEvent = (IGeneratedEvent)addEventOverrideMethod.Invoke(
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

		// declare backing field
		implementation.Declare(definition, addedEvent);

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
				(_, msilGenerator) => implementation.ImplementRaiserMethod(definition, addedEvent, msilGenerator));
		}

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the implementation of the event
		// (the implemented event should behave the same as a standard event implementation, use the same test code...)
		TestEventImplementation(
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

	#region Adding Properties

	#region Test Data

	/// <summary>
	/// Test data for tests targeting property overrides.
	/// </summary>
	public static IEnumerable<object[]> AddPropertyOverrideTestData
	{
		get
		{
			// get all events of the test base class
			PropertyInfo[] properties = typeof(TInheritedTestClass).GetProperties(ExactBindingFlags);

			foreach (PropertyInfo property in properties)
			{
				if (IsOverrideable(property.GetMethod) || IsOverrideable(property.SetMethod))
				{
					// the property is abstract, virtual or override
					// => it can be overridden
					object[] testObjects;
					if (property.PropertyType == typeof(int)) testObjects = [1, 2, 3];
					else if (property.PropertyType == typeof(string)) testObjects = ["A", "B", "C"];
					else
					{
						throw new NotSupportedException(
							$"The property type is not supported, yet. Add test objects in {nameof(ClassDefinitionTests_ConcreteBaseClass_WithBaseClass<TInheritedTestClass>)}.{nameof(AddPropertyOverrideTestData)}, please.");
					}

					yield return
					[
						property.Name,                      // name of the property
						property.PropertyType,              // Type of the property
						property.GetMethod?.ToVisibility(), // visibility of the get accessor
						property.SetMethod?.ToVisibility(), // visibility of the set accessor
						testObjects
					];
				}
			}
		}
	}

	#endregion

	#region AddPropertyOverride<T>(IInheritedProperty<T> property, IPropertyImplementation implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddPropertyOverride{T}(IInheritedProperty{T},IPropertyImplementation)"/> method
	/// using <see cref="PropertyImplementation_TestDataStorage"/> to implement get/set accessors.
	/// </summary>
	/// <param name="name">Name of the property to override.</param>
	/// <param name="expectedPropertyType">Expected type of the property.</param>
	/// <param name="expectedGetAccessorVisibility">
	/// Expected visibility of the overridden get accessor (<c>null</c> if the property does not have a get accessor).
	/// </param>
	/// <param name="expectedSetAccessorVisibility">
	/// Expected visibility of the overridden set accessor (<c>null</c> if the property does not have a set accessor).
	/// </param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyOverrideTestData))]
	public void AddPropertyOverrideT_WithImplementationStrategy(
		string      name,
		Type        expectedPropertyType,
		Visibility? expectedGetAccessorVisibility,
		Visibility? expectedSetAccessorVisibility,
		object[]    testObjects)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition();

		// get the inherited property to override by its name
		IInheritedProperty propertyToOverride = definition.InheritedProperties.SingleOrDefault(x => x.Name == name);
		Assert.NotNull(propertyToOverride);

		// get the AddPropertyOverride(...) method to test
		MethodInfo addPropertyOverrideMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddPropertyOverride))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyToOverride.PropertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual(
					[
						typeof(IInheritedProperty<>).MakeGenericType(propertyToOverride.PropertyType),
						typeof(IPropertyImplementation)
					]));
		Debug.Assert(addPropertyOverrideMethod != null);

		// create the implementation strategy
		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		var implementation = new PropertyImplementation_TestDataStorage(handle);

		// invoke the method to add the property to the type definition
		var addedProperty = (IGeneratedProperty)addPropertyOverrideMethod.Invoke(definition, [propertyToOverride, implementation]);
		Assert.NotNull(addedProperty);
		Assert.Equal(PropertyKind.Override, addedProperty.Kind);
		Assert.Equal(expectedGetAccessorVisibility, addedProperty.GetAccessor?.Visibility);
		Assert.Equal(expectedSetAccessorVisibility, addedProperty.SetAccessor?.Visibility);
		Assert.Equal(expectedPropertyType, addedProperty.PropertyType);
		Assert.Same(implementation, addedProperty.Implementation);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the property
		TestPropertyImplementation_TestDataStorage(
			definition,
			instance,
			name,
			expectedGetAccessorVisibility,
			expectedSetAccessorVisibility,
			PropertyKind.Override,
			expectedPropertyType,
			testObjects,
			handle);
	}

	#endregion

	#region AddPropertyOverride(IInheritedProperty property, IPropertyImplementation implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddPropertyOverride(IInheritedProperty,IPropertyImplementation)"/> method
	/// using <see cref="PropertyImplementation_TestDataStorage"/> to implement get/set accessors.
	/// </summary>
	/// <param name="name">Name of the property to override.</param>
	/// <param name="expectedPropertyType">Expected type of the property.</param>
	/// <param name="expectedGetAccessorVisibility">
	/// Expected visibility of the overridden get accessor (<c>null</c> if the property does not have a get accessor).
	/// </param>
	/// <param name="expectedSetAccessorVisibility">
	/// Expected visibility of the overridden set accessor (<c>null</c> if the property does not have a set accessor).
	/// </param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyOverrideTestData))]
	public void AddPropertyOverride_WithImplementationStrategy(
		string      name,
		Type        expectedPropertyType,
		Visibility? expectedGetAccessorVisibility,
		Visibility? expectedSetAccessorVisibility,
		object[]    testObjects)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition();

		// get the inherited property to override by its name
		IInheritedProperty propertyToOverride = definition.InheritedProperties.SingleOrDefault(x => x.Name == name);
		Assert.NotNull(propertyToOverride);

		// create the implementation strategy
		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		var implementation = new PropertyImplementation_TestDataStorage(handle);

		// invoke the method to add the property to the type definition
		IGeneratedProperty addedProperty = definition.AddPropertyOverride(propertyToOverride, implementation);
		Assert.NotNull(addedProperty);
		Assert.Equal(PropertyKind.Override, addedProperty.Kind);
		Assert.Equal(expectedGetAccessorVisibility, addedProperty.GetAccessor?.Visibility);
		Assert.Equal(expectedSetAccessorVisibility, addedProperty.SetAccessor?.Visibility);
		Assert.Equal(expectedPropertyType, addedProperty.PropertyType);
		Assert.Same(implementation, addedProperty.Implementation);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the property
		TestPropertyImplementation_TestDataStorage(
			definition,
			instance,
			name,
			expectedGetAccessorVisibility,
			expectedSetAccessorVisibility,
			PropertyKind.Override,
			expectedPropertyType,
			testObjects,
			handle);
	}

	#endregion

	#region AddPropertyOverride<T>(IInheritedProperty<T> property, PropertyAccessorImplementationCallback getAccessorImplementationCallback, PropertyAccessorImplementationCallback setAccessorImplementationCallback)

	/// <summary>
	/// Tests the
	/// <see
	///     cref="ClassDefinition.AddPropertyOverride{T}(IInheritedProperty{T},PropertyAccessorImplementationCallback,PropertyAccessorImplementationCallback)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the property to override.</param>
	/// <param name="expectedPropertyType">Expected type of the property.</param>
	/// <param name="expectedGetAccessorVisibility">
	/// Expected visibility of the overridden get accessor (<c>null</c> if the property does not have a get accessor).
	/// </param>
	/// <param name="expectedSetAccessorVisibility">
	/// Expected visibility of the overridden set accessor (<c>null</c> if the property does not have a set accessor).
	/// </param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyOverrideTestData))]
	public void AddPropertyOverrideT_WithImplementationCallbacks(
		string      name,
		Type        expectedPropertyType,
		Visibility? expectedGetAccessorVisibility,
		Visibility? expectedSetAccessorVisibility,
		object[]    testObjects)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition();

		// get the inherited property to override by its name
		IInheritedProperty propertyToOverride = definition.InheritedProperties.SingleOrDefault(x => x.Name == name);
		Assert.NotNull(propertyToOverride);

		// get the AddPropertyOverride(...) method to test
		MethodInfo addPropertyOverrideMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddPropertyOverride))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyToOverride.PropertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual(
					[
						typeof(IInheritedProperty<>).MakeGenericType(propertyToOverride.PropertyType),
						typeof(PropertyAccessorImplementationCallback),
						typeof(PropertyAccessorImplementationCallback)
					]));
		Debug.Assert(addPropertyOverrideMethod != null);

		// create the implementation strategy
		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		_ = new PropertyImplementation_TestDataStorage(handle);

		// invoke the method to add the property to the type definition
		void GetAccessorImplementationCallback(IGeneratedProperty p, ILGenerator g) => EmitPropertyGetAccessorWithTestDataStorageCallback(p, handle, g);
		void SetAccessorImplementationCallback(IGeneratedProperty p, ILGenerator g) => EmitPropertySetAccessorWithTestDataStorageCallback(p, handle, g);
		var addedProperty = (IGeneratedProperty)addPropertyOverrideMethod.Invoke(
			definition,
			[
				propertyToOverride,
				(PropertyAccessorImplementationCallback)GetAccessorImplementationCallback,
				(PropertyAccessorImplementationCallback)SetAccessorImplementationCallback
			]);
		Assert.NotNull(addedProperty);
		Assert.Equal(PropertyKind.Override, addedProperty.Kind);
		Assert.Equal(expectedGetAccessorVisibility, addedProperty.GetAccessor?.Visibility);
		Assert.Equal(expectedSetAccessorVisibility, addedProperty.SetAccessor?.Visibility);
		Assert.Equal(expectedPropertyType, addedProperty.PropertyType);
		Assert.Null(addedProperty.Implementation);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the property
		TestPropertyImplementation_TestDataStorage(
			definition,
			instance,
			name,
			expectedGetAccessorVisibility,
			expectedSetAccessorVisibility,
			PropertyKind.Override,
			expectedPropertyType,
			testObjects,
			handle);
	}

	#endregion

	#region AddPropertyOverride(IInheritedProperty property, PropertyAccessorImplementationCallback getAccessorImplementationCallback, PropertyAccessorImplementationCallback setAccessorImplementationCallback)

	/// <summary>
	/// Tests the
	/// <see cref="ClassDefinition.AddPropertyOverride(IInheritedProperty,PropertyAccessorImplementationCallback,PropertyAccessorImplementationCallback)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the property to override.</param>
	/// <param name="expectedPropertyType">Expected type of the property.</param>
	/// <param name="expectedGetAccessorVisibility">
	/// Expected visibility of the overridden get accessor (<c>null</c> if the property does not have a get accessor).
	/// </param>
	/// <param name="expectedSetAccessorVisibility">
	/// Expected visibility of the overridden set accessor (<c>null</c> if the property does not have a set accessor).
	/// </param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyOverrideTestData))]
	public void AddPropertyOverride_WithImplementationCallbacks(
		string      name,
		Type        expectedPropertyType,
		Visibility? expectedGetAccessorVisibility,
		Visibility? expectedSetAccessorVisibility,
		object[]    testObjects)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition();

		// get the inherited property to override by its name
		IInheritedProperty propertyToOverride = definition.InheritedProperties.SingleOrDefault(x => x.Name == name);
		Assert.NotNull(propertyToOverride);

		// create the implementation strategy
		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		_ = new PropertyImplementation_TestDataStorage(handle);

		// invoke the method to add the property to the type definition
		void GetAccessorImplementationCallback(IGeneratedProperty p, ILGenerator g) => EmitPropertyGetAccessorWithTestDataStorageCallback(p, handle, g);
		void SetAccessorImplementationCallback(IGeneratedProperty p, ILGenerator g) => EmitPropertySetAccessorWithTestDataStorageCallback(p, handle, g);
		IGeneratedProperty addedProperty = definition.AddPropertyOverride(
			propertyToOverride,
			GetAccessorImplementationCallback,
			SetAccessorImplementationCallback);
		Assert.NotNull(addedProperty);
		Assert.Equal(PropertyKind.Override, addedProperty.Kind);
		Assert.Equal(expectedGetAccessorVisibility, addedProperty.GetAccessor?.Visibility);
		Assert.Equal(expectedSetAccessorVisibility, addedProperty.SetAccessor?.Visibility);
		Assert.Equal(expectedPropertyType, addedProperty.PropertyType);
		Assert.Null(addedProperty.Implementation);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the property
		TestPropertyImplementation_TestDataStorage(
			definition,
			instance,
			name,
			expectedGetAccessorVisibility,
			expectedSetAccessorVisibility,
			PropertyKind.Override,
			expectedPropertyType,
			testObjects,
			handle);
	}

	#endregion

	#endregion

	#region Adding Methods

	#region Test Data

	/// <summary>
	/// Test data for tests targeting adding new methods.
	/// </summary>
	public static IEnumerable<object[]> AddMethodOverrideTestData
	{
		get
		{
			// get all events of the test base class
			MethodInfo[] methods = typeof(TInheritedTestClass).GetMethods(ExactBindingFlags);

			foreach (MethodInfo method in methods.Where(method => IsOverrideable(method) && !method.IsSpecialName && method.DeclaringType != typeof(object)))
			{
				// the method is abstract, virtual or override
				// => it can be overridden

				// prepare some arguments for testing the method
				// (all parameter types and the return type must be the same type, otherwise the simple implementation crashes...)
				CreateMethodTestData(
					method.ReturnType,
					method.GetParameters().Length,
					out Type[] parameterTypes,
					out object[] testArguments,
					out object expectedTestResult);

				Debug.Assert(parameterTypes.SequenceEqual(method.GetParameters().Select(x => x.ParameterType).ToArray()));

				yield return
				[
					method.Name,
					method.ReturnType,
					parameterTypes,
					method.ToVisibility(),
					testArguments,
					expectedTestResult
				];
			}
		}
	}

	#endregion

	#region AddMethodOverride(IInheritedMethod methodToOverride, IMethodImplementation implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddMethodOverride(IInheritedMethod,IMethodImplementation)"/> method
	/// using <see cref="TestMethodImplementation"/> to implement it.
	/// </summary>
	/// <param name="name">Name of the method to override.</param>
	/// <param name="expectedReturnType">Expected return type of the overridden method.</param>
	/// <param name="expectedParameterTypes">Expected parameter types of the overridden method.</param>
	/// <param name="expectedVisibility">Expected visibility of the overridden method.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodOverrideTestData))]
	public void AddMethodOverride_WithImplementationStrategy(
		string     name,
		Type       expectedReturnType,
		Type[]     expectedParameterTypes,
		Visibility expectedVisibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		AddMethodOverride_Common(
			name,
			expectedReturnType,
			expectedParameterTypes,
			expectedVisibility,
			testArguments,
			expectedTestResult,
			(definition, inheritedMethod) => definition.AddMethodOverride(inheritedMethod, new TestMethodImplementation()));
	}

	#endregion

	#region AddMethodOverride(IInheritedMethod methodToOverride, MethodImplementationCallback implementationCallback)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddMethodOverride(IInheritedMethod,MethodImplementationCallback)"/> method.
	/// </summary>
	/// <param name="name">Name of the method to override.</param>
	/// <param name="expectedReturnType">Expected return type of the overridden method.</param>
	/// <param name="expectedParameterTypes">Expected parameter types of the overridden method.</param>
	/// <param name="expectedVisibility">Expected visibility of the overridden method.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodOverrideTestData))]
	public void AddMethodOverride_WithImplementationCallback(
		string     name,
		Type       expectedReturnType,
		Type[]     expectedParameterTypes,
		Visibility expectedVisibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		AddMethodOverride_Common(
			name,
			expectedReturnType,
			expectedParameterTypes,
			expectedVisibility,
			testArguments,
			expectedTestResult,
			(definition, inheritedMethod) => definition.AddMethodOverride(inheritedMethod, TestMethodImplementation.Callback));
	}

	#endregion

	#region Common Test Code

	/// <summary>
	/// Common test code for methods the override an inherited method.
	/// </summary>
	/// <param name="name">Name of the method to override.</param>
	/// <param name="expectedReturnType">Expected return type of the overridden method.</param>
	/// <param name="expectedParameterTypes">Expected parameter types of the overridden method.</param>
	/// <param name="expectedVisibility">Expected visibility of the overridden method.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	/// <param name="addMethodAction">Callback that actually adds the override to the type definition.</param>
	private void AddMethodOverride_Common(
		string                                                    name,
		Type                                                      expectedReturnType,
		Type[]                                                    expectedParameterTypes,
		Visibility                                                expectedVisibility,
		object[]                                                  testArguments,
		object                                                    expectedTestResult,
		Func<ClassDefinition, IInheritedMethod, IGeneratedMethod> addMethodAction)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition(null);

		// get the inherited method to override
		IInheritedMethod inheritedMethod = definition
			.InheritedMethods
			.SingleOrDefault(method => method.Name == name);
		Assert.NotNull(inheritedMethod);

		// add the method to the definition
		IGeneratedMethod addedMethod = addMethodAction(definition, inheritedMethod);
		Assert.NotNull(addedMethod);
		AssertMethodName(name, addedMethod.Name);
		Assert.Equal(MethodKind.Override, addedMethod.Kind);
		Assert.Equal(expectedReturnType, addedMethod.ReturnType);
		Assert.Equal(expectedParameterTypes, addedMethod.ParameterTypes);
		Assert.Equal(expectedVisibility, addedMethod.Visibility);
		Assert.Equal((MethodAttributes)0, addedMethod.AdditionalAttributes);
		Assert.Equal(expectedVisibility.ToMethodAttributes() | MethodKind.Override.ToMethodAttributes() | MethodAttributes.HideBySig, addedMethod.Attributes);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the method implementation
		// test the method implementation
		TestMethodImplementation(
			definition,             // type definition with the method to test
			instance,               // instance of the test class providing the method to test
			name,                   // name of the method to test
			MethodKind.Override,    // expected kind of the method
			expectedVisibility,     // expected visibility of the method
			expectedReturnType,     // expected return type of the method
			expectedParameterTypes, // expected parameter types of the method
			testArguments,          // arguments to pass to the method when testing it
			expectedTestResult);    // result the tested method is expected to return
	}

	#endregion

	#endregion
}
