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

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Internal interface of a generated field.
	/// </summary>
	internal interface IGeneratedFieldInternal : IGeneratedField
	{
		/// <summary>
		/// Adds the method to the type builder.
		/// </summary>
		void AddToTypeBuilder();

		/// <summary>
		/// Adds code to initialize the field with the specified default value (if any).
		/// </summary>
		/// <param name="msil">IL Generator attached to a constructor.</param>
		void ImplementFieldInitialization(ILGenerator msil);
	}
}
