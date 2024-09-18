///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;

// ReSharper disable SuggestBaseTypeForParameter

namespace GriffinPlus.Lib.CodeGeneration.Tests;

using static Helpers;

/// <summary>
/// Common tests around the <see cref="ClassDefinition"/> class.
/// </summary>
public class ClassDefinitionTests_AbstractBaseClass
{
	#region GetAbstractPropertiesWithoutOverride()

	/// <summary>
	/// Test data for
	/// </summary>
	public static IEnumerable<object[]> GetAbstractPropertiesWithoutOverrideTestData
	{
		get
		{
			yield return [typeof(TestBaseClass_Abstract)];
			yield return [typeof(TestBaseClass_BaseClassMembersHidden_Abstract)];
		}
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.GetAbstractPropertiesWithoutOverride"/> method.
	/// </summary>
	[Theory]
	[MemberData(nameof(GetAbstractPropertiesWithoutOverrideTestData))]
	private void GetAbstractPropertiesWithoutOverride(Type baseClass)
	{
		var definition = new ClassDefinition(baseClass, null, ClassAttributes.None);

		// check whether all abstract properties are returned without implementing any overrider...
		IInheritedProperty[] actualProperties = definition.GetAbstractPropertiesWithoutOverride();
		PropertyInfo[] actualPropertyInfos = actualProperties.Select(x => x.PropertyInfo).ToArray();
		PropertyInfo[] expectedPropertyInfos = GetInheritedProperties(baseClass, includeHidden: false).Where(IsAbstract).ToArray();
		int expectedPropertyCount = expectedPropertyInfos.Length;
		Assert.True(expectedPropertyCount >= 2);
		Assert.Equal(actualPropertyInfos, expectedPropertyInfos);

		// now override the first and the last property with a simple implementation
		// (the kind of implementation is not important here, just some accepted strategy)
		actualProperties[0].Override(PropertyImplementations.Simple);
		actualProperties[^1].Override(PropertyImplementations.Simple);
		expectedPropertyCount -= 2;

		// now override the first abstract property and check again
		// (the first and the last property should not be returned anymore)
		actualProperties = definition.GetAbstractPropertiesWithoutOverride();
		actualPropertyInfos = actualProperties.Select(x => x.PropertyInfo).ToArray();
		expectedPropertyInfos = expectedPropertyInfos.Where(IsAbstract).Skip(1).Take(expectedPropertyCount).ToArray();
		Assert.Equal(expectedPropertyCount, expectedPropertyInfos.Length);
		Assert.Equal(actualPropertyInfos, expectedPropertyInfos);
	}

	#endregion


	//	/// <summary>
	///// Tests the <see cref="ClassDefinition.AddEventOverride{T}(IInheritedEvent{T},IEventImplementation)"/> method
	///// using <see cref="EventImplementation_Standard"/> to implement add/remove accessors and the event raiser method.
	///// </summary>
	//[Theory]
	//[MemberData(nameof(AddEventOverrideTestData_WithImplementationStrategy_Standard))]
	//public void AddEventOverride_WithImplementationStrategy_Standard(
	//	string eventName,
	//	bool       addEventRaiserMethod,
	//	string     eventRaiserName,
	//	Visibility eventRaiserVisibility,
	//	Type       expectedEventRaiserReturnType,
	//	Type[]     expectedEventRaiserParameterTypes)
	//{
	//	// create a new type definition
	//	ClassDefinition definition = CreateTypeDefinition();

	//	// create an instance of the implementation strategy
	//	Type implementationType = typeof(EventImplementation_Standard);
	//	IEventImplementation implementation = addEventRaiserMethod
	//		                                      ? (IEventImplementation)Activator.CreateInstance(implementationType, eventRaiserName, eventRaiserVisibility)
	//		                                      : (IEventImplementation)Activator.CreateInstance(implementationType);

	//	// determine the events that can be overridden
	//	IInheritedEvent[] eventsToOverride = definition
	//		.InheritedEvents
	//		.Where(x => x.Kind is EventKind.Abstract or EventKind.Virtual or EventKind.Override)
	//		.ToArray();

	//	// just to be sure: check the number of inherited overrideable events
	//	Assert.Equal(3, eventsToOverride.Length);

	//	foreach (IInheritedEvent @event in eventsToOverride)
	//	{
	//		// get the AddEvent(...) method to test
	//		MethodInfo addEventMethod = typeof(ClassDefinition)
	//			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
	//			.Where(method => method.Name == nameof(ClassDefinition.AddEventOverride))
	//			.Where(method => method.GetGenericArguments().Length == 1)
	//			.Select(method => method.MakeGenericMethod(@event.EventHandlerType))
	//			.Single(
	//				method => method
	//					.GetParameters()
	//					.Select(parameter => parameter.ParameterType)
	//					.SequenceEqual(
	//					[
	//						typeof(IInheritedEvent<>).MakeGenericType(@event.EventHandlerType),
	//						typeof(IEventImplementation)
	//					]));

	//		// invoke the method to add the event to the type definition
	//		var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(definition, [@event, implementation]);
	//		Assert.NotNull(addedEvent);
	//		Assert.Equal(EventKind.Override, addedEvent.Kind);
	//		Assert.Equal(@event.Visibility, addedEvent.Visibility);
	//		Assert.Equal(@event.EventHandlerType, addedEvent.EventHandlerType);
	//		Assert.Same(implementation, addedEvent.Implementation);
	//	}

	//	// create the defined type, check the result against the definition and create an instance of that type
	//	Type type = definition.CreateType();
	//	CheckTypeAgainstDefinition(type, definition);
	//	object instance = Activator.CreateInstance(type);

	//	// test the implementation of the events
	//	foreach (IInheritedEvent @event in eventsToOverride)
	//	{
	//		TestEventImplementation_Standard(
	//			definition,
	//			instance,
	//			EventKind.Override,
	//			@event.Name,
	//			@event.EventHandlerType,
	//			addEventRaiserMethod,
	//			eventRaiserName,
	//			expectedEventRaiserReturnType,
	//			expectedEventRaiserParameterTypes);
	//	}
	//}
}
