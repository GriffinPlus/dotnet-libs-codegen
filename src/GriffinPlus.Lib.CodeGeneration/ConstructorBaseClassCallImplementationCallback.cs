///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// A callback that implements a call to the base class constructor if the type in creation derives from another type.
/// </summary>
/// <param name="generatedConstructor">Constructor that needs to emit code for calling a constructor of its base class.</param>
/// <param name="msilGenerator">MSIL code generator attached to the constructor.</param>
public delegate void ConstructorBaseClassCallImplementationCallback(IGeneratedConstructor generatedConstructor, ILGenerator msilGenerator);
