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
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Helper functions providing support for various tasks within the code generation subsystem.
	/// </summary>
	public static class CodeGenHelpers
	{
		/// <summary>
		/// Extension method providing conversion of a <see cref="Visibility"/> value to a <see cref="FieldAttributes"/> value.
		/// </summary>
		/// <param name="modifier">Access modifier to convert.</param>
		/// <returns>A <see cref="FieldAttributes"/> value corresponding to the specified access modifier.</returns>
		public static FieldAttributes ToFieldAttributes(this Visibility modifier)
		{
			switch (modifier)
			{
				case Visibility.Public: return FieldAttributes.Public;
				case Visibility.Protected: return FieldAttributes.Family;
				case Visibility.ProtectedInternal: return FieldAttributes.FamORAssem;
				case Visibility.Internal: return FieldAttributes.Assembly;
				case Visibility.Private: return FieldAttributes.Private;
				default: throw new NotSupportedException("The specified access modifier is not supported.");
			}
		}

		/// <summary>
		/// Extension method providing conversion of a <see cref="Visibility"/> value to a <see cref="MethodAttributes"/> value.
		/// </summary>
		/// <param name="modifier">Access modifier to convert.</param>
		/// <returns>A <see cref="MethodAttributes"/> value corresponding to the specified access modifier.</returns>
		public static MethodAttributes ToMethodAttributes(this Visibility modifier)
		{
			switch (modifier)
			{
				case Visibility.Public: return MethodAttributes.Public;
				case Visibility.Protected: return MethodAttributes.Family;
				case Visibility.ProtectedInternal: return MethodAttributes.FamORAssem;
				case Visibility.Internal: return MethodAttributes.Assembly;
				case Visibility.Private: return MethodAttributes.Private;
				default: throw new NotSupportedException("The specified access modifier is not supported.");
			}
		}

		/// <summary>
		/// Gets the visibility of the specified method.
		/// </summary>
		/// <param name="method">Method to get the visibility from.</param>
		/// <returns>Visibility of the specified method.</returns>
		public static Visibility ToVisibility(this MethodBase method)
		{
			if (method.IsPublic) return Visibility.Public;
			if (method.IsFamily) return Visibility.Protected;
			if (method.IsFamilyOrAssembly) return Visibility.ProtectedInternal;
			if (method.IsAssembly) return Visibility.Internal;
			if (method.IsPrivate) return Visibility.Private;
			throw new NotSupportedException("The visibility of the method is not supported.");
		}

		/// <summary>
		/// Gets the visibility of the specified field.
		/// </summary>
		/// <param name="field">Field to get the visibility from.</param>
		/// <returns>Visibility of the specified field.</returns>
		public static Visibility ToVisibility(this FieldInfo field)
		{
			if (field.IsPublic) return Visibility.Public;
			if (field.IsFamily) return Visibility.Protected;
			if (field.IsFamilyOrAssembly) return Visibility.ProtectedInternal;
			if (field.IsAssembly) return Visibility.Internal;
			if (field.IsPrivate) return Visibility.Private;
			throw new NotSupportedException("The visibility of the field is not supported.");
		}

		/// <summary>
		/// Gets the event kind of the specified method (must be a event add/remove method).
		/// </summary>
		/// <param name="accessor">Event accessor method to get the event kind from.</param>
		/// <returns>The kind of event the specified accessor belongs to.</returns>
		public static EventKind ToEventKind(this MethodInfo accessor)
		{
			if (accessor.IsStatic) return EventKind.Static;
			if (accessor.IsAbstract) return EventKind.Abstract;

			MethodInfo baseDefinition = accessor.GetBaseDefinition();
			if (baseDefinition != accessor)
			{
				Debug.Assert(accessor.IsVirtual);
				return EventKind.Override;
			}
			else
			{
				if (accessor.IsVirtual) return EventKind.Virtual;
				else return EventKind.Normal;
			}
		}

		/// <summary>
		/// Gets the property kind of the specified method (must be a property getter/setter method).
		/// </summary>
		/// <param name="accessor">Property accessor method to get the property kind from.</param>
		/// <returns>The kind of property the specified accessor belongs to.</returns>
		public static PropertyKind ToPropertyKind(this MethodInfo accessor)
		{
			if (accessor.IsStatic) return PropertyKind.Static;
			if (accessor.IsAbstract) return PropertyKind.Abstract;

			MethodInfo baseDefinition = accessor.GetBaseDefinition();
			if (baseDefinition != accessor)
			{
				Debug.Assert(accessor.IsVirtual);
				return PropertyKind.Override;
			}
			else
			{
				if (accessor.IsVirtual) return PropertyKind.Virtual;
				else return PropertyKind.Normal;
			}
		}

		/// <summary>
		/// Gets the method kind of a property's accessor method.
		/// </summary>
		/// <param name="kind">Kind of the property.</param>
		/// <returns>Kind of the property's accessor methods.</returns>
		public static MethodKind ToMethodKind(this PropertyKind kind)
		{
			switch (kind)
			{
				case PropertyKind.Static: return MethodKind.Static;
				case PropertyKind.Normal: return MethodKind.Normal;
				case PropertyKind.Virtual: return MethodKind.Virtual;
				case PropertyKind.Abstract: return MethodKind.Abstract;
				case PropertyKind.Override: return MethodKind.Override;
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Gets the method kind of the specified method.
		/// </summary>
		/// <param name="method">Method to get the method kind from.</param>
		/// <returns>The kind of method the specified method belongs to.</returns>
		public static MethodKind ToMethodKind(this MethodInfo method)
		{
			if (method.IsStatic) return MethodKind.Static;
			if (method.IsAbstract) return MethodKind.Abstract;

			MethodInfo baseDefinition = method.GetBaseDefinition();
			if (baseDefinition != method)
			{
				Debug.Assert(method.IsVirtual);
				return MethodKind.Override;
			}
			else
			{
				if (method.IsVirtual) return MethodKind.Virtual;
				else return MethodKind.Normal;
			}
		}

		/// <summary>
		/// Gets the appropriate method attribute flags representing the specified method kind.
		/// </summary>
		/// <param name="kind">Method kind to get the attribute flags for.</param>
		/// <returns>The method attribute flags representing the specified method kind.</returns>
		public static MethodAttributes ToMethodAttributes(this MethodKind kind)
		{
			switch (kind)
			{
				case MethodKind.Static: return MethodAttributes.Static;
				case MethodKind.Normal: return 0;
				case MethodKind.Abstract: return MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Abstract;
				case MethodKind.Virtual: return MethodAttributes.Virtual | MethodAttributes.NewSlot;
				case MethodKind.Override: return MethodAttributes.Virtual | MethodAttributes.ReuseSlot;
				default: throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Gets the appropriate calling convention for the specified method kind.
		/// </summary>
		/// <param name="kind">Method kind to get the calling convention for.</param>
		/// <param name="isVArgs">true for variable argument lists; otherwise false.</param>
		/// <returns>The calling convention to use for the specified method kind.</returns>
		public static CallingConventions ToCallingConvention(this MethodKind kind, bool isVArgs)
		{
			CallingConventions convention;

			switch (kind)
			{
				case MethodKind.Static:
					convention = CallingConventions.Standard;
					break;
				case MethodKind.Normal:
				case MethodKind.Abstract:
				case MethodKind.Virtual:
				case MethodKind.Override:
					convention = CallingConventions.HasThis;
					break;
				default:
					throw new NotImplementedException();
			}

			if (isVArgs) convention |= CallingConventions.VarArgs;

			return convention;
		}

		/// <summary>
		/// Gets the method kind of an event's accessor method.
		/// </summary>
		/// <param name="kind">Kind of the event.</param>
		/// <returns>Kind of the event's accessor methods.</returns>
		public static MethodKind ToMethodKind(this EventKind kind)
		{
			switch (kind)
			{
				case EventKind.Static: return MethodKind.Static;
				case EventKind.Normal: return MethodKind.Normal;
				case EventKind.Virtual: return MethodKind.Virtual;
				case EventKind.Abstract: return MethodKind.Abstract;
				case EventKind.Override: return MethodKind.Override;
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Checks whether the specified type is public and all nested types are public, too;
		/// otherwise an exception is thrown (=> the created assembly will not be able to access it)
		/// </summary>
		/// <param name="typeToCheck">Type to check.</param>
		/// <exception cref="ArgumentException">The specified type itself or a type that is part of the specified type is not public.</exception>
		public static void CheckTypeIsTotallyPublic(Type typeToCheck)
		{
			CheckTypeIsTotallyPublic(typeToCheck, typeToCheck);
		}

		/// <summary>
		/// Checks whether the specified type is public and all nested types are public, too;
		/// otherwise an exception is thrown (=> the created assembly will not be able to access it)
		/// </summary>
		/// <param name="typeToCheck">Type to check.</param>
		/// <param name="specifiedType">The original type that is the root of the check.</param>
		/// <exception cref="ArgumentException">The specified type itself or a type that is part of the specified type is not public.</exception>
		private static void CheckTypeIsTotallyPublic(Type typeToCheck, Type specifiedType)
		{
			string error;

			// in the initial call, the root type is null
			if (specifiedType == null) specifiedType = typeToCheck;

			// ensure that the declaring type is public as well
			if (typeToCheck.IsNested)
			{
				CheckTypeIsTotallyPublic(typeToCheck.DeclaringType, specifiedType);
			}

			// check whether the type is public
			if (!typeToCheck.IsPublic && !typeToCheck.IsNestedPublic)
			{
				if (typeToCheck == specifiedType) error = string.Format("The specified type ({0}) is not public.", specifiedType.FullName);
				else error = string.Format("The specified type ({0}) depends on a type ({1}) that is not public.", specifiedType.FullName, typeToCheck.FullName);
				throw new ArgumentException(error);
			}

			if (typeof(Delegate).IsAssignableFrom(typeToCheck))
			{
				// a delegate
				// => check return value type and parameter types
				MethodInfo method = typeToCheck.GetMethod("Invoke");
				CheckTypeIsTotallyPublic(method.ReturnType, specifiedType);
				foreach (ParameterInfo parameterInfo in method.GetParameters())
				{
					CheckTypeIsTotallyPublic(parameterInfo.ParameterType, specifiedType);
				}

				// everything is fine...
				return;
			}

			// check base type
			if (typeToCheck.BaseType != null)
			{
				CheckTypeIsTotallyPublic(typeToCheck.BaseType, specifiedType);
			}
		}

		/// <summary>
		/// Emits the appropriate MSIL opcode to load an argument onto the evaluation stack.
		/// </summary>
		/// <param name="msil">IL generator to emit opcodes to.</param>
		/// <param name="argumentIndex">Index of the argument to load.</param>
		public static void EmitLoadArgument(ILGenerator msil, int argumentIndex)
		{
			if (argumentIndex < 0) throw new ArgumentOutOfRangeException(nameof(argumentIndex));
			if (argumentIndex == 0) msil.Emit(OpCodes.Ldarg_0);
			else if (argumentIndex == 1) msil.Emit(OpCodes.Ldarg_1);
			else if (argumentIndex == 2) msil.Emit(OpCodes.Ldarg_2);
			else if (argumentIndex == 3) msil.Emit(OpCodes.Ldarg_3);
			else if (argumentIndex <= 255) msil.Emit(OpCodes.Ldarg_S, argumentIndex);
			else msil.Emit(OpCodes.Ldarg, argumentIndex);
		}

		/// <summary>
		/// Emits the appropriate MSIL opcode to load a 32-bit integer contant onto the evaluation stack.
		/// </summary>
		/// <param name="msil">IL generator to emit opcodes to.</param>
		/// <param name="x">Constant to load.</param>
		public static void EmitLoadConstant(ILGenerator msil, int x)
		{
			switch (x)
			{
				case -1: msil.Emit(OpCodes.Ldc_I4_M1); break;
				case 0: msil.Emit(OpCodes.Ldc_I4_0); break;
				case 1: msil.Emit(OpCodes.Ldc_I4_1); break;
				case 2: msil.Emit(OpCodes.Ldc_I4_2); break;
				case 3: msil.Emit(OpCodes.Ldc_I4_3); break;
				case 4: msil.Emit(OpCodes.Ldc_I4_4); break;
				case 5: msil.Emit(OpCodes.Ldc_I4_5); break;
				case 6: msil.Emit(OpCodes.Ldc_I4_6); break;
				case 7: msil.Emit(OpCodes.Ldc_I4_7); break;
				case 8: msil.Emit(OpCodes.Ldc_I4_8); break;
				default:
					if (x >= -128 && x <= 127) msil.Emit(OpCodes.Ldc_I4_S, x);
					else msil.Emit(OpCodes.Ldc_I4, x);
					break;
			}
		}

		/// <summary>
		/// Emits the appropriate opcode to load the default value of the specified type onto the evaluation stack.
		/// </summary>
		/// <param name="msil">IL generator to emit opcodes to.</param>
		/// <param name="type">Type whose default value is to load.</param>
		/// <param name="box">true to box value types; false to keep value types.</param>
		public static void EmitLoadDefaultValue(ILGenerator msil, Type type, bool box)
		{
			if (type.IsValueType)
			{
				if (type == typeof(SByte) || type == typeof(Byte) || type == typeof(Int16) || type == typeof(UInt16) || type == typeof(Int32) || type == typeof(UInt32))
				{
					msil.Emit(OpCodes.Ldc_I4_0);
				}
				else if (type == typeof(Int64) || type == typeof(UInt64))
				{
					msil.Emit(OpCodes.Ldc_I8, 0L);
				}
				else if (type == typeof(Single))
				{
					msil.Emit(OpCodes.Ldc_R4, 0.0f);
				}
				else if (type == typeof(Double))
				{
					msil.Emit(OpCodes.Ldc_R8, 0.0);
				}
				else
				{
					Debug.Assert(!type.IsPrimitive, "Primitive types should have their own special implementation.");
					LocalBuilder builder = msil.DeclareLocal(type);
					msil.Emit(OpCodes.Ldloca, builder);
					msil.Emit(OpCodes.Initobj, type);
					msil.Emit(OpCodes.Ldloc_0);
				}

				if (box) msil.Emit(OpCodes.Box, type);
			}
			else
			{
				msil.Emit(OpCodes.Ldnull);
			}
		}
	}
}
