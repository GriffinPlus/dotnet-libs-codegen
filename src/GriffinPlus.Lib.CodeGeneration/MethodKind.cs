///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Method kind specifying whether a method is static, virtual, abstract or an override of a virtual/abstract method.
/// </summary>
public enum MethodKind
{
	/// <summary>
	/// The method is static.
	/// </summary>
	Static,

	/// <summary>
	/// The method is a normal instance method, i.e. not abstract, virtual or an override.
	/// </summary>
	Normal,

	/// <summary>
	/// The method is virtual.
	/// </summary>
	Virtual,

	/// <summary>
	/// The method is abstract.
	/// </summary>
	Abstract,

	/// <summary>
	/// The method  is an override of an inherited virtual or abstract method.
	/// </summary>
	Override
}
