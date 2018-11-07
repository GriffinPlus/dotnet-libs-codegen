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
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// A code generation module that maps the all methods to callbacks
	/// (useful for implementing small tasks and in tests).
	/// </summary>
	public class CallbackCodeGenModule : CodeGenModule
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CallbackCodeGenModule"/> class.
		/// </summary>
		public CallbackCodeGenModule()
		{

		}

		/// <summary>
		/// Gets or sets the delegate to call during the initialization step.
		/// </summary>
		public Action<CallbackCodeGenModule> Initialize { get; set; }

		/// <summary>
		/// Gets or sets the delegate to call during the declaration step.
		/// </summary>
		public Action<CallbackCodeGenModule> Declare { get; set; }

		/// <summary>
		/// Gets or sets the delegate to call during the implementation step.
		/// </summary>
		public Action<CallbackCodeGenModule> Implement { get; set; }

		/// <summary>
		/// Gets or sets the delegate to call during the class constructor implementation step.
		/// </summary>
		public Action<CallbackCodeGenModule, ILGenerator> ImplementClassConstructor { get; set; }

		/// <summary>
		/// Gets or sets the delegate to call during the constructor implementation step.
		/// </summary>
		public Action<CallbackCodeGenModule, ILGenerator, ConstructorDefinition> ImplementConstructor { get; set; }

		/// <summary>
		/// Gets or sets the delegate to call during the cleanup step.
		/// </summary>
		public Action<CallbackCodeGenModule> Cleanup { get; set; }

		/// <summary>
		/// Performs additional module initialization calling the <see cref="Initialize"/> callback.
		/// </summary>
		protected override void OnInitialize()
		{
			Initialize?.Invoke(this);
		}

		/// <summary>
		/// Performs additional module cleanup calling the <see cref="Cleanup"/> callback.
		/// </summary>
		protected override void OnCleanup()
		{
			Cleanup?.Invoke(this);
		}

		/// <summary>
		/// Declares fields, events, properties and methods calling the <see cref="Declare"/> callback.
		/// </summary>
		protected override void OnDeclare()
		{
			Declare?.Invoke(this);
		}

		/// <summary>
		/// Implements declared event raisers, property accessors and methods calling the <see cref="Implement"/> callback.
		/// </summary>
		protected override void OnImplement()
		{
			Implement?.Invoke(this);
		}

		/// <summary>
		/// Adds code to the class constructor of the type in creation calling the <see cref="ImplementClassConstructor"/> callback.
		/// </summary>
		/// <param name="msil">IL Generator attached to the class constructor.</param>
		protected override void OnImplementClassConstructor(ILGenerator msil)
		{
			ImplementClassConstructor?.Invoke(this, msil);
		}

		/// <summary>
		/// Adds code to a constructor of the type in creation calling the <see cref="ImplementConstructor"/> callback.
		/// </summary>
		/// <param name="msil">IL Generator attached to the appropriate constructor.</param>
		/// <param name="definition">Definition of the constructor being implemented.</param>
		protected override void OnImplementConstructor(ILGenerator msil, ConstructorDefinition definition)
		{
			ImplementConstructor?.Invoke(this, msil, definition);
		}

	}
}
