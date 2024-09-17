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
#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
using System.Windows;

#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
// namespace is not needed on non-windows platforms
#else
#error Unhandled Target Framework.
#endif

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Definition of a type to create dynamically.
/// </summary>
public sealed class ClassDefinition : TypeDefinition
{
	/// <summary>
	/// Initializes a new definition of a class not deriving from a base type.
	/// </summary>
	/// <param name="name">Name of the class to create (<c>null</c> to create a name dynamically).</param>
	/// <param name="attributes">Attributes of the class.</param>
	public ClassDefinition(
		string          name       = null,
		ClassAttributes attributes = ClassAttributes.None) : base(
		module: null,
		isValueType: false,
		name: name,
		attributes: (TypeAttributes)attributes) { }

	/// <summary>
	/// Initializes a new definition of a class deriving from the specified base class.
	/// </summary>
	/// <param name="baseClass">Base class to derive the created class from.</param>
	/// <param name="name">Name of the class to create (<c>null</c> to keep the name of the base class).</param>
	/// <param name="attributes">Attributes of the class.</param>
	/// <exception cref="ArgumentNullException"><paramref name="baseClass"/> is <c>null</c>.</exception>
	public ClassDefinition(
		Type            baseClass,
		string          name       = null,
		ClassAttributes attributes = ClassAttributes.None) : base(
		module: null,
		baseClass: baseClass,
		name: name,
		attributes: (TypeAttributes)attributes) { }

	/// <summary>
	/// Initializes a new definition of a class not deriving from a base type
	/// (associates the type definition with the specified module definition, for internal use only).
	/// </summary>
	/// <param name="module">Module definition to associate the class definition with.</param>
	/// <param name="name">Name of the class to create (<c>null</c> to create a name dynamically).</param>
	/// <param name="attributes">Attributes of the class.</param>
	internal ClassDefinition(
		ModuleDefinition module,
		string           name       = null,
		ClassAttributes  attributes = ClassAttributes.None) : base(
		module: module,
		isValueType: false,
		name: name,
		attributes: (TypeAttributes)attributes) { }

	/// <summary>
	/// Initializes a new definition of a class deriving from the specified base class
	/// (associates the type definition with the specified module definition, for internal use only).
	/// </summary>
	/// <param name="module">Module definition to associate the class definition with.</param>
	/// <param name="baseClass">Base class to derive the created class from.</param>
	/// <param name="name">Name of the class to create (<c>null</c> to keep the name of the base class).</param>
	/// <param name="attributes">Attributes of the class.</param>
	/// <exception cref="ArgumentNullException"><paramref name="baseClass"/> is <c>null</c>.</exception>
	internal ClassDefinition(
		ModuleDefinition module,
		Type             baseClass,
		string           name       = null,
		ClassAttributes  attributes = ClassAttributes.None) : base(
		module: module,
		baseClass: baseClass,
		name: name,
		attributes: (TypeAttributes)attributes) { }

	/// <summary>
	/// Adds pass-through constructors for public constructors exposed by the base class, if any.
	/// This only adds constructors that have not been added explicitly before.
	/// </summary>
	/// <returns>The added constructors.</returns>
	public IGeneratedConstructor[] AddPassThroughConstructors()
	{
		var constructors = new List<IGeneratedConstructor>();

		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		Debug.Assert(TypeBuilder.BaseType != null, "TypeBuilder.BaseType != null");
		ConstructorInfo[] constructorInfos = TypeBuilder.BaseType.GetConstructors(flags);
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (ConstructorInfo constructorInfo in constructorInfos)
		{
			// skip constructor if it is private or internal
			// (cannot be accessed by a derived type defined in another assembly)
			if (constructorInfo.IsPrivate || constructorInfo.IsAssembly) continue;

			// ensure that the pass-through constructor to generate is not specified explicitly
			Type[] parameterTypes = constructorInfo.GetParameters().Select(x => x.ParameterType).ToArray();
			bool skipConstructor = Constructors.Any(definition => definition.ParameterTypes.SequenceEqual(parameterTypes));

			if (skipConstructor)
			{
				// a constructor with the parameters was explicitly defined
				// => skip adding pass-through constructor
				continue;
			}

			// add pass-through constructor
			IGeneratedConstructor constructor = AddConstructor(Visibility.Public, parameterTypes, ImplementPassThroughConstructorBaseCall);
			constructors.Add(constructor);
		}

		return [.. constructors];
	}

	/// <summary>
	/// Emits code to call the base class constructor with the same parameter types.
	/// </summary>
	/// <param name="generatedConstructor">Constructor that needs to emit code for calling a constructor of its base class.</param>
	/// <param name="msilGenerator">MSIL code generator attached to the constructor.</param>
	private void ImplementPassThroughConstructorBaseCall(IGeneratedConstructor generatedConstructor, ILGenerator msilGenerator)
	{
		// find base class constructor
		Type[] parameterTypes = generatedConstructor.ParameterTypes.ToArray();
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		Debug.Assert(TypeBuilder.BaseType != null, "TypeBuilder.BaseType != null");
		ConstructorInfo baseClassConstructor = TypeBuilder.BaseType.GetConstructor(flags, Type.DefaultBinder, parameterTypes, null);
		Debug.Assert(baseClassConstructor != null, nameof(baseClassConstructor) + " != null");

		// load arguments onto the evaluation stack
		msilGenerator.Emit(OpCodes.Ldarg_0);
		for (int i = 0; i < parameterTypes.Length; i++)
		{
			CodeGenHelpers.EmitLoadArgument(msilGenerator, i + 1);
		}

		// call base class constructor
		msilGenerator.Emit(OpCodes.Call, baseClassConstructor);
	}

	#region Adding Events

	/// <summary>
	/// Adds a new abstract instance event to the type definition (add/remove methods only).
	/// </summary>
	/// <typeparam name="T">Type of the event to add.</typeparam>
	/// <param name="eventName">Name of the event to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the event.</param>
	/// <returns>The added event.</returns>
	public IGeneratedEvent<T> AddAbstractEvent<T>(string eventName, Visibility visibility) where T : Delegate
	{
		EnsureThatIdentifierHasNotBeenUsedYet(eventName);
		var generatedEvent = new GeneratedEvent<T>(
			this,
			EventKind.Abstract,
			eventName,
			visibility,
			null);
		GeneratedEventsInternal.Add(generatedEvent);
		return generatedEvent;
	}

	/// <summary>
	/// Adds a new virtual instance event to the type definition.
	/// </summary>
	/// <typeparam name="T">Type of the event to add.</typeparam>
	/// <param name="eventName">Name of the event to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the event.</param>
	/// <param name="implementation">
	/// Implementation strategy that implements the add/remove accessor methods and the event raiser method, if added.
	/// </param>
	/// <returns>The added event.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedEvent<T> AddVirtualEvent<T>(
		string               eventName,
		Visibility           visibility,
		IEventImplementation implementation) where T : Delegate
	{
		EnsureThatIdentifierHasNotBeenUsedYet(eventName);
		var generatedEvent = new GeneratedEvent<T>(
			this,
			EventKind.Virtual,
			eventName,
			visibility,
			implementation);
		GeneratedEventsInternal.Add(generatedEvent);
		return generatedEvent;
	}

	/// <summary>
	/// Adds a new virtual instance event to the type definition.
	/// </summary>
	/// <typeparam name="T">Type of the event to add.</typeparam>
	/// <param name="name">Name of the event to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the event.</param>
	/// <param name="addAccessorImplementationCallback">A callback that implements the add accessor method of the event.</param>
	/// <param name="removeAccessorImplementationCallback">A callback that implements the remove accessor method of the event.</param>
	/// <returns>The added event.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="addAccessorImplementationCallback"/> or <paramref name="removeAccessorImplementationCallback"/> is <c>null</c>.
	/// </exception>
	public IGeneratedEvent<T> AddVirtualEvent<T>(
		string                              name,
		Visibility                          visibility,
		EventAccessorImplementationCallback addAccessorImplementationCallback,
		EventAccessorImplementationCallback removeAccessorImplementationCallback) where T : Delegate
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var generatedEvent = new GeneratedEvent<T>(
			this,
			EventKind.Virtual,
			name,
			visibility,
			addAccessorImplementationCallback,
			removeAccessorImplementationCallback);
		GeneratedEventsInternal.Add(generatedEvent);
		return generatedEvent;
	}

	/// <summary>
	/// Overrides the specified inherited event in the type definition.
	/// </summary>
	/// <param name="eventToOverride">Event to override.</param>
	/// <param name="implementation">
	/// Implementation strategy that implements the add/remove accessor methods and the event raiser method, if added.
	/// </param>
	/// <returns>The added event overriding the specified inherited event.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedEvent<T> AddEventOverride<T>(IInheritedEvent<T> eventToOverride, IEventImplementation implementation) where T : Delegate
	{
		EnsureThatIdentifierHasNotBeenUsedYet(eventToOverride.Name);

		// ensure that the property is abstract, virtual or an overrider
		switch (eventToOverride.Kind)
		{
			case EventKind.Abstract:
			case EventKind.Virtual:
			case EventKind.Override:
				break;

			case EventKind.Static:
			case EventKind.Normal:
			default:
				throw new CodeGenException($"The specified event ({eventToOverride.Name}) is neither abstract, virtual nor an override of a virtual/abstract event.");
		}

		// create event override
		var overrider = new GeneratedEvent<T>(this, eventToOverride, implementation);
		GeneratedEventsInternal.Add(overrider);
		return overrider;
	}

	/// <summary>
	/// Overrides the specified inherited event in the type definition.
	/// </summary>
	/// <param name="eventToOverride">Event to override.</param>
	/// <param name="addAccessorImplementationCallback">A callback that implements the add accessor method of the event.</param>
	/// <param name="removeAccessorImplementationCallback">A callback that implements the remove accessor method of the event.</param>
	/// <returns>The added event overriding the specified inherited event.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="addAccessorImplementationCallback"/> or <paramref name="removeAccessorImplementationCallback"/> is <c>null</c>.
	/// </exception>
	public IGeneratedEvent<T> AddEventOverride<T>(
		IInheritedEvent<T>                  eventToOverride,
		EventAccessorImplementationCallback addAccessorImplementationCallback,
		EventAccessorImplementationCallback removeAccessorImplementationCallback) where T : Delegate
	{
		EnsureThatIdentifierHasNotBeenUsedYet(eventToOverride.Name);

		// ensure that the property is abstract, virtual or an overrider
		switch (eventToOverride.Kind)
		{
			case EventKind.Abstract:
			case EventKind.Virtual:
			case EventKind.Override:
				break;

			case EventKind.Static:
			case EventKind.Normal:
			default:
				throw new CodeGenException($"The specified event ({eventToOverride.Name}) is neither abstract, virtual nor an override of a virtual/abstract event.");
		}

		// create event override
		var overrider = new GeneratedEvent<T>(this, eventToOverride, addAccessorImplementationCallback, removeAccessorImplementationCallback);
		GeneratedEventsInternal.Add(overrider);
		return overrider;
	}

	#endregion

	#region Adding Properties

	/// <summary>
	/// Adds a new abstract instance property to the type definition.
	/// </summary>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <returns>The added property.</returns>
	public IGeneratedProperty<T> AddAbstractProperty<T>(string name)
	{
		if (!TypeBuilder.IsClass) throw new InvalidOperationException("The type is not a class, cannot add an abstract property.");
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		var property = new GeneratedProperty<T>(this, PropertyKind.Abstract, name, null);
		GeneratedPropertiesInternal.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new abstract instance property to the type definition.
	/// </summary>
	/// <param name="type">Type of the property to add.</param>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <returns>The added property.</returns>
	public IGeneratedProperty AddAbstractProperty(Type type, string name)
	{
		if (type == null) throw new ArgumentNullException(nameof(type));
		if (!TypeBuilder.IsClass) throw new InvalidOperationException("The type is not a class, cannot add an abstract property.");

		EnsureThatIdentifierHasNotBeenUsedYet(name);

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Abstract, name]);

		GeneratedPropertiesInternal.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new virtual instance property to the type definition.<br/>
	/// The property does not have accessor methods.<br/>
	/// The desired accessor methods can be added using:<br/>
	/// - <see cref="IGeneratedProperty.AddGetAccessor"/><br/>
	/// - <see cref="IGeneratedProperty.AddSetAccessor"/>
	/// </summary>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <returns>The added property.</returns>
	public IGeneratedProperty<T> AddVirtualProperty<T>(string name)
	{
		if (!TypeBuilder.IsClass) throw new InvalidOperationException("The type is not a class, cannot add a virtual property.");
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		var property = new GeneratedProperty<T>(this, PropertyKind.Virtual, name);
		GeneratedPropertiesInternal.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new virtual instance property to the type definition.<br/>
	/// The property does not have accessor methods.<br/>
	/// The desired accessor methods can be added using:<br/>
	/// - <see cref="IGeneratedProperty.AddGetAccessor"/><br/>
	/// - <see cref="IGeneratedProperty.AddSetAccessor"/>
	/// </summary>
	/// <param name="type">Type of the property to add.</param>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <returns>The added property.</returns>
	public IGeneratedProperty AddVirtualProperty(Type type, string name)
	{
		if (type == null) throw new ArgumentNullException(nameof(type));
		if (!TypeBuilder.IsClass) throw new InvalidOperationException("The type is not a class, cannot add a virtual property.");
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Virtual, name]);

		GeneratedPropertiesInternal.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new virtual instance property to the type definition.<br/>
	/// The property has accessor methods implemented by the specified implementation strategy.
	/// </summary>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessors of the property.</param>
	/// <returns>The added property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedProperty<T> AddVirtualProperty<T>(string name, IPropertyImplementation implementation)
	{
		if (!TypeBuilder.IsClass) throw new InvalidOperationException("The type is not a class, cannot add a virtual property.");
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		var property = new GeneratedProperty<T>(this, PropertyKind.Virtual, name, implementation);
		GeneratedPropertiesInternal.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new virtual instance property to the type definition.<br/>
	/// The property has accessor methods implemented by the specified implementation strategy.
	/// </summary>
	/// <param name="type">Type of the property to add.</param>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessors of the property.</param>
	/// <returns>The added property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedProperty AddVirtualProperty(Type type, string name, IPropertyImplementation implementation)
	{
		if (type == null) throw new ArgumentNullException(nameof(type));
		if (!TypeBuilder.IsClass) throw new InvalidOperationException("The type is not a class, cannot add a virtual property.");
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string), typeof(IPropertyImplementation)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Virtual, name, implementation]);

		GeneratedPropertiesInternal.Add(property);
		return property;
	}

	/// <summary>
	/// Adds an override for the specified inherited property.<br/>
	/// The property has accessor methods implemented by the specified implementation strategy.
	/// </summary>
	/// <param name="property">Property to add an override for.</param>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessors of the property.</param>
	/// <returns>The added property override.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedProperty<T> AddPropertyOverride<T>(
		IInheritedProperty<T>   property,
		IPropertyImplementation implementation)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(property.Name);

		// ensure that the property is abstract, virtual or overriding an abstract/virtual property
		switch (property.Kind)
		{
			case PropertyKind.Abstract:
			case PropertyKind.Virtual:
			case PropertyKind.Override:
				break;

			case PropertyKind.Static:
			case PropertyKind.Normal:
			default:
				throw new CodeGenException($"The specified property ({property.Name}) is neither abstract, nor virtual nor an overrider.");
		}

		// add the property
		var overrider = new GeneratedProperty<T>(this, property, implementation);
		GeneratedPropertiesInternal.Add(overrider);
		return overrider;
	}

	/// <summary>
	/// Adds an override for the specified inherited property.<br/>
	/// The property has accessor methods implemented by the specified implementation strategy.
	/// </summary>
	/// <param name="property">Property to add an override for.</param>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessors of the property.</param>
	/// <returns>The added property override.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedProperty AddPropertyOverride(
		IInheritedProperty      property,
		IPropertyImplementation implementation)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(property.Name);

		// ensure that the property is abstract, virtual or overriding an abstract/virtual property
		switch (property.Kind)
		{
			case PropertyKind.Abstract:
			case PropertyKind.Virtual:
			case PropertyKind.Override:
				break;

			case PropertyKind.Static:
			case PropertyKind.Normal:
			default:
				throw new CodeGenException($"The specified property ({property.Name}) is neither abstract, nor virtual nor an overrider.");
		}

		// add the property
		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(property.PropertyType)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[
					typeof(TypeDefinition),
					typeof(IInheritedProperty<>).MakeGenericType(property.PropertyType),
					typeof(PropertyImplementation)
				],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var overrider = (IGeneratedProperty)constructor.Invoke([this, property, implementation]);
		GeneratedPropertiesInternal.Add(overrider);
		return overrider;
	}

	/// <summary>
	/// Adds an override for the specified inherited property.<br/>
	/// The property accessors are implemented by the specified implementation callbacks.
	/// </summary>
	/// <param name="property">Property to add an override for.</param>
	/// <param name="getAccessorImplementationCallback">
	/// A callback that implements the 'get' accessor method of the property
	/// (<c>null</c>, if the inherited property does not have a 'get' accessor method).
	/// </param>
	/// <param name="setAccessorImplementationCallback">
	/// A callback that implements the 'set' accessor method of the property
	/// (<c>null</c>, if the inherited property does not have a 'set' accessor method).
	/// </param>
	/// <returns>The added property override.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="property"/> has a 'get' accessor, but <paramref name="getAccessorImplementationCallback"/> is <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="property"/> has a 'set' accessor, but <paramref name="setAccessorImplementationCallback"/> is <c>null</c>.
	/// </exception>
	public IGeneratedProperty<T> AddPropertyOverride<T>(
		IInheritedProperty<T>                  property,
		PropertyAccessorImplementationCallback getAccessorImplementationCallback,
		PropertyAccessorImplementationCallback setAccessorImplementationCallback)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(property.Name);

		// ensure that the property is abstract, virtual or an overrider
		switch (property.Kind)
		{
			case PropertyKind.Abstract:
			case PropertyKind.Virtual:
			case PropertyKind.Override:
				break;

			case PropertyKind.Static:
			case PropertyKind.Normal:
			default:
				throw new CodeGenException($"The specified property ({property.Name}) is neither abstract, nor virtual nor an overrider.");
		}

		// add the property
		var overrider = new GeneratedProperty<T>(this, property, getAccessorImplementationCallback, setAccessorImplementationCallback);
		GeneratedPropertiesInternal.Add(overrider);
		return overrider;
	}

	/// <summary>
	/// Adds an override for the specified inherited property.<br/>
	/// The property accessors are implemented by the specified implementation callbacks.
	/// </summary>
	/// <param name="property">Property to add an override for.</param>
	/// <param name="getAccessorImplementationCallback">
	/// A callback that implements the 'get' accessor method of the property
	/// (<c>null</c>, if the inherited property does not have a 'get' accessor method).
	/// </param>
	/// <param name="setAccessorImplementationCallback">
	/// A callback that implements the 'set' accessor method of the property
	/// (<c>null</c>, if the inherited property does not have a 'set' accessor method).
	/// </param>
	/// <returns>The added property override.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="property"/> has a 'get' accessor, but <paramref name="getAccessorImplementationCallback"/> is <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="property"/> has a 'set' accessor, but <paramref name="setAccessorImplementationCallback"/> is <c>null</c>.
	/// </exception>
	public IGeneratedProperty AddPropertyOverride(
		IInheritedProperty                     property,
		PropertyAccessorImplementationCallback getAccessorImplementationCallback,
		PropertyAccessorImplementationCallback setAccessorImplementationCallback)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(property.Name);

		// ensure that the property is abstract, virtual or an overrider
		switch (property.Kind)
		{
			case PropertyKind.Abstract:
			case PropertyKind.Virtual:
			case PropertyKind.Override:
				break;

			case PropertyKind.Static:
			case PropertyKind.Normal:
			default:
				throw new CodeGenException($"The specified property ({property.Name}) is neither abstract, nor virtual nor an overrider.");
		}

		// add the property
		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(property.PropertyType)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[
					typeof(TypeDefinition),
					typeof(IInheritedProperty<>).MakeGenericType(property.PropertyType),
					typeof(PropertyAccessorImplementationCallback),
					typeof(PropertyAccessorImplementationCallback)
				],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var overrider = (IGeneratedProperty)constructor.Invoke([this, property, getAccessorImplementationCallback, setAccessorImplementationCallback]);
		GeneratedPropertiesInternal.Add(overrider);
		return overrider;
	}

	#endregion

	#region Adding Dependency Properties

#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
	/// <summary>
	/// Adds a new dependency property to the type definition (without initial value).<br/>
	/// The property will have the default value of the specified type initially.
	/// </summary>
	/// <typeparam name="T">Type of the dependency property.</typeparam>
	/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
	/// <param name="isReadOnly">
	/// <c>true</c> if the dependency property is read-only;<br/>
	/// <c>false</c> if it is read-write.
	/// </param>
	/// <returns>The added dependency property.</returns>
	/// <exception cref="CodeGenException">
	/// The created type does not derive from <see cref="DependencyObject"/>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> was already used to declare some other field, property, event or method.
	/// </exception>
	public IGeneratedDependencyProperty<T> AddDependencyProperty<T>(string name, bool isReadOnly)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		EnsureThatTypeDerivesFrom(typeof(DependencyObject));
		var dependencyProperty = new GeneratedDependencyProperty<T>(this, name, isReadOnly);
		GeneratedDependencyPropertiesInternal.Add(dependencyProperty);
		return dependencyProperty;
	}

	/// <summary>
	/// Adds a new dependency property to the type definition (without initial value).<br/>
	/// The property will have the default value of the specified type initially.
	/// </summary>
	/// <param name="type">Type of the dependency property.</param>
	/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
	/// <param name="isReadOnly">
	/// <c>true</c> if the dependency property is read-only;<br/>
	/// <c>false</c> if it is read-write.
	/// </param>
	/// <returns>The added dependency property.</returns>
	/// <exception cref="CodeGenException">
	/// The created type does not derive from <see cref="DependencyObject"/>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> was already used to declare some other field, property, event or method.
	/// </exception>
	public IGeneratedDependencyProperty AddDependencyProperty(Type type, string name, bool isReadOnly)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		EnsureThatTypeDerivesFrom(typeof(DependencyObject));

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedDependencyProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(string), typeof(bool)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var dependencyProperty = (IGeneratedDependencyProperty)constructor.Invoke([this, name, isReadOnly]);
		GeneratedDependencyPropertiesInternal.Add(dependencyProperty);
		return dependencyProperty;
	}

	/// <summary>
	/// Adds a new dependency property to the type definition (with initial value).
	/// </summary>
	/// <typeparam name="T">Type of the dependency property.</typeparam>
	/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
	/// <param name="isReadOnly">
	/// <c>true</c> if the dependency property is read-only;<br/>
	/// <c>false</c> if it is read-write.
	/// </param>
	/// <param name="initialValue">Initial value of the dependency property.</param>
	/// <returns>The added dependency property.</returns>
	/// <exception cref="CodeGenException">
	/// The created type does not derive from <see cref="DependencyObject"/>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> was already used to declare some other field, property, event or method.
	/// </exception>
	public IGeneratedDependencyProperty<T> AddDependencyProperty<T>(string name, bool isReadOnly, T initialValue)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		EnsureThatTypeDerivesFrom(typeof(DependencyObject));
		var dependencyProperty = new GeneratedDependencyProperty<T>(this, name, isReadOnly, initialValue);
		GeneratedDependencyPropertiesInternal.Add(dependencyProperty);
		return dependencyProperty;
	}

	/// <summary>
	/// Adds a new dependency property to the type definition (with initial value).
	/// </summary>
	/// <param name="type">Type of the dependency property.</param>
	/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
	/// <param name="isReadOnly">
	/// <c>true</c> if the dependency property is read-only;<br/>
	/// <c>false</c> if it is read-write.
	/// </param>
	/// <param name="initialValue">Initial value of the dependency property.</param>
	/// <returns>The added dependency property.</returns>
	/// <exception cref="CodeGenException">
	/// The created type does not derive from <see cref="DependencyObject"/>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> was already used to declare some other field, property, event or method.
	/// </exception>
	public IGeneratedDependencyProperty AddDependencyProperty(
		Type   type,
		string name,
		bool   isReadOnly,
		object initialValue)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		EnsureThatTypeDerivesFrom(typeof(DependencyObject));

		if (type == null) throw new ArgumentNullException(nameof(type));

		if (initialValue == null && type.IsValueType)
			throw new ArgumentException($"The specified initial value is null, but the property type ({type.FullName}) is a value type.", nameof(initialValue));

		if (initialValue != null && !type.IsInstanceOfType(initialValue))
			throw new ArgumentException($"The specified initial value ({initialValue}) is not assignable to a property of the specified type ({type.FullName}).", nameof(initialValue));

		ConstructorInfo constructor = typeof(GeneratedDependencyProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(string), typeof(bool), type],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var dependencyProperty = (IGeneratedDependencyProperty)constructor.Invoke([this, name, isReadOnly, initialValue]);
		GeneratedDependencyPropertiesInternal.Add(dependencyProperty);
		return dependencyProperty;
	}

	/// <summary>
	/// Adds a new dependency property to the type definition (with custom initializer).
	/// </summary>
	/// <typeparam name="T">Type of the dependency property.</typeparam>
	/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
	/// <param name="isReadOnly">
	/// <c>true</c> if the dependency property is read-only;<br/>
	/// <c>false</c> if it is read-write.
	/// </param>
	/// <param name="initializer">
	/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
	/// value for the generated dependency property.
	/// </param>
	/// <returns>The added dependency property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="initializer"/> is <c>null</c>.</exception>
	/// <exception cref="CodeGenException">
	/// The created type does not derive from <see cref="DependencyObject"/>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> was already used to declare some other field, property, event or method.
	/// </exception>
	public IGeneratedDependencyProperty<T> AddDependencyProperty<T>(
		string                        name,
		bool                          isReadOnly,
		DependencyPropertyInitializer initializer)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		EnsureThatTypeDerivesFrom(typeof(DependencyObject));
		var dependencyProperty = new GeneratedDependencyProperty<T>(this, name, isReadOnly, initializer);
		GeneratedDependencyPropertiesInternal.Add(dependencyProperty);
		return dependencyProperty;
	}

	/// <summary>
	/// Adds a new dependency property to the type definition (with custom initializer).
	/// </summary>
	/// <param name="type">Type of the dependency property.</param>
	/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
	/// <param name="isReadOnly">
	/// <c>true</c> if the dependency property is read-only;<br/>
	/// <c>false</c> if it is read-write.
	/// </param>
	/// <param name="initializer">
	/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
	/// value for the generated dependency property.
	/// </param>
	/// <returns>The added dependency property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="initializer"/> is <c>null</c>.</exception>
	/// <exception cref="CodeGenException">
	/// The created type does not derive from <see cref="DependencyObject"/>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> was already used to declare some other field, property, event or method.
	/// </exception>
	public IGeneratedDependencyProperty AddDependencyProperty(
		Type                          type,
		string                        name,
		bool                          isReadOnly,
		DependencyPropertyInitializer initializer)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		EnsureThatTypeDerivesFrom(typeof(DependencyObject));

		if (type == null) throw new ArgumentNullException(nameof(type));
		if (initializer == null) throw new ArgumentNullException(nameof(initializer));

		ConstructorInfo constructor = typeof(GeneratedDependencyProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(string), typeof(bool), typeof(DependencyPropertyInitializer)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var dependencyProperty = (IGeneratedDependencyProperty)constructor.Invoke([this, name, isReadOnly, initializer]);
		GeneratedDependencyPropertiesInternal.Add(dependencyProperty);
		return dependencyProperty;
	}

	/// <summary>
	/// Adds a new dependency property to the type definition (with factory callback for an initial value).
	/// </summary>
	/// <typeparam name="T">Type of the dependency property.</typeparam>
	/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
	/// <param name="isReadOnly">
	/// <c>true</c> if the dependency property is read-only;<br/>
	/// <c>false</c> if it is read-write.
	/// </param>
	/// <param name="provideInitialValueCallback">Factory callback providing the initial value of the dependency property.</param>
	/// <returns>The added dependency property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="provideInitialValueCallback"/> is <c>null</c>.</exception>
	/// <exception cref="CodeGenException">
	/// The created type does not derive from <see cref="DependencyObject"/>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> was already used to declare some other field, property, event or method.
	/// </exception>
	public IGeneratedDependencyProperty<T> AddDependencyProperty<T>(string name, bool isReadOnly, ProvideValueCallback<T> provideInitialValueCallback)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		EnsureThatTypeDerivesFrom(typeof(DependencyObject));
		var dependencyProperty = new GeneratedDependencyProperty<T>(this, name, isReadOnly, provideInitialValueCallback);
		GeneratedDependencyPropertiesInternal.Add(dependencyProperty);
		return dependencyProperty;
	}

	/// <summary>
	/// Adds a new dependency property to the type definition (with factory callback for an initial value).
	/// </summary>
	/// <param name="type">Type of the dependency property.</param>
	/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
	/// <param name="isReadOnly">
	/// <c>true</c> if the dependency property is read-only;<br/>
	/// <c>false</c> if it is read-write.
	/// </param>
	/// <param name="provideInitialValueCallback">Factory callback providing the initial value of the dependency property.</param>
	/// <returns>The added dependency property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="provideInitialValueCallback"/> is <c>null</c>.</exception>
	/// <exception cref="CodeGenException">
	/// The created type does not derive from <see cref="DependencyObject"/>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> was already used to declare some other field, property, event or method.
	/// </exception>
	public IGeneratedDependencyProperty AddDependencyProperty(
		Type                 type,
		string               name,
		bool                 isReadOnly,
		ProvideValueCallback provideInitialValueCallback)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		EnsureThatTypeDerivesFrom(typeof(DependencyObject));

		if (type == null) throw new ArgumentNullException(nameof(type));
		if (provideInitialValueCallback == null) throw new ArgumentNullException(nameof(provideInitialValueCallback));

		ConstructorInfo constructor = typeof(GeneratedDependencyProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(string), typeof(bool), typeof(ProvideValueCallback)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var dependencyProperty = (IGeneratedDependencyProperty)constructor.Invoke([this, name, isReadOnly, provideInitialValueCallback]);
		GeneratedDependencyPropertiesInternal.Add(dependencyProperty);
		return dependencyProperty;
	}

#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
	// Dependency properties are not supported on .NET Standard and .NET5/6/7/8 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif

	#endregion

	#region Adding Methods

	/// <summary>
	/// Adds a new abstract instance method to the type definition.
	/// </summary>
	/// <param name="name">Name of the method to add (<c>null</c> to create a random name).</param>
	/// <param name="returnType">Return type of the method.</param>
	/// <param name="parameterTypes">Types of the method's parameters.</param>
	/// <param name="visibility">Visibility of the method.</param>
	/// <param name="additionalMethodAttributes">Additional method attributes to 'or' with other attributes.</param>
	/// <returns>The added method.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="returnType"/> or <paramref name="parameterTypes"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="name"/> is not a valid language independent identifier.<br/>
	/// -or-<br/>
	/// <paramref name="parameterTypes"/> contains a null reference.
	/// </exception>
	public IGeneratedMethod AddAbstractMethod(
		string           name,
		Type             returnType,
		Type[]           parameterTypes,
		Visibility       visibility,
		MethodAttributes additionalMethodAttributes = 0)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var method = new GeneratedMethod(this, MethodKind.Abstract, name, returnType, parameterTypes, visibility, additionalMethodAttributes, (IMethodImplementation)null);
		GeneratedMethodsInternal.Add(method);
		return method;
	}

	/// <summary>
	/// Adds a new virtual instance method to the type definition.
	/// </summary>
	/// <param name="name">Name of the method to add (<c>null</c> to create a random name).</param>
	/// <param name="returnType">Return type of the method.</param>
	/// <param name="parameterTypes">Types of the method's parameters.</param>
	/// <param name="visibility">Visibility of the method.</param>
	/// <param name="implementation">Implementation strategy that implements the method.</param>
	/// <param name="additionalMethodAttributes">Additional method attributes to 'or' with other attributes.</param>
	/// <returns>The added method.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="returnType"/>, <paramref name="parameterTypes"/> or <paramref name="implementation"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="name"/> is not a valid language independent identifier.<br/>
	/// -or-<br/>
	/// <paramref name="parameterTypes"/> contains a null reference.
	/// </exception>
	public IGeneratedMethod AddVirtualMethod(
		string                name,
		Type                  returnType,
		Type[]                parameterTypes,
		Visibility            visibility,
		IMethodImplementation implementation,
		MethodAttributes      additionalMethodAttributes = 0)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var method = new GeneratedMethod(this, MethodKind.Virtual, name, returnType, parameterTypes, visibility, additionalMethodAttributes, implementation);
		GeneratedMethodsInternal.Add(method);
		return method;
	}

	/// <summary>
	/// Adds a new virtual instance method to the type definition.
	/// </summary>
	/// <param name="name">Name of the method to add (<c>null</c> to create a random name).</param>
	/// <param name="returnType">Return type of the method.</param>
	/// <param name="parameterTypes">Types of the method's parameters.</param>
	/// <param name="visibility">Visibility of the method.</param>
	/// <param name="implementationCallback">Callback that implements the method.</param>
	/// <param name="additionalMethodAttributes">Additional method attributes to 'or' with other attributes.</param>
	/// <returns>The added method.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="returnType"/>, <paramref name="parameterTypes"/> or <paramref name="implementationCallback"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="name"/> is not a valid language independent identifier.<br/>
	/// -or-<br/>
	/// <paramref name="parameterTypes"/> contains a null reference.
	/// </exception>
	public IGeneratedMethod AddVirtualMethod(
		string                       name,
		Type                         returnType,
		Type[]                       parameterTypes,
		Visibility                   visibility,
		MethodImplementationCallback implementationCallback,
		MethodAttributes             additionalMethodAttributes = 0)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var method = new GeneratedMethod(this, MethodKind.Virtual, name, returnType, parameterTypes, visibility, additionalMethodAttributes, implementationCallback);
		GeneratedMethodsInternal.Add(method);
		return method;
	}

	/// <summary>
	/// Adds an override for an inherited method.
	/// </summary>
	/// <param name="method">Method to add an override for.</param>
	/// <param name="implementation">Implementation strategy that implements the method.</param>
	/// <returns>The added method override.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="method"/> or <paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedMethod AddMethodOverride(IInheritedMethod method, IMethodImplementation implementation)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(method.Name);

		// ensure that the property is abstract, virtual or an overrider
		switch (method.Kind)
		{
			case MethodKind.Abstract:
			case MethodKind.Virtual:
			case MethodKind.Override:
				break;

			case MethodKind.Static:
			case MethodKind.Normal:
			default:
				throw new CodeGenException($"The specified method ({method.Name}) is neither abstract, nor virtual nor an overrider.");
		}

		// create method
		var overriddenMethod = new GeneratedMethod(this, method, implementation);
		GeneratedMethodsInternal.Add(overriddenMethod);
		return overriddenMethod;
	}

	/// <summary>
	/// Adds an override for an inherited method.
	/// </summary>
	/// <param name="method">Method to add an override for.</param>
	/// <param name="implementationCallback">Callback that implements the method.</param>
	/// <returns>The added method override.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="method"/> or <paramref name="implementationCallback"/> is <c>null</c>.</exception>
	public IGeneratedMethod AddMethodOverride(IInheritedMethod method, MethodImplementationCallback implementationCallback)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(method.Name);

		// ensure that the property is abstract, virtual or an overrider
		switch (method.Kind)
		{
			case MethodKind.Abstract:
			case MethodKind.Virtual:
			case MethodKind.Override:
				break;

			case MethodKind.Static:
			case MethodKind.Normal:
			default:
				throw new CodeGenException($"The specified method ({method.Name}) is neither abstract, nor virtual nor an overrider.");
		}

		// create method
		var overriddenMethod = new GeneratedMethod(this, method, implementationCallback);
		GeneratedMethodsInternal.Add(overriddenMethod);
		return overriddenMethod;
	}

	#endregion
}
