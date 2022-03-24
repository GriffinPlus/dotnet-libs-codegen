///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// A callback that implements the method.
	/// </summary>
	/// <param name="method">The generated method to implement.</param>
	/// <param name="msilGenerator">MSIL generator attached to method to implement.</param>
	public delegate void MethodImplementationCallback(IGeneratedMethod method, ILGenerator msilGenerator);

}
