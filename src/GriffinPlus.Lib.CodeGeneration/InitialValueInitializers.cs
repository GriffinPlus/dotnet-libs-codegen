///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// A method that provides an implementation pushing an initial value provided by an <see cref="IInitialValueProvider"/>
	/// onto the evaluation stack.
	/// </summary>
	/// <param name="provider">The initial value provider.</param>
	/// <param name="msilGenerator">MSIL generator to use.</param>
	delegate void InitialValueInitializer(IInitialValueProvider provider, ILGenerator msilGenerator);

	/// <summary>
	/// A collection of callbacks pushing the value of an initial value provider onto the evaluation stack.
	/// </summary>
	class InitialValueInitializers
	{
		private static readonly Dictionary<Type, InitialValueInitializer> sInitialValueInitializers;

		static InitialValueInitializers()
		{
			sInitialValueInitializers = new Dictionary<Type, InitialValueInitializer>
			{
				{
					typeof(bool),
					(provider, msilGenerator) => { CodeGenHelpers.EmitLoadConstant(msilGenerator, (bool)provider.InitialValue ? 1 : 0); }
				},
				{
					typeof(char),
					(provider, msilGenerator) => { CodeGenHelpers.EmitLoadConstant(msilGenerator, (char)provider.InitialValue); }
				},
				{
					typeof(sbyte),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						if (value is Enum) value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());
						Debug.Assert(value is sbyte);
						CodeGenHelpers.EmitLoadConstant(msilGenerator, (sbyte)value);
					}
				},
				{
					typeof(byte),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						if (value is Enum) value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());
						Debug.Assert(value is byte);
						CodeGenHelpers.EmitLoadConstant(msilGenerator, (byte)value);
					}
				},
				{
					typeof(short),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						if (value is Enum) value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());
						Debug.Assert(value is short);
						CodeGenHelpers.EmitLoadConstant(msilGenerator, (short)value);
					}
				},
				{
					typeof(ushort),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						if (value is Enum) value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());
						Debug.Assert(value is ushort);
						CodeGenHelpers.EmitLoadConstant(msilGenerator, (ushort)value);
					}
				},
				{
					typeof(int),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						if (value is Enum) value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());
						Debug.Assert(value is int);
						CodeGenHelpers.EmitLoadConstant(msilGenerator, (int)value);
					}
				},
				{
					typeof(uint),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						if (value is Enum) value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());
						Debug.Assert(value is uint);
						CodeGenHelpers.EmitLoadConstant(msilGenerator, (uint)value);
					}
				},
				{
					typeof(long),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						if (value is Enum) value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());
						Debug.Assert(value is long);
						CodeGenHelpers.EmitLoadConstant(msilGenerator, (long)value);
					}
				},
				{
					typeof(ulong),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						if (value is Enum) value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());
						Debug.Assert(value is ulong);
						CodeGenHelpers.EmitLoadConstant(msilGenerator, (ulong)value);
					}
				},
				{
					typeof(float),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						Debug.Assert(value is float);
						msilGenerator.Emit(OpCodes.Ldc_R4, (float)value);
					}
				},
				{
					typeof(double),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						Debug.Assert(value is double);
						msilGenerator.Emit(OpCodes.Ldc_R8, (double)value);
					}
				},
				{
					typeof(string),
					(provider, msilGenerator) =>
					{
						object value = provider.InitialValue;
						Debug.Assert(value == null || value is string);
						if (value == null) msilGenerator.Emit(OpCodes.Ldnull);
						else msilGenerator.Emit(OpCodes.Ldstr, (string)value);
					}
				}
			};
		}

		/// <summary>
		/// Tries to gets the initializer for the specified type.
		/// </summary>
		/// <param name="type">Type of the value to get the initializer for.</param>
		/// <param name="initializer">Receives the initializer for the specified type.</param>
		/// <returns><c>true</c> if the specified type is supported; otherwise <c>false</c>.</returns>
		public static bool TryGetInitializer(Type type, out InitialValueInitializer initializer)
		{
			if (type.IsEnum) type = type.GetEnumUnderlyingType();
			return sInitialValueInitializers.TryGetValue(type, out initializer);
		}
	}

}
