///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Interface of generated and inherited constructors.
/// </summary>
public interface IConstructor
{
	/// <summary>
	/// Gets the <see cref="System.Reflection.ConstructorInfo"/> associated with the constructor.
	/// </summary>
	ConstructorInfo ConstructorInfo { get; }

	/// <summary>
	/// Gets the visibility of the constructor.
	/// </summary>
	Visibility Visibility { get; }

	/// <summary>
	/// Gets the types of the constructor parameters.
	/// </summary>
	IEnumerable<Type> ParameterTypes { get; }
}
