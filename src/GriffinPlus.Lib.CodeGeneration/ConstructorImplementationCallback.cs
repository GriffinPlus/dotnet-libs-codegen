///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// A callback that contributes to the constructor of a generated type.
/// </summary>
/// <param name="constructor">The constructor to implement.</param>
/// <param name="msilGenerator">MSIL code generator attached to the constructor.</param>
public delegate void ConstructorImplementationCallback(IGeneratedConstructor constructor, ILGenerator msilGenerator);
