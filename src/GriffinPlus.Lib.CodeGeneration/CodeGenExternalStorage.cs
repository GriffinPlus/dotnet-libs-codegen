///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Helper class that stores external objects like initialization callback delegates outside generated types to
/// allow generated code to access it as it is not possible to use existing objects when generating MSIL.
/// </summary>
public static class CodeGenExternalStorage
{
	private static readonly Dictionary<Type, object[]> sObjectsByGeneratedType = [];

	/// <summary>
	/// Associates a set of external objects with the specified generated type.
	/// </summary>
	/// <param name="generatedType">The generated Type.</param>
	/// <param name="objects">Objects to associate with the type.</param>
	public static void Add(Type generatedType, object[] objects)
	{
		lock (sObjectsByGeneratedType)
		{
			sObjectsByGeneratedType.Add(generatedType, objects);
		}
	}

	/// <summary>
	/// Gets the objects associated with the specified generated type.
	/// </summary>
	/// <param name="generatedType">The generated type.</param>
	/// <returns>The objects associated with the specified generated type.</returns>
	public static object[] Get(Type generatedType)
	{
		lock (sObjectsByGeneratedType)
		{
			return sObjectsByGeneratedType[generatedType];
		}
	}
}
