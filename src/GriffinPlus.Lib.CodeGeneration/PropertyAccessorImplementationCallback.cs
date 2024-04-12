///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// A callback that implements the 'get'/'set' accessor method of the specified property.
/// </summary>
/// <param name="property">The property the accessor method to implement belongs to.</param>
/// <param name="msilGenerator">MSIL generator attached to the 'get'/'set' accessor method to implement.</param>
public delegate void PropertyAccessorImplementationCallback(IGeneratedProperty property, ILGenerator msilGenerator);
