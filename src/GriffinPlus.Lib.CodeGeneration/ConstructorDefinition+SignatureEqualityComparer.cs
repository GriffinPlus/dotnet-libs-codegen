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
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	partial class ConstructorDefinition
	{
		/// <summary>
		/// An equality comparer only taking the signature of the constructor (its parameter types) into account.
		/// It does neither consider the visibility nor the implementation callback.
		/// </summary>
		private class SignatureEqualityComparer : EqualityComparer<ConstructorDefinition>
		{
			/// <summary>
			/// Checks whether the specified constructor definitions equal each other taking the equality
			/// criteria of the equality comparer into account.
			/// </summary>
			/// <param name="x">First constructor definition to compare.</param>
			/// <param name="y">Seconds constructor definition to compare.</param>
			/// <returns>true, if the specified constructor definitions are equal; otherwise false.</returns>
			public override bool Equals(ConstructorDefinition x, ConstructorDefinition y)
			{
				if (x == null && y == null) return true;
				else if (x == null || y == null) return false;
				return x.mParameterTypes.SequenceEqual(y.mParameterTypes);
			}

			/// <summary>
			/// Gets a hash code for the specified constructor definition taking the equality criteria of the
			/// equality comparer into account.
			/// </summary>
			/// <param name="obj">Constructor definition to get the hash code for.</param>
			/// <returns>The requested hash code.</returns>
			public override int GetHashCode(ConstructorDefinition obj)
			{
				int hashCode = 0;
				foreach (var type in obj.mParameterTypes)
				{
					hashCode ^= type.GetHashCode();
				}
				return hashCode;
			}
		}
	}
}
