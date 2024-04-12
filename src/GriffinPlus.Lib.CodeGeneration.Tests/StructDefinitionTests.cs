///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Common tests around the <see cref="StructDefinition"/> class.
/// </summary>
public class StructDefinitionTests : TypeDefinitionTests_Common<StructDefinition>
{
	/// <summary>
	/// Creates a new type definition instance to test.
	/// </summary>
	/// <param name="name">Name of the type to create (<c>null</c> to create a random name).</param>
	/// <returns>The created type definition instance.</returns>
	public override StructDefinition CreateTypeDefinition(string name = null)
	{
		return new StructDefinition(name);
	}
}
