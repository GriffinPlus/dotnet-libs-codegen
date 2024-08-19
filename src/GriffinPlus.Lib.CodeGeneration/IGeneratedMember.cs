///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Common interface of generated members.
/// </summary>
public interface IGeneratedMember
{
	/// <summary>
	/// Gets the type definition that member belongs to.
	/// </summary>
	TypeDefinition TypeDefinition { get; }
}
