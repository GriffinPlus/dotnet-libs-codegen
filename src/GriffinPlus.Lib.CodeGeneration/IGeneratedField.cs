///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Untyped interface of a generated field.
/// </summary>
public interface IGeneratedField : IField, IGeneratedMember, IInitialValueProvider
{
	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.FieldBuilder"/> associated with the field.
	/// </summary>
	FieldBuilder FieldBuilder { get; }
}
