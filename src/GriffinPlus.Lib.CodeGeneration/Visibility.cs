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
	/// Access modifiers for fields, property accessors and methods.
	/// </summary>
	public enum Visibility
	{
		/// <summary>
		/// The visibility is not specified, i.e. the associated member is not generated.
		/// </summary>
		NotSpecified,

		/// <summary>
		/// The associated member is 'public', i.e. it can be access from anywhere.
		/// </summary>
		Public,

		/// <summary>
		/// The associated member is 'protected', i.e. it can be accessed from within the declaring type or types deriving from it.
		/// </summary>
		Protected,

		/// <summary>
		/// The associated member is 'protected internal', i.e. it can be accessed from within the declaring type, types deriving from it or from within the same assembly.
		/// </summary>
		ProtectedInternal,

		/// <summary>
		/// The associated member is 'internal', i.e. it can be accessed from within the same assembly only.
		/// </summary>
		Internal,

		/// <summary>
		/// The associated member is 'private', i.e. it can be accessed from within the declaring type only.
		/// </summary>
		Private,
	}
}
