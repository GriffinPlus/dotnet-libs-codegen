///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// A method that provides an implementation pushing an object onto the evaluation stack to use as the initial value
	/// for the generated dependency property.
	/// </summary>
	/// <param name="dependencyProperty">The dependency property to initialize.</param>
	/// <param name="msilGenerator">MSIL generator to use.</param>
	public delegate void DependencyPropertyInitializer(IGeneratedDependencyProperty dependencyProperty, ILGenerator msilGenerator);

}

#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
// Dependency properties are not supported on .NET Standard and .NET5/6/7/8 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif
