///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

// ReSharper disable SuggestBaseTypeForParameter

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Definition of a type to create dynamically.
/// </summary>
public abstract class TypeDefinition
{
	private readonly Stack<TypeBuilder> mTypeBuilders;

	/// <summary>
	/// Initializes a new <see cref="TypeDefinition"/> for a struct or a class not deriving from a base type.
	/// </summary>
	/// <param name="module">Module definition to associate the type definition with (<c>null</c> to create a new module definition).</param>
	/// <param name="isValueType">
	/// <c>true</c> if the type to define is a value type (struct);<br/>
	/// <c>false</c> if it is a reference type (class).
	/// </param>
	/// <param name="name">Name of the type to create (<c>null</c> to create a random name).</param>
	internal TypeDefinition(ModuleDefinition module, bool isValueType, string name = null)
	{
		// no base class means that the type effectively derives from System.ValueType for value types and System.Object for classes
		BaseClassType = isValueType ? typeof(ValueType) : typeof(object);

		// determine the name of the generated type
		TypeName = string.IsNullOrWhiteSpace(name) ? "DynamicType_" + Guid.NewGuid().ToString("N") : name;
		CodeGenHelpers.EnsureNameIsValidLanguageIndependentIdentifier(TypeName);

		// create a builder for type to create
		ModuleDefinition = module ?? new ModuleDefinition();
		TypeBuilder = CreateTypeBuilder(out mTypeBuilders);
	}

	/// <summary>
	/// Initializes a new <see cref="TypeDefinition"/> for a class deriving from the specified base class.
	/// </summary>
	/// <param name="module">Module definition to associate the type definition with (<c>null</c> to create a new module definition).</param>
	/// <param name="baseClass">Base class to derive the created class from.</param>
	/// <param name="name">Name of the class to create (<c>null</c> to keep the name of the base class).</param>
	/// <exception cref="ArgumentNullException"><paramref name="baseClass"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="baseClass"/> is not a class.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> is not a valid type name.
	/// </exception>
	internal TypeDefinition(ModuleDefinition module, Type baseClass, string name = null)
	{
		if (baseClass == null) throw new ArgumentNullException(nameof(baseClass));

		// ensure that the base class is really a class that is totally public
		// (otherwise the generated assembly will not be able to access it)
		if (!baseClass.IsClass) throw new ArgumentException($"The specified type ({baseClass.FullName}) is not a class.", nameof(baseClass));
		CodeGenHelpers.EnsureTypeIsTotallyPublic(baseClass);
		BaseClassType = baseClass;

		// determine the name of the generated type
		// (take the name of the base type if no name is specified)
		TypeName = string.IsNullOrWhiteSpace(name) ? BaseClassType.FullName : name;
		CodeGenHelpers.EnsureNameIsValidTypeName(TypeName);

		// create a builder for type to create
		ModuleDefinition = module ?? new ModuleDefinition();
		TypeBuilder = CreateTypeBuilder(out mTypeBuilders);
	}

	/// <summary>
	/// Creates a builder for the type to generate.
	/// </summary>
	/// <param name="typeBuilders">
	/// Receives the type builders that are needed to generate the requested type as well
	/// (nested types need to be generated bottom up, so each nesting level has its own type builder).
	/// </param>
	/// <returns>The created type builder.</returns>
	private TypeBuilder CreateTypeBuilder(out Stack<TypeBuilder> typeBuilders)
	{
		// create a type builder
		typeBuilders = new Stack<TypeBuilder>();
		string[] splitTypeNameTokens = TypeName.Split('+');
		if (splitTypeNameTokens.Length > 1)
		{
			TypeBuilder parent = ModuleDefinition.ModuleBuilder.DefineType(splitTypeNameTokens[0], TypeAttributes.Public | TypeAttributes.Class, null);
			typeBuilders.Push(parent);
			for (int i = 1; i < splitTypeNameTokens.Length; i++)
			{
				if (i + 1 < splitTypeNameTokens.Length)
				{
					parent = parent.DefineNestedType(splitTypeNameTokens[i], TypeAttributes.NestedPublic | TypeAttributes.Class);
					typeBuilders.Push(parent);
				}
				else
				{
					parent = parent.DefineNestedType(splitTypeNameTokens[i], TypeAttributes.NestedPublic | TypeAttributes.Class, BaseClassType);
					typeBuilders.Push(parent);
				}
			}
		}
		else
		{
			TypeBuilder builder = ModuleDefinition.ModuleBuilder.DefineType(splitTypeNameTokens[0], TypeAttributes.Public | TypeAttributes.Class, BaseClassType);
			typeBuilders.Push(builder);
		}

		return typeBuilders.Peek();
	}

	/// <summary>
	/// Gets the name of the type to create.
	/// </summary>
	public string TypeName { get; }

	/// <summary>
	/// Gets the base type of the type to create.
	/// </summary>
	public Type BaseClassType { get; }

	/// <summary>
	/// Gets the definition of the module the type in creation is defined in.
	/// </summary>
	public ModuleDefinition ModuleDefinition { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.TypeBuilder"/> that is used to create the type.
	/// </summary>
	public TypeBuilder TypeBuilder { get; }

	/// <summary>
	/// Gets the collection of external objects that is added to the created type.
	/// </summary>
	internal List<object> ExternalObjects { get; } = [];

	#region Information about Base Class Constructors

	private List<IConstructor> mBaseClassConstructors;

	/// <summary>
	/// Gets constructors of the base type that are accessible to the type in creation.
	/// </summary>
	public IEnumerable<IConstructor> BaseClassConstructors
	{
		get
		{
			// return the cached set of base class constructors, if available
			if (mBaseClassConstructors != null)
				return mBaseClassConstructors;

			// determine base class constructors and cache them
			mBaseClassConstructors = [];
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			foreach (ConstructorInfo constructorInfo in BaseClassType.GetConstructors(flags))
			{
				// skip constructor if it is private or internal
				// (cannot be accessed by a derived type defined in another assembly)
				if (constructorInfo.IsPrivate || constructorInfo.IsAssembly) continue;

				// keep constructor
				mBaseClassConstructors.Add(new Constructor(constructorInfo));
			}

			return mBaseClassConstructors;
		}
	}

	#endregion

	#region Information about Inherited Fields, Events, Properties and Methods

	/// <summary>
	/// Gets the fields the type to create inherits from its base type.
	/// </summary>
	public IEnumerable<IInheritedField> InheritedFields => GetInheritedFields(false);

	/// <summary>
	/// Gets the events the type to create inherits from its base type.
	/// </summary>
	public IEnumerable<IInheritedEvent> InheritedEvents => GetInheritedEvents(false);

	/// <summary>
	/// Gets the properties the type to create inherits from its base type.
	/// </summary>
	public IEnumerable<IInheritedProperty> InheritedProperties => GetInheritedProperties(false);

	/// <summary>
	/// Gets the methods the type to create inherits from its base type.
	/// Does not contain inherited add/remove accessor methods of events and 'get'/'set' accessor methods of properties.
	/// You can use <see cref="IInheritedEvent.AddAccessor"/> and <see cref="IInheritedEvent.RemoveAccessor"/> to get
	/// event accessor methods. Property accessor methods are available via <see cref="IInheritedProperty.GetAccessor"/>
	/// and <see cref="IInheritedProperty.SetAccessor"/>.
	/// </summary>
	public IEnumerable<IInheritedMethod> InheritedMethods => GetInheritedMethods(false);

	/// <summary>
	/// Gets fields inherited from the base class.
	/// </summary>
	/// <param name="includeHidden">
	/// <c>true</c> to include fields that have been hidden by more specific types if the base type derives from some other type on its own;<br/>
	/// <c>false</c> to return only the most specific fields (default, same as <see cref="InheritedFields"/>).
	/// </param>
	/// <returns>The inherited fields.</returns>
	public IEnumerable<IInheritedField> GetInheritedFields(bool includeHidden)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var inheritedFields = new HashSet<IInheritedField>();
		Type typeToInspect = BaseClassType;
		while (typeToInspect != null)
		{
			foreach (FieldInfo fieldInfo in typeToInspect.GetFields(flags))
			{
				// skip field if it is 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				if (fieldInfo.IsPrivate || fieldInfo.IsAssembly) continue;

				// skip field if a field with the same signature is already in the set of fields
				// and hidden fields should not be returned
				if (!includeHidden && inheritedFields.Any(x => HasSameSignature(x.FieldInfo, fieldInfo)))
					continue;

				// the field is accessible from a derived class in some other assembly
				Type inheritedFieldType = typeof(InheritedField<>).MakeGenericType(fieldInfo.FieldType);
				var inheritedField = (IInheritedField)Activator.CreateInstance(
					inheritedFieldType,
					BindingFlags.Instance | BindingFlags.NonPublic,
					Type.DefaultBinder,
					[this, fieldInfo],
					CultureInfo.InvariantCulture);

				inheritedFields.Add(inheritedField);
			}

			typeToInspect = typeToInspect.BaseType;
		}

		return inheritedFields;
	}

	/// <summary>
	/// Gets events inherited from the base class.
	/// </summary>
	/// <param name="includeHidden">
	/// <c>true</c> to include events that have been hidden by more specific types if the base type derives from some other type on its own;<br/>
	/// <c>false</c> to return only the most specific events (default, same as <see cref="InheritedEvents"/>).
	/// </param>
	/// <returns>The inherited events.</returns>
	public IEnumerable<IInheritedEvent> GetInheritedEvents(bool includeHidden)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var inheritedEvents = new HashSet<IInheritedEvent>();
		Type typeToInspect = BaseClassType;
		while (typeToInspect != null)
		{
			foreach (EventInfo eventInfo in typeToInspect.GetEvents(flags))
			{
				// skip event if it is 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				MethodInfo addMethod = eventInfo.GetAddMethod(true);
				MethodInfo removeMethod = eventInfo.GetRemoveMethod(true);
				if (addMethod == null || addMethod.IsPrivate || addMethod.IsAssembly) continue;
				if (removeMethod == null || removeMethod.IsPrivate || removeMethod.IsAssembly) continue;

				// skip event if it is virtual and already in the set of events (also covers abstract, virtual and overridden events)
				// => only the most specific implementation gets into the returned set of events
				if ((addMethod.IsVirtual || removeMethod.IsVirtual) && inheritedEvents.Any(x => HasSameSignature(x.EventInfo, eventInfo)))
					continue;

				// skip event if an event with the same signature is already in the set of events
				// and hidden events should not be returned
				if (!includeHidden && inheritedEvents.Any(x => HasSameSignature(x.EventInfo, eventInfo)))
					continue;

				// the event is accessible from a derived class in some other assembly
				Debug.Assert(eventInfo.EventHandlerType != null, "eventInfo.EventHandlerType != null");
				Type inheritedEventType = typeof(InheritedEvent<>).MakeGenericType(eventInfo.EventHandlerType);
				var inheritedEvent = (IInheritedEvent)Activator.CreateInstance(
					inheritedEventType,
					BindingFlags.Instance | BindingFlags.NonPublic,
					Type.DefaultBinder,
					[this, eventInfo],
					CultureInfo.InvariantCulture);

				inheritedEvents.Add(inheritedEvent);
			}

			typeToInspect = typeToInspect.BaseType;
		}

		return inheritedEvents;
	}

	/// <summary>
	/// Gets properties inherited from the base class.
	/// </summary>
	/// <param name="includeHidden">
	/// <c>true</c> to include properties that have been hidden by more specific types if the base type derives from some other type on its own;<br/>
	/// <c>false</c> to return only the most specific properties (default, same as <see cref="InheritedProperties"/>).
	/// </param>
	/// <returns>The inherited properties.</returns>
	public IEnumerable<IInheritedProperty> GetInheritedProperties(bool includeHidden)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var inheritedProperties = new HashSet<IInheritedProperty>();
		Type typeToInspect = BaseClassType;
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
				if (inheritedProperties.Any(x => HasSameSignature(x.PropertyInfo, propertyInfo)))
				{
					if (getAccessor != null && getAccessor.IsVirtual) continue;
					if (setAccessor != null && setAccessor.IsVirtual) continue;
				}

				// skip property if a property with the same signature is already in the set of properties
				// and hidden properties should not be returned
				if (!includeHidden && inheritedProperties.Any(x => HasSameSignature(x.PropertyInfo, propertyInfo)))
					continue;

				// the property is accessible from a derived class in some other assembly
				Type inheritedPropertyType = typeof(InheritedProperty<>).MakeGenericType(propertyInfo.PropertyType);
				var property = (IInheritedProperty)Activator.CreateInstance(
					inheritedPropertyType,
					BindingFlags.Instance | BindingFlags.NonPublic,
					Type.DefaultBinder,
					[this, propertyInfo],
					CultureInfo.InvariantCulture);

				inheritedProperties.Add(property);
			}

			typeToInspect = typeToInspect.BaseType;
		}

		return inheritedProperties;
	}

	/// <summary>
	/// Gets methods inherited from the base class.
	/// </summary>
	/// <param name="includeHidden">
	/// <c>true</c> to include methods that have been hidden by more specific types if the base type derives from some other type on its own;<br/>
	/// <c>false</c> to return only the most specific methods (default, same as <see cref="InheritedMethods"/>).
	/// </param>
	/// <returns>The inherited methods.</returns>
	public IEnumerable<IInheritedMethod> GetInheritedMethods(bool includeHidden)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var inheritedMethods = new HashSet<IInheritedMethod>();

		Type typeToInspect = BaseClassType;
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
				if (methodInfo.IsVirtual && inheritedMethods.Any(x => HasSameSignature(x.MethodInfo, methodInfo)))
					continue;

				// skip property if a method with the same signature is already in the set of methods
				// and hidden methods should not be returned
				if (!includeHidden && inheritedMethods.Any(x => HasSameSignature(x.MethodInfo, methodInfo)))
					continue;

				// the method is accessible from a derived class in some other assembly
				var inheritedMethod = new InheritedMethod(this, methodInfo);
				inheritedMethods.Add(inheritedMethod);
			}

			typeToInspect = typeToInspect.BaseType;
		}

		return inheritedMethods;
	}

	#endregion

	#region Information about Generated Fields, Events, Properties and Methods

	private readonly List<IGeneratedFieldInternal>  mGeneratedFields     = [];
	private readonly List<IGeneratedEvent>          mGeneratedEvents     = [];
	private readonly List<IGeneratedProperty>       mGeneratedProperties = [];
	private readonly List<IGeneratedMethodInternal> mGeneratedMethods    = [];

#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
	private readonly List<IGeneratedDependencyProperty> mGeneratedDependencyProperties = [];
#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
	// Dependency properties are not supported on .NET Standard and .NET5/6/7/8 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif

	/// <summary>
	/// Gets the fields that have already been generated on the type.
	/// </summary>
	public IEnumerable<IGeneratedField> GeneratedFields => mGeneratedFields;

	/// <summary>
	/// Gets the events that have already been generated on the type.
	/// </summary>
	public IEnumerable<IGeneratedEvent> GeneratedEvents => mGeneratedEvents;

	/// <summary>
	/// Gets the properties that have already been generated on the type.
	/// </summary>
	public IEnumerable<IGeneratedProperty> GeneratedProperties => mGeneratedProperties;

	/// <summary>
	/// Gets the methods that have already been generated on the type.
	/// Does not contain generated add/remove accessor methods of events and 'get'/'set' accessor methods of properties.
	/// You can use <see cref="IGeneratedEvent.AddAccessor"/> and <see cref="IGeneratedEvent.RemoveAccessor"/> to get
	/// event accessor methods. Property accessor methods are available via <see cref="IGeneratedProperty.GetAccessor"/>
	/// and <see cref="IGeneratedProperty.SetAccessor"/>.
	/// </summary>
	public IEnumerable<IGeneratedMethod> GeneratedMethods => mGeneratedMethods.Where(x => !x.MethodInfo.IsSpecialName);

#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
	/// <summary>
	/// Gets the dependency properties that have already been generated on the type.
	/// </summary>
	public IEnumerable<IGeneratedDependencyProperty> GeneratedDependencyProperties => mGeneratedDependencyProperties;
#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
	// Dependency properties are not supported on .NET Standard and .NET5/6/7 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif

	#endregion

	#region Adding Implemented Interfaces

	/// <summary>
	/// Gets the interfaces implemented by the created type.
	/// </summary>
	public IEnumerable<Type> ImplementedInterfaces => TypeBuilder.GetTypeInfo().ImplementedInterfaces;

	/// <summary>
	/// Adds the specified interface to the type in creation.
	/// </summary>
	/// <typeparam name="T">Interface to add to the type in creation.</typeparam>
	public void AddImplementedInterface<T>()
	{
		TypeBuilder.AddInterfaceImplementation(typeof(T));
	}

	#endregion

	#region Adding Constructors

	private readonly List<IGeneratedConstructorInternal> mConstructors = [];

	/// <summary>
	/// Gets constructor definitions determining what constructors need to be created.
	/// </summary>
	public IEnumerable<IGeneratedConstructor> Constructors => mConstructors;

	/// <summary>
	/// Adds a new constructor to the type definition.
	/// </summary>
	/// <param name="visibility">Visibility of the constructor.</param>
	/// <param name="parameterTypes">Constructor parameter types.</param>
	/// <param name="baseClassConstructorCallImplementation">
	/// Callback that implements the call to a base class constructor if the type in creation derives from another type;
	/// <c>null</c> to call the parameterless constructor of the base class.
	/// </param>
	/// <param name="implementConstructorCallback">
	/// Callback that emits additional code to execute after the constructor of the base class has run;
	/// <c>null</c> to skip emitting additional code.
	/// </param>
	public IGeneratedConstructor AddConstructor(
		Visibility                                     visibility,
		Type[]                                         parameterTypes,
		ConstructorBaseClassCallImplementationCallback baseClassConstructorCallImplementation = null,
		ConstructorImplementationCallback              implementConstructorCallback           = null)
	{
		// check parameter types
		if (parameterTypes == null) throw new ArgumentNullException(nameof(parameterTypes));
		if (mConstructors.FirstOrDefault(x => x.ParameterTypes.SequenceEqual(parameterTypes)) != null)
			throw new CodeGenException("A constructor with the same signature is already part of the definition.");

		// create constructor
		var constructor = new GeneratedConstructor(
			this,
			visibility,
			parameterTypes,
			baseClassConstructorCallImplementation,
			implementConstructorCallback);

		mConstructors.Add(constructor);
		return constructor;
	}

	#endregion

	#region Adding Fields

	/// <summary>
	/// Adds a new instance field to the type definition (without an initial value).
	/// </summary>
	/// <typeparam name="T">Type of the field to add.</typeparam>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <returns>The added field.</returns>
	public IGeneratedField<T> AddField<T>(string name, Visibility visibility)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var field = new GeneratedField<T>(this, false, name, visibility);
		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new instance field to the type definition (without an initial value).
	/// </summary>
	/// <param name="type">Type of the field to add.</param>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException">The specified <paramref name="type"/> is <c>null</c>.</exception>
	public IGeneratedField AddField(Type type, string name, Visibility visibility)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedField<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(bool), typeof(string), typeof(Visibility)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var field = (IGeneratedFieldInternal)constructor.Invoke([this, false, name, visibility]);

		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new instance field to the type definition (with initial value).
	/// </summary>
	/// <typeparam name="T">Type of the field to add.</typeparam>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="initialValue">Initial value of the field.</param>
	/// <returns>The added field.</returns>
	public IGeneratedField<T> AddField<T>(string name, Visibility visibility, T initialValue)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var field = new GeneratedField<T>(this, false, name, visibility, initialValue);
		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new instance field to the type definition (with initial value).
	/// </summary>
	/// <param name="type">Type of the field to add.</param>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="initialValue">Initial value of the field (must be of the specified <paramref name="type"/>).</param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException">The specified <paramref name="type"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentException">
	/// The initial value is not assignable to a field of the specified type.<br/>
	/// -or-<br/>
	/// The field to add is a value type, initial value <c>null</c> is not allowed.
	/// </exception>
	public IGeneratedField AddField(
		Type       type,
		string     name,
		Visibility visibility,
		object     initialValue)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		if (initialValue != null && !type.IsInstanceOfType(initialValue))
			throw new ArgumentException("The initial value is not assignable to a field of the specified type.", nameof(initialValue));

		if (initialValue == null && type.IsValueType)
			throw new ArgumentException("The field to add is a value type, initial value <null> is not allowed.", nameof(initialValue));

		ConstructorInfo constructor = typeof(GeneratedField<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(bool), typeof(string), typeof(Visibility), type],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var field = (IGeneratedFieldInternal)constructor.Invoke([this, false, name, visibility, initialValue]);

		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new instance field to the type definition (with custom initializer).
	/// </summary>
	/// <typeparam name="T">Type of the field to add.</typeparam>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="initializer">
	/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
	/// value for the generated field.
	/// </param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="initializer"/> is <c>null</c>.</exception>
	public IGeneratedField<T> AddField<T>(string name, Visibility visibility, FieldInitializer initializer)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var field = new GeneratedField<T>(this, false, name, visibility, initializer);
		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new instance field to the type definition (with custom initializer).
	/// </summary>
	/// <param name="type">Type of the field to add.</param>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="initializer">
	/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
	/// value for the generated field.
	/// </param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException">
	/// The specified <paramref name="type"/> or <paramref name="initializer"/> is <c>null</c>.
	/// </exception>
	public IGeneratedField AddField(
		Type             type,
		string           name,
		Visibility       visibility,
		FieldInitializer initializer)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedField<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(bool), typeof(string), typeof(Visibility), typeof(FieldInitializer)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var field = (IGeneratedFieldInternal)constructor.Invoke([this, false, name, visibility, initializer]);

		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new instance field to the type definition (with factory callback for an initial value).
	/// </summary>
	/// <typeparam name="T">Type of the field to add.</typeparam>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="provideInitialValueCallback">Factory callback providing the initial value of the field.</param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="provideInitialValueCallback"/> is <c>null</c>.</exception>
	public IGeneratedField<T> AddField<T>(string name, Visibility visibility, ProvideValueCallback<T> provideInitialValueCallback)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var field = new GeneratedField<T>(this, false, name, visibility, provideInitialValueCallback);
		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new instance field to the type definition (with factory callback for an initial value).
	/// </summary>
	/// <param name="type">Type of the field to add.</param>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="provideInitialValueCallback">Factory callback providing the initial value of the field.</param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException">
	/// The specified <paramref name="type"/> or <paramref name="provideInitialValueCallback"/> is <c>null</c>.
	/// </exception>
	public IGeneratedField AddField(
		Type                 type,
		string               name,
		Visibility           visibility,
		ProvideValueCallback provideInitialValueCallback)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedField<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(bool), typeof(string), typeof(Visibility), typeof(ProvideValueCallback)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var field = (IGeneratedFieldInternal)constructor.Invoke([this, false, name, visibility, provideInitialValueCallback]);

		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new static field to the type definition (without an initial value).
	/// </summary>
	/// <typeparam name="T">Type of the field to add.</typeparam>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <returns>The added field.</returns>
	public IGeneratedField<T> AddStaticField<T>(string name, Visibility visibility)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var field = new GeneratedField<T>(this, true, name, visibility);
		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new static field to the type definition (without an initial value).
	/// </summary>
	/// <param name="type">Type of the field to add.</param>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <returns>The added field.</returns>
	public IGeneratedField AddStaticField(Type type, string name, Visibility visibility)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedField<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(bool), typeof(string), typeof(Visibility)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var field = (IGeneratedFieldInternal)constructor.Invoke([this, true, name, visibility]);

		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new static field to the type definition (with initial value).
	/// </summary>
	/// <typeparam name="T">Type of the field to add.</typeparam>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="initialValue">Initial value of the field.</param>
	/// <returns>The added field.</returns>
	public IGeneratedField<T> AddStaticField<T>(string name, Visibility visibility, T initialValue)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var field = new GeneratedField<T>(this, true, name, visibility, initialValue);
		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new static field to the type definition (with initial value).
	/// </summary>
	/// <param name="type">Type of the field to add.</param>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="initialValue">Initial value of the field.</param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException">The specified <paramref name="type"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentException">
	/// The initial value is not assignable to a field of the specified type.<br/>
	/// -or-<br/>
	/// The field to add is a value type, initial value <c>null</c> is not allowed.
	/// </exception>
	public IGeneratedField AddStaticField(
		Type       type,
		string     name,
		Visibility visibility,
		object     initialValue)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		if (initialValue != null && !type.IsInstanceOfType(initialValue))
			throw new ArgumentException("The initial value is not assignable to a field of the specified type.", nameof(initialValue));

		if (initialValue == null && type.IsValueType)
			throw new ArgumentException("The field to add is a value type, initial value <null> is not allowed.", nameof(initialValue));

		ConstructorInfo constructor = typeof(GeneratedField<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(bool), typeof(string), typeof(Visibility), type],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var field = (IGeneratedFieldInternal)constructor.Invoke([this, true, name, visibility, initialValue]);

		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new static field to the type definition (with custom initializer).
	/// </summary>
	/// <typeparam name="T">Type of the field to add.</typeparam>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="initializer">
	/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
	/// value for the generated field.
	/// </param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="initializer"/> is <c>null</c>.</exception>
	public IGeneratedField<T> AddStaticField<T>(string name, Visibility visibility, FieldInitializer initializer)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var field = new GeneratedField<T>(this, true, name, visibility, initializer);
		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new static field to the type definition (with custom initializer).
	/// </summary>
	/// <param name="type">Type of the field to add.</param>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="initializer">
	/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
	/// value for the generated field.
	/// </param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException">
	/// The specified <paramref name="type"/> or <paramref name="initializer"/> is <c>null</c>.
	/// </exception>
	public IGeneratedField AddStaticField(
		Type             type,
		string           name,
		Visibility       visibility,
		FieldInitializer initializer)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedField<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(bool), typeof(string), typeof(Visibility), typeof(FieldInitializer)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var field = (IGeneratedFieldInternal)constructor.Invoke([this, true, name, visibility, initializer]);

		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new static field to the type definition (with factory callback for an initial value).
	/// </summary>
	/// <typeparam name="T">Type of the field to add.</typeparam>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="provideInitialValueCallback">Factory callback providing the initial value of the field.</param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="provideInitialValueCallback"/> is <c>null</c>.</exception>
	public IGeneratedField<T> AddStaticField<T>(string name, Visibility visibility, ProvideValueCallback<T> provideInitialValueCallback)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var field = new GeneratedField<T>(this, true, name, visibility, provideInitialValueCallback);
		mGeneratedFields.Add(field);
		return field;
	}

	/// <summary>
	/// Adds a new static field to the type definition (with factory callback for an initial value).
	/// </summary>
	/// <param name="type">Type of the field to add.</param>
	/// <param name="name">Name of the field to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the field.</param>
	/// <param name="provideInitialValueCallback">Factory callback providing the initial value of the field.</param>
	/// <returns>The added field.</returns>
	/// <exception cref="ArgumentNullException">
	/// The specified <paramref name="type"/> or <paramref name="provideInitialValueCallback"/> is <c>null</c>.
	/// </exception>
	public IGeneratedField AddStaticField(
		Type                 type,
		string               name,
		Visibility           visibility,
		ProvideValueCallback provideInitialValueCallback)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		ConstructorInfo constructor = typeof(GeneratedField<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(bool), typeof(string), typeof(Visibility), typeof(ProvideValueCallback)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var field = (IGeneratedFieldInternal)constructor.Invoke([this, true, name, visibility, provideInitialValueCallback]);

		mGeneratedFields.Add(field);
		return field;
	}

	#endregion

	#region Adding Events

	/// <summary>
	/// Adds a new instance event to the type definition.
	/// </summary>
	/// <typeparam name="T">Type of the event to add.</typeparam>
	/// <param name="name">Name of the event to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the event.</param>
	/// <param name="implementation">
	/// Implementation strategy that implements the add/remove accessor methods and the event raiser method, if added.
	/// </param>
	/// <returns>The added event.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedEvent<T> AddEvent<T>(string name, Visibility visibility, IEventImplementation implementation) where T : Delegate
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var generatedEvent = new GeneratedEvent<T>(
			this,
			EventKind.Normal,
			name,
			visibility,
			implementation);
		mGeneratedEvents.Add(generatedEvent);
		return generatedEvent;
	}

	/// <summary>
	/// Adds a new instance event to the type definition.
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
	public IGeneratedEvent<T> AddEvent<T>(
		string                              name,
		Visibility                          visibility,
		EventAccessorImplementationCallback addAccessorImplementationCallback,
		EventAccessorImplementationCallback removeAccessorImplementationCallback) where T : Delegate
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var generatedEvent = new GeneratedEvent<T>(
			this,
			EventKind.Normal,
			name,
			visibility,
			addAccessorImplementationCallback,
			removeAccessorImplementationCallback);
		mGeneratedEvents.Add(generatedEvent);
		return generatedEvent;
	}

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
		mGeneratedEvents.Add(generatedEvent);
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
		mGeneratedEvents.Add(generatedEvent);
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
		mGeneratedEvents.Add(generatedEvent);
		return generatedEvent;
	}

	/// <summary>
	/// Adds a new static event to the type definition.
	/// </summary>
	/// <typeparam name="T">Type of the event to add.</typeparam>
	/// <param name="eventName">Name of the event to add (<c>null</c> to create a random name).</param>
	/// <param name="visibility">Visibility of the event.</param>
	/// <param name="implementation">
	/// Implementation strategy that implements the add/remove accessor methods and the event raiser method, if added.
	/// </param>
	/// <returns>The added event.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedEvent<T> AddStaticEvent<T>(
		string               eventName,
		Visibility           visibility,
		IEventImplementation implementation) where T : Delegate
	{
		EnsureThatIdentifierHasNotBeenUsedYet(eventName);
		var generatedEvent = new GeneratedEvent<T>(
			this,
			EventKind.Static,
			eventName,
			visibility,
			implementation);
		mGeneratedEvents.Add(generatedEvent);
		return generatedEvent;
	}

	/// <summary>
	/// Adds a new static event to the type definition.
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
	public IGeneratedEvent<T> AddStaticEvent<T>(
		string                              name,
		Visibility                          visibility,
		EventAccessorImplementationCallback addAccessorImplementationCallback,
		EventAccessorImplementationCallback removeAccessorImplementationCallback) where T : Delegate
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var generatedEvent = new GeneratedEvent<T>(
			this,
			EventKind.Static,
			name,
			visibility,
			addAccessorImplementationCallback,
			removeAccessorImplementationCallback);
		mGeneratedEvents.Add(generatedEvent);
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
		mGeneratedEvents.Add(overrider);
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
		mGeneratedEvents.Add(overrider);
		return overrider;
	}

	#endregion

	#region Adding Properties

	/// <summary>
	/// Adds a new instance property to the type definition.<br/>
	/// The property does not have accessor methods.<br/>
	/// The desired accessor methods can be added using:<br/>
	/// - <see cref="IGeneratedProperty.AddGetAccessor"/><br/>
	/// - <see cref="IGeneratedProperty.AddSetAccessor"/>
	/// </summary>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <returns>The added property.</returns>
	public IGeneratedProperty<T> AddProperty<T>(string name)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var property = new GeneratedProperty<T>(this, PropertyKind.Normal, name);
		mGeneratedProperties.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new instance property to the type definition.<br/>
	/// The property does not have accessor methods.<br/>
	/// The desired accessor methods can be added using:<br/>
	/// - <see cref="IGeneratedProperty.AddGetAccessor"/><br/>
	/// - <see cref="IGeneratedProperty.AddSetAccessor"/>
	/// </summary>
	/// <param name="type">Type of the property to add.</param>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <returns>The added property.</returns>
	public IGeneratedProperty AddProperty(Type type, string name)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Normal, name]);

		mGeneratedProperties.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new instance property to the type definition.<br/>
	/// The property has accessor methods implemented by the specified implementation strategy.
	/// </summary>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessors of the property.</param>
	/// <returns>The added property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedProperty<T> AddProperty<T>(string name, IPropertyImplementation implementation)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var property = new GeneratedProperty<T>(this, PropertyKind.Normal, name, implementation);
		mGeneratedProperties.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new instance property to the type definition.<br/>
	/// The property has accessor methods implemented by the specified implementation strategy.
	/// </summary>
	/// <param name="type">Type of the property to add.</param>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessors of the property.</param>
	/// <returns>The added property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedProperty AddProperty(Type type, string name, IPropertyImplementation implementation)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string), typeof(IPropertyImplementation)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Normal, name, implementation]);

		mGeneratedProperties.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new abstract instance property to the type definition.
	/// </summary>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <returns>The added property.</returns>
	public IGeneratedProperty<T> AddAbstractProperty<T>(string name)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var property = new GeneratedProperty<T>(this, PropertyKind.Abstract, name, null);
		mGeneratedProperties.Add(property);
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
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Abstract, name]);

		mGeneratedProperties.Add(property);
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
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var property = new GeneratedProperty<T>(this, PropertyKind.Virtual, name);
		mGeneratedProperties.Add(property);
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
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Virtual, name]);

		mGeneratedProperties.Add(property);
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
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var property = new GeneratedProperty<T>(this, PropertyKind.Virtual, name, implementation);
		mGeneratedProperties.Add(property);
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
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string), typeof(IPropertyImplementation)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Virtual, name, implementation]);

		mGeneratedProperties.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new static property to the type definition.<br/>
	/// The property does not have accessor methods.<br/>
	/// The desired accessor methods can be added using:<br/>
	/// - <see cref="IGeneratedProperty.AddGetAccessor"/><br/>
	/// - <see cref="IGeneratedProperty.AddSetAccessor"/>
	/// </summary>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <returns>The added property.</returns>
	public IGeneratedProperty<T> AddStaticProperty<T>(string name)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var property = new GeneratedProperty<T>(this, PropertyKind.Static, name);
		mGeneratedProperties.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new static property to the type definition.<br/>
	/// The property does not have accessor methods.<br/>
	/// The desired accessor methods can be added using:<br/>
	/// - <see cref="IGeneratedProperty.AddGetAccessor"/><br/>
	/// - <see cref="IGeneratedProperty.AddSetAccessor"/>
	/// </summary>
	/// <param name="type">Type of the property to add.</param>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <returns>The added property.</returns>
	public IGeneratedProperty AddStaticProperty(Type type, string name)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Static, name]);

		mGeneratedProperties.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new static property to the type definition.<br/>
	/// The property has accessor methods implemented by the specified implementation strategy.
	/// </summary>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessors of the property.</param>
	/// <returns>The added property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedProperty<T> AddStaticProperty<T>(string name, IPropertyImplementation implementation)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var property = new GeneratedProperty<T>(this, PropertyKind.Static, name, implementation);
		mGeneratedProperties.Add(property);
		return property;
	}

	/// <summary>
	/// Adds a new static property to the type definition.<br/>
	/// The property has accessor methods implemented by the specified implementation strategy.
	/// </summary>
	/// <param name="type">Type of the property to add.</param>
	/// <param name="name">Name of the property to add (<c>null</c> to create a random name).</param>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessors of the property.</param>
	/// <returns>The added property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedProperty AddStaticProperty(Type type, string name, IPropertyImplementation implementation)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		if (type == null) throw new ArgumentNullException(nameof(type));

		ConstructorInfo constructor = typeof(GeneratedProperty<>)
			.MakeGenericType(type)
			.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				Type.DefaultBinder,
				[typeof(TypeDefinition), typeof(PropertyKind), typeof(string), typeof(IPropertyImplementation)],
				null);
		Debug.Assert(constructor != null, nameof(constructor) + " != null");
		var property = (IGeneratedProperty)constructor.Invoke([this, PropertyKind.Static, name, implementation]);

		mGeneratedProperties.Add(property);
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
		mGeneratedProperties.Add(overrider);
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
		mGeneratedProperties.Add(overrider);
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
		mGeneratedProperties.Add(overrider);
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
		mGeneratedProperties.Add(overrider);
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
		mGeneratedDependencyProperties.Add(dependencyProperty);
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
		mGeneratedDependencyProperties.Add(dependencyProperty);
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
		mGeneratedDependencyProperties.Add(dependencyProperty);
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
		mGeneratedDependencyProperties.Add(dependencyProperty);
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
		mGeneratedDependencyProperties.Add(dependencyProperty);
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
		mGeneratedDependencyProperties.Add(dependencyProperty);
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
		mGeneratedDependencyProperties.Add(dependencyProperty);
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
		mGeneratedDependencyProperties.Add(dependencyProperty);
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
	/// Adds a new instance method to the type definition.
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
	public IGeneratedMethod AddMethod(
		string                name,
		Type                  returnType,
		Type[]                parameterTypes,
		Visibility            visibility,
		IMethodImplementation implementation,
		MethodAttributes      additionalMethodAttributes = 0)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var method = new GeneratedMethod(this, MethodKind.Normal, name, returnType, parameterTypes, visibility, additionalMethodAttributes, implementation);
		mGeneratedMethods.Add(method);
		return method;
	}

	/// <summary>
	/// Adds a new instance method to the type definition.
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
	public IGeneratedMethod AddMethod(
		string                       name,
		Type                         returnType,
		Type[]                       parameterTypes,
		Visibility                   visibility,
		MethodImplementationCallback implementationCallback,
		MethodAttributes             additionalMethodAttributes = 0)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var method = new GeneratedMethod(this, MethodKind.Normal, name, returnType, parameterTypes, visibility, additionalMethodAttributes, implementationCallback);
		mGeneratedMethods.Add(method);
		return method;
	}

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
		mGeneratedMethods.Add(method);
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
		mGeneratedMethods.Add(method);
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
		mGeneratedMethods.Add(method);
		return method;
	}

	/// <summary>
	/// Adds a new static method to the type definition.
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
	public IGeneratedMethod AddStaticMethod(
		string                name,
		Type                  returnType,
		Type[]                parameterTypes,
		Visibility            visibility,
		IMethodImplementation implementation,
		MethodAttributes      additionalMethodAttributes = 0)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var method = new GeneratedMethod(this, MethodKind.Static, name, returnType, parameterTypes, visibility, additionalMethodAttributes, implementation);
		mGeneratedMethods.Add(method);
		return method;
	}

	/// <summary>
	/// Adds a new static method to the type definition.
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
	public IGeneratedMethod AddStaticMethod(
		string                       name,
		Type                         returnType,
		Type[]                       parameterTypes,
		Visibility                   visibility,
		MethodImplementationCallback implementationCallback,
		MethodAttributes             additionalMethodAttributes = 0)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);
		var method = new GeneratedMethod(this, MethodKind.Static, name, returnType, parameterTypes, visibility, additionalMethodAttributes, implementationCallback);
		mGeneratedMethods.Add(method);
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
		mGeneratedMethods.Add(overriddenMethod);
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
		mGeneratedMethods.Add(overriddenMethod);
		return overriddenMethod;
	}

	/// <summary>
	/// Adds a method to the type definition.
	/// </summary>
	/// <param name="kind">
	/// Kind of the property to add. May be one of the following:<br/>
	/// - <see cref="MethodKind.Static"/><br/>
	/// - <see cref="MethodKind.Normal"/><br/>
	/// - <see cref="MethodKind.Virtual"/><br/>
	/// - <see cref="MethodKind.Abstract"/>
	/// </param>
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
	/// <paramref name="kind"/> is an invalid method kind.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> is not a valid language independent identifier.<br/>
	/// -or-<br/>
	/// <paramref name="parameterTypes"/> contains a null reference.
	/// </exception>
	public IGeneratedMethod AddMethod(
		MethodKind            kind,
		string                name,
		Type                  returnType,
		Type[]                parameterTypes,
		Visibility            visibility,
		IMethodImplementation implementation,
		MethodAttributes      additionalMethodAttributes = 0)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		// ensure that a valid property kind was specified
		switch (kind)
		{
			case MethodKind.Static:
			case MethodKind.Normal:
			case MethodKind.Virtual:
			case MethodKind.Abstract:
				break;

			case MethodKind.Override:
				throw new ArgumentException(
					$"Method kind must not be '{MethodKind.Override}'. " +
					$"Overrides should be done using the {nameof(IInheritedMethod)}.{nameof(IInheritedMethod.Override)}() methods.");

			default:
				throw new ArgumentException("Invalid method kind.", nameof(kind));
		}

		// create method
		var method = new GeneratedMethod(this, kind, name, returnType, parameterTypes, visibility, additionalMethodAttributes, implementation);
		mGeneratedMethods.Add(method);
		return method;
	}

	/// <summary>
	/// Adds a method to the type definition.
	/// </summary>
	/// <param name="kind">
	/// Kind of the property to add. May be one of the following:<br/>
	/// - <see cref="MethodKind.Static"/><br/>
	/// - <see cref="MethodKind.Normal"/><br/>
	/// - <see cref="MethodKind.Virtual"/><br/>
	/// - <see cref="MethodKind.Abstract"/>
	/// </param>
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
	/// <paramref name="kind"/> is an invalid method kind.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> is not a valid language independent identifier.<br/>
	/// -or-<br/>
	/// <paramref name="parameterTypes"/> contains a null reference.
	/// </exception>
	public IGeneratedMethod AddMethod(
		MethodKind                   kind,
		string                       name,
		Type                         returnType,
		Type[]                       parameterTypes,
		Visibility                   visibility,
		MethodImplementationCallback implementationCallback,
		MethodAttributes             additionalMethodAttributes = 0)
	{
		EnsureThatIdentifierHasNotBeenUsedYet(name);

		// ensure that a valid property kind was specified
		switch (kind)
		{
			case MethodKind.Static:
			case MethodKind.Normal:
			case MethodKind.Virtual:
			case MethodKind.Abstract:
				break;

			case MethodKind.Override:
				throw new ArgumentException(
					$"Method kind must not be '{MethodKind.Override}'. " +
					$"Overrides should be done using the {nameof(IInheritedMethod)}.{nameof(IInheritedMethod.Override)}() methods.");

			default:
				throw new ArgumentException("Invalid method kind.", nameof(kind));
		}

		// create method
		var method = new GeneratedMethod(this, kind, name, returnType, parameterTypes, visibility, additionalMethodAttributes, implementationCallback);
		mGeneratedMethods.Add(method);
		return method;
	}

	#endregion

	#region Creating the Defined Type

	/// <summary>
	/// Creates the defined type.
	/// </summary>
	/// <returns>The created type.</returns>
	public Type CreateType()
	{
		// add class constructor
		AddClassConstructor(TypeBuilder);

		// add constructors
		AddConstructors();

		// implement methods (includes event accessor methods and property accessor methods)
		foreach (IGeneratedMethodInternal generatedMethod in mGeneratedMethods) generatedMethod.Implement();

		// create the defined type
		Type createdType = null;
		while (mTypeBuilders.Count > 0)
		{
			TypeBuilder builder = mTypeBuilders.Pop();
			if (createdType == null) createdType = builder.CreateTypeInfo().AsType();
			else builder.CreateTypeInfo().AsType();
		}

		CodeGenExternalStorage.Add(createdType, [.. ExternalObjects]);
		return createdType;
	}

	/// <summary>
	/// Adds the class constructor to the specified type and calls the specified modules to add their code to it.
	/// </summary>
	private void AddClassConstructor(TypeBuilder typeBuilder)
	{
		// define the class constructor
		ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
			MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
			CallingConventions.Standard,
			Type.EmptyTypes);

		// get MSIL generator attached to the constructor
		ILGenerator msilGenerator = constructorBuilder.GetILGenerator();

		// add field initialization code
		foreach (IGeneratedFieldInternal field in mGeneratedFields.Where(x => x.IsStatic))
		{
			field.ImplementFieldInitialization(typeBuilder, msilGenerator);
		}

		// add opcode to return from the class constructor
		msilGenerator.Emit(OpCodes.Ret);
	}

	/// <summary>
	/// Adds a parameterless constructor to the specified type and calls the specified modules to add their code to it.
	/// </summary>
	private void AddConstructors()
	{
		// add default constructor if no constructor is defined until now...
		if (mConstructors.Count == 0)
		{
			var defaultConstructor = new GeneratedConstructor(this, Visibility.Public, Type.EmptyTypes, null, null);
			mConstructors.Add(defaultConstructor);
		}

		// create constructors
		foreach (IGeneratedConstructorInternal constructor in mConstructors)
		{
			ILGenerator msilGenerator = constructor.ConstructorBuilder.GetILGenerator();

			// call parameterless constructor of the base class if the created class has a base class
			if (TypeBuilder.BaseType != null)
			{
				if (constructor.BaseClassConstructorCallImplementationCallback != null)
				{
					// the constructor will use user-supplied code to call a base class constructor
					constructor.BaseClassConstructorCallImplementationCallback(constructor, msilGenerator);
				}
				else
				{
					// the constructor does not have any special handling of the base class constructor call
					// => call parameterless constructor
					const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
					ConstructorInfo constructorInfo = TypeBuilder.BaseType.GetConstructor(bindingFlags, null, Type.EmptyTypes, null);
					if (constructorInfo == null)
					{
						string error = $"The base class ({TypeBuilder.BaseType.FullName}) does not have an accessible parameterless constructor.";
						throw new CodeGenException(error);
					}

					msilGenerator.Emit(OpCodes.Ldarg_0);
					msilGenerator.Emit(OpCodes.Call, constructorInfo);
				}
			}

			// emit field initialization code
			foreach (IGeneratedFieldInternal field in mGeneratedFields.Where(x => !x.IsStatic))
			{
				field.ImplementFieldInitialization(TypeBuilder, msilGenerator);
			}

			// emit additional constructor code
			constructor.ImplementConstructorCallback?.Invoke(constructor, msilGenerator);

			// emit 'ret' to return from the constructor
			msilGenerator.Emit(OpCodes.Ret);
		}
	}

	#endregion

	#region Helpers

	/// <summary>
	/// Gets inherited abstract properties that have not been implemented/overridden, yet.
	/// </summary>
	/// <returns>Inherited abstract properties that still need to be overridden to allow the type to be created.</returns>
	public IInheritedProperty[] GetAbstractPropertiesWithoutOverride()
	{
		IEnumerable<IInheritedProperty> abstractProperties =
			from property in InheritedProperties.Where(x => x.Kind == PropertyKind.Abstract)
			let overrider = mGeneratedProperties.Find(x => x.Kind == PropertyKind.Override && x.Name == property.Name)
			where overrider == null
			select property;

		return [.. abstractProperties];
	}

	/// <summary>
	/// Gets the method with the specified name and the specified parameters
	/// (works with inherited and generated methods).
	/// </summary>
	/// <param name="name">Name of the method to get.</param>
	/// <param name="parameterTypes">Types of the method's parameters.</param>
	/// <returns>
	/// The requested method;<br/>
	/// <c>null</c>, if the method was not found.
	/// </returns>
	public IMethod GetMethod(string name, Type[] parameterTypes)
	{
		return (IMethod)mGeneratedMethods.FirstOrDefault(m => m.Name == name && m.ParameterTypes.SequenceEqual(parameterTypes)) ??
		       InheritedMethods.FirstOrDefault(m => m.Name == name && m.ParameterTypes.SequenceEqual(parameterTypes));
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
	private static bool HasSameSignature(FieldInfo x, FieldInfo y)
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
	private static bool HasSameSignature(EventInfo x, EventInfo y)
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
	private static bool HasSameSignature(PropertyInfo x, PropertyInfo y)
	{
		if (x.Name != y.Name) return false;
		return x.PropertyType == y.PropertyType;
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
	private static bool HasSameSignature(MethodInfo x, MethodInfo y)
	{
		return x.Name == y.Name && x.GetParameters().Select(z => z.ParameterType).SequenceEqual(y.GetParameters().Select(z => z.ParameterType));
	}

	/// <summary>
	/// Ensures that the specified identifier (field, property, method) has not been used, yet.
	/// </summary>
	/// <param name="identifier">Name of the identifier to check.</param>
	/// <exception cref="CodeGenException">The identifier with the specified name has already been declared.</exception>
	private void EnsureThatIdentifierHasNotBeenUsedYet(string identifier)
	{
		if (identifier == null) return; // null means that a unique name is chosen => no conflict...

		if (mGeneratedFields.Any(field => field.Name == identifier))
		{
			throw new CodeGenException($"The specified identifier ({identifier}) has already been used to declare a field.");
		}

		if (mGeneratedEvents.Any(@event => @event.Name == identifier))
		{
			throw new CodeGenException($"The specified identifier ({identifier}) has already been used to declare an event.");
		}

		if (mGeneratedProperties.Any(property => property.Name == identifier))
		{
			throw new CodeGenException($"The specified identifier ({identifier}) has already been used to declare a property.");
		}

		if (mGeneratedMethods.Any(method => method.Name == identifier))
		{
			throw new CodeGenException($"The specified identifier ({identifier}) has already been used to declare a method.");
		}
	}

	/// <summary>
	/// Ensures that the defined type derives from the specified type.
	/// </summary>
	/// <param name="type">Type the defined types is expected to derive from.</param>
	private void EnsureThatTypeDerivesFrom(Type type)
	{
		if (!type.IsAssignableFrom(TypeBuilder))
		{
			throw new CodeGenException($"The defined type ({TypeBuilder.FullName}) does not derive from '{type.FullName}'.");
		}
	}

	#endregion
}
