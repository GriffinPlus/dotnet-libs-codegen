///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// An inherited method.
/// </summary>
[DebuggerDisplay("Declaring Type = {MethodInfo.DeclaringType.FullName}, Method Info = {MethodInfo}")]
class InheritedMethod : Member, IInheritedMethod
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InheritedMethod"/> class.
	/// </summary>
	/// <param name="typeDefinition">The type definition the member belongs to.</param>
	/// <param name="method">Method the type in creation has inherited.</param>
	internal InheritedMethod(TypeDefinition typeDefinition, MethodInfo method) : base(typeDefinition)
	{
		MethodInfo = method;
		Visibility = MethodInfo.ToVisibility();
		ParameterTypes = MethodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
	}

	/// <summary>
	/// Gets the name of the method.
	/// </summary>
	public string Name => MethodInfo.Name;

	/// <summary>
	/// Gets the kind of the method.
	/// </summary>
	public MethodKind Kind => MethodInfo.ToMethodKind();

	/// <summary>
	/// Gets the return type of the method.
	/// </summary>
	public Type ReturnType => MethodInfo.ReturnType;

	/// <summary>
	/// Gets the parameter types of the method.
	/// </summary>
	public IEnumerable<Type> ParameterTypes { get; }

	/// <summary>
	/// Gets the access modifier of the method.
	/// </summary>
	public Visibility Visibility { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.MethodInfo"/> associated with the method.
	/// </summary>
	public MethodInfo MethodInfo { get; }

	/// <summary>
	/// Adds an override for the current method.
	/// </summary>
	/// <param name="implementation">Implementation strategy that implements the method.</param>
	/// <returns>The generated method.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	public IGeneratedMethod Override<T>(IMethodImplementation implementation)
	{
		return TypeDefinition.AddMethodOverride(this, implementation);
	}

	/// <summary>
	/// Adds an override for the current method.
	/// </summary>
	/// <param name="implementationCallback">A callback that implements the method.</param>
	/// <returns>The generated method.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementationCallback"/> is <c>null</c>.</exception>
	public IGeneratedMethod Override<T>(MethodImplementationCallback implementationCallback)
	{
		return TypeDefinition.AddMethodOverride(this, implementationCallback);
	}
}
