///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// A method that provides an implementation pushing an object onto the evaluation stack to use as the initial value
/// for the generated field.
/// </summary>
/// <param name="field">The field to initialize.</param>
/// <param name="msilGenerator">MSIL generator to use.</param>
public delegate void FieldInitializer(IGeneratedField field, ILGenerator msilGenerator);
