///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Attributes of a class.<br/>
/// This is a subset of the <see cref="TypeAttributes"/> flags.
/// </summary>
public enum ClassAttributes
{
	/// <summary>
	/// No class attributes.
	/// </summary>
	None = 0,

	/// <summary>
	/// The class is abstract.
	/// </summary>
	Abstract = TypeAttributes.Abstract,

	/// <summary>
	/// The class is sealed.
	/// </summary>
	Sealed = TypeAttributes.Sealed
}
