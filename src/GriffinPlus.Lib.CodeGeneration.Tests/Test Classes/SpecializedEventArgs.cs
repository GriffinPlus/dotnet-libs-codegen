///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Some specialized event arguments for testing purposes.
/// </summary>
public class SpecializedEventArgs : EventArgs
{
	public new static readonly SpecializedEventArgs Empty = new();
}
