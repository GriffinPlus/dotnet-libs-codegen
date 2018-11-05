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

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Interface for inherited and generated events.
	/// </summary>
	public interface IEvent
	{
		/// <summary>
		/// Gets the name of the event.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the type of the event.
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// Gets the kind of the event.
		/// </summary>
		EventKind Kind { get; }

		/// <summary>
		/// Gets the access modifier of the event.
		/// </summary>
		Visibility Visibility { get; }

		/// <summary>
		/// Gets the 'add' accessor method.
		/// </summary>
		IMethod AddAccessor { get; }

		/// <summary>
		/// Gets the 'remove' accessor method.
		/// </summary>
		IMethod RemoveAccessor { get; }
	}
}
