///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;

// ReSharper disable ConvertToLambdaExpression
// ReSharper disable SuggestBaseTypeForParameter

namespace GriffinPlus.Lib.CodeGeneration.Tests;

using static Helpers;

/// <summary>
/// Common tests around the <see cref="ClassDefinition"/> class.
/// </summary>
public abstract class ClassDefinitionTests_AbstractBaseClass<TInheritedTestClass>
{
	/// <summary>
	/// Creates a new type definition instance to test.
	/// </summary>
	/// <param name="name">Name of the type to create (<c>null</c> to create a random name).</param>
	/// <param name="attributes">
	/// Attributes of the type to create
	/// (only flags that are part of <see cref="ClassAttributes"/> are valid).
	/// </param>
	/// <returns>The created type definition instance.</returns>
	public abstract ClassDefinition CreateTypeDefinition(string name = null, TypeAttributes attributes = 0);

	#region GetAbstractPropertiesWithoutOverride()

	/// <summary>
	/// Tests the <see cref="ClassDefinition.GetAbstractPropertiesWithoutOverride"/> method.
	/// </summary>
	[Fact]
	private void GetAbstractPropertiesWithoutOverride()
	{
		ClassDefinition definition = CreateTypeDefinition();

		// check whether all abstract properties are returned without implementing any overrider...
		IInheritedProperty[] actualPropertyDefinitions = definition.GetAbstractPropertiesWithoutOverride();
		PropertyInfo[] actualPropertyInfos = actualPropertyDefinitions.Select(inheritedPropertyDefinition => inheritedPropertyDefinition.PropertyInfo).ToArray();
		PropertyInfo[] expectedPropertyInfos = GetInheritedProperties(typeof(TInheritedTestClass), includeHidden: false).Where(IsAbstract).ToArray();
		int expectedPropertyCount = expectedPropertyInfos.Length;
		Assert.True(expectedPropertyCount >= 2);
		Assert.Equal(actualPropertyInfos, expectedPropertyInfos);

		// now override the first and the last property with a simple implementation
		// (the kind of implementation is not important here, just some accepted strategy)
		actualPropertyDefinitions[0].Override(new PropertyImplementation_TestDataStorage(-1));  // will not work at the end, but
		actualPropertyDefinitions[^1].Override(new PropertyImplementation_TestDataStorage(-1)); // it does not matter for the test...
		expectedPropertyCount -= 2;

		// now override the first abstract property and check again
		// (the first and the last property should not be returned anymore)
		actualPropertyDefinitions = definition.GetAbstractPropertiesWithoutOverride();
		actualPropertyInfos = actualPropertyDefinitions.Select(inheritedPropertyDefinition => inheritedPropertyDefinition.PropertyInfo).ToArray();
		expectedPropertyInfos = expectedPropertyInfos.Where(IsAbstract).Skip(1).Take(expectedPropertyCount).ToArray();
		Assert.Equal(expectedPropertyCount, expectedPropertyInfos.Length);
		Assert.Equal(actualPropertyInfos, expectedPropertyInfos);
	}

	#endregion

	/// <summary>
	/// Tests using the following methods to implement the abstract base class using implementation strategies:<br/>
	/// - <see cref="ClassDefinition.AddEventOverride{T}(IInheritedEvent{T},IEventImplementation)"/><br/>
	/// - <see cref="ClassDefinition.AddPropertyOverride{T}(IInheritedProperty{T},IPropertyImplementation)"/><br/>
	/// - <see cref="ClassDefinition.AddMethodOverride(IInheritedMethod,IMethodImplementation)"/>
	/// </summary>
	// [Fact]
	public void ImplementAbstractBaseClass_Generic_WithImplementationStrategies()
	{
		ImplementAbstractBaseClass_Common(
			(
				typeDefinition,
				inheritedEventToOverride) =>
			{
				// get the AddEventOverride(...) method to test
				MethodInfo addEventOverrideMethod = typeof(ClassDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(ClassDefinition.AddEventOverride))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(inheritedEventToOverride.EventHandlerType!))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual(
							[
								typeof(IInheritedEvent<>).MakeGenericType(inheritedEventToOverride.EventHandlerType!),
								typeof(IEventImplementation)
							]));

				// create an instance of the implementation strategy
				const Visibility eventRaiserVisibility = Visibility.Public;
				IEventImplementation implementation = new TestEventImplementation(
					GetEventRaiserName(inheritedEventToOverride.EventInfo),
					eventRaiserVisibility);

				// invoke the method to add the event override to the type definition
				var addedEventDefinition = (IGeneratedEvent)addEventOverrideMethod.Invoke(
					typeDefinition,
					[
						inheritedEventToOverride,
						implementation
					]);
				Assert.NotNull(addedEventDefinition);

				// the generated event should use the passed implementation
				Assert.Same(addedEventDefinition.Implementation, implementation);

				return addedEventDefinition;
			},
			(
				typeDefinition,
				inheritedPropertyToOverride,
				handle) =>
			{
				IPropertyImplementation implementation = new PropertyImplementation_TestDataStorage(handle);

				// get the AddPropertyOverride(...) method to test
				MethodInfo addPropertyOverrideMethod = typeof(ClassDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(ClassDefinition.AddPropertyOverride))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(inheritedPropertyToOverride.PropertyType))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual(
							[
								typeof(IInheritedProperty<>).MakeGenericType(inheritedPropertyToOverride.PropertyType),
								typeof(IPropertyImplementation)
							]));

				// invoke the method to add the property override to the type definition
				var addedPropertyDefinition = (IGeneratedProperty)addPropertyOverrideMethod.Invoke(
					typeDefinition,
					[
						inheritedPropertyToOverride,
						implementation
					]);
				Assert.NotNull(addedPropertyDefinition);

				// the generated property should use the passed implementation
				Assert.Same(addedPropertyDefinition.Implementation, implementation);

				return addedPropertyDefinition;
			},
			(typeDefinition, inheritedMethodToOverride) =>
			{
				var implementation = new TestMethodImplementation();

				IGeneratedMethod addedMethodDefinition = typeDefinition.AddMethodOverride(
					inheritedMethodToOverride,
					implementation);

				// the generated method should use the passed implementation
				Assert.Same(addedMethodDefinition.Implementation, implementation);

				return addedMethodDefinition;
			});
	}

	/// <summary>
	/// Tests using the following methods to implement the abstract base class using implementation strategies:<br/>
	/// - <see cref="ClassDefinition.AddEventOverride{T}(IInheritedEvent{T},EventAccessorImplementationCallback, EventAccessorImplementationCallback)"/><br/>
	/// -
	/// <see
	///     cref="ClassDefinition.AddPropertyOverride{T}(IInheritedProperty{T},PropertyAccessorImplementationCallback,PropertyAccessorImplementationCallback)"/>
	/// <br/>
	/// - <see cref="ClassDefinition.AddMethodOverride(IInheritedMethod,MethodImplementationCallback)"/>
	/// </summary>
	// [Fact]
	public void ImplementAbstractBaseClass_Generic_WithImplementationCallbacks()
	{
		ImplementAbstractBaseClass_Common(
			(
				typeDefinition,
				inheritedEventToOverride) =>
			{
				// get the AddEventOverride(...) method to test
				MethodInfo addEventOverrideMethod = typeof(ClassDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(ClassDefinition.AddEventOverride))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(inheritedEventToOverride.EventHandlerType!))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual(
							[
								typeof(IInheritedEvent<>).MakeGenericType(inheritedEventToOverride.EventHandlerType!),
								typeof(EventAccessorImplementationCallback),
								typeof(EventAccessorImplementationCallback)
							]));

				// create an instance of the implementation strategy
				// (the strategy itself is not used, but its methods are called directly to declare stuff and implement the accessors)
				const Visibility eventRaiserVisibility = Visibility.Public;
				IEventImplementation implementation = new TestEventImplementation(
					GetEventRaiserName(inheritedEventToOverride.EventInfo),
					eventRaiserVisibility);

				// invoke the method to add the event override to the type definition
				// (the add accessor is implemented first, so declare additional stuff there)
				var addedEventDefinition = (IGeneratedEvent)addEventOverrideMethod.Invoke(
					typeDefinition,
					[
						inheritedEventToOverride,
						(EventAccessorImplementationCallback)
						((eventToImplement, msilGenerator) =>
							{
								implementation.Declare(typeDefinition, eventToImplement);
								implementation.ImplementAddAccessorMethod(typeDefinition, eventToImplement, msilGenerator);
							}),
						(EventAccessorImplementationCallback)
						((eventToImplement, msilGenerator) =>
							{
								implementation.ImplementRemoveAccessorMethod(typeDefinition, eventToImplement, msilGenerator);
							})
					]);
				Assert.NotNull(addedEventDefinition);

				// the generated event should not use an implementation strategy
				Assert.Null(addedEventDefinition.Implementation);

				return addedEventDefinition;
			},
			(
				typeDefinition,
				inheritedPropertyToOverride,
				handle) =>
			{
				IPropertyImplementation implementation = new PropertyImplementation_TestDataStorage(handle);

				// get the AddPropertyOverride(...) method to test
				MethodInfo addPropertyOverrideMethod = typeof(ClassDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(ClassDefinition.AddPropertyOverride))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(inheritedPropertyToOverride.PropertyType))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual(
							[
								typeof(IInheritedProperty<>).MakeGenericType(inheritedPropertyToOverride.PropertyType),
								typeof(PropertyAccessorImplementationCallback),
								typeof(PropertyAccessorImplementationCallback)
							]));

				// invoke the method to add the property override to the type definition
				// (the get accessor is implemented first, so declare additional stuff there)
				var addedPropertyDefinition = (IGeneratedProperty)addPropertyOverrideMethod.Invoke(
					typeDefinition,
					[
						inheritedPropertyToOverride,
						(PropertyAccessorImplementationCallback)
						((property, msilGenerator) =>
							{
								implementation.Declare(property.TypeDefinition, property);
								implementation.ImplementGetAccessorMethod(property.TypeDefinition, property, msilGenerator);
							}),
						(PropertyAccessorImplementationCallback)
						((property, msilGenerator) =>
							{
								implementation.ImplementSetAccessorMethod(property.TypeDefinition, property, msilGenerator);
							})
					]);
				Assert.NotNull(addedPropertyDefinition);

				// the generated property should not use an implementation strategy
				Assert.Null(addedPropertyDefinition.Implementation);

				return addedPropertyDefinition;
			},
			(typeDefinition, inheritedMethodToOverride) =>
			{
				IGeneratedMethod addedMethodDefinition = typeDefinition.AddMethodOverride(inheritedMethodToOverride, TestMethodImplementation.Callback);

				// the generated method should not use an implementation strategy
				Assert.Null(addedMethodDefinition.Implementation);

				return addedMethodDefinition;
			});
	}

	/// <summary>
	/// Tests using the following methods to implement the abstract base class using implementation strategies:<br/>
	/// - <see cref="ClassDefinition.AddEventOverride(IInheritedEvent,IEventImplementation)"/><br/>
	/// - <see cref="ClassDefinition.AddPropertyOverride(IInheritedProperty,IPropertyImplementation)"/><br/>
	/// - <see cref="ClassDefinition.AddMethodOverride(IInheritedMethod,IMethodImplementation)"/>
	/// </summary>
	// [Fact]
	public void ImplementAbstractBaseClass_NonGeneric_WithImplementationStrategies()
	{
		ImplementAbstractBaseClass_Common(
			(
				typeDefinition,
				inheritedEventToOverride) =>
			{
				var implementation = new TestEventImplementation(GetEventRaiserName(inheritedEventToOverride.EventInfo), Visibility.Public);
				IGeneratedEvent addedEventDefinition = typeDefinition.AddEventOverride(inheritedEventToOverride, implementation);

				// the generated event should use the passed implementation
				Assert.Same(addedEventDefinition.Implementation, implementation);

				return addedEventDefinition;
			},
			(
				typeDefinition,
				inheritedPropertyToOverride,
				handle) =>
			{
				var implementation = new PropertyImplementation_TestDataStorage(handle);
				IGeneratedProperty addedPropertyDefinition = typeDefinition.AddPropertyOverride(inheritedPropertyToOverride, implementation);

				// the generated property should use the passed implementation
				Assert.Same(addedPropertyDefinition.Implementation, implementation);

				return addedPropertyDefinition;
			},
			(typeDefinition, inheritedMethodToOverride) =>
			{
				var implementation = new TestMethodImplementation();
				IGeneratedMethod addedMethodDefinition = typeDefinition.AddMethodOverride(inheritedMethodToOverride, implementation);

				// the generated method should use the passed implementation
				Assert.Same(addedMethodDefinition.Implementation, implementation);

				return addedMethodDefinition;
			});
	}

	/// <summary>
	/// Tests using the following methods to implement the abstract base class using implementation strategies:<br/>
	/// - <see cref="ClassDefinition.AddEventOverride(IInheritedEvent,EventAccessorImplementationCallback, EventAccessorImplementationCallback)"/><br/>
	/// -
	/// <see cref="ClassDefinition.AddPropertyOverride(IInheritedProperty,PropertyAccessorImplementationCallback,PropertyAccessorImplementationCallback)"/>
	/// <br/>
	/// - <see cref="ClassDefinition.AddMethodOverride(IInheritedMethod,MethodImplementationCallback)"/>
	/// </summary>
	// [Fact]
	public void ImplementAbstractBaseClass_NonGeneric_WithImplementationCallbacks()
	{
		ImplementAbstractBaseClass_Common(
			(
				typeDefinition,
				inheritedEventToOverride) =>
			{
				// create an instance of the implementation strategy
				// (the strategy itself is not used, but its methods are called directly to declare stuff and implement the accessors)
				const Visibility eventRaiserVisibility = Visibility.Public;
				IEventImplementation implementation = new TestEventImplementation(
					GetEventRaiserName(inheritedEventToOverride.EventInfo),
					eventRaiserVisibility);

				// add the event override to the type definition
				// (the add accessor is implemented first, so declare additional stuff there)
				IGeneratedEvent addedEventDefinition = typeDefinition.AddEventOverride(
					inheritedEventToOverride,
					(eventToImplement, msilGenerator) =>
					{
						implementation.Declare(typeDefinition, eventToImplement);
						implementation.ImplementAddAccessorMethod(typeDefinition, eventToImplement, msilGenerator);
					},
					(eventToImplement, msilGenerator) =>
					{
						implementation.ImplementRemoveAccessorMethod(typeDefinition, eventToImplement, msilGenerator);
					});

				// the generated event should not use an implementation strategy
				Assert.Null(addedEventDefinition.Implementation);

				return addedEventDefinition;
			},
			(
				typeDefinition,
				inheritedPropertyToOverride,
				handle) =>
			{
				IPropertyImplementation implementation = new PropertyImplementation_TestDataStorage(handle);

				// add the property override to the type definition
				IGeneratedProperty addedPropertyDefinition = typeDefinition.AddPropertyOverride(
					inheritedPropertyToOverride,
					(property, msilGenerator) =>
					{
						implementation.Declare(property.TypeDefinition, property);
						implementation.ImplementGetAccessorMethod(property.TypeDefinition, property, msilGenerator);
					},
					(property, msilGenerator) =>
					{
						implementation.ImplementSetAccessorMethod(property.TypeDefinition, property, msilGenerator);
					});

				// the generated property should not use an implementation strategy
				Assert.Null(addedPropertyDefinition.Implementation);

				return addedPropertyDefinition;
			},
			(typeDefinition, inheritedMethodToOverride) =>
			{
				IGeneratedMethod addedMethodDefinition = typeDefinition.AddMethodOverride(inheritedMethodToOverride, TestMethodImplementation.Callback);

				// the generated property should not use an implementation strategy
				Assert.Null(addedMethodDefinition.Implementation);

				return addedMethodDefinition;
			});
	}

	private void ImplementAbstractBaseClass_Common(
		Func<ClassDefinition, IInheritedEvent, IGeneratedEvent>            overrideEventCallback,
		Func<ClassDefinition, IInheritedProperty, int, IGeneratedProperty> overridePropertyCallback,
		Func<ClassDefinition, IInheritedMethod, IGeneratedMethod>          overrideMethodCallback)
	{
		// ------------------------------------------------------------------------------------------------------------------
		// create a new type definition deriving from an abstract base class
		// ------------------------------------------------------------------------------------------------------------------

		ClassDefinition typeDefinition = CreateTypeDefinition();

		// ------------------------------------------------------------------------------------------------------------------
		// override abstract events
		// ------------------------------------------------------------------------------------------------------------------

		// determine the abstract events of the base class
		EventInfo[] abstractEventInfos = GetEventsAccessibleFromDerivedType(typeof(TInheritedTestClass), true)
			.Where(x => IsAbstract(x.AddMethod) || IsAbstract(x.RemoveMethod))
			.ToArray();

		foreach (EventInfo abstractEventInfo in abstractEventInfos)
		{
			// get the inherited event from the class definition
			IInheritedEvent inheritedEventToOverride = typeDefinition.InheritedEvents.SingleOrDefault(inheritedEvent => inheritedEvent.Name == abstractEventInfo.Name);
			Assert.NotNull(inheritedEventToOverride);
			Assert.Equal(abstractEventInfo, inheritedEventToOverride.EventInfo);

			// add event to the type definition
			IGeneratedEvent addedEventDefinition = overrideEventCallback(typeDefinition, inheritedEventToOverride);

			// the generated event property should be an overrider with the expected event handler
			Assert.NotNull(addedEventDefinition);
			Assert.Equal(EventKind.Override, addedEventDefinition.Kind);
			Assert.Equal(abstractEventInfo.ToVisibility(), addedEventDefinition.Visibility);
			Assert.Equal(abstractEventInfo.EventHandlerType, addedEventDefinition.EventHandlerType);
		}

		// ------------------------------------------------------------------------------------------------------------------
		// override abstract properties
		// ------------------------------------------------------------------------------------------------------------------

		// determine the abstract properties of the base class
		PropertyInfo[] abstractPropertyInfos = GetInheritedProperties(typeof(TInheritedTestClass), true)
			.Where(x => IsAbstract(x.GetMethod) || IsAbstract(x.SetMethod))
			.ToArray();

		using var storage = new TestDataStorage();
		var testDataByProperty = new Dictionary<PropertyInfo, (int, object[])>(); // (Handle,TestObjects)
		foreach (PropertyInfo abstractPropertyInfo in abstractPropertyInfos)
		{
			// create an instance of the implementation strategy and wire up the test objects
			// (the strategy itself is not used, but its methods are called directly to declare stuff and implement the accessors)
			object[] propertyTestArguments = null;
			if (abstractPropertyInfo.PropertyType == typeof(int))
			{
				propertyTestArguments = [1, 2];
			}
			else if (abstractPropertyInfo.PropertyType == typeof(string))
			{
				propertyTestArguments = ["InitialValue", "NewValue"];
			}
			Assert.NotNull(propertyTestArguments);
			int handle = storage.Add(propertyTestArguments[0]); // initial value
			testDataByProperty.Add(abstractPropertyInfo, (handle, propertyTestArguments));

			// get the inherited property from the class definition
			IInheritedProperty inheritedPropertyToOverride = typeDefinition.InheritedProperties.SingleOrDefault(inheritedProperty => inheritedProperty.Name == abstractPropertyInfo.Name);
			Assert.NotNull(inheritedPropertyToOverride);
			Assert.Equal(abstractPropertyInfo, inheritedPropertyToOverride.PropertyInfo);

			// add the property to the type definition
			IGeneratedProperty addedPropertyDefinition = overridePropertyCallback(
				typeDefinition,
				inheritedPropertyToOverride,
				handle);

			// the generated property should be an overrider with the expected property type
			Assert.NotNull(addedPropertyDefinition);
			Assert.Equal(PropertyKind.Override, addedPropertyDefinition.Kind);
			Assert.Equal(abstractPropertyInfo.PropertyType, addedPropertyDefinition.PropertyType);

			// the get accessor should have the expected visibility, no parameters and return the property type
			Assert.Equal(abstractPropertyInfo.GetMethod != null, addedPropertyDefinition.GetAccessor != null);
			if (abstractPropertyInfo.GetMethod != null && addedPropertyDefinition.GetAccessor != null)
			{
				// check the visibility of the generated getter
				Assert.Equal(abstractPropertyInfo.GetMethod.ToVisibility(), addedPropertyDefinition.GetAccessor.Visibility);

				// the generated getter should have no parameters
				Assert.Empty(addedPropertyDefinition.GetAccessor.ParameterTypes);

				// the generated getter should have the property type as its return type
				Assert.Equal(abstractPropertyInfo.GetMethod.ReturnType, addedPropertyDefinition.GetAccessor.MethodInfo.ReturnType);
			}

			// the set accessor should have the expected visibility, a parameter of the property type and no return type
			Assert.Equal(abstractPropertyInfo.SetMethod != null, addedPropertyDefinition.SetAccessor != null);
			if (abstractPropertyInfo.SetMethod != null && addedPropertyDefinition.SetAccessor != null)
			{
				// check the visibility of the generated setter
				Assert.Equal(abstractPropertyInfo.SetMethod.ToVisibility(), addedPropertyDefinition.SetAccessor.Visibility);

				// the generated setter should have a single parameter of the property type
				Assert.Single(addedPropertyDefinition.SetAccessor.ParameterTypes);
				Assert.Equal(abstractPropertyInfo.PropertyType, addedPropertyDefinition.SetAccessor.ParameterTypes[0]);

				// the generated setter should not have a return type
				Assert.Equal(typeof(void), addedPropertyDefinition.SetAccessor.ReturnType);
			}
		}

		// ------------------------------------------------------------------------------------------------------------------
		// override abstract methods
		// ------------------------------------------------------------------------------------------------------------------

		// determine the abstract methods of the base class
		MethodInfo[] abstractMethodInfos = GetMethodsAccessibleFromDerivedType(typeof(TInheritedTestClass), true)
			.Where(IsAbstract)
			.ToArray();

		foreach (MethodInfo abstractMethodInfo in abstractMethodInfos)
		{
			// get the inherited method from the class definition
			IInheritedMethod inheritedMethodToOverride = typeDefinition
				.InheritedMethods
				.SingleOrDefault(
					inheritedMethod =>
						inheritedMethod.Name == abstractMethodInfo.Name &&
						inheritedMethod.ParameterTypes.SequenceEqual(abstractMethodInfo.GetParameters().Select(parameter => parameter.ParameterType)));
			Assert.NotNull(inheritedMethodToOverride);
			Assert.Equal(abstractMethodInfo, inheritedMethodToOverride.MethodInfo);

			// add the method to the type definition
			IGeneratedMethod addedMethodDefinition = overrideMethodCallback(typeDefinition, inheritedMethodToOverride);

			// the generated method should be an overrider with the expected signature
			Assert.NotNull(addedMethodDefinition);
			Assert.Equal(MethodKind.Override, addedMethodDefinition.Kind);
			Assert.Equal(abstractMethodInfo.ToVisibility(), addedMethodDefinition.Visibility);
			Assert.Equal(abstractMethodInfo.GetParameters().Select(parameter => parameter.ParameterType), addedMethodDefinition.ParameterTypes);
			Assert.Equal(abstractMethodInfo.ReturnType, addedMethodDefinition.MethodInfo.ReturnType);
		}

		// ------------------------------------------------------------------------------------------------------------------
		// create the defined type, check the result against the definition and create an instance of that type
		// ------------------------------------------------------------------------------------------------------------------

		Type type = typeDefinition.CreateType();
		CheckTypeAgainstDefinition(type, typeDefinition);
		object instance = Activator.CreateInstance(type);

		// ------------------------------------------------------------------------------------------------------------------
		// test the implementation of the events
		// ------------------------------------------------------------------------------------------------------------------

		foreach (EventInfo eventInfo in abstractEventInfos)
		{
			// all base class events are EventHandler<EventArgs>
			// => event raiser will always be: void Raise<EventName>()
			TestEventImplementation(
				typeDefinition,                // type definition with the event to test
				instance,                      // instance of the test class providing the event to test
				eventInfo.Name,                // name of the event to test
				EventKind.Override,            // expected kind of the event (always an overrider)
				eventInfo.ToVisibility(),      // expected visibility of the event
				eventInfo.EventHandlerType,    // expected type of the event
				true,                          // implementation strategy should have added an event raiser method
				GetEventRaiserName(eventInfo), // expected name of the event raiser method
				typeof(void),                  // expected return type of the event raiser method
				Type.EmptyTypes);              // expected parameter types of the event raiser method
		}

		// ------------------------------------------------------------------------------------------------------------------
		// test the implementation of the properties
		// ------------------------------------------------------------------------------------------------------------------

		foreach (PropertyInfo propertyInfo in abstractPropertyInfos)
		{
			MethodInfo getAccessorMethod = propertyInfo.GetGetMethod();
			MethodInfo setAccessorMethod = propertyInfo.GetSetMethod();
			TestPropertyImplementation_TestDataStorage(
				typeDefinition,                                                                   // type definition with the property to test
				instance,                                                                         // instance of the test class providing the property to test
				propertyInfo.Name,                                                                // name of the property to test
				getAccessorMethod != null ? getAccessorMethod.ToVisibility() : Visibility.Public, // expected visibility of the get accessor
				setAccessorMethod != null ? setAccessorMethod.ToVisibility() : Visibility.Public, // expected visibility of the set accessor
				PropertyKind.Override,                                                            // expected kind of the property (always an overrider)
				propertyInfo.PropertyType,                                                        // expected type of the property
				testDataByProperty[propertyInfo].Item2,                                           // test object array (index 0: initial value, index 1: new value)
				testDataByProperty[propertyInfo].Item1);                                          // test object handle
		}

		// ------------------------------------------------------------------------------------------------------------------
		// test the implementation of the methods
		// ------------------------------------------------------------------------------------------------------------------

		foreach (MethodInfo methodInfo in abstractMethodInfos)
		{
			CreateMethodTestData(
				methodInfo.ReturnType,
				methodInfo.GetParameters().Length,
				out Type[] parameterTypes,
				out object[] testArguments,
				out object expectedTestResult);

			Assert.Equal(parameterTypes, methodInfo.GetParameters().Select(parameter => parameter.ParameterType));

			TestMethodImplementation(
				typeDefinition,            // type definition with the method to test
				instance,                  // instance of the test class providing the method to test
				methodInfo.Name,           // name of the method to test
				MethodKind.Override,       // expected kind of the method (always an overrider)
				methodInfo.ToVisibility(), // expected visibility of the method
				methodInfo.ReturnType,     // expected return type of the method
				parameterTypes,            // expected parameter types of the method
				testArguments,             // arguments to pass to the method when testing it
				expectedTestResult);       // the expected return value of the method when testing it
		}
	}

	private static string GetEventRaiserName(EventInfo @event)
	{
		return "Raise" + @event.Name;
	}
}
