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
	/// Interface for inherited and generated dependency properties.
	/// </summary>
	public interface IDependencyProperty
	{
		/// <summary>
		/// Gets the type of the property.
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a value indicating whether the dependency property is read-only (true) or read-write (false).
		/// </summary>
		bool IsReadOnly { get; }

		/// <summary>
		/// Gets the accessor property associated with the dependency property.
		/// </summary>
		IProperty AccessorProperty { get; }
	}
}
