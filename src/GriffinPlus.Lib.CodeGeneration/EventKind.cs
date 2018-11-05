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

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Event kind specifying whether an event (respectively its add/remove accessor) is static, normal, virtual, abstract or an override.
	/// </summary>
	public enum EventKind
	{
		/// <summary>
		/// The event is static.
		/// </summary>
		Static,

		/// <summary>
		/// The event is a normal member event, i.e. not abstract, virtual or an override.
		/// </summary>
		Normal,

		/// <summary>
		/// The event is virtual.
		/// </summary>
		Virtual,

		/// <summary>
		/// The event is abstract.
		/// </summary>
		Abstract,

		/// <summary>
		/// The event  is an override of an inherited virtual or abstract event.
		/// </summary>
		Override
	}
}
