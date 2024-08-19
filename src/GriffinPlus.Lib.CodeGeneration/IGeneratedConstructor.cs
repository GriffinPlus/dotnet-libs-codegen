///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Interface of generated constructors.
/// </summary>
public interface IGeneratedConstructor : IConstructor, IGeneratedMember
{
	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.ConstructorBuilder"/> associated with the constructor.
	/// </summary>
	ConstructorBuilder ConstructorBuilder { get; }
}
