﻿///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Common tests around the <see cref="ClassDefinition"/> class.
/// The type to create derives from a base class that derives from another class on its own and hides its members.
/// </summary>
public sealed class ClassDefinitionTests_Common_WithBaseClassHidingInheritedMembers : ClassDefinitionTests_Common
{
	/// <summary>
	/// Creates a new type definition instance to test.
	/// </summary>
	/// <param name="name">Name of the type to create (<c>null</c> to create a random name).</param>
	/// <returns>The created type definition instance.</returns>
	public override ClassDefinition CreateTypeDefinition(string name = null)
	{
		return new ClassDefinition(typeof(TestBaseClass_BaseClassMembersHidden), name);
	}
}
