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
	/// Interface of the most basic code generation module.
	/// </summary>
	public interface ICodeGenModule
	{
		/// <summary>
		/// Gets the modules the current module depends on.
		/// </summary>
		ICodeGenModule[] Dependencies {
			get;
		}

		/// <summary>
		/// Initializes module specific data.
		/// </summary>
		/// <param name="engine">The code generation engine creating the desired type.</param>
		void Initialize(CodeGenEngine engine);

		/// <summary>
		/// Cleans up the module after processing has finished.
		/// </summary>
		void Cleanup();

		/// <summary>
		/// Declares fields, events, properties and methods.
		/// </summary>
		void Declare();

		/// <summary>
		/// Implements declared event raisers, property accessors and methods.
		/// </summary>
		void Implement();

		/// <summary>
		/// Adds code to the class constructor of the created type
		/// (must _not_ emit a 'ret' instruction to return from the constructor).
		/// </summary>
		/// <param name="msil">IL Generator attached to the class constructor.</param>
		void ImplementClassConstruction(ILGenerator msil);

		/// <summary>
		/// Adds code to the constructor of the created type
		/// (must _not_ emit a 'ret' instruction to return from the constructor).
		/// </summary>
		/// <param name="msil">IL Generator attached to the appropriate constructor.</param>
		/// <param name="definition">Definition of the constructor being implemented.</param>
		void ImplementConstruction(ILGenerator msil, ConstructorDefinition definition);
	}
}
