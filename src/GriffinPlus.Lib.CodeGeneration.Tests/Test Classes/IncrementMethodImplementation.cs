///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// A method implementation strategy that increments the argument of the method by 1.<br/>
/// The signature of the method must be: int Method(int)
/// </summary>
public class IncrementMethodImplementation : IMethodImplementation
{
	public void Declare(TypeDefinition typeDefinition, IGeneratedMethod method) { }

	public void Implement(TypeDefinition typeDefinition, IGeneratedMethod method, ILGenerator msilGenerator)
	{
		Debug.Assert(method.ParameterTypes.Count == 1);
		Debug.Assert(method.ParameterTypes[0] == method.ReturnType);
		Debug.Assert(method.ParameterTypes[0] == typeof(int));
		msilGenerator.Emit(method.Kind == MethodKind.Static ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
		msilGenerator.Emit(OpCodes.Ldc_I4, 1);
		msilGenerator.Emit(OpCodes.Add);
		msilGenerator.Emit(OpCodes.Ret);
	}
}
