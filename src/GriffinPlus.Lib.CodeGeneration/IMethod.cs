///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Interface of inherited and generated methods.
/// </summary>
public interface IMethod
{
	/// <summary>
	/// Gets the name of the method.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the kind of the method.
	/// </summary>
	MethodKind Kind { get; }

	/// <summary>
	/// Gets the return type of the method.
	/// </summary>
	Type ReturnType { get; }

	/// <summary>
	/// Gets the parameter types of the method.
	/// </summary>
	IReadOnlyList<Type> ParameterTypes { get; }

	/// <summary>
	/// Gets the access modifier of the method.
	/// </summary>
	Visibility Visibility { get; }

	/// <summary>
	/// Gets the attributes of the method.
	/// </summary>
	MethodAttributes Attributes { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.MethodInfo"/> associated with the method.
	/// </summary>
	MethodInfo MethodInfo { get; }
}
