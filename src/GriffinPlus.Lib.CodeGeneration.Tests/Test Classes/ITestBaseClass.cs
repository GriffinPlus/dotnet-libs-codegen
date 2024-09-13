﻿///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Interface for base classes of for dynamically created test classes.
/// </summary>
public interface ITestBaseClass
{
	/// <summary>
	/// The argument passed to <see cref="TestBaseClass(int)"/> or <see cref="TestBaseClass(string)"/>,
	/// respectively <see cref="TestBaseClass_Abstract(int)"/> or <see cref="TestBaseClass_Abstract(string)"/>.
	/// </summary>
	public object ConstructorArgument { get; }
};