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

namespace GriffinPlus.Lib.CodeGeneration.Tests;

using static Helpers;

/// <summary>
/// Common tests around the <see cref="ClassDefinition"/> class.
/// </summary>
public abstract class ClassDefinitionTests_Common : TypeDefinitionTests_Common<ClassDefinition>
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
		const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;
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

	#region Adding Events (TODO, override test cases missing)

	#region AddAbstractEvent<T>(string name, Visibility visibility)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddAbstractEvent{T}(string,Visibility)"/> method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	[Theory]
	[MemberData(nameof(AddAbstractEventTestData))]
	public void AddAbstractEvent(string name, Visibility visibility, Type eventHandlerType)
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

	#region AddVirtualEvent<T>(string name, Visibility visibility, IEventImplementation{T} implementation)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddVirtualEvent{T}(string,Visibility,IEventImplementation)"/> method
	/// using <see cref="EventImplementation_Standard"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	/// <param name="addEventRaiserMethod">
	/// <c>true</c> to add the event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
	/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
	/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
	/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationStrategy_Standard))]
	public void AddVirtualEvent_WithImplementationStrategy_Standard(
		string     name,
		Visibility visibility,
		Type       eventHandlerType,
		bool       addEventRaiserMethod,
		string     eventRaiserName,
		Visibility eventRaiserVisibility,
		Type       expectedEventRaiserReturnType,
		Type[]     expectedEventRaiserParameterTypes)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition();

		// create an instance of the implementation strategy
		Type implementationType = typeof(EventImplementation_Standard);
		IEventImplementation implementation = addEventRaiserMethod
			                                      ? (IEventImplementation)Activator.CreateInstance(implementationType, eventRaiserName, eventRaiserVisibility)
			                                      : (IEventImplementation)Activator.CreateInstance(implementationType);

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

		// invoke the method to add the event to the type definition
		var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(definition, [name, visibility, implementation]);
		Assert.NotNull(addedEvent);
		Assert.Equal(EventKind.Virtual, addedEvent.Kind);
		Assert.Equal(visibility, addedEvent.Visibility);
		Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
		Assert.Same(implementation, addedEvent.Implementation);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the implementation of the event
		TestEventImplementation_Standard(
			definition,
			instance,
			EventKind.Virtual,
			addedEvent.Name,
			eventHandlerType,
			addEventRaiserMethod,
			eventRaiserName,
			expectedEventRaiserReturnType,
			expectedEventRaiserParameterTypes);
	}

	#endregion

	#region AddVirtualEvent<T>(string name, Visibility visibility, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback) --- TODO!

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddVirtualEvent{T}(string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationCallbacks))]
	public void AddVirtualEvent_WithImplementationCallbacks(string name, Visibility visibility)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition();

		// add a backing field for the event's multicast delegate
		// (always of type EventHandler<EventArgs>, no need to test other types as this affects raising the event only)
		Type eventHandlerType = typeof(EventHandler<EventArgs>);
		IGeneratedField backingField = definition.AddField(eventHandlerType, name: null, Visibility.Public);

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
					.SequenceEqual(
					[
						typeof(string),
						typeof(Visibility),
						typeof(EventAccessorImplementationCallback),
						typeof(EventAccessorImplementationCallback)
					]));

		// invoke the method to add the event to the type definition
		// (the callbacks implement the standard event behavior to allow re-using test code for the 'standard' event implementation strategy)
		void ImplementGetAccessor(IGeneratedEvent @event, ILGenerator msilGenerator) => ImplementEventAccessor(@event, true, backingField.FieldBuilder, msilGenerator);
		void ImplementSetAccessor(IGeneratedEvent @event, ILGenerator msilGenerator) => ImplementEventAccessor(@event, false, backingField.FieldBuilder, msilGenerator);
		var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
			definition,
			[
				name,
				visibility,
				(EventAccessorImplementationCallback)ImplementGetAccessor,
				(EventAccessorImplementationCallback)ImplementSetAccessor
			]);
		Assert.NotNull(addedEvent);
		Assert.Equal(EventKind.Virtual, addedEvent.Kind);
		Assert.Equal(visibility, addedEvent.Visibility);
		Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
		Assert.Null(addedEvent.Implementation);

		// add an event raiser method to the type definition
		// (should always just be: public void FireMyEvent();
		const MethodKind kind = MethodKind.Normal;
		string eventRaiserName = "FireMyEvent";
		Type eventRaiserReturnType = typeof(void);
		Type[] eventRaiserParameterTypes = [];
		const Visibility eventRaiserVisibility = Visibility.Public;
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

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the implementation of the event
		// (the implemented event should behave the same as a standard event implementation, use the same test code...)
		TestEventImplementation_Standard(
			definition,
			instance,
			EventKind.Virtual,
			addedEvent.Name,
			eventHandlerType,
			true,
			eventRaiserName,
			eventRaiserReturnType,
			eventRaiserParameterTypes);
	}

	#endregion

	#region AddEventOverride<T>(IInheritedEvent<T> eventToOverride, IEventImplementation<T> implementation) --- TODO!

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddEventOverride{T}(IInheritedEvent{T},IEventImplementation)"/> method
	/// using <see cref="EventImplementation_Standard"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	public void AddEventOverride_WithImplementationStrategy_Standard()
	{
		// TODO: Implement...
	}

	#endregion

	#region AddEventOverride<T>(IInheritedEvent<T>, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback) --- TODO!

	#endregion

	#endregion

	#region Adding Properties (TODO, abstract and override test cases missing)

	#region Test Data

	/// <summary>
	/// Test data for tests targeting <see cref="ClassDefinition.AddAbstractEvent{T}(string,Visibility)"/>.
	/// </summary>
	public static IEnumerable<object[]> AddAbstractEventTestData
	{
		get
		{
			foreach (string name in new[] { "Event", null })
			foreach (Visibility visibility in Visibilities)
			{
				// System.EventHandler
				yield return
				[
					name,                // name of the event
					visibility,          // visibility of the event
					typeof(EventHandler) // event handler type
				];

				// System.EventHandler<EventArgs>
				yield return
				[
					name,                           // name of the event
					visibility,                     // visibility of the event
					typeof(EventHandler<EventArgs>) // event handler type
				];

				// System.EventHandler<SpecializedEventArgs>
				yield return
				[
					name,                                      // name of the event
					visibility,                                // visibility of the event
					typeof(EventHandler<SpecializedEventArgs>) // event handler type
				];

				// System.Action
				yield return
				[
					name,          // name of the event
					visibility,    // visibility of the event
					typeof(Action) // event handler type
				];

				// System.Action<int>
				yield return
				[
					name,               // name of the event
					visibility,         // visibility of the event
					typeof(Action<int>) // event handler type
				];

				// System.Func<int>
				yield return
				[
					name,              // name of the event
					visibility,        // visibility of the event
					typeof(Func<long>) // event handler type
				];

				// System.Func<int,long>
				yield return
				[
					name,                   // name of the event
					visibility,             // visibility of the event
					typeof(Func<int, long>) // event handler type
				];
			}
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
	/// <param name="_">Test values to use when playing with accessor methods (not used by this test case).</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddAbstractPropertyT(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   _)
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
	/// <param name="_">Test values to use when playing with accessor methods (not used by this test case).</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddAbstractProperty(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   _)
	{
		// create a new type definition
		ClassDefinition definition = CreateTypeDefinition(null, TypeAttributes.Abstract);

		// invoke the method to add the property to the type definition
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
		MethodInfo addPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddVirtualProperty))
			.Single(
				method => !method.IsGenericMethod && method
					          .GetParameters()
					          .Select(parameter => parameter.ParameterType)
					          .SequenceEqual([typeof(Type), typeof(string)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_getSet" : null]);
		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_getOnly" : null]);
		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_setOnly" : null]);
		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_none" : null]);

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
		MethodInfo addPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddVirtualProperty))
			.Single(
				method => !method.IsGenericMethod && method
					          .GetParameters()
					          .Select(parameter => parameter.ParameterType)
					          .SequenceEqual([typeof(Type), typeof(string), typeof(IPropertyImplementation)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		var implementation = new PropertyImplementation_TestDataStorage(handle);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_getSet" : null,
				implementation
			]);

		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_getOnly" : null,
				implementation
			]);

		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_setOnly" : null,
				implementation
			]);

		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
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
