///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.CodeDom.Compiler;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Base class for fields, events, properties and methods in the code generation engine.
/// </summary>
public class Member
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Member"/> class.
	/// </summary>
	/// <param name="typeDefinition">The type definition the member belongs to.</param>
	internal Member(TypeDefinition typeDefinition)
	{
		TypeDefinition = typeDefinition ?? throw new ArgumentNullException(nameof(typeDefinition));
	}

	/// <summary>
	/// Gets the type definition the member belongs to.
	/// </summary>
	public TypeDefinition TypeDefinition { get; }

	#region Freezing

	/// <summary>
	/// Gets a value indicating whether the member is frozen.
	/// </summary>
	protected bool IsFrozen { get; private set; }

	/// <summary>
	/// Freezes the member protecting it against further changes.
	/// </summary>
	protected void Freeze()
	{
		IsFrozen = true;
	}

	/// <summary>
	/// Checks whether the member is frozen and throws an exception if it is.
	/// </summary>
	protected void EnsureNotFrozen()
	{
		if (IsFrozen) throw new InvalidOperationException("The member is frozen and must not be changed any further.");
	}

	#endregion

	/// <summary>
	/// Checks whether the specified name is a valid language independent identifier and throws an exception,
	/// if it violates the naming constraints.
	/// </summary>
	/// <param name="name">Identifier to check.</param>
	/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
	internal static void EnsureNameIsValidLanguageIndependentIdentifier(string name)
	{
		if (name == null) throw new ArgumentNullException(nameof(name), "The identifier must not be a null reference.");
		if (!CodeGenerator.IsValidLanguageIndependentIdentifier(name))
			throw new ArgumentException($"'{name}' is not a valid identifier.");
	}
}
