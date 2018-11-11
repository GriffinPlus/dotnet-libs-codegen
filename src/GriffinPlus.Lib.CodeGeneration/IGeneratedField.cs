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
	/// A generated field.
	/// </summary>
	public interface IGeneratedField : IField
	{
		#region Properties

		/// <summary>
		/// Gets a default value of the field (if any).
		/// </summary>
		object DefaultValue
		{
			get;
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.Emit.FieldBuilder"/> associated with the field.
		/// </summary>
		FieldBuilder FieldBuilder
		{
			get;
		}

		#endregion

	}
}
