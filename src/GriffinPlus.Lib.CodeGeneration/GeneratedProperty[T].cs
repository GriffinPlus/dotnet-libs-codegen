///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// A generated property.
/// </summary>
class GeneratedProperty<T> : Member, IGeneratedProperty<T>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedProperty{T}"/> class.
	/// The generated property does not have accessor methods.
	/// The desired accessor methods can be added using <see cref="AddGetAccessor"/> and <see cref="AddSetAccessor"/>.
	/// </summary>
	/// <param name="typeDefinition">The type definition the property belongs to.</param>
	/// <param name="kind">Kind of property determining whether the property is static, abstract, virtual or an override.</param>
	/// <param name="name">Name of the property (<c>null</c> to create a random name).</param>
	/// <param name="implementation">
	/// Implementation strategy that implements the 'get'/'set' accessor methods of the property.
	/// Must be <c>null</c>, if <paramref name="kind"/> is <see cref="PropertyKind.Abstract"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="typeDefinition"/> is <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="kind"/> is not <see cref="PropertyKind.Abstract"/> and <paramref name="implementation"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="kind"/> is <see cref="PropertyKind.Abstract"/> and <paramref name="implementation"/> is not <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> is not a valid language independent identifier.
	/// </exception>
	internal GeneratedProperty(
		TypeDefinition          typeDefinition,
		PropertyKind            kind,
		string                  name,
		IPropertyImplementation implementation) :
		base(typeDefinition)
	{
		if (kind == PropertyKind.Abstract && implementation != null) throw new ArgumentException($"Property kind is '{kind}', an implementation strategy must not be specified.");
		if (kind != PropertyKind.Abstract && implementation == null) throw new ArgumentNullException(nameof(implementation));

		Kind = kind;
		Implementation = implementation;

		// set the name of the property and check whether it is a valid identifier
		Name = string.IsNullOrWhiteSpace(name) ? "Property_" + Guid.NewGuid().ToString("N") : name;
		EnsureNameIsValidLanguageIndependentIdentifier(Name);

		// ensure that the property type is public and all nested types are public, too
		// => otherwise the dynamically created assembly is not able to access it
		CodeGenHelpers.EnsureTypeIsTotallyPublic(typeof(T));

		// create a property builder for the property
		PropertyBuilder = TypeDefinition.TypeBuilder.DefineProperty(
			Name,
			PropertyAttributes.None,
			PropertyType,
			Type.EmptyTypes);

		// declare implementation strategy specific members
		Implementation?.Declare(TypeDefinition, this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedProperty{T}"/> class.
	/// The generated property does not have accessor methods.
	/// The desired accessor methods can be added using <see cref="AddGetAccessor"/> and <see cref="AddSetAccessor"/>.
	/// </summary>
	/// <param name="typeDefinition">The type definition the property belongs to.</param>
	/// <param name="kind">Kind of property determining whether the property is static, abstract, virtual or an override.</param>
	/// <param name="name">Name of the property (<c>null</c> to create a random name).</param>
	/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
	internal GeneratedProperty(
		TypeDefinition typeDefinition,
		PropertyKind   kind,
		string         name) :
		base(typeDefinition)
	{
		Kind = kind;

		// set the name of the property and check whether it is a valid identifier
		Name = string.IsNullOrWhiteSpace(name) ? "Property_" + Guid.NewGuid().ToString("N") : name;
		EnsureNameIsValidLanguageIndependentIdentifier(Name);

		// ensure that the property type is public and all nested types are public, too
		// => otherwise the dynamically created assembly is not able to access it
		CodeGenHelpers.EnsureTypeIsTotallyPublic(typeof(T));

		// create a property builder for the property
		PropertyBuilder = TypeDefinition.TypeBuilder.DefineProperty(
			Name,
			PropertyAttributes.None,
			PropertyType,
			Type.EmptyTypes);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedProperty{T}"/> class (for overrides).
	/// </summary>
	/// <param name="classDefinition">The class definition the property belongs to.</param>
	/// <param name="inheritedProperty">Inherited property to override.</param>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessor methods of the property.</param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="classDefinition"/>, <paramref name="inheritedProperty"/> or <paramref name="implementation"/> is <c>null</c>.
	/// </exception>
	internal GeneratedProperty(
		ClassDefinition         classDefinition,
		IInheritedProperty<T>   inheritedProperty,
		IPropertyImplementation implementation) :
		base(classDefinition)
	{
		// check parameters
		if (inheritedProperty == null) throw new ArgumentNullException(nameof(inheritedProperty));

		Name = inheritedProperty.Name;
		Kind = PropertyKind.Override;
		Implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));

		if (inheritedProperty.GetAccessor != null)
		{
			GetAccessor = classDefinition.AddMethodOverride(
				inheritedProperty.GetAccessor,
				(_, msilGenerator) => Implementation.ImplementGetAccessorMethod(
					TypeDefinition,
					this,
					msilGenerator));
		}

		if (inheritedProperty.SetAccessor != null)
		{
			SetAccessor = classDefinition.AddMethodOverride(
				inheritedProperty.SetAccessor,
				(_, msilGenerator) => Implementation.ImplementSetAccessorMethod(
					TypeDefinition,
					this,
					msilGenerator));
		}

		// declare implementation strategy specific members
		Implementation?.Declare(TypeDefinition, this);

		Freeze();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedProperty{T}"/> class (for overrides).
	/// </summary>
	/// <param name="classDefinition">The class definition the property belongs to.</param>
	/// <param name="inheritedProperty">Inherited property to override.</param>
	/// <param name="getAccessorImplementationCallback">
	/// A callback that implements the 'get' accessor method of the property
	/// (<c>null</c>, if the inherited property does not have a 'get' accessor method).
	/// </param>
	/// <param name="setAccessorImplementationCallback">
	/// A callback that implements the 'set' accessor method of the property
	/// (<c>null</c>, if the inherited property does not have a 'set' accessor method).
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="classDefinition"/> or <paramref name="inheritedProperty"/> is <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="inheritedProperty"/> has a 'get' accessor, but <paramref name="getAccessorImplementationCallback"/> is <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="inheritedProperty"/> has a 'set' accessor, but <paramref name="setAccessorImplementationCallback"/> is <c>null</c>.
	/// </exception>
	internal GeneratedProperty(
		ClassDefinition                        classDefinition,
		IInheritedProperty<T>                  inheritedProperty,
		PropertyAccessorImplementationCallback getAccessorImplementationCallback,
		PropertyAccessorImplementationCallback setAccessorImplementationCallback) :
		base(classDefinition)
	{
		// check parameters
		if (inheritedProperty == null) throw new ArgumentNullException(nameof(inheritedProperty));


		Name = inheritedProperty.Name;
		Kind = PropertyKind.Override;

		if (inheritedProperty.GetAccessor != null)
		{
			if (getAccessorImplementationCallback == null) throw new ArgumentNullException(nameof(getAccessorImplementationCallback));

			GetAccessor = classDefinition.AddMethodOverride(
				inheritedProperty.GetAccessor,
				(_, msilGenerator) => getAccessorImplementationCallback(this, msilGenerator));
		}

		if (inheritedProperty.SetAccessor != null)
		{
			if (setAccessorImplementationCallback == null) throw new ArgumentNullException(nameof(setAccessorImplementationCallback));

			SetAccessor = classDefinition.AddMethodOverride(
				inheritedProperty.SetAccessor,
				(_, msilGenerator) => setAccessorImplementationCallback(this, msilGenerator));
		}

		Freeze();
	}

	/// <summary>
	/// Gets the name of the property.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the type of the property.
	/// </summary>
	public Type PropertyType => typeof(T);

	/// <summary>
	/// Gets the property kind indicating whether the property is static, virtual, abstract or an override of an abstract/virtual property.
	/// </summary>
	public PropertyKind Kind { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.PropertyBuilder"/> associated with the property.
	/// </summary>
	public PropertyBuilder PropertyBuilder { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.PropertyInfo"/> associated with the property.
	/// </summary>
	PropertyInfo IProperty.PropertyInfo => PropertyBuilder;

	/// <summary>
	/// Gets the 'get' accessor method
	/// (<c>null</c>, if the property does not have a 'get' accessor)
	/// </summary>
	IMethod IProperty.GetAccessor => GetAccessor;

	/// <summary>
	/// Gets the 'get' accessor method
	/// (<c>null</c>, if the property does not have a 'get' accessor)
	/// </summary>
	public IGeneratedMethod GetAccessor { get; private set; }

	/// <summary>
	/// Gets the 'set' accessor method
	/// (<c>null</c>, if the property does not have a 'set' accessor)
	/// </summary>
	IMethod IProperty.SetAccessor => SetAccessor;

	/// <summary>
	/// Gets the 'set' accessor method
	/// (<c>null</c>, if the property does not have a 'set' accessor)
	/// </summary>
	public IGeneratedMethod SetAccessor { get; private set; }

	/// <summary>
	/// Gets the implementation strategy used to implement the property
	/// (<c>null</c>, if implementation callbacks are used).
	/// </summary>
	public IPropertyImplementation Implementation { get; }

	/// <summary>
	/// Adds a 'get' accessor method to the property.
	/// </summary>
	/// <param name="visibility">Visibility of the 'get' accessor to add.</param>
	/// <param name="getAccessorImplementationCallback">
	/// A callback that implements the 'get' accessor method of the property
	/// (<c>null</c> to let the implementation strategy implement the method if specified when adding the property).
	/// </param>
	/// <returns>The added 'get' accessor method.</returns>
	/// <exception cref="ArgumentException">
	/// <paramref name="getAccessorImplementationCallback"/> is not <c>null</c> and the property is abstract.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="getAccessorImplementationCallback"/> is <c>null</c> and the property was created without an implementation strategy.
	/// </exception>
	/// <exception cref="InvalidOperationException">The 'get' accessor method was already added to the property.</exception>
	public IGeneratedMethod AddGetAccessor(
		Visibility                             visibility                        = Visibility.Public,
		PropertyAccessorImplementationCallback getAccessorImplementationCallback = null)
	{
		if (Kind == PropertyKind.Abstract)
		{
			if (getAccessorImplementationCallback != null)
			{
				throw new ArgumentException(
					"The implementation callback must not be specified as the property is abstract.",
					nameof(getAccessorImplementationCallback));
			}
		}
		else
		{
			if (getAccessorImplementationCallback == null && Implementation == null)
			{
				throw new ArgumentNullException(
					nameof(getAccessorImplementationCallback),
					"The implementation callback must be specified as the property was generated without an implementation strategy.");
			}
		}

		// ensure the property is not frozen
		// (overrides must provide the same get/set accessor methods as the base type)
		EnsureNotFrozen();

		// abort if the get accessor method was already added to the type definition
		if (GetAccessor != null)
			throw new InvalidOperationException("The get accessor method was already added.");

		// add the 'get' accessor method
		if (Kind == PropertyKind.Abstract)
		{
			GetAccessor = TypeDefinition.AddMethod(
				Kind.ToAccessorMethodKind(),
				"get_" + Name,
				PropertyType,
				Type.EmptyTypes,
				visibility,
				(MethodImplementationCallback)null,
				MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Abstract);
		}
		else
		{
			if (getAccessorImplementationCallback != null)
			{
				GetAccessor = TypeDefinition.AddMethod(
					Kind.ToAccessorMethodKind(),
					"get_" + Name,
					PropertyType,
					Type.EmptyTypes,
					visibility,
					(_, msilGenerator) =>
					{
						getAccessorImplementationCallback(this, msilGenerator);
					},
					MethodAttributes.SpecialName | MethodAttributes.HideBySig);
			}
			else
			{
				GetAccessor = TypeDefinition.AddMethod(
					Kind.ToAccessorMethodKind(),
					"get_" + Name,
					PropertyType,
					Type.EmptyTypes,
					visibility,
					(_, msilGenerator) =>
					{
						Implementation.ImplementGetAccessorMethod(TypeDefinition, this, msilGenerator);
					},
					MethodAttributes.SpecialName | MethodAttributes.HideBySig);
			}
		}

		// link the accessor method to the property
		PropertyBuilder.SetGetMethod(GetAccessor.MethodBuilder);

		return GetAccessor;
	}

	/// <summary>
	/// Adds a 'set' accessor method to the property.
	/// </summary>
	/// <param name="visibility">Visibility of the 'set' accessor to add.</param>
	/// <param name="setAccessorImplementationCallback">
	/// A callback that implements the 'set' accessor method of the property
	/// (<c>null</c> to let the implementation strategy implement the method if specified when adding the property).
	/// </param>
	/// <returns>The added 'set' accessor method.</returns>
	/// <exception cref="ArgumentException">
	/// <paramref name="setAccessorImplementationCallback"/> is not <c>null</c> and the property is abstract.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="setAccessorImplementationCallback"/> is <c>null</c> and the property was created without an implementation strategy.
	/// </exception>
	/// <exception cref="InvalidOperationException">The 'set' accessor method was already added to the property.</exception>
	public IGeneratedMethod AddSetAccessor(
		Visibility                             visibility                        = Visibility.Public,
		PropertyAccessorImplementationCallback setAccessorImplementationCallback = null)
	{
		if (Kind == PropertyKind.Abstract)
		{
			if (setAccessorImplementationCallback != null)
			{
				throw new ArgumentException(
					"The implementation callback must not be specified as the property is abstract.",
					nameof(setAccessorImplementationCallback));
			}
		}
		else
		{
			if (setAccessorImplementationCallback == null && Implementation == null)
			{
				throw new ArgumentNullException(
					nameof(setAccessorImplementationCallback),
					"The implementation callback must be specified as the property was generated without an implementation strategy.");
			}
		}

		// ensure the property is not frozen
		// (overrides must provide the same get/set accessor methods as the base type)
		EnsureNotFrozen();

		// abort if the set accessor method was already added to the type definition
		if (SetAccessor != null)
			throw new InvalidOperationException("The set accessor method was already added.");

		// add the 'set' accessor method
		if (Kind == PropertyKind.Abstract)
		{
			SetAccessor = TypeDefinition.AddMethod(
				Kind.ToAccessorMethodKind(),
				"set_" + Name,
				typeof(void),
				[PropertyType],
				visibility,
				(MethodImplementationCallback)null,
				MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Abstract);
		}
		else
		{
			if (setAccessorImplementationCallback != null)
			{
				SetAccessor = TypeDefinition.AddMethod(
					Kind.ToAccessorMethodKind(),
					"set_" + Name,
					typeof(void),
					[PropertyType],
					visibility,
					(_, msilGenerator) =>
					{
						setAccessorImplementationCallback(this, msilGenerator);
					},
					MethodAttributes.SpecialName | MethodAttributes.HideBySig);
			}
			else
			{
				SetAccessor = TypeDefinition.AddMethod(
					Kind.ToAccessorMethodKind(),
					"set_" + Name,
					typeof(void),
					[PropertyType],
					visibility,
					(_, msilGenerator) =>
					{
						Implementation.ImplementSetAccessorMethod(TypeDefinition, this, msilGenerator);
					},
					MethodAttributes.SpecialName | MethodAttributes.HideBySig);
			}
		}

		// link the accessor method to the property
		PropertyBuilder.SetSetMethod(SetAccessor.MethodBuilder);

		return SetAccessor;
	}
}
