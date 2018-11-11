///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://griffin.plus)
//
// Copyright 2018 Sascha Falk <sascha@falk-online.eu>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed
// on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for
// the specific language governing permissions and limitations under the License.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Helper class that stores external objects like initialization callback delegates outside of the
	/// generated type to allow generated code to access it as it is not possible to use existing objects
	/// when generating MSIL.
	/// </summary>
	public static class CodeGenExternalStorage
	{
		private static Dictionary<Type, object[]> sObjectsByGeneratedType = new Dictionary<Type, object[]>();

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
}
