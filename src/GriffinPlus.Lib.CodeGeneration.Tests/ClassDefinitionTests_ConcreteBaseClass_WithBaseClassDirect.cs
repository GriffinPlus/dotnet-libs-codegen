﻿///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Common tests around the <see cref="ClassDefinition"/> class.
/// The type to create derives from a base class that does not derive from any other class.
/// </summary>
public class ClassDefinitionTests_ConcreteBaseClass_WithBaseClassDirect : ClassDefinitionTests_ConcreteBaseClass_WithBaseClass<TestBaseClass_Concrete>
{
	/// <summary>
	/// Creates a new type definition instance to test.
	/// </summary>
	/// <param name="name">Name of the type to create (<c>null</c> to create a random name).</param>
	/// <param name="attributes">
	/// Attributes of the type to create
	/// (only flags that are part of <see cref="ClassAttributes"/> are valid).
	/// </param>
	/// <returns>The created type definition instance.</returns>
	public override ClassDefinition CreateTypeDefinition(string name = null, TypeAttributes attributes = 0)
	{
		return new ClassDefinition(typeof(TestBaseClass_Concrete), name, (ClassAttributes)attributes);
	}
}
