﻿///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
	/// Interface of the base class for fields, events, properties and methods in the code generation engine.
	/// </summary>
	public interface IMember
	{
		/// <summary>
		/// Gets a value indicating whether the member is frozen.
		/// </summary>
		bool IsFrozen
		{
			get;
		}

		/// <summary>
		/// Freezes the member, so it cannot be modified lateron.
		/// </summary>
		void Freeze();

	}
}
