///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Interface of inherited and generated properties.
/// </summary>
public interface IProperty
{
	/// <summary>
	/// Gets the name of the property.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the type of the property.
	/// </summary>
	Type PropertyType { get; }

	/// <summary>
	/// Gets the property kind indicating whether the property is static, virtual, abstract or an override of an abstract/virtual property.
	/// </summary>
	PropertyKind Kind { get; }

	/// <summary>
	/// Gets the 'get' accessor method
	/// (<c>null</c>, if the property does not have a 'get' accessor).
	/// </summary>
	IMethod GetAccessor { get; }

	/// <summary>
	/// Gets the 'set' accessor method
	/// (<c>null</c>, if the property does not have a 'set' accessor).
	/// </summary>
	IMethod SetAccessor { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.PropertyInfo"/> associated with the property.
	/// </summary>
	PropertyInfo PropertyInfo { get; }
}
