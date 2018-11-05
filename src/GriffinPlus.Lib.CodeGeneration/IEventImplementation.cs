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
	/// Interface a class implementing an event implementation strategy must implement.
	/// </summary>
	public interface IEventImplementation
	{
		/// <summary>
		/// Reviews the default declaration of the event and adds additional type declarations, if necessary.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		/// <param name="e">The event to review.</param>
		void Declare(CodeGenEngine engine, GeneratedEvent e);

		/// <summary>
		/// Implements the event.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		/// <param name="e">The event to implement.</param>
		void Implement(CodeGenEngine engine, GeneratedEvent e);

		/// <summary>
		/// Is called when the event the implementation strategy is attached to is removed from the type in creation.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		void OnRemoving(CodeGenEngine engine);
	}
}
