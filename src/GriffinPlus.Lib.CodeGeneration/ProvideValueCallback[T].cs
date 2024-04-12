///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// A factory callback that provides a value.
/// </summary>
/// <typeparam name="T">Type of the value to provide.</typeparam>
/// <returns>The provided value.</returns>
public delegate T ProvideValueCallback<out T>();
