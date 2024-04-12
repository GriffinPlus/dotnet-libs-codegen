///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// A generated method.
/// </summary>
class GeneratedMethod : Member, IGeneratedMethodInternal
{
	private readonly IMethodImplementation        mImplementation;
	private readonly MethodImplementationCallback mImplementationCallback;
	private          Type[]                       mParameterTypes;

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMethod"/> class.
	/// </summary>
	/// <param name="typeDefinition">The type definition the method belongs to.</param>
	/// <param name="kind">Kind of method determining whether the method is static, abstract, virtual or an override.</param>
	/// <param name="name">Name of the method (<c>null</c> to create a random name).</param>
	/// <param name="returnType">Return type of the method.</param>
	/// <param name="parameterTypes">Types of the method parameters.</param>
	/// <param name="visibility">Visibility of the method.</param>
	/// <param name="additionalMethodAttributes">Additional method attributes to 'or' with other attributes.</param>
	/// <param name="implementation">
	/// Implementation strategy that implements the method.
	/// Must be <c>null</c>, if <paramref name="kind"/> is <see cref="MethodKind.Abstract"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="typeDefinition"/>, <paramref name="returnType"/> or <paramref name="parameterTypes"/> is <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="kind"/> is not <see cref="MethodKind.Abstract"/> and <paramref name="implementation"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="kind"/> is <see cref="MethodKind.Abstract"/> and <paramref name="implementation"/> is not <c>null</c>.<br/>
	/// -or-<br/>
	/// <paramref name="name"/> is not a valid language independent identifier.<br/>
	/// -or-<br/>
	/// <paramref name="parameterTypes"/> contains a null reference.
	/// </exception>
	internal GeneratedMethod(
		TypeDefinition        typeDefinition,
		MethodKind            kind,
		string                name,
		Type                  returnType,
		Type[]                parameterTypes,
		Visibility            visibility,
		MethodAttributes      additionalMethodAttributes,
		IMethodImplementation implementation) :
		base(typeDefinition)
	{
		if (kind == MethodKind.Abstract && implementation != null) throw new ArgumentException($"Method kind is '{kind}', an implementation strategy must not be specified.");
		if (kind != MethodKind.Abstract && implementation == null) throw new ArgumentNullException(nameof(implementation));
		mImplementation = implementation;
		Initialize(kind, name, returnType, parameterTypes, visibility, additionalMethodAttributes);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMethod"/> class.
	/// </summary>
	/// <param name="typeDefinition">The type definition the method belongs to.</param>
	/// <param name="kind">Kind of method determining whether the method is static, abstract, virtual or an override.</param>
	/// <param name="name">Name of the method (<c>null</c> to create a random name).</param>
	/// <param name="returnType">Return type of the method.</param>
	/// <param name="parameterTypes">Types of the method parameters.</param>
	/// <param name="visibility">Visibility of the method.</param>
	/// <param name="additionalMethodAttributes">Additional method attributes to 'or' with other attributes.</param>
	/// <param name="implementationCallback">
	/// A callback that implements the method.
	/// Must be <c>null</c>, if <paramref name="kind"/> is <see cref="MethodKind.Abstract"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="typeDefinition"/>, <paramref name="returnType"/> or <paramref name="parameterTypes"/> is <c>null</c><br/>
	/// -or-<br/>
	/// <paramref name="kind"/> is not <see cref="MethodKind.Abstract"/> and <paramref name="implementationCallback"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="kind"/> is <see cref="MethodKind.Abstract"/> and <paramref name="implementationCallback"/> is not <c>null</c><br/>
	/// -or-<br/>
	/// <paramref name="name"/> is not a valid language independent identifier<br/>
	/// -or-<br/>
	/// <paramref name="parameterTypes"/> contains a null reference.
	/// </exception>
	internal GeneratedMethod(
		TypeDefinition               typeDefinition,
		MethodKind                   kind,
		string                       name,
		Type                         returnType,
		Type[]                       parameterTypes,
		Visibility                   visibility,
		MethodAttributes             additionalMethodAttributes,
		MethodImplementationCallback implementationCallback) :
		base(typeDefinition)
	{
		if (kind == MethodKind.Abstract && implementationCallback != null) throw new ArgumentException($"Method kind is '{kind}', an implementation callback must not be specified.");
		if (kind != MethodKind.Abstract && implementationCallback == null) throw new ArgumentNullException(nameof(implementationCallback));
		mImplementationCallback = implementationCallback;
		Initialize(kind, name, returnType, parameterTypes, visibility, additionalMethodAttributes);
	}

	/// <summary>
	/// Performs common initializations during construction.
	/// </summary>
	/// <param name="kind">Kind of method determining whether the method is static, abstract, virtual or an override.</param>
	/// <param name="name">Name of the method (<c>null</c> to create a random name).</param>
	/// <param name="returnType">Return type of the method.</param>
	/// <param name="parameterTypes">Types of the method parameters.</param>
	/// <param name="visibility">Visibility of the method.</param>
	/// <param name="additionalMethodAttributes">Additional method attributes to 'or' with other attributes.</param>
	/// <exception cref="ArgumentNullException"><paramref name="returnType"/> or <paramref name="parameterTypes"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="name"/> is not a valid language independent identifier<br/>
	/// -or-<br/>
	/// <paramref name="parameterTypes"/> contains a null reference.
	/// </exception>
	private void Initialize(
		MethodKind       kind,
		string           name,
		Type             returnType,
		Type[]           parameterTypes,
		Visibility       visibility,
		MethodAttributes additionalMethodAttributes)
	{
		// check parameters
		if (returnType == null) throw new ArgumentNullException(nameof(returnType));
		CodeGenHelpers.EnsureTypeIsTotallyPublic(returnType);
		if (parameterTypes == null) throw new ArgumentNullException(nameof(parameterTypes));
		if (parameterTypes.Any(x => x == null)) throw new ArgumentException("List of parameter types contains a null reference.");
		foreach (Type type in parameterTypes) CodeGenHelpers.EnsureTypeIsTotallyPublic(type);

		// generate random name if name was not specified and check whether the name is a valid identifier
		name = string.IsNullOrWhiteSpace(name) ? "Method_" + Guid.NewGuid().ToString("N") : name;
		EnsureNameIsValidLanguageIndependentIdentifier(name);

		// keep parameter types separately from the method builder as MethodBuilder.GetParameters() does not work as expected
		mParameterTypes = new Type[parameterTypes.Length];
		Array.Copy(parameterTypes, mParameterTypes, parameterTypes.Length);

		// create the method builder
		MethodBuilder = TypeDefinition.TypeBuilder.DefineMethod(
			name,
			visibility.ToMethodAttributes() | kind.ToMethodAttributes() | additionalMethodAttributes,
			kind.ToCallingConvention(false),
			returnType,
			parameterTypes);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMethod"/> class (for overrides).
	/// </summary>
	/// <param name="typeDefinition">The type definition the method belongs to.</param>
	/// <param name="method">Inherited method to override.</param>
	/// <param name="implementation">Implementation strategy that implements the method.</param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="typeDefinition"/>, <paramref name="method"/> or <paramref name="implementation"/> is <c>null</c>.
	/// </exception>
	internal GeneratedMethod(TypeDefinition typeDefinition, IInheritedMethod method, IMethodImplementation implementation) :
		base(typeDefinition)
	{
		mImplementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
		Initialize(method);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMethod"/> class (for overrides).
	/// </summary>
	/// <param name="typeDefinition">The type definition the method belongs to.</param>
	/// <param name="method">Inherited method to override.</param>
	/// <param name="implementationCallback">A callback that implements the method.</param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="typeDefinition"/>, <paramref name="method"/> or <paramref name="implementationCallback"/> is <c>null</c>.
	/// </exception>
	internal GeneratedMethod(TypeDefinition typeDefinition, IInheritedMethod method, MethodImplementationCallback implementationCallback) :
		base(typeDefinition)
	{
		mImplementationCallback = implementationCallback ?? throw new ArgumentNullException(nameof(implementationCallback));
		Initialize(method);
	}

	/// <summary>
	/// Performs common initializations during construction.
	/// </summary>
	/// <param name="method">Inherited method to override.</param>
	/// <exception cref="ArgumentNullException"><paramref name="method"/> is <c>null</c>.</exception>
	private void Initialize(IInheritedMethod method)
	{
		if (method == null) throw new ArgumentNullException(nameof(method));

		// keep parameter types separately from the method builder as MethodBuilder.GetParameters() does not work as expected
		mParameterTypes = method.ParameterTypes.ToArray();

		// create the method builder
		MethodBuilder = TypeDefinition.TypeBuilder.DefineMethod(
			method.Name,
			(method.MethodInfo.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.NewSlot)) | MethodAttributes.ReuseSlot,
			method.MethodInfo.CallingConvention,
			method.ReturnType,
			method.ParameterTypes.ToArray());
	}

	/// <summary>
	/// Gets the name of the method.
	/// </summary>
	public string Name => MethodBuilder.Name;

	/// <summary>
	/// Gets the kind of the method.
	/// </summary>
	public MethodKind Kind => MethodBuilder.ToMethodKind();

	/// <summary>
	/// Gets the return type of the method.
	/// </summary>
	public Type ReturnType => MethodBuilder.ReturnType;

	/// <summary>
	/// Gets the parameter types of the method.
	/// </summary>
	public IEnumerable<Type> ParameterTypes => mParameterTypes;

	/// <summary>
	/// Gets the access modifier of the method.
	/// </summary>
	public Visibility Visibility => MethodBuilder.ToVisibility();

	/// <summary>
	/// Gets the <see cref="System.Reflection.MethodInfo"/> associated with the method.
	/// </summary>
	MethodInfo IMethod.MethodInfo => MethodBuilder;

	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.MethodBuilder"/> associated with the method.
	/// </summary>
	public MethodBuilder MethodBuilder { get; private set; }

	/// <summary>
	/// Adds other fields, events, properties and methods to the definition of the type in creation.
	/// </summary>
	void IGeneratedMethodInternal.DeclareImplementationSpecificMembers()
	{
		mImplementation?.Declare(TypeDefinition, this);
	}

	/// <summary>
	/// Implements the method.
	/// </summary>
	void IGeneratedMethodInternal.Implement()
	{
		mImplementation?.Implement(TypeDefinition, this, MethodBuilder.GetILGenerator());
		mImplementationCallback?.Invoke(this, MethodBuilder.GetILGenerator());
	}
}
