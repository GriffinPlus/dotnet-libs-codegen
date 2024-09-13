///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Dummy type for parameter for constructors that are public
/// (allows to see the type of the constructor in the debugger at the first glance).
/// </summary>
public enum ParameterType_Public
{
	Value = 1
}

/// <summary>
/// Dummy type for parameter for constructors that are protected internal
/// (allows to see the type of the constructor in the debugger at the first glance).
/// </summary>
public enum ParameterType_ProtectedInternal
{
	Value = 2
}

/// <summary>
/// Dummy type for parameter for constructors that are protected
/// (allows to see the type of the constructor in the debugger at the first glance).
/// </summary>
public enum ParameterType_Protected
{
	Value = 3
}

/// <summary>
/// Dummy type for parameter for constructors that are internal
/// (allows to see the type of the constructor in the debugger at the first glance).
/// </summary>
public enum ParameterType_Internal
{
	Value = 4
}

/// <summary>
/// Dummy type for parameter for constructors that are private
/// (allows to see the type of the constructor in the debugger at the first glance).
/// </summary>
public enum ParameterType_Private
{
	Value = 5
}
