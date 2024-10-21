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
public abstract class ClassDefinitionTests_ConcreteBaseClass_Base : TypeDefinitionTests_Common<ClassDefinition>
{
	#region AddPassThroughConstructors()

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddPassThroughConstructors"/> method.
	/// </summary>
	[Fact]
	public void AddPassThroughConstructors()
	{
		// create a class definition deriving from the base class and creating pass-through constructors
		ClassDefinition definition = CreateTypeDefinition();
		definition.AddPassThroughConstructors();
		Type type = definition.CreateType();

		// check whether the expected constructors have been added
		CheckTypeAgainstDefinition(type, definition);

		// try to get the generated constructors of our test base class
		const BindingFlags bindingFlags = ExactDeclaredOnlyBindingFlags & ~BindingFlags.Static;
		ConstructorInfo constructor1 = type.GetConstructor(bindingFlags, Type.DefaultBinder, [], null);
		ConstructorInfo constructor2 = type.GetConstructor(bindingFlags, Type.DefaultBinder, [typeof(ParameterType_Public)], null);
		ConstructorInfo constructor3 = type.GetConstructor(bindingFlags, Type.DefaultBinder, [typeof(ParameterType_ProtectedInternal)], null);
		ConstructorInfo constructor4 = type.GetConstructor(bindingFlags, Type.DefaultBinder, [typeof(ParameterType_Protected)], null);
		ConstructorInfo constructor5 = type.GetConstructor(bindingFlags, Type.DefaultBinder, [typeof(ParameterType_Internal)], null);
		ConstructorInfo constructor6 = type.GetConstructor(bindingFlags, Type.DefaultBinder, [typeof(ParameterType_Private)], null);
		ConstructorInfo constructor7 = type.GetConstructor(bindingFlags, Type.DefaultBinder, [typeof(int)], null);
		ConstructorInfo constructor8 = type.GetConstructor(bindingFlags, Type.DefaultBinder, [typeof(string)], null);

		if (type.BaseType == typeof(object))
		{
			// the generated class does not have a base class,
			// respectively the base class is System.Object

			// test constructor 1: public default constructor
			// (this is the only constructor System.Object provides)
			Assert.NotNull(constructor1);
			object instance1 = constructor1.Invoke([]);
			Assert.NotNull(instance1);

			// the other constructors expected from the test base classes should not exist
			// as System.Object does not provide them
			Assert.Null(constructor2); // public constructor
			Assert.Null(constructor3); // protected internal constructor
			Assert.Null(constructor4); // protected constructor
			Assert.Null(constructor5); // internal constructor
			Assert.Null(constructor6); // private constructor
			Assert.Null(constructor7); // public constructor with int argument (value type)
			Assert.Null(constructor8); // public constructor with string argument (reference type)
		}
		else
		{
			// the generated class has a base class

			// test constructor 1: public default constructor
			Assert.NotNull(constructor1);
			var instance1 = (ITestBaseClass)constructor1.Invoke([]);
			Assert.Null(instance1.ConstructorArgument);

			// test constructor 2: public constructor with enum argument
			Assert.NotNull(constructor2);
			var instance2 = (ITestBaseClass)constructor2.Invoke([ParameterType_Public.Value]);
			Assert.Equal(ParameterType_Public.Value, instance2.ConstructorArgument);

			// test constructor 3: protected internal constructor with enum argument
			Assert.NotNull(constructor3);
			var instance3 = (ITestBaseClass)constructor3.Invoke([ParameterType_ProtectedInternal.Value]);
			Assert.Equal(ParameterType_ProtectedInternal.Value, instance3.ConstructorArgument);

			// test constructor 4: protected constructor with enum argument
			Assert.NotNull(constructor4);
			var instance4 = (ITestBaseClass)constructor4.Invoke([ParameterType_Protected.Value]);
			Assert.Equal(ParameterType_Protected.Value, instance4.ConstructorArgument);

			// test constructor 5: internal constructor with enum argument
			// (should not have been generated due to accessibility reasons)
			Assert.Null(constructor5);

			// test constructor 6: private constructor with enum argument
			// (should not have been generated due to accessibility reasons)
			Assert.Null(constructor6);

			// test constructor 7: public constructor with an int argument (value type)
			Assert.NotNull(constructor7);
			var instance7 = (ITestBaseClass)constructor7.Invoke([42]);
			Assert.Equal(42, instance7.ConstructorArgument);

			// test constructor 8: public constructor with a string argument (reference type)
			Assert.NotNull(constructor8);
			var instance8 = (ITestBaseClass)constructor8.Invoke(["Test"]);
			Assert.Equal("Test", instance8.ConstructorArgument);
		}
	}

	#endregion

	#region Adding Events

	#region Test Data

	/// <summary>
	/// Test data for tests targeting <see cref="ClassDefinition.AddAbstractEvent{T}(string,Visibility)"/>.
	/// </summary>
	public static IEnumerable<object[]> AddAbstractEventTestData
	{
		get
		{
			// ------------------------------------------------------------------------------------
			// different event names
			// ------------------------------------------------------------------------------------
			foreach (string name in MethodNames)
			{
				yield return
				[
					name,                // name
					Visibility.Public,   // visibility
					typeof(EventHandler) // event handler type
				];
			}

			// ------------------------------------------------------------------------------------
			// different visibilities
			// ------------------------------------------------------------------------------------

			foreach (Visibility visibility in Visibilities)
			{
				yield return
				[
					null,                // name (random)
					visibility,          // visibility
					typeof(EventHandler) // event handler type
				];
			}

			// ------------------------------------------------------------------------------------
			// different types
			// ------------------------------------------------------------------------------------

			// System.EventHandler (covered above)
			//yield return
			//[
			//	null,                // name (random)
			//	Visibility.Public,   // visibility
			//	typeof(EventHandler) // event handler type
			//];

			// System.EventHandler<EventArgs> (covered above)
			//yield return
			//[
			//	null,                           // name (random)
			//	Visibility.Public,              // visibility
			//	typeof(EventHandler<EventArgs>) // event handler type
			//];

			// System.EventHandler<EventArgs>
			yield return
			[
				null,                                      // name (random)
				Visibility.Public,                         // visibility
				typeof(EventHandler<SpecializedEventArgs>) // event handler type
			];

			// System.Action
			yield return
			[
				null,              // name (random)
				Visibility.Public, // visibility
				typeof(Action)     // event handler type
			];

			// System.Action<int>
			yield return
			[
				null,               // name (random)
				Visibility.Public,  // visibility
				typeof(Action<int>) // event handler type
			];

			// System.Func<int>
			yield return
			[
				null,              // name (random)
				Visibility.Public, // visibility
				typeof(Func<int>)  // event handler type
			];

			// System.Func<int,long>
			yield return
			[
				null,                   // name (random)
				Visibility.Public,      // visibility
				typeof(Func<int, long>) // event handler type
			];
		}
	}

	#endregion

	#region AddAbstractEvent<T>(string name, Visibility visibility)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddAbstractEvent{T}(string,Visibility)"/> method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	[Theory]
	[MemberData(nameof(AddAbstractEventTestData))]
	public void AddAbstractEventT(
		string     name,
		Visibility visibility,
		Type       eventHandlerType)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition(null, TypeAttributes.Abstract);

		// get the AddAbstractEvent(...) method to test
		MethodInfo addEventMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddAbstractEvent))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(eventHandlerType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(Visibility)]));

		// invoke the method to add the event to the type definition
		var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
			definition,
			[name, visibility]);
		Assert.NotNull(addedEvent);
		Assert.Equal(EventKind.Abstract, addedEvent.Kind);
		Assert.Equal(visibility, addedEvent.Visibility);
		Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
		Assert.Null(addedEvent.Implementation);

		// create the defined type and check the result against the definition
		// (creating an instance of that type is not possible as it contains an abstract member)
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
	}

	#endregion

	#region AddAbstractEvent(Type type, string name, Visibility visibility)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddAbstractEvent(Type,string,Visibility)"/> method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	[Theory]
	[MemberData(nameof(AddAbstractEventTestData))]
	public void AddAbstractEvent(
		string     name,
		Visibility visibility,
		Type       eventHandlerType)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition(null, TypeAttributes.Abstract);

		// add the event to the type definition
		IGeneratedEvent addedEvent = definition.AddAbstractEvent(eventHandlerType, name, visibility);
		Assert.NotNull(addedEvent);
		Assert.Equal(EventKind.Abstract, addedEvent.Kind);
		Assert.Equal(visibility, addedEvent.Visibility);
		Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
		Assert.Null(addedEvent.Implementation);

		// create the defined type and check the result against the definition
		// (creating an instance of that type is not possible as it contains an abstract member)
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
	}

	#endregion

	#region AddVirtualEvent<T>(string name, Visibility visibility, IEventImplementation{T} implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddVirtualEvent{T}(string,Visibility,IEventImplementation)"/> method
	/// using <see cref="TestEventImplementation"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	/// <param name="letStrategyAddEventRaiserMethod">
	/// <c>true</c> to let the strategy add the event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
	/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
	/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
	/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationStrategy))]
	public void AddVirtualEventT_WithImplementationStrategy(
		string     name,
		Visibility visibility,
		Type       eventHandlerType,
		bool       letStrategyAddEventRaiserMethod,
		string     eventRaiserName,
		Visibility eventRaiserVisibility,
		Type       expectedEventRaiserReturnType,
		Type[]     expectedEventRaiserParameterTypes)
	{
		TestAddEvent_WithImplementationStrategy(
			name: name,
			kind: EventKind.Virtual,
			visibility: visibility,
			letStrategyAddEventRaiserMethod: letStrategyAddEventRaiserMethod,
			eventRaiserName: eventRaiserName,
			eventRaiserVisibility: eventRaiserVisibility,
			eventHandlerType: eventHandlerType,
			expectedEventRaiserReturnType: expectedEventRaiserReturnType,
			expectedEventRaiserParameterTypes: expectedEventRaiserParameterTypes,
			addEventCallback: (definition, implementation) =>
			{
				// get the AddEvent(...) method to test
				MethodInfo addEventMethod = typeof(ClassDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(ClassDefinition.AddVirtualEvent))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(eventHandlerType))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual([typeof(string), typeof(Visibility), typeof(IEventImplementation)]));

				// add the event to the type definition
				var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(definition, [name, visibility, implementation]);
				Assert.NotNull(addedEvent);
				Assert.Same(implementation, addedEvent.Implementation);

				return addedEvent;
			});
	}

	#endregion

	#region AddVirtualEvent(Type type, string name, Visibility visibility, IEventImplementation implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddVirtualEvent(Type,string,Visibility,IEventImplementation)"/> method
	/// using <see cref="TestEventImplementation"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	/// <param name="letStrategyAddEventRaiserMethod">
	/// <c>true</c> to let the strategy add the event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
	/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
	/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
	/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationStrategy))]
	public void AddVirtualEvent_WithImplementationStrategy(
		string     name,
		Visibility visibility,
		Type       eventHandlerType,
		bool       letStrategyAddEventRaiserMethod,
		string     eventRaiserName,
		Visibility eventRaiserVisibility,
		Type       expectedEventRaiserReturnType,
		Type[]     expectedEventRaiserParameterTypes)
	{
		TestAddEvent_WithImplementationStrategy(
			name: name,
			kind: EventKind.Virtual,
			eventHandlerType: eventHandlerType,
			visibility: visibility,
			letStrategyAddEventRaiserMethod: letStrategyAddEventRaiserMethod,
			eventRaiserName: eventRaiserName,
			eventRaiserVisibility: eventRaiserVisibility,
			expectedEventRaiserReturnType: expectedEventRaiserReturnType,
			expectedEventRaiserParameterTypes: expectedEventRaiserParameterTypes,
			addEventCallback: (definition, implementation) =>
			{
				IGeneratedEvent addedEvent = definition.AddVirtualEvent(eventHandlerType, name, visibility, implementation);
				Assert.NotNull(addedEvent);
				Assert.Same(implementation, addedEvent.Implementation);

				return addedEvent;
			});
	}

	#endregion

	#region AddVirtualEvent<T>(string name, Visibility visibility, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddVirtualEvent{T}(string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationCallbacks))]
	public void AddVirtualEventT_WithImplementationCallbacks(
		string     name,
		Visibility visibility,
		Type       eventHandlerType)
	{
		TestAddEvent_WithImplementationCallbacks(
			name: name,
			kind: EventKind.Virtual,
			eventHandlerType: eventHandlerType,
			visibility: visibility,
			addEventCallback: (definition, implementation) =>
			{
				// prepare callback to add the 'add' accessor and the 'remove' accessor
				// (the callbacks implement the standard event behavior to allow re-using test code for the 'standard' event implementation strategy)
				void ImplementAddAccessorCallback(IGeneratedEvent    @event, ILGenerator msilGenerator) => implementation.ImplementAddAccessorMethod(definition, @event, msilGenerator);
				void ImplementRemoveAccessorCallback(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementRemoveAccessorMethod(definition, @event, msilGenerator);

				// get the AddEvent<T>(...) method to test
				MethodInfo addEventMethod = typeof(ClassDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(ClassDefinition.AddVirtualEvent))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(eventHandlerType))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual([typeof(string), typeof(Visibility), typeof(EventAccessorImplementationCallback), typeof(EventAccessorImplementationCallback)]));

				// invoke the method to add the event to the type definition
				var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
					definition,
					[
						name,
						visibility,
						(EventAccessorImplementationCallback)ImplementAddAccessorCallback,
						(EventAccessorImplementationCallback)ImplementRemoveAccessorCallback
					]);

				Assert.NotNull(addedEvent);
				return addedEvent;
			});
	}

	#endregion

	#region AddVirtualEvent(Type type, string name, Visibility visibility, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)

	/// <summary>
	/// Tests the
	/// <see cref="ClassDefinition.AddVirtualEvent(Type,string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationCallbacks))]
	public void AddVirtualEvent_WithImplementationCallbacks(
		string     name,
		Visibility visibility,
		Type       eventHandlerType)
	{
		TestAddEvent_WithImplementationCallbacks(
			name: name,
			kind: EventKind.Virtual,
			eventHandlerType: eventHandlerType,
			visibility: visibility,
			addEventCallback: (definition, implementation) =>
			{
				// prepare callback to add the 'add' accessor and the 'remove' accessor
				// (the callbacks implement the standard event behavior to allow re-using test code for the 'standard' event implementation strategy)
				void ImplementAddAccessorCallback(IGeneratedEvent    @event, ILGenerator msilGenerator) => implementation.ImplementAddAccessorMethod(definition, @event, msilGenerator);
				void ImplementRemoveAccessorCallback(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementRemoveAccessorMethod(definition, @event, msilGenerator);

				// add the event to the type definition
				IGeneratedEvent addedEvent = definition.AddVirtualEvent(
					eventHandlerType,
					name,
					visibility,
					ImplementAddAccessorCallback,
					ImplementRemoveAccessorCallback);

				Assert.NotNull(addedEvent);
				return addedEvent;
			});
	}

	#endregion

	// The following methods are tested in ClassDefinitionTests_ConcreteBaseClass_WithBaseClass:
	// - AddEventOverride<T>(IInheritedEvent<T> eventToOverride, IEventImplementation<T> implementation)
	// - AddEventOverride<T>(IInheritedEvent<T> eventToOverride, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)
	// - AddEventOverride(IInheritedEvent eventToOverride, IEventImplementation implementation)
	// - AddEventOverride(IInheritedEvent eventToOverride, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)

	#endregion

	#region Adding Properties

	#region Test Data

	/// <summary>
	/// Test data for tests targeting adding new abstract properties.
	/// </summary>
	public static IEnumerable<object[]> AddAbstractPropertyTestData
	{
		get
		{
			// ------------------------------------------------------------------------------------
			// different method names
			// ------------------------------------------------------------------------------------
			foreach (string name in PropertyNames)
			{
				yield return
				[
					name,             // name
					typeof(int),      // property type
					Visibility.Public // visibility
				];
			}

			// ------------------------------------------------------------------------------------
			// different visibilities
			// ------------------------------------------------------------------------------------

			foreach (Visibility visibility in Visibilities)
			{
				yield return
				[
					null,         // name (random)
					typeof(long), // property type (type does not matter, but 'long' would cause a duplicate test case)
					visibility    // visibility
				];
			}

			// ------------------------------------------------------------------------------------
			// different property types
			// ------------------------------------------------------------------------------------

			// value (covered above)
			//yield return
			//[
			//	null,                    // name (random)
			//	typeof(int),             // property type
			//	Visibility.Public        // visibility
			//];

			// reference
			yield return
			[
				null,             // name (random)
				typeof(string),   // property type
				Visibility.Public // visibility
			];
		}
	}

	#endregion

	#region AddAbstractProperty<T>(string name)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddAbstractProperty{T}(string)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	[Theory]
	[MemberData(nameof(AddAbstractPropertyTestData))]
	public void AddAbstractPropertyT(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition(null, TypeAttributes.Abstract);

		// get the AddAbstractProperty<T>(...) method to test
		MethodInfo addPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddAbstractProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string)]));

		// invoke the method to add the property to the type definition
		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_getSet" : null]);
		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_getOnly" : null]);
		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_setOnly" : null]);
		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_none" : null]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Abstract,
			propertyType,
			accessorVisibility,
			null,
			-1,
			null,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddAbstractProperty(Type type, string name)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddAbstractProperty(Type,string)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	[Theory]
	[MemberData(nameof(AddAbstractPropertyTestData))]
	public void AddAbstractProperty(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition(null, TypeAttributes.Abstract);

		// add properties with different combinations of accessors to the type definition
		IGeneratedProperty addedProperty_getSet = definition.AddAbstractProperty(propertyType, name + "_getSet");
		IGeneratedProperty addedProperty_getOnly = definition.AddAbstractProperty(propertyType, name + "_getOnly");
		IGeneratedProperty addedProperty_setOnly = definition.AddAbstractProperty(propertyType, name + "_setOnly");
		IGeneratedProperty addedProperty_none = definition.AddAbstractProperty(propertyType, name + "_none");

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Abstract,
			propertyType,
			accessorVisibility,
			null,
			-1,
			null,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddVirtualProperty<T>(string name)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddVirtualProperty{T}(string)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddVirtualPropertyT_WithoutImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property (actually one property with get/set accessors,
		// one property with get accessor only, one property with set accessor only and one property without a get/set accessor)
		ClassDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddVirtualProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_getSet" : null]);
		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_getOnly" : null]);
		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_setOnly" : null]);
		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_none" : null]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Virtual,
			propertyType,
			accessorVisibility,
			null,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddVirtualProperty(Type type, string name)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddVirtualProperty(Type,string)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddVirtualProperty_WithoutImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property
		ClassDefinition definition = CreateTypeDefinition();

		// add properties with different combinations of accessors to the type definition
		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);

		IGeneratedProperty addedProperty_getSet = definition.AddVirtualProperty(
			propertyType,
			name != null ? name + "_getSet" : null);

		IGeneratedProperty addedProperty_getOnly = definition.AddVirtualProperty(
			propertyType,
			name != null ? name + "_getOnly" : null);

		IGeneratedProperty addedProperty_setOnly = definition.AddVirtualProperty(
			propertyType,
			name != null ? name + "_setOnly" : null);

		IGeneratedProperty addedProperty_none = definition.AddVirtualProperty(
			propertyType,
			name != null ? name + "_none" : null);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Virtual,
			propertyType,
			accessorVisibility,
			null,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddVirtualProperty<T>(string name, IPropertyImplementation implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddVirtualProperty{T}(string,IPropertyImplementation)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddVirtualPropertyT_WithImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property (actually one property with get/set accessors,
		// one property with get accessor only, one property with set accessor only and one property without a get/set accessor)
		ClassDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddVirtualProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(IPropertyImplementation)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		var implementation = new PropertyImplementation_TestDataStorage(handle);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_getSet" : null,
				implementation
			]);

		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_getOnly" : null,
				implementation
			]);

		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_setOnly" : null,
				implementation
			]);

		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_none" : null,
				implementation
			]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Virtual,
			propertyType,
			accessorVisibility,
			implementation,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddVirtualProperty(Type type, string name, IPropertyImplementation implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddVirtualProperty(Type,string,IPropertyImplementation)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddVirtualProperty_WithImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property
		ClassDefinition definition = CreateTypeDefinition();

		// add properties with different combinations of accessors to the type definition
		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		var implementation = new PropertyImplementation_TestDataStorage(handle);

		IGeneratedProperty addedProperty_getSet = definition.AddVirtualProperty(
			propertyType,
			name != null ? name + "_getSet" : null,
			implementation);

		IGeneratedProperty addedProperty_getOnly = definition.AddVirtualProperty(
			propertyType,
			name != null ? name + "_getOnly" : null,
			implementation);

		IGeneratedProperty addedProperty_setOnly = definition.AddVirtualProperty(
			propertyType,
			name != null ? name + "_setOnly" : null,
			implementation);

		IGeneratedProperty addedProperty_none = definition.AddVirtualProperty(
			propertyType,
			name != null ? name + "_none" : null,
			implementation);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Virtual,
			propertyType,
			accessorVisibility,
			implementation,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	// The following methods are tested in ClassDefinitionTests_ConcreteBaseClass_WithBaseClass:
	// - AddPropertyOverride<T>(IInheritedProperty<T> propertyToOverride, IPropertyImplementation{T} implementation)
	// - AddPropertyOverride<T>(IInheritedProperty<T> propertyToOverride, PropertyAccessorImplementationCallback getAccessorImplementationCallback, PropertyAccessorImplementationCallback setAccessorImplementationCallback)
	// - AddPropertyOverride(IInheritedProperty propertyToOverride, IPropertyImplementation implementation)
	// - AddPropertyOverride(IInheritedProperty propertyToOverride, PropertyAccessorImplementationCallback getAccessorImplementationCallback, PropertyAccessorImplementationCallback setAccessorImplementationCallback)

	#endregion

	#region Adding Methods

	#region Test Data

	/// <summary>
	/// Test data for tests targeting adding new abstract methods.
	/// </summary>
	public static IEnumerable<object[]> AddAbstractMethodTestData
	{
		get
		{
			// ------------------------------------------------------------------------------------
			// different method names
			// ------------------------------------------------------------------------------------
			foreach (string name in MethodNames)
			{
				yield return
				[
					name,             // name
					typeof(void),     // return type
					Type.EmptyTypes,  // parameter types
					Visibility.Public // visibility
				];
			}

			// ------------------------------------------------------------------------------------
			// different visibilities
			// ------------------------------------------------------------------------------------

			foreach (Visibility visibility in Visibilities)
			{
				yield return
				[
					null,            // name (random)
					typeof(int),     // return type (type does not matter, but 'void' would cause a duplicate test case)
					Type.EmptyTypes, // parameter types
					visibility       // visibility
				];
			}

			// ------------------------------------------------------------------------------------
			// different parameters and return types
			// ------------------------------------------------------------------------------------

			// parameter types: 1x value
			// return type: value
			yield return
			[
				null,                    // name (random)
				typeof(long),            // return type
				new[] { typeof(sbyte) }, // parameter types
				Visibility.Public        // visibility
			];

			// parameter types: 2x value
			// return type: value
			yield return
			[
				null,                                   // name (random)
				typeof(long),                           // return type
				new[] { typeof(sbyte), typeof(short) }, // parameter types
				Visibility.Public                       // visibility
			];

			// parameter types: 1x reference
			// return type: reference
			yield return
			[
				null,                      // name (random)
				typeof(long[]),            // return type
				new[] { typeof(sbyte[]) }, // parameter types
				Visibility.Public          // visibility
			];

			// parameter types: 2x reference
			// return type: reference
			yield return
			[
				null,                                       // name (random)
				typeof(long[]),                             // return type
				new[] { typeof(sbyte[]), typeof(short[]) }, // parameter types
				Visibility.Public                           // visibility
			];
		}
	}

	#endregion

	#region AddAbstractMethod(string name, Type returnType, Type[] parameterTypes, Visibility visibility, MethodAttributes additionalAttributes = 0)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddAbstractMethod(string,Type,Type[],Visibility,MethodAttributes)"/> method.
	/// </summary>
	/// <param name="name">Name of the method to add.</param>
	/// <param name="returnType">Return type of the method to add.</param>
	/// <param name="parameterTypes">Types of parameters of the method to add.</param>
	/// <param name="visibility">Visibility of the method to add.</param>
	[Theory]
	[MemberData(nameof(AddAbstractMethodTestData))]
	public void AddAbstractMethod(
		string     name,
		Type       returnType,
		Type[]     parameterTypes,
		Visibility visibility)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition(null, TypeAttributes.Abstract);

		// test the method
		TestAddMethod(
			definition,
			MethodKind.Abstract,
			name,
			returnType,
			parameterTypes,
			visibility,
			null,
			null,
			() => definition.AddAbstractMethod(name, returnType, parameterTypes, visibility, 0));
	}

	#endregion

	#region AddVirtualMethod(string name, Type returnType, Type[] parameterTypes, Visibility visibility, IMethodImplementation implementation, MethodAttributes additionalAttributes = 0)

	/// <summary>
	/// Tests the
	/// <see cref="ClassDefinition.AddVirtualMethod(string,Type,Type[],Visibility,IMethodImplementation,MethodAttributes)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the method to add.</param>
	/// <param name="returnType">Return type of the method to add.</param>
	/// <param name="parameterTypes">Types of parameters of the method to add.</param>
	/// <param name="visibility">Visibility of the method to add.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodTestData))]
	public void AddVirtualMethod_WithImplementationStrategy(
		string     name,
		Type       returnType,
		Type[]     parameterTypes,
		Visibility visibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition(null);

		// test the method
		TestAddMethod(
			definition,
			MethodKind.Virtual,
			name,
			returnType,
			parameterTypes,
			visibility,
			testArguments,
			expectedTestResult,
			() =>
			{
				var implementation = new TestMethodImplementation();
				IGeneratedMethod addedMethodDefinition = definition.AddVirtualMethod(
					name,
					returnType,
					parameterTypes,
					visibility,
					implementation);
				Assert.Same(implementation, addedMethodDefinition.Implementation);
				return addedMethodDefinition;
			});
	}

	#endregion

	#region AddVirtualMethod(string name, Type returnType, Type[] parameterTypes, Visibility visibility, MethodImplementationCallback implementation, MethodAttributes additionalAttributes = 0)

	/// <summary>
	/// Tests the
	/// <see cref="ClassDefinition.AddVirtualMethod(string,Type,Type[],Visibility,MethodImplementationCallback,MethodAttributes)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the method to add.</param>
	/// <param name="returnType">Return type of the method to add.</param>
	/// <param name="parameterTypes">Types of parameters of the method to add.</param>
	/// <param name="visibility">Visibility of the method to add.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodTestData))]
	public void AddVirtualMethod_WithImplementationCallback(
		string     name,
		Type       returnType,
		Type[]     parameterTypes,
		Visibility visibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition(null);

		// test the method
		TestAddMethod(
			definition,
			MethodKind.Virtual,
			name,
			returnType,
			parameterTypes,
			visibility,
			testArguments,
			expectedTestResult,
			() =>
			{
				IGeneratedMethod addedMethodDefinition = definition.AddVirtualMethod(
					name,
					returnType,
					parameterTypes,
					visibility,
					TestMethodImplementation.Callback);
				Assert.Null(addedMethodDefinition.Implementation);
				return addedMethodDefinition;
			});
	}

	#endregion

	// The following methods are tested in ClassDefinitionTests_ConcreteBaseClass_WithBaseClass:
	// - AddMethodOverride(IInheritedMethod methodToOverride, IMethodImplementation implementation)
	// - AddMethodOverride(IInheritedMethod methodToOverride, MethodImplementationCallback implementationCallback)

	#endregion
}
