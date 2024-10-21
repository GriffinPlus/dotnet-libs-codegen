///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// A method implementation strategy that simply returns the last argument.<br/>
/// The signature of the method must be:<br/>
/// - <c>T Method(T arg1) - arg1</c> is returned.<br/>
/// - <c>T Method(T arg2, T arg2)</c> - arg2 is returned.<br/>
/// - <c>T Method(T arg1,...,T argn)</c> - argn is returned.<br/>
/// If the method does not have any parameters (<c>T Method()</c>), the default value of <c>T</c> is returned.
/// </summary>
public class TestMethodImplementation : IMethodImplementation
{
	public void Declare(TypeDefinition typeDefinition, IGeneratedMethod method) { }

	public void Implement(
		TypeDefinition   typeDefinition,
		IGeneratedMethod method,
		ILGenerator      msilGenerator)
	{
		Callback(method, msilGenerator);
	}

	public static void Callback(IGeneratedMethod method, ILGenerator msilGenerator)
	{
		Type type = method.ReturnType;
		Debug.Assert(method.ParameterTypes.All(x => x == type));

		if (method.ParameterTypes.Count > 0)
		{
			msilGenerator.Emit(
				OpCodes.Ldarg,
				method.ParameterTypes.Count + (method.Kind == MethodKind.Static ? 0 : 1) - 1);
		}
		else
		{
			if (type.IsValueType)
			{
				LocalBuilder builder = msilGenerator.DeclareLocal(type);
				msilGenerator.Emit(OpCodes.Ldloca, builder);
				msilGenerator.Emit(OpCodes.Initobj, type);
				msilGenerator.Emit(OpCodes.Ldloc, builder);
			}
			else
			{
				msilGenerator.Emit(OpCodes.Ldnull);
			}
		}

		msilGenerator.Emit(OpCodes.Ret);
	}
}
