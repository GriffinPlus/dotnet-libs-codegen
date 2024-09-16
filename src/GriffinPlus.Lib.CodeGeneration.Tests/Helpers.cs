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
	/// <summary>
	/// Gets constructors of the specified type that can be accessed by a type deriving from that type.
	/// </summary>
	/// <param name="type">Type to inspect.</param>
	/// <returns>The constructors of the specified type that can be accessed by a type deriving from that type.</returns>
	public static HashSet<ConstructorInfo> GetConstructorsAccessibleFromDerivedType(Type type)
	{
		var constructorInfos = new HashSet<ConstructorInfo>();
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		foreach (ConstructorInfo constructorInfo in type.GetConstructors(flags))
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
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var eventInfos = new HashSet<EventInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (EventInfo eventInfo in typeToInspect.GetEvents(flags))
			{
				// skip event if it is 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				MethodInfo addMethod = eventInfo.GetAddMethod(true);
				MethodInfo removeMethod = eventInfo.GetRemoveMethod(true);
				Debug.Assert(addMethod != null, nameof(addMethod) + " != null");
				Debug.Assert(removeMethod != null, nameof(removeMethod) + " != null");
				if (addMethod.IsPrivate || addMethod.IsAssembly) continue;
				if (removeMethod.IsPrivate || removeMethod.IsAssembly) continue;

				// skip event if it is virtual and already in the set of events (also covers abstract, virtual and overridden events)
				// => only the most specific implementation gets into the returned set of events
				if ((addMethod.IsVirtual || removeMethod.IsVirtual) && eventInfos.Any(x => HasSameSignature(x, eventInfo)))
					continue;

				// skip event if an event with the same signature is already in the set of events
				// and hidden events should not be returned
				if (!includeHidden && eventInfos.Any(x => HasSameSignature(x, eventInfo)))
					continue;

				// the event is accessible from a derived class in some other assembly
				eventInfos.Add(eventInfo);
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
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var fieldInfos = new HashSet<FieldInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (FieldInfo fieldInfo in typeToInspect.GetFields(flags))
			{
				// skip field if it is 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				if (fieldInfo.IsPrivate || fieldInfo.IsAssembly) continue;

				// skip field if a field with the same signature is already in the set of fields
				// and hidden fields should not be returned
				if (!includeHidden && fieldInfos.Any(x => HasSameSignature(x, fieldInfo)))
					continue;

				// the field is accessible from a derived class in some other assembly
				fieldInfos.Add(fieldInfo);
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
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var methodInfos = new HashSet<MethodInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (MethodInfo methodInfo in typeToInspect.GetMethods(flags))
			{
				// skip method if it is 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				if (methodInfo.IsPrivate || methodInfo.IsAssembly) continue;

				// skip methods with a special name as these methods usually cannot be called by the user directly
				// (add/remove accessor methods of events and get/set accessor methods of properties)
				if (methodInfo.IsSpecialName) continue;

				// skip method if it is virtual and already in the set of methods (also covers abstract, virtual and overridden events)
				// => only the most specific implementation gets into the returned set of methods
				if (methodInfo.IsVirtual && methodInfos.Any(x => HasSameSignature(x, methodInfo)))
					continue;

				// skip property if a method with the same signature is already in the set of methods
				// and hidden methods should not be returned
				if (!includeHidden && methodInfos.Any(x => HasSameSignature(x, methodInfo)))
					continue;

				// the method is accessible from a derived class in some other assembly
				methodInfos.Add(methodInfo);
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
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var propertyInfos = new HashSet<PropertyInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (PropertyInfo propertyInfo in typeToInspect.GetProperties(flags))
			{
				// skip accessors that are 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				int callableAccessorCount = 0;

				// check visibility of the 'get' accessor
				MethodInfo getAccessor = propertyInfo.GetGetMethod(true);
				if (getAccessor != null && !getAccessor.IsPrivate && !getAccessor.IsAssembly)
					callableAccessorCount++;

				// check visibility of the 'set' accessor
				MethodInfo setAccessor = propertyInfo.GetSetMethod(true);
				if (setAccessor != null && !setAccessor.IsPrivate && !setAccessor.IsAssembly)
					callableAccessorCount++;

				// skip property if neither a get accessor method nor a set accessor method are accessible
				if (callableAccessorCount == 0) continue;

				// skip property if it is already in the set of properties and its accessor methods are virtual
				// (the check for virtual also covers abstract, virtual and override methods)
				// => only the most specific implementation gets into the returned set of properties
				if (propertyInfos.Any(x => HasSameSignature(x, propertyInfo)))
				{
					if (getAccessor != null && getAccessor.IsVirtual) continue;
					if (setAccessor != null && setAccessor.IsVirtual) continue;
				}

				// skip property if a property with the same signature is already in the set of properties
				// and hidden properties should not be returned
				if (!includeHidden && propertyInfos.Any(x => HasSameSignature(x, propertyInfo)))
					continue;

				// the property is accessible from a derived class in some other assembly
				propertyInfos.Add(propertyInfo);
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
		MethodInfo getter = property.GetGetMethod(true);
		MethodInfo setter = property.GetSetMethod(true);
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
		return x.Name == y.Name && x.GetParameters().Select(z => z.ParameterType).SequenceEqual(y.GetParameters().Select(z => z.ParameterType));
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
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var inheritedProperties = new HashSet<PropertyInfo>();
		Type typeToInspect = type;
		while (typeToInspect != null)
		{
			foreach (PropertyInfo property in typeToInspect.GetProperties(flags))
			{
				// skip accessors that are 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				int callableAccessorCount = 0;

				// check visibility of the 'get' accessor
				MethodInfo getAccessor = property.GetGetMethod(true);
				if (getAccessor != null && !getAccessor.IsPrivate && !getAccessor.IsAssembly)
					callableAccessorCount++;

				// check visibility of the 'set' accessor
				MethodInfo setAccessor = property.GetSetMethod(true);
				if (setAccessor != null && !setAccessor.IsPrivate && !setAccessor.IsAssembly)
					callableAccessorCount++;

				// skip property if neither a get accessor method nor a set accessor method are accessible
				if (callableAccessorCount == 0) continue;

				// skip property if it is already in the set of properties and its accessor methods are virtual
				// (the check for virtual also covers abstract, virtual and override methods)
				// => only the most specific implementation gets into the returned set of properties
				if (inheritedProperties.Any(x => HasSameSignature(x, property)))
				{
					if (getAccessor != null && getAccessor.IsVirtual) continue;
					if (setAccessor != null && setAccessor.IsVirtual) continue;
				}

				// skip property if a property with the same signature is already in the set of properties
				// and hidden properties should not be returned
				if (!includeHidden && inheritedProperties.Any(x => HasSameSignature(x, property)))
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
		var expectedConstructors = new HashSet<ConstructorInfo>(definition.Constructors.Select(x => x.ConstructorInfo));
		const BindingFlags constructorBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		var actualConstructors = new HashSet<ConstructorInfo>(type.GetConstructors(constructorBindingFlags));
		Assert.Equal(expectedConstructors, actualConstructors, ConstructorEqualityComparer.Instance);

		// prepare binding flags for lookups using reflection (include all members that are declared by the specified type)
		const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		// check whether the expected fields have been declared
		var expectedFields = new HashSet<FieldInfo>(definition.GeneratedFields.Select(x => x.FieldInfo));
		var actualFields = new HashSet<FieldInfo>(type.GetFields(bindingFlags));
		Assert.Equal(expectedFields, actualFields, FieldEqualityComparer.Instance);

		// check whether the expected events have been declared
		// (the EventComparisonWrapper class handles the comparison on its own, no external comparer needed)
		var expectedEvents = new HashSet<EventComparisonWrapper>(definition.GeneratedEvents.Select(x => new EventComparisonWrapper(x)));
		var actualEvents = new HashSet<EventComparisonWrapper>(type.GetEvents(bindingFlags).Select(x => new EventComparisonWrapper(x)));
		Assert.Equal(expectedEvents, actualEvents);

		// check whether the expected properties have been declared
		var expectedProperties = new HashSet<PropertyComparisonWrapper>(definition.GeneratedProperties.Select(x => new PropertyComparisonWrapper(x)));
		var actualProperties = new HashSet<PropertyComparisonWrapper>(type.GetProperties(bindingFlags).Select(x => new PropertyComparisonWrapper(x)));
		Assert.Equal(expectedProperties, actualProperties);

		// check whether the expected methods have been declared
		var expectedMethods = new HashSet<MethodComparisonWrapper>(definition.GeneratedMethods.Select(x => new MethodComparisonWrapper(x)));
		var actualMethods = new HashSet<MethodComparisonWrapper>(type.GetMethods(bindingFlags).Where(x => !x.IsSpecialName).Select(x => new MethodComparisonWrapper(x)));
		Assert.Equal(expectedMethods, actualMethods);
	}

	#region Helpers concerning Events

	/// <summary>
	/// Tests an event that has been implemented using the <see cref="EventImplementation_Standard"/> implementation strategy.
	/// </summary>
	/// <param name="definition">Type definition the event to test belongs to.</param>
	/// <param name="instance">Instance of the dynamically created type that contains the event.</param>
	/// <param name="eventKind">The expected kind of the generated event.</param>
	/// <param name="eventName">Name of the added event.</param>
	/// <param name="eventHandlerType">
	/// Type of the event handler. For the sake of simplicity only the following event handler types are supported:
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
	/// <param name="eventRaiserName">
	/// Name of the added event raiser method
	/// (<c>null</c> to let the implementation strategy choose a name).
	/// </param>
	/// <param name="expectedEventRaiserReturnType">The expected return type of the event raiser method, if any.</param>
	/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the event raiser method, if any.</param>
	public static void TestEventImplementation_Standard(
		TypeDefinition definition,
		object         instance,
		EventKind      eventKind,
		string         eventName,
		Type           eventHandlerType,
		bool           strategyAddsEventRaiserMethod,
		string         eventRaiserName,
		Type           expectedEventRaiserReturnType,
		Type[]         expectedEventRaiserParameterTypes)
	{
		// get the type of the generated instance
		Type generatedType = instance.GetType();

		// get generated event accessor methods
		BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		MethodInfo addAccessorMethod = generatedType.GetMethod("add_" + eventName, bindingFlags);
		MethodInfo removeAccessorMethod = generatedType.GetMethod("remove_" + eventName, bindingFlags);

		// check whether the generated accessor methods have the expected properties
		Assert.NotNull(addAccessorMethod);
		Assert.NotNull(removeAccessorMethod);
		switch (eventKind)
		{
			case EventKind.Static:
				Assert.True(addAccessorMethod.IsStatic);    // static
				Assert.True(removeAccessorMethod.IsStatic); // static
				break;

			case EventKind.Normal:
				Assert.False(addAccessorMethod.IsStatic);     // member
				Assert.False(addAccessorMethod.IsVirtual);    // regular (not abstract, virtual or override)
				Assert.False(removeAccessorMethod.IsStatic);  // member
				Assert.False(removeAccessorMethod.IsVirtual); // regular (not abstract, virtual or override)
				break;

			case EventKind.Virtual:
				Assert.False(addAccessorMethod.IsStatic);                                                                         // member
				Assert.True(addAccessorMethod.IsVirtual && !addAccessorMethod.IsAbstract && !addAccessorMethod.IsFinal);          // => virtual or override
				Assert.True(addAccessorMethod.Equals(addAccessorMethod.GetBaseDefinition()));                                     // => virtual
				Assert.False(removeAccessorMethod.IsStatic);                                                                      // member
				Assert.True(removeAccessorMethod.IsVirtual && !removeAccessorMethod.IsAbstract && !removeAccessorMethod.IsFinal); // => virtual or override
				Assert.True(removeAccessorMethod.Equals(removeAccessorMethod.GetBaseDefinition()));                               // => virtual
				break;

			case EventKind.Abstract:
				Assert.False(addAccessorMethod.IsStatic);     // member
				Assert.True(addAccessorMethod.IsAbstract);    // abstract (not virtual or override)
				Assert.False(removeAccessorMethod.IsStatic);  // member
				Assert.True(removeAccessorMethod.IsAbstract); // abstract (not virtual or override)
				break;

			case EventKind.Override:
				Assert.False(addAccessorMethod.IsStatic);                                                                         // member
				Assert.True(addAccessorMethod.IsVirtual && !addAccessorMethod.IsAbstract && !addAccessorMethod.IsFinal);          // => virtual or override
				Assert.False(addAccessorMethod.Equals(addAccessorMethod.GetBaseDefinition()));                                    // => override
				Assert.False(removeAccessorMethod.IsStatic);                                                                      // member
				Assert.True(removeAccessorMethod.IsVirtual && !removeAccessorMethod.IsAbstract && !removeAccessorMethod.IsFinal); // => virtual or override
				Assert.False(removeAccessorMethod.Equals(removeAccessorMethod.GetBaseDefinition()));                              // => override
				break;
		}

		// abort if the implementation strategy is not expected to have added an event raiser method
		if (!strategyAddsEventRaiserMethod)
		{
			Assert.Empty(definition.GeneratedMethods);
			return;
		}

		// the implementation strategy should have added an event raiser method

		// determine the name of the event raiser method the implementation strategy should have added
		string expectedEventRaiserName = eventRaiserName ?? "On" + eventName;

		// check whether the implementation strategy has added the event raiser method to the type definition
		Assert.Single(definition.GeneratedMethods);
		IGeneratedMethod eventRaiserMethodDefinition = definition.GeneratedMethods.Single();
		Assert.Equal(expectedEventRaiserName, eventRaiserMethodDefinition.Name);
		Assert.Equal(expectedEventRaiserReturnType, eventRaiserMethodDefinition.ReturnType);
		Assert.Equal(expectedEventRaiserParameterTypes, eventRaiserMethodDefinition.ParameterTypes);

		// get the event raiser method
		MethodInfo eventRaiserMethod = generatedType.GetMethods(bindingFlags).SingleOrDefault(x => x.Name == expectedEventRaiserName);
		Assert.NotNull(eventRaiserMethod);

		// prepare an event handler to register with the event
		Delegate handler = null;
		bool handlerWasCalled = false;
		object[] eventRaiserArguments = null;
		object expectedReturnValue = null;
		if (eventHandlerType == typeof(EventHandler))
		{
			// set up an event handler to test with
			eventRaiserArguments = [];
			handler = new EventHandler(
				(sender, e) =>
				{
					if (eventKind == EventKind.Static)
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
		else if (eventHandlerType == typeof(EventHandler<EventArgs>))
		{
			// set up an event handler to test with
			eventRaiserArguments = [];
			handler = new EventHandler<EventArgs>(
				(sender, e) =>
				{
					if (eventKind == EventKind.Static)
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
		else if (eventHandlerType == typeof(EventHandler<SpecializedEventArgs>))
		{
			// set up an event handler to test with
			eventRaiserArguments = [SpecializedEventArgs.Empty];
			handler = new EventHandler<SpecializedEventArgs>(
				(sender, e) =>
				{
					if (eventKind == EventKind.Static)
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
		else if (eventHandlerType == typeof(Action))
		{
			// set up an event handler to test with
			eventRaiserArguments = [];
			handler = new Action(() => { handlerWasCalled = true; });
		}
		else if (eventHandlerType == typeof(Action<int>))
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
		else if (eventHandlerType == typeof(Func<long>))
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
		else if (eventHandlerType == typeof(Func<int, long>))
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
		addAccessorMethod.Invoke(instance, [handler]);
		object actualHandlerReturnValue = eventRaiserMethod.Invoke(instance, eventRaiserArguments);
		Assert.True(handlerWasCalled);
		Assert.Equal(expectedReturnValue, actualHandlerReturnValue);

		// remove the event handler from the event and raise it
		// => the handler should not be called anymore
		handlerWasCalled = false;
		removeAccessorMethod.Invoke(instance, [handler]);
		eventRaiserMethod.Invoke(instance, eventRaiserArguments);
		Assert.False(handlerWasCalled);
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
		MethodInfo delegateMethod = typeof(Delegate).GetMethod(isAdd ? "Combine" : "Remove", [typeof(Delegate), typeof(Delegate)]);
		Debug.Assert(delegateMethod != null, nameof(delegateMethod) + " != null");

		// get the System.Threading.Interlocked.CompareExchange(ref object, object, object) method
		MethodInfo interlockedCompareExchangeGenericMethod = typeof(Interlocked).GetMethods().Single(m => m.Name == "CompareExchange" && m.GetGenericArguments().Length == 1);
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
	/// <param name="method">The method to implement.</param>
	/// <param name="backingFieldBuilder">The field builder of the backing field.</param>
	/// <param name="event">The event the raiser method to implement belongs to.</param>
	/// <param name="msilGenerator">MSIL generator attached to the event raiser method to implement.</param>
	public static void ImplementEventRaiserMethod(
		IGeneratedMethod method,
		FieldBuilder     backingFieldBuilder,
		IGeneratedEvent  @event,
		ILGenerator      msilGenerator)
	{
		Assert.Same(msilGenerator, method.MethodBuilder.GetILGenerator());
		Assert.Equal(typeof(EventHandler<EventArgs>), @event.EventHandlerType);

		// the event type is System.EventHandler<EventArgs>
		// => the event raiser will have the signature: void OnEvent()
		FieldInfo eventArgsEmpty = typeof(EventArgs).GetField("Empty");
		Debug.Assert(eventArgsEmpty != null);
		LocalBuilder handlerLocalBuilder = msilGenerator.DeclareLocal(backingFieldBuilder.FieldType);
		Label label = msilGenerator.DefineLabel();

		if (@event.Kind == EventKind.Static)
		{
			msilGenerator.Emit(OpCodes.Ldsfld, backingFieldBuilder);
			msilGenerator.Emit(OpCodes.Stloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Brfalse_S, label);
			msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Ldnull);                 // load sender (null)
			msilGenerator.Emit(OpCodes.Ldsfld, eventArgsEmpty); // load event arguments
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
			if (method.TypeDefinition.TypeBuilder.IsValueType)
			{
				msilGenerator.Emit(OpCodes.Ldobj, method.TypeDefinition.TypeBuilder);
				msilGenerator.Emit(OpCodes.Box, method.TypeDefinition.TypeBuilder);
			}

			msilGenerator.Emit(OpCodes.Ldsfld, eventArgsEmpty); // load event arguments
		}

		MethodInfo invokeMethod = backingFieldBuilder.FieldType.GetMethod("Invoke");
		Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
		msilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
		msilGenerator.MarkLabel(label);
		msilGenerator.Emit(OpCodes.Ret);
	}

	#endregion
}
