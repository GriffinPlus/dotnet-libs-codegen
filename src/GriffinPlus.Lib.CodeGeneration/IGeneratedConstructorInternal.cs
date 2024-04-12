///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Internal interface of a generated constructor.
/// </summary>
interface IGeneratedConstructorInternal : IGeneratedConstructor
{
	/// <summary>
	/// Gets the callback that adds code to call a constructor of the base class of the type in creation
	/// (<c>null</c> to call the parameterless constructor of the base class).
	/// </summary>
	ConstructorBaseClassCallImplementationCallback BaseClassConstructorCallImplementationCallback { get; }

	/// <summary>
	/// Gets the callback that emits additional code to execute after the constructor of the base class
	/// and field initializers have run; <c>null</c> to skip emitting additional code.
	/// </summary>
	ConstructorImplementationCallback ImplementConstructorCallback { get; }
}
