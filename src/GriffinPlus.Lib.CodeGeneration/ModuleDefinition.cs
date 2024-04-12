///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Definition of a module to dynamically create types within.
/// </summary>
public sealed class ModuleDefinition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ModuleDefinition"/> class.
	/// </summary>
	/// <param name="assemblyName">
	/// Name of the dynamic assembly the named transient dynamic module for types to create resides in (must be a valid assembly name).
	/// May be <c>null</c> to create a random name for the dynamic assembly (default).
	/// </param>
	/// <param name="moduleName">
	/// Name of the transient dynamic module created within the dynamic assembly.
	/// Types that are defined using this instance of the module definition reside in this module.
	/// May be <c>null</c> to create a random name for the dynamic module (default).
	/// </param>
	public ModuleDefinition(string assemblyName = null, string moduleName = null)
	{
		assemblyName ??= Guid.NewGuid().ToString("D");
		moduleName ??= Guid.NewGuid().ToString("D");

		AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
		ModuleBuilder = AssemblyBuilder.DefineDynamicModule(moduleName);
	}

	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.AssemblyBuilder"/> the named transient dynamic module was created within.
	/// </summary>
	public AssemblyBuilder AssemblyBuilder { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.ModuleBuilder"/> associated with the module definition dynamically created types reside in.
	/// </summary>
	public ModuleBuilder ModuleBuilder { get; }

	/// <summary>
	/// Creates a new definition for a class not deriving from a base type.
	/// </summary>
	/// <param name="name">Name of the class to create (<c>null</c> to create a random name).</param>
	public ClassDefinition CreateClassDefinition(string name = null)
	{
		return new ClassDefinition(this, name);
	}

	/// <summary>
	/// Creates a new definition for a class deriving from the specified base class.
	/// </summary>
	/// <param name="baseClass">Base class to derive the created class from.</param>
	/// <param name="name">Name of the class to create (<c>null</c> to keep the name of the base class).</param>
	/// <exception cref="ArgumentNullException"><paramref name="baseClass"/> is <c>null</c>.</exception>
	public ClassDefinition CreateClassDefinition(Type baseClass, string name = null)
	{
		return new ClassDefinition(this, baseClass, name);
	}

	/// <summary>
	/// Creates a new definition for a struct.
	/// </summary>
	/// <param name="name">Name of the struct to create (<c>null</c> to create a random name).</param>
	public StructDefinition CreateStructDefinition(string name = null)
	{
		return new StructDefinition(this, name);
	}
}
