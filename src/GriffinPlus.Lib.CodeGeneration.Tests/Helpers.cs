///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Linq;

using Xunit;

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
}
