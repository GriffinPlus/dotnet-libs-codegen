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
using System.Threading;

using Xunit;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Helpers used within the unit tests.
/// </summary>
public static class Helpers
{
	public const BindingFlags ExactBindingFlags             = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding;
	public const BindingFlags ExactDeclaredOnlyBindingFlags = ExactBindingFlags | BindingFlags.DeclaredOnly;

	/// <summary>
	/// Gets constructors of the specified type that can be accessed by a type deriving from that type.
	/// </summary>
	/// <param name="type">Type to inspect.</param>
	/// <returns>The constructors of the specified type that can be accessed by a type deriving from that type.</returns>
	public static HashSet<ConstructorInfo> GetConstructorsAccessibleFromDerivedType(Type type)
	{
		var constructorInfos = new HashSet<ConstructorInfo>();
		foreach (ConstructorInfo constructorInfo in type.GetConstructors(ExactDeclaredOnlyBindingFlags & ~BindingFlags.Static))
		{
			// skip constructor if it is private or internal
			// (cannot be accessed by a derived type defined in another assembly)
			if (constructorInfo.IsPrivate || constructorInfo.IsAssembly) continue;

			// keep constructor
			constructorInfos.Add(constructorInfo);
		}

		return constructorInfos;
	}

	/// <summary>
	/// Gets the events that can be accessed by a type deriving from the specified type.
	/// </summary>
	/// <param name="type">Type to inspect.</param>
	/// <param name="includeHidden">
	/// <c>true</c> to include hidden events;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <returns>The events that can be accessed by a type deriving from the specified type.</returns>
	public static HashSet<EventInfo> GetEventsAccessibleFromDerivedType(Type type, bool includeHidden)
	{
		var eventInfos = new HashSet<EventInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (EventInfo @event in typeToInspect.GetEvents(ExactDeclaredOnlyBindingFlags))
			{
				// skip event if it is 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				MethodInfo addMethod = @event.GetAddMethod(nonPublic: true);
				MethodInfo removeMethod = @event.GetRemoveMethod(nonPublic: true);
				Debug.Assert(addMethod != null, nameof(addMethod) + " != null");
				Debug.Assert(removeMethod != null, nameof(removeMethod) + " != null");
				if (addMethod.IsPrivate || addMethod.IsAssembly) continue;
				if (removeMethod.IsPrivate || removeMethod.IsAssembly) continue;

				// skip event if it is virtual and already in the set of events (also covers abstract, virtual and overridden events)
				// => only the most specific implementation gets into the returned set of events
				if ((addMethod.IsVirtual || removeMethod.IsVirtual) && eventInfos.Any(otherEvent => HasSameSignature(otherEvent, @event)))
					continue;

				// skip event if an event with the same signature is already in the set of events
				// and hidden events should not be returned
				if (!includeHidden && eventInfos.Any(otherEvent => HasSameSignature(otherEvent, @event)))
					continue;

				// the event is accessible from a derived class in some other assembly
				eventInfos.Add(@event);
			}

			typeToInspect = typeToInspect.BaseType;
		}

		return eventInfos;
	}

	/// <summary>
	/// Gets the fields that can be accessed by a type deriving from the specified type.
	/// </summary>
	/// <param name="type">Type to inspect.</param>
	/// <param name="includeHidden">
	/// <c>true</c> to include hidden fields;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <returns>The fields that can be accessed by a type deriving from the specified type.</returns>
	public static HashSet<FieldInfo> GetFieldsAccessibleFromDerivedType(Type type, bool includeHidden)
	{
		// all fields that are neither private nor internal are accessible to derived types
		var fieldInfos = new HashSet<FieldInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (FieldInfo field in typeToInspect.GetFields(ExactDeclaredOnlyBindingFlags))
			{
				// skip field if it is 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				if (field.IsPrivate || field.IsAssembly) continue;

				// skip field if a field with the same signature is already in the set of fields
				// and hidden fields should not be returned
				if (!includeHidden && fieldInfos.Any(otherField => HasSameSignature(otherField, field)))
					continue;

				// the field is accessible from a derived class in some other assembly
				fieldInfos.Add(field);
			}

			typeToInspect = typeToInspect.BaseType;
		}

		return fieldInfos;
	}

	/// <summary>
	/// Gets the methods that can be accessed by a type deriving from the specified type.
	/// </summary>
	/// <param name="type">Type to inspect.</param>
	/// <param name="includeHidden">
	/// <c>true</c> to include hidden methods;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <returns>The methods that can be accessed by a type deriving from the specified type.</returns>
	public static HashSet<MethodInfo> GetMethodsAccessibleFromDerivedType(Type type, bool includeHidden)
	{
		var methodInfos = new HashSet<MethodInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (MethodInfo method in typeToInspect.GetMethods(ExactDeclaredOnlyBindingFlags))
			{
				// skip method if it is 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				if (method.IsPrivate || method.IsAssembly) continue;

				// skip methods with a special name as these methods usually cannot be called by the user directly
				// (add/remove accessor methods of events and get/set accessor methods of properties)
				if (method.IsSpecialName) continue;

				// skip method if it is virtual and already in the set of methods (also covers abstract, virtual and overridden events)
				// => only the most specific implementation gets into the returned set of methods
				if (method.IsVirtual && methodInfos.Any(otherMethod => HasSameSignature(otherMethod, method)))
					continue;

				// skip property if a method with the same signature is already in the set of methods
				// and hidden methods should not be returned
				if (!includeHidden && methodInfos.Any(otherMethod => HasSameSignature(otherMethod, method)))
					continue;

				// the method is accessible from a derived class in some other assembly
				methodInfos.Add(method);
			}

			typeToInspect = typeToInspect.BaseType;
		}

		return methodInfos;
	}

	/// <summary>
	/// Gets the properties that can be accessed by a type deriving from the specified type.
	/// </summary>
	/// <param name="type">Type to inspect.</param>
	/// <param name="includeHidden">
	/// <c>true</c> to include hidden properties;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <returns>The properties that can be accessed by a type deriving from the specified type.</returns>
	public static HashSet<PropertyInfo> GetPropertiesAccessibleFromDerivedType(Type type, bool includeHidden)
	{
		var propertyInfos = new HashSet<PropertyInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (PropertyInfo property in typeToInspect.GetProperties(ExactDeclaredOnlyBindingFlags))
			{
				// skip accessors that are 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				int callableAccessorCount = 0;

				// check visibility of the 'get' accessor
				MethodInfo getAccessor = property.GetGetMethod(nonPublic: true);
				if (getAccessor != null && !getAccessor.IsPrivate && !getAccessor.IsAssembly)
					callableAccessorCount++;

				// check visibility of the 'set' accessor
				MethodInfo setAccessor = property.GetSetMethod(nonPublic: true);
				if (setAccessor != null && !setAccessor.IsPrivate && !setAccessor.IsAssembly)
					callableAccessorCount++;

				// skip property if neither a get accessor method nor a set accessor method are accessible
				if (callableAccessorCount == 0) continue;

				// skip property if it is already in the set of properties and its accessor methods are virtual
				// (the check for virtual also covers abstract, virtual and override methods)
				// => only the most specific implementation gets into the returned set of properties
				if (propertyInfos.Any(otherProperty => HasSameSignature(otherProperty, property)))
				{
					if (getAccessor != null && getAccessor.IsVirtual) continue;
					if (setAccessor != null && setAccessor.IsVirtual) continue;
				}

				// skip property if a property with the same signature is already in the set of properties
				// and hidden properties should not be returned
				if (!includeHidden && propertyInfos.Any(otherProperty => HasSameSignature(otherProperty, property)))
					continue;

				// the property is accessible from a derived class in some other assembly
				propertyInfos.Add(property);
			}

			typeToInspect = typeToInspect.BaseType;
		}

		return propertyInfos;
	}

	/// <summary>
	/// Checks whether the signatures (name + field type) of the specified fields are the same.
	/// </summary>
	/// <param name="x">Field to compare.</param>
	/// <param name="y">Field to compare to.</param>
	/// <returns>
	/// <c>true</c> if the specified fields have the same signature;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public static bool HasSameSignature(FieldInfo x, FieldInfo y)
	{
		if (x.Name != y.Name) return false;
		return x.FieldType == y.FieldType;
	}

	/// <summary>
	/// Checks whether the signatures (name + event handler type) of the specified events are the same.
	/// </summary>
	/// <param name="x">Event to compare.</param>
	/// <param name="y">Event to compare to.</param>
	/// <returns>
	/// <c>true</c> if the specified events have the same signature;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public static bool HasSameSignature(EventInfo x, EventInfo y)
	{
		if (x.Name != y.Name) return false;
		return x.EventHandlerType == y.EventHandlerType;
	}

	/// <summary>
	/// Checks whether the signatures (name + property type) of the specified properties are the same.
	/// </summary>
	/// <param name="x">Property to compare.</param>
	/// <param name="y">Property to compare to.</param>
	/// <returns>
	/// <c>true</c> if the specified properties have the same signature;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public static bool HasSameSignature(PropertyInfo x, PropertyInfo y)
	{
		if (x.Name != y.Name) return false;
		return x.PropertyType == y.PropertyType;
	}

	/// <summary>
	/// Checks whether the specified property is abstract.
	/// </summary>
	/// <param name="property">Property to check.</param>
	/// <returns>
	/// <c>true</c> if the specified property is abstract;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public static bool IsAbstract(PropertyInfo property)
	{
		MethodInfo getter = property.GetGetMethod(nonPublic: true);
		MethodInfo setter = property.GetSetMethod(nonPublic: true);
		if (getter != null && getter.IsAbstract) return true;
		if (setter != null && setter.IsAbstract) return true;
		return false;
	}

	/// <summary>
	/// Checks whether the signatures (name + return type + parameter types) of the specified methods are the same.
	/// </summary>
	/// <param name="x">Method to compare.</param>
	/// <param name="y">Method to compare to.</param>
	/// <returns>
	/// <c>true</c> if the specified methods have the same signature;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public static bool HasSameSignature(MethodBase x, MethodBase y)
	{
		return x.Name == y.Name && x.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(y.GetParameters().Select(parameter => parameter.ParameterType));
	}

	/// <summary>
	/// Gets properties that a derived class inherits from the specified class.
	/// </summary>
	/// <param name="type">Type to get inherited properties from.</param>
	/// <param name="includeHidden">
	/// <c>true</c> to include properties that have been hidden by more specific types if the base type derives from some other type on its own;<br/>
	/// <c>false</c> to return only the most specific properties.
	/// </param>
	/// <returns>The inherited properties.</returns>
	public static IEnumerable<PropertyInfo> GetInheritedProperties(Type type, bool includeHidden)
	{
		var inheritedProperties = new HashSet<PropertyInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (PropertyInfo property in typeToInspect.GetProperties(ExactDeclaredOnlyBindingFlags))
			{
				// skip accessors that are 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				int callableAccessorCount = 0;

				// check visibility of the 'get' accessor
				MethodInfo getAccessor = property.GetGetMethod(nonPublic: true);
				if (getAccessor != null && !getAccessor.IsPrivate && !getAccessor.IsAssembly)
					callableAccessorCount++;

				// check visibility of the 'set' accessor
				MethodInfo setAccessor = property.GetSetMethod(nonPublic: true);
				if (setAccessor != null && !setAccessor.IsPrivate && !setAccessor.IsAssembly)
					callableAccessorCount++;

				// skip property if neither a get accessor method nor a set accessor method are accessible
				if (callableAccessorCount == 0) continue;

				// skip property if it is already in the set of properties and its accessor methods are virtual
				// (the check for virtual also covers abstract, virtual and override methods)
				// => only the most specific implementation gets into the returned set of properties
				if (inheritedProperties.Any(otherProperty => HasSameSignature(otherProperty, property)))
				{
					if (getAccessor != null && getAccessor.IsVirtual) continue;
					if (setAccessor != null && setAccessor.IsVirtual) continue;
				}

				// skip property if a property with the same signature is already in the set of properties
				// and hidden properties should not be returned
				if (!includeHidden && inheritedProperties.Any(otherProperty => HasSameSignature(otherProperty, property)))
					continue;

				// the property is accessible from a derived class in some other assembly
				inheritedProperties.Add(property);
			}

			typeToInspect = typeToInspect.BaseType;
		}

		return inheritedProperties;
	}

	/// <summary>
	/// Checks the specified type against the type definition and determines whether the type was created correctly.
	/// This checks member declaration, but not their implementation as this would need to dig into the msil stream.
	/// </summary>
	/// <param name="type">Type to check.</param>
	/// <param name="definition">Definition the type should comply with.</param>
	public static void CheckTypeAgainstDefinition(Type type, TypeDefinition definition)
	{
		// check whether the full name of the type equals the defined type name
		Assert.Equal(definition.TypeName, type.FullName);

		// check whether the expected constructors have been declared
		// (TODO: add checking for static constructor, requires pimping up the type definition)
		var expectedConstructors = new HashSet<ConstructorInfo>(collection: definition.Constructors.Select(constructor => constructor.ConstructorInfo));
		var actualConstructors = new HashSet<ConstructorInfo>(collection: type.GetConstructors(ExactDeclaredOnlyBindingFlags & ~BindingFlags.Static));
		Assert.Equal(expectedConstructors, actualConstructors, ConstructorEqualityComparer.Instance);

		// check whether the expected fields have been declared
		var expectedFields = new HashSet<FieldInfo>(collection: definition.GeneratedFields.Select(field => field.FieldInfo));
		var actualFields = new HashSet<FieldInfo>(collection: type.GetFields(ExactDeclaredOnlyBindingFlags));
		Assert.Equal(expectedFields, actualFields, FieldEqualityComparer.Instance);

		// check whether the expected events have been declared
		// (the EventComparisonWrapper class handles the comparison on its own, no external comparer needed)
		var expectedEvents = new HashSet<EventComparisonWrapper>(collection: definition.GeneratedEvents.Select(@event => new EventComparisonWrapper(@event)));
		var actualEvents = new HashSet<EventComparisonWrapper>(collection: type.GetEvents(ExactDeclaredOnlyBindingFlags).Select(@event => new EventComparisonWrapper(@event)));
		Assert.Equal(expectedEvents, actualEvents);

		// check whether the expected properties have been declared
		var expectedProperties = new HashSet<PropertyComparisonWrapper>(collection: definition.GeneratedProperties.Select(property => new PropertyComparisonWrapper(property)));
		var actualProperties = new HashSet<PropertyComparisonWrapper>(collection: type.GetProperties(ExactDeclaredOnlyBindingFlags).Select(property => new PropertyComparisonWrapper(property)));
		Assert.Equal(expectedProperties, actualProperties);

		// check whether the expected methods have been declared
		var expectedMethods = new HashSet<MethodComparisonWrapper>(collection: definition.GeneratedMethods.Select(method => new MethodComparisonWrapper(method)));
		var actualMethods = new HashSet<MethodComparisonWrapper>(collection: type.GetMethods(ExactDeclaredOnlyBindingFlags).Where(method => !method.IsSpecialName).Select(method => new MethodComparisonWrapper(method)));
		Assert.Equal(expectedMethods, actualMethods);
	}

	#region Helpers concerning Events

	/// <summary>
	/// Tests an event that has been implemented using the <see cref="TestEventImplementation"/> implementation strategy.
	/// </summary>
	/// <param name="definition">Type definition the event to test belongs to.</param>
	/// <param name="instance">Instance of the dynamically created type that contains the event.</param>
	/// <param name="eventName">Name of the added event.</param>
	/// <param name="expectedEventKind">The expected kind of the generated event.</param>
	/// <param name="expectedEventVisibility">The expected visibility of the generated event.</param>
	/// <param name="expectedEventHandlerType">
	/// Expected type of the event handler. For the sake of simplicity only the following event handler types are supported:
	/// <see cref="EventHandler"/> (should generate a raiser method without return value and arguments),
	/// <see cref="EventHandler{TEventArgs}"/> with <see cref="EventArgs"/> (should generate a raiser method without return value and arguments),
	/// <see cref="EventHandler{TEventArgs}"/> with <see cref="SpecializedEventArgs"/> (should generate a raiser method without return value taking
	/// <see cref="SpecializedEventArgs"/> as argument),
	/// <see cref="Action"/> (should generate a raiser method without return value and arguments) and
	/// <see cref="Action{Int32}"/> (should generate a raiser method without return value taking <see cref="Int32"/> as argument) and
	/// <see cref="Func{Int64}"/> (should generate a raiser method with return value of type <see cref="Int64"/>, but without arguments) and
	/// <see cref="Func{Int32,Int64}"/> (should generate a raiser method with return value of type <see cref="Int64"/> taking <see cref="Int32"/> as
	/// argument).
	/// </param>
	/// <param name="strategyAddsEventRaiserMethod">
	/// <c>true</c> if the implementation strategy should have added an event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="expectedEventRaiserName">Expected name of the added event raiser method.</param>
	/// <param name="expectedEventRaiserReturnType">The expected return type of the event raiser method, if any.</param>
	/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the event raiser method, if any.</param>
	public static void TestEventImplementation_Standard(
		TypeDefinition definition,
		object         instance,
		string         eventName,
		EventKind      expectedEventKind,
		Visibility     expectedEventVisibility,
		Type           expectedEventHandlerType,
		bool           strategyAddsEventRaiserMethod,
		string         expectedEventRaiserName,
		Type           expectedEventRaiserReturnType,
		Type[]         expectedEventRaiserParameterTypes)
	{
		// get the type of the generated instance
		Type generatedType = instance.GetType();

		// get the definition of the event and its accessor methods from the type definition
		IGeneratedEvent eventDefinition = definition.GeneratedEvents.SingleOrDefault(@event => @event.Name == eventName);
		EventInfo generatedEvent = generatedType.GetEvents(ExactDeclaredOnlyBindingFlags).SingleOrDefault(@event => @event.Name == eventName);
		Assert.NotNull(eventDefinition);
		Assert.NotNull(generatedEvent);
		AssertGeneratedEventCompliesToTheDefinition(eventDefinition, generatedEvent);

		// abort if the implementation strategy is not expected to have added an event raiser method
		if (!strategyAddsEventRaiserMethod)
		{
			Assert.Empty(definition.GeneratedMethods);
			return;
		}

		// check whether the implementation strategy has added the event raiser method to the type definition
		IGeneratedMethod eventRaiserMethodDefinition = definition.GeneratedMethods.SingleOrDefault(method => method.Name == expectedEventRaiserName);
		Assert.NotNull(eventRaiserMethodDefinition);
		Assert.Equal(expectedEventRaiserName, eventRaiserMethodDefinition.Name);
		Assert.Equal(expectedEventRaiserReturnType, eventRaiserMethodDefinition.ReturnType);
		Assert.Equal(expectedEventRaiserParameterTypes, eventRaiserMethodDefinition.ParameterTypes);

		// get the event raiser method
		MethodInfo eventRaiserMethod = generatedType.GetMethods(ExactDeclaredOnlyBindingFlags).SingleOrDefault(method => method.Name == expectedEventRaiserName);
		Assert.NotNull(eventRaiserMethod);

		// prepare an event handler to register with the event
		Delegate handler = null;
		bool handlerWasCalled = false;
		object[] eventRaiserArguments = null;
		object expectedReturnValue = null;
		if (expectedEventHandlerType == typeof(EventHandler))
		{
			// set up an event handler to test with
			eventRaiserArguments = [];
			handler = new EventHandler(
				(sender, e) =>
				{
					if (expectedEventKind == EventKind.Static)
					{
						Assert.Null(sender);
					}
					else
					{
						if (definition.TypeBuilder.IsValueType) Assert.Equal(instance, sender);
						else Assert.Same(instance, sender);
					}

					Assert.Same(EventArgs.Empty, e);
					handlerWasCalled = true;
				});
		}
		else if (expectedEventHandlerType == typeof(EventHandler<EventArgs>))
		{
			// set up an event handler to test with
			eventRaiserArguments = [];
			handler = new EventHandler<EventArgs>(
				(sender, e) =>
				{
					if (expectedEventKind == EventKind.Static)
					{
						Assert.Null(sender);
					}
					else
					{
						if (definition.TypeBuilder.IsValueType) Assert.Equal(instance, sender);
						else Assert.Same(instance, sender);
					}

					Assert.Same(EventArgs.Empty, e);
					handlerWasCalled = true;
				});
		}
		else if (expectedEventHandlerType == typeof(EventHandler<SpecializedEventArgs>))
		{
			// set up an event handler to test with
			eventRaiserArguments = [SpecializedEventArgs.Empty];
			handler = new EventHandler<SpecializedEventArgs>(
				(sender, e) =>
				{
					if (expectedEventKind == EventKind.Static)
					{
						Assert.Null(sender);
					}
					else
					{
						if (definition.TypeBuilder.IsValueType) Assert.Equal(instance, sender);
						else Assert.Same(instance, sender);
					}

					Assert.Same(SpecializedEventArgs.Empty, e);
					handlerWasCalled = true;
				});
		}
		else if (expectedEventHandlerType == typeof(Action))
		{
			// set up an event handler to test with
			eventRaiserArguments = [];
			handler = new Action(() => { handlerWasCalled = true; });
		}
		else if (expectedEventHandlerType == typeof(Action<int>))
		{
			// set up an event handler to test with
			const int testValue = 42;
			eventRaiserArguments = [testValue];
			handler = new Action<int>(
				value =>
				{
					Assert.Equal(testValue, value);
					handlerWasCalled = true;
				});
		}
		else if (expectedEventHandlerType == typeof(Func<long>))
		{
			// set up an event handler to test with
			const long handlerReturnValue = 100;
			eventRaiserArguments = [];
			expectedReturnValue = handlerReturnValue;
			handler = new Func<long>(
				() =>
				{
					handlerWasCalled = true;
					return handlerReturnValue;
				});
		}
		else if (expectedEventHandlerType == typeof(Func<int, long>))
		{
			// set up an event handler to test with
			const int handlerArgument = 42;
			const long handlerReturnValue = 100;
			eventRaiserArguments = [handlerArgument];
			expectedReturnValue = handlerReturnValue;
			handler = new Func<int, long>(
				value =>
				{
					Assert.Equal(handlerArgument, value);
					handlerWasCalled = true;
					return handlerReturnValue;
				});
		}
		else
		{
			Debug.Fail("Unhandled test case.");
		}

		// add event handler to the event and raise it
		// => the handler should be called
		handlerWasCalled = false;
		generatedEvent.AddMethod.Invoke(instance, [handler]);
		object actualHandlerReturnValue = eventRaiserMethod.Invoke(instance, eventRaiserArguments);
		Assert.True(handlerWasCalled);
		Assert.Equal(expectedReturnValue, actualHandlerReturnValue);

		// remove the event handler from the event and raise it
		// => the handler should not be called anymore
		handlerWasCalled = false;
		generatedEvent.RemoveMethod.Invoke(instance, [handler]);
		eventRaiserMethod.Invoke(instance, eventRaiserArguments);
		Assert.False(handlerWasCalled);
	}

	/// <summary>
	/// Checks whether the specified generated event complies to its definition.
	/// </summary>
	/// <param name="eventDefinition">The definition of the event to test.</param>
	/// <param name="generatedEvent">The generated event to test.</param>
	public static void AssertGeneratedEventCompliesToTheDefinition(IEvent eventDefinition, EventInfo generatedEvent)
	{
		Assert.NotNull(generatedEvent);
		Assert.Equal(eventDefinition.Name, generatedEvent.Name);
		Assert.Equal(eventDefinition.Kind, generatedEvent.ToEventKind());
		Assert.Equal(eventDefinition.EventHandlerType, generatedEvent.EventHandlerType);

		// the 'add' accessor and the 'remove' accessor should always be present
		MethodInfo generatedAddAccessorMethod = generatedEvent.DeclaringType!.GetMethod(name: "add_" + eventDefinition.Name, ExactDeclaredOnlyBindingFlags);
		MethodInfo generatedRemoveAccessorMethod = generatedEvent.DeclaringType!.GetMethod(name: "remove_" + eventDefinition.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(eventDefinition.AddAccessor);
		Assert.NotNull(eventDefinition.RemoveAccessor);
		Assert.NotNull(generatedAddAccessorMethod);
		Assert.NotNull(generatedRemoveAccessorMethod);

		// the accessor methods should have the same kind and visibility as the event
		Assert.Equal(eventDefinition.Visibility, eventDefinition.AddAccessor.Visibility);
		Assert.Equal(eventDefinition.Visibility, eventDefinition.RemoveAccessor.Visibility);
		Assert.Equal((MethodKind)eventDefinition.Kind, eventDefinition.AddAccessor.Kind);
		Assert.Equal((MethodKind)eventDefinition.Kind, eventDefinition.RemoveAccessor.Kind);

		// check the accessor methods
		AssertGeneratedMethodCompliesToTheDefinition(eventDefinition.AddAccessor, generatedAddAccessorMethod);
		AssertGeneratedMethodCompliesToTheDefinition(eventDefinition.RemoveAccessor, generatedRemoveAccessorMethod);
	}

	/// <summary>
	/// Implements the add/remove accessor of the event.
	/// </summary>
	/// <param name="isAdd">
	/// <c>true</c> to implement the 'add' accessor method;<br/>
	/// <c>false</c> to implement the 'remove' accessor method.
	/// </param>
	/// <param name="eventToImplement">Event to implement.</param>
	/// <param name="backingFieldBuilder">The field builder of the backing field.</param>
	/// <param name="msilGenerator">MSIL generator to use for implementing the accessor.</param>
	public static void ImplementEventAccessor(
		IGeneratedEvent eventToImplement,
		bool            isAdd,
		FieldBuilder    backingFieldBuilder,
		ILGenerator     msilGenerator)
	{
		// the type of the event to implement should be the same as the type of the backing field
		Assert.Same(backingFieldBuilder.FieldType, eventToImplement.EventHandlerType);

		// the MSIL generator should be the same as the MSIL generator returned by the generated event
		Assert.Same(
			msilGenerator,
			isAdd
				? eventToImplement.AddAccessor.MethodBuilder.GetILGenerator()
				: eventToImplement.RemoveAccessor.MethodBuilder.GetILGenerator());

		Type backingFieldType = backingFieldBuilder.FieldType;

		// get the Delegate.Combine() method  when adding a handler and Delegate.Remove() when removing a handler
		MethodInfo delegateMethod = typeof(Delegate).GetMethod(isAdd ? nameof(Delegate.Combine) : nameof(Delegate.Remove), [typeof(Delegate), typeof(Delegate)]);
		Debug.Assert(delegateMethod != null, nameof(delegateMethod) + " != null");

		// get the System.Threading.Interlocked.CompareExchange(ref object, object, object) method
		MethodInfo interlockedCompareExchangeGenericMethod = typeof(Interlocked)
			.GetMethods()
			.Single(
				method =>
					method.Name == nameof(Interlocked.CompareExchange) &&
					method.GetGenericArguments().Length == 1);
		MethodInfo interlockedCompareExchangeMethod = interlockedCompareExchangeGenericMethod.MakeGenericMethod(backingFieldType);

		// emit code to combine the handler with the multicast delegate in the backing field respectively remove the handler from it
		Debug.Assert(msilGenerator == (isAdd ? eventToImplement.AddAccessor.MethodBuilder.GetILGenerator() : eventToImplement.RemoveAccessor.MethodBuilder.GetILGenerator()));
		msilGenerator.DeclareLocal(backingFieldType); // local 0
		msilGenerator.DeclareLocal(backingFieldType); // local 1
		msilGenerator.DeclareLocal(backingFieldType); // local 2
		Label retryLabel = msilGenerator.DefineLabel();
		if (eventToImplement.Kind == EventKind.Static)
		{
			msilGenerator.Emit(OpCodes.Ldsfld, backingFieldBuilder);
			msilGenerator.Emit(OpCodes.Stloc_0);
			msilGenerator.MarkLabel(retryLabel);
			msilGenerator.Emit(OpCodes.Ldloc_0);
			msilGenerator.Emit(OpCodes.Stloc_1);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Ldarg_0);
			msilGenerator.EmitCall(OpCodes.Call, delegateMethod, null);
			msilGenerator.Emit(OpCodes.Castclass, backingFieldType);
			msilGenerator.Emit(OpCodes.Stloc_2);
			msilGenerator.Emit(OpCodes.Ldsflda, backingFieldBuilder);
			msilGenerator.Emit(OpCodes.Ldloc_2);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Call, interlockedCompareExchangeMethod);
			msilGenerator.Emit(OpCodes.Stloc_0);
			msilGenerator.Emit(OpCodes.Ldloc_0);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Bne_Un_S, retryLabel);
			msilGenerator.Emit(OpCodes.Ret);
		}
		else
		{
			msilGenerator.Emit(OpCodes.Ldarg_0);
			msilGenerator.Emit(OpCodes.Ldfld, backingFieldBuilder);
			msilGenerator.Emit(OpCodes.Stloc_0);
			msilGenerator.MarkLabel(retryLabel);
			msilGenerator.Emit(OpCodes.Ldloc_0);
			msilGenerator.Emit(OpCodes.Stloc_1);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Ldarg_1);
			msilGenerator.EmitCall(OpCodes.Call, delegateMethod, null);
			msilGenerator.Emit(OpCodes.Castclass, backingFieldType);
			msilGenerator.Emit(OpCodes.Stloc_2);
			msilGenerator.Emit(OpCodes.Ldarg_0);
			msilGenerator.Emit(OpCodes.Ldflda, backingFieldBuilder);
			msilGenerator.Emit(OpCodes.Ldloc_2);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Call, interlockedCompareExchangeMethod);
			msilGenerator.Emit(OpCodes.Stloc_0);
			msilGenerator.Emit(OpCodes.Ldloc_0);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Bne_Un_S, retryLabel);
			msilGenerator.Emit(OpCodes.Ret);
		}
	}

	/// <summary>
	/// Implements the event raiser method.
	/// </summary>
	/// <param name="methodToImplement">The method to implement.</param>
	/// <param name="backingFieldBuilder">The field builder of the backing field.</param>
	/// <param name="eventDefinition">The event the raiser method to implement belongs to.</param>
	/// <param name="msilGenerator">MSIL generator attached to the event raiser method to implement.</param>
	public static void ImplementEventRaiserMethod(
		IGeneratedMethod methodToImplement,
		FieldBuilder     backingFieldBuilder,
		IGeneratedEvent  eventDefinition,
		ILGenerator      msilGenerator)
	{
		Assert.Same(msilGenerator, methodToImplement.MethodBuilder.GetILGenerator());
		Assert.Equal(typeof(EventHandler<EventArgs>), eventDefinition.EventHandlerType); // only EventHandler<EventArgs> is supported in tests!

		// the event type is System.EventHandler<EventArgs>
		// => the event raiser will have the signature: void OnEvent()
		FieldInfo eventArgsEmpty = typeof(EventArgs).GetField(nameof(EventArgs.Empty));
		Debug.Assert(eventArgsEmpty != null);
		LocalBuilder handlerLocalBuilder = msilGenerator.DeclareLocal(backingFieldBuilder.FieldType);
		Label label = msilGenerator.DefineLabel();

		if (eventDefinition.Kind == EventKind.Static)
		{
			msilGenerator.Emit(OpCodes.Ldsfld, backingFieldBuilder);
			msilGenerator.Emit(OpCodes.Stloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Brfalse_S, label);
			msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Ldnull); // load sender (null)
		}
		else
		{
			msilGenerator.Emit(OpCodes.Ldarg_0);
			msilGenerator.Emit(OpCodes.Ldfld, backingFieldBuilder);
			msilGenerator.Emit(OpCodes.Stloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Brfalse_S, label);
			msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Ldarg_0); // load sender (this)
			if (methodToImplement.TypeDefinition.TypeBuilder.IsValueType)
			{
				msilGenerator.Emit(OpCodes.Ldobj, methodToImplement.TypeDefinition.TypeBuilder);
				msilGenerator.Emit(OpCodes.Box, methodToImplement.TypeDefinition.TypeBuilder);
			}
		}

		// load event arguments
		msilGenerator.Emit(OpCodes.Ldsfld, eventArgsEmpty);

		// invoke the event
		MethodInfo invokeMethod = backingFieldBuilder.FieldType.GetMethod(nameof(EventHandler<EventArgs>.Invoke));
		Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
		msilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
		msilGenerator.MarkLabel(label);
		msilGenerator.Emit(OpCodes.Ret);
	}

	#endregion

	#region Helpers concerning Properties

	/// <summary>
	/// Tests a property that has been implemented using the <see cref="PropertyImplementation_TestDataStorage"/> implementation strategy.
	/// </summary>
	/// <param name="definition">Type definition the property to test belongs to.</param>
	/// <param name="instance">Instance of the dynamically created type that contains the property.</param>
	/// <param name="propertyName">Name of the property to test.</param>
	/// <param name="expectedGetAccessorVisibility">The expected visibility of the generated property get accessor.</param>
	/// <param name="expectedSetAccessorVisibility">The expected visibility of the generated property set accessor.</param>
	/// <param name="expectedPropertyKind">The expected kind of the generated property.</param>
	/// <param name="expectedPropertyType">The expected type of the property.</param>
	/// <param name="testObjects">Objects to test with (index 0: initial property value, index 1: object to set).</param>
	/// <param name="testDataHandle">Handle of the test objects in the test data storage associated with the property.</param>
	public static void TestPropertyImplementation_TestDataStorage(
		TypeDefinition definition,
		object         instance,
		string         propertyName,
		Visibility     expectedGetAccessorVisibility,
		Visibility     expectedSetAccessorVisibility,
		PropertyKind   expectedPropertyKind,
		Type           expectedPropertyType,
		object[]       testObjects,
		int            testDataHandle)
	{
		// get the type of the generated instance
		Type generatedType = instance.GetType();

		// get the property definition from the type definition
		IGeneratedProperty propertyDefinition = definition.GeneratedProperties.SingleOrDefault(property => property.Name == propertyName);
		Assert.NotNull(propertyDefinition);

		// check whether the property has been created as defined
		PropertyInfo generatedProperty = generatedType.GetProperty(propertyName, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(generatedProperty);
		AssertGeneratedPropertyCompliesToTheDefinition(propertyDefinition, generatedProperty);

		// abort if the property is abstract
		// (there is no implementation to test...)
		if (propertyDefinition.Kind == PropertyKind.Abstract)
			return;

		// reset test data in the backing storage
		TestDataStorage.Set(testDataHandle, testObjects[0]);

		// check whether the get accessor returns the expected initial value
		if (generatedProperty.CanRead)
			Assert.Equal(testObjects[0], generatedProperty.GetValue(instance));

		// check whether the set accessor modifies the test data in the backing storage
		if (generatedProperty.CanWrite)
		{
			// property has a set accessor
			// => use it to modify the data in the backing storage
			generatedProperty.SetValue(instance, testObjects[1]);
			Assert.Equal(testObjects[1], TestDataStorage.Get(testDataHandle));
		}
		else
		{
			// property does not have a set accessor
			// => directly change data in the backing storage
			TestDataStorage.Set(testDataHandle, testObjects[1]);
		}

		// check whether the get accessor returns the changed value
		if (generatedProperty.CanRead)
			Assert.Equal(testObjects[1], generatedProperty.GetValue(instance));
	}

	/// <summary>
	/// Checks whether the specified method complies to its definition.
	/// </summary>
	/// <param name="propertyDefinition">The definition of the method to test.</param>
	/// <param name="generatedProperty">The generated method to test.</param>
	public static void AssertGeneratedPropertyCompliesToTheDefinition(IProperty propertyDefinition, PropertyInfo generatedProperty)
	{
		Assert.NotNull(generatedProperty);
		Assert.Equal(propertyDefinition.Name, generatedProperty.Name);
		Assert.Equal(propertyDefinition.Kind, generatedProperty.ToPropertyKind());
		Assert.Equal(propertyDefinition.PropertyType, generatedProperty.PropertyType);
		MethodInfo getAccessorMethod = generatedProperty.DeclaringType!.GetMethod("get_" + propertyDefinition.Name, ExactDeclaredOnlyBindingFlags);
		MethodInfo setAccessorMethod = generatedProperty.DeclaringType!.GetMethod("set_" + propertyDefinition.Name, ExactDeclaredOnlyBindingFlags);
		AssertGeneratedMethodCompliesToTheDefinition(propertyDefinition.GetAccessor, getAccessorMethod);
		AssertGeneratedMethodCompliesToTheDefinition(propertyDefinition.SetAccessor, setAccessorMethod);
	}

	#endregion

	#region Helpers concerning Methods

	/// <summary>
	/// Tests a method that has been implemented using the <see cref="IncrementMethodImplementation"/> implementation strategy.
	/// </summary>
	/// <param name="definition">Type definition the property to test belongs to.</param>
	/// <param name="instance">Instance of the dynamically created type that contains the method.</param>
	/// <param name="methodName">Name of the method to test.</param>
	/// <param name="expectedMethodKind">The expected kind of the generated method.</param>
	/// <param name="expectedVisibility">The expected visibility of the generated method.</param>
	/// <param name="expectedReturnType">The expected return type of the generated method.</param>
	/// <param name="expectedParameterTypes">The expected parameter types of the generated method.</param>
	public static void TestMethodImplementation_Increment(
		TypeDefinition definition,
		object         instance,
		string         methodName,
		MethodKind     expectedMethodKind,
		Visibility     expectedVisibility,
		Type           expectedReturnType,
		Type[]         expectedParameterTypes)
	{
		// get the type of the generated instance
		Type generatedType = instance.GetType();

		// get the method definition from the type definition
		IGeneratedMethod methodDefinition = definition
			.GeneratedMethods
			.SingleOrDefault(
				method => method.Name == methodName &&
				          method.ReturnType == expectedReturnType &&
				          method.ParameterTypes.SequenceEqual(expectedParameterTypes));
		Assert.NotNull(methodDefinition);

		// check whether the method has been created as defined
		MethodInfo generatedMethod = generatedType
			.GetMethods(ExactDeclaredOnlyBindingFlags)
			.SingleOrDefault(method => method.Name == methodName && method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(expectedParameterTypes));
		Assert.NotNull(generatedMethod);
		AssertGeneratedMethodCompliesToTheDefinition(methodDefinition, generatedMethod);

		// abort if the method is abstract
		// (there is no implementation to test...)
		if (methodDefinition.Kind == MethodKind.Abstract)
			return;

		// invoke the generated method
		// (it just increments the specified value by 1)
		const int inputValue = 1;
		object result = generatedMethod.Invoke(instance, [inputValue]);
		Assert.IsType<int>(result);
		Assert.Equal(inputValue + 1, result);
	}

	/// <summary>
	/// Checks whether the specified method complies to its definition.
	/// </summary>
	/// <param name="methodDefinition">The definition of the method to test.</param>
	/// <param name="generatedMethod">The generated method to test.</param>
	public static void AssertGeneratedMethodCompliesToTheDefinition(IMethod methodDefinition, MethodInfo generatedMethod)
	{
		if (methodDefinition == null)
		{
			Assert.Null(generatedMethod);
			return;
		}

		Assert.NotNull(generatedMethod);
		Assert.Equal(methodDefinition.Kind, generatedMethod.ToMethodKind());
		Assert.Equal(methodDefinition.Visibility, generatedMethod.ToVisibility());
		Assert.Equal(methodDefinition.ReturnType, generatedMethod.ReturnType);
		Assert.Equal(methodDefinition.ParameterTypes, generatedMethod.GetParameters().Select(x => x.ParameterType));

		switch (methodDefinition.Kind)
		{
			case MethodKind.Static:
				Assert.True(generatedMethod.IsStatic); // static
				break;

			case MethodKind.Normal:
				Assert.False(generatedMethod.IsStatic);  // member
				Assert.False(generatedMethod.IsVirtual); // regular (not abstract, virtual or override)
				break;

			case MethodKind.Virtual:
				Assert.False(generatedMethod.IsStatic);                                                            // member
				Assert.True(generatedMethod.IsVirtual && !generatedMethod.IsAbstract && !generatedMethod.IsFinal); // => virtual or override
				Assert.True(generatedMethod.Equals(generatedMethod.GetBaseDefinition()));                          // => virtual
				break;

			case MethodKind.Abstract:
				Assert.False(generatedMethod.IsStatic);  // member
				Assert.True(generatedMethod.IsAbstract); // abstract (not virtual or override)
				break;

			case MethodKind.Override:
				Assert.False(generatedMethod.IsStatic);                                                            // member
				Assert.True(generatedMethod.IsVirtual && !generatedMethod.IsAbstract && !generatedMethod.IsFinal); // => virtual or override
				Assert.False(generatedMethod.Equals(generatedMethod.GetBaseDefinition()));                         // => override
				break;

			default:
				throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Returns a value indicating whether the specified method is overridable.
	/// </summary>
	/// <param name="method">Method to check.</param>
	/// <returns>
	/// <c>true</c> if the specified method is overridable;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public static bool IsOverrideable(MethodInfo method)
	{
		// an overridable method is non-final virtual method that is public, protected (Family) or protected internal (FamilyOrAssembly)
		// (final virtual methods are interface implementations...)
		return method != null &&
		       method.IsVirtual &&
		       !method.IsFinal &&
		       (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
	}

	/// <summary>
	/// Returns a value indicating whether the specified method is abstract.
	/// </summary>
	/// <param name="method">Method to check.</param>
	/// <returns>
	/// <c>true</c> if the specified method is overridable;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public static bool IsAbstract(MethodInfo method)
	{
		// a method is abstract, if it is overridable and marked as abstract
		return IsOverrideable(method) && method.IsAbstract;
	}

	#endregion
}
