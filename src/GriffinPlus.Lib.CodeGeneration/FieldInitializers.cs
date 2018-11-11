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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// A method that provides an implementation returning an object on the evaluation stack to store in a generated field.
	/// </summary>
	/// <param name="msil">MSIL generator to use.</param>
	/// <param name="field">Field to implement the method for.</param>
	public delegate void FieldInitializer(ILGenerator msil, IGeneratedField field);

	/// <summary>
	/// A collection of field initializers for primitive types.
	/// </summary>
	internal class FieldInitializers
	{
		public readonly static Dictionary<Type,FieldInitializer> Default;

		/// <summary>
		/// Initializes the <see cref="GeneratedField{T}"/> class.
		/// </summary>
		static FieldInitializers()
		{
			Default = new Dictionary<Type, FieldInitializer>();

			Default.Add(typeof(SByte),   (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(SByte)field.DefaultValue);  });
			Default.Add(typeof(Byte),    (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(Byte)field.DefaultValue);   });
			Default.Add(typeof(Int16),   (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(Int16)field.DefaultValue);  });
			Default.Add(typeof(UInt16),  (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(UInt16)field.DefaultValue); });
			Default.Add(typeof(Int32),   (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)field.DefaultValue);         });
			Default.Add(typeof(UInt32),  (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(UInt32)field.DefaultValue); });
			Default.Add(typeof(Int64),   (msil,field) => { msil.Emit(OpCodes.Ldc_I8, (Int64)field.DefaultValue);         });
			Default.Add(typeof(UInt64),  (msil,field) => { msil.Emit(OpCodes.Ldc_I8, (Int64)(UInt64)field.DefaultValue); });
			Default.Add(typeof(Single),  (msil,field) => { msil.Emit(OpCodes.Ldc_R4, (Single)field.DefaultValue);        });
			Default.Add(typeof(Double),  (msil,field) => { msil.Emit(OpCodes.Ldc_R8, (Double)field.DefaultValue);        });
		}
	}
}
