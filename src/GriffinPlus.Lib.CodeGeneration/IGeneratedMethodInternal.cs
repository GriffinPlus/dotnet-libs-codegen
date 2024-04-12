///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Internal interface of a generated method.
/// </summary>
interface IGeneratedMethodInternal : IGeneratedMethod
{
	/// <summary>
	/// Adds other fields, events, properties and methods to the definition of the type in creation.
	/// </summary>
	void DeclareImplementationSpecificMembers();

	/// <summary>
	/// Implements the method.
	/// </summary>
	void Implement();
}
