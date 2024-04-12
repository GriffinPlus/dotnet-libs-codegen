///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Interface of inherited and generated fields.
/// </summary>
public interface IField
{
	/// <summary>
	/// Gets the name of the field.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the type of the field.
	/// </summary>
	Type FieldType { get; }

	/// <summary>
	/// Gets a value indicating whether the field is class variable (<c>true</c>) or an instance variable (<c>false</c>).
	/// </summary>
	bool IsStatic { get; }

	/// <summary>
	/// Gets the access modifier of the field.
	/// </summary>
	Visibility Visibility { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.FieldInfo"/> associated with the field.
	/// </summary>
	FieldInfo FieldInfo { get; }
}
