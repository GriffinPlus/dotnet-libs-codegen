///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Interface of method implementation strategies.
/// </summary>
public interface IMethodImplementation
{
	/// <summary>
	/// Adds other fields, events, properties and methods to the definition of the type in creation.
	/// </summary>
	/// <param name="typeDefinition">Definition of the type in creation.</param>
	/// <param name="method">The method to implement.</param>
	void Declare(TypeDefinition typeDefinition, IGeneratedMethod method);

	/// <summary>
	/// Implements the method.
	/// </summary>
	/// <param name="typeDefinition">The type definition.</param>
	/// <param name="method">The method to implement.</param>
	/// <param name="msilGenerator">MSIL generator attached to the method to implement.</param>
	void Implement(
		TypeDefinition   typeDefinition,
		IGeneratedMethod method,
		ILGenerator      msilGenerator);
}
