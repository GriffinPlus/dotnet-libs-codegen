///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Helper functions providing support for various tasks within the code generation subsystem.
	/// </summary>
	public static class CodeGenHelpers
	{
		#region Concerning Fields

		/// <summary>
		/// Gets the <see cref="FieldAttributes"/> corresponding to the visibility.
		/// </summary>
		/// <param name="visibility">Visibility to get the <see cref="FieldAttributes"/> from.</param>
		/// <returns>The corresponding <see cref="FieldAttributes"/>.</returns>
		public static FieldAttributes ToFieldAttributes(this Visibility visibility)
		{
			switch (visibility)
			{
				case Visibility.Public:            return FieldAttributes.Public;
				case Visibility.Protected:         return FieldAttributes.Family;
				case Visibility.ProtectedInternal: return FieldAttributes.FamORAssem;
				case Visibility.Internal:          return FieldAttributes.Assembly;
				case Visibility.Private:           return FieldAttributes.Private;

				default: // should never occur
					throw new NotImplementedException($"The specified visibility ({visibility}) is not supported.");
			}
		}

		/// <summary>
		/// Gets the field's visibility.
		/// </summary>
		/// <param name="field">Field to get the visibility from.</param>
		/// <returns>Visibility of the field.</returns>
		public static Visibility ToVisibility(this FieldInfo field)
		{
			switch (field.Attributes & FieldAttributes.FieldAccessMask)
			{
				case FieldAttributes.Public:
					return Visibility.Public;

				case FieldAttributes.Family:
					return Visibility.Protected;

				case FieldAttributes.FamORAssem:
					return Visibility.ProtectedInternal;

				case FieldAttributes.Assembly:
					return Visibility.Internal;

				case FieldAttributes.Private:
					return Visibility.Private;

				case FieldAttributes.FamANDAssem:
					throw new NotImplementedException($"The access modifier of the field ({field.Attributes & FieldAttributes.FieldAccessMask}) is not supported under C#.");

				default:
					throw new NotImplementedException("The visibility of the field is not supported."); // should never occur
			}
		}

		#endregion

		#region Concerning Events

		/// <summary>
		/// Gets the kind of accessor methods belonging to an event of that kind.
		/// </summary>
		/// <param name="kind">Kind of the event to get the kind of accessor methods from.</param>
		/// <returns>Kind of the event's accessor methods.</returns>
		public static MethodKind ToMethodKind(this EventKind kind)
		{
			switch (kind)
			{
				case EventKind.Static:   return MethodKind.Static;
				case EventKind.Normal:   return MethodKind.Normal;
				case EventKind.Virtual:  return MethodKind.Virtual;
				case EventKind.Abstract: return MethodKind.Abstract;
				case EventKind.Override: return MethodKind.Override;

				default:
					throw new NotImplementedException($"The event kind ({kind}) is not supported.");
			}
		}

		/// <summary>
		/// Gets the event's visibility.
		/// </summary>
		/// <param name="event">Event to get the visibility from.</param>
		/// <returns>Visibility of the event.</returns>
		/// <exception cref="ArgumentException">The event has neither an add accessor method nor a remove accessor method.</exception>
		public static Visibility ToVisibility(this EventInfo @event)
		{
			// determine the event kind from the add/remove accessor method
			// (both accessor methods should have the same attributes)
			var accessor = @event.AddMethod ?? @event.RemoveMethod;
			if (accessor == null) throw new ArgumentException("The event has neither an add accessor method nor a remove accessor method.");
			MethodAttributes attributes = accessor.Attributes;

			switch (attributes & MethodAttributes.MemberAccessMask)
			{
				case MethodAttributes.Public:
					return Visibility.Public;

				case MethodAttributes.Family:
					return Visibility.Protected;

				case MethodAttributes.FamORAssem:
					return Visibility.ProtectedInternal;

				case MethodAttributes.Assembly:
					return Visibility.Internal;

				case MethodAttributes.Private:
					return Visibility.Private;

				case MethodAttributes.FamANDAssem:
					throw new NotImplementedException($"The access modifier of the event accessor method ({attributes & MethodAttributes.MemberAccessMask}) is not supported under C#.");

				default:
					throw new NotImplementedException("The visibility of the event is not supported."); // should never occur
			}
		}

		/// <summary>
		/// Gets the kind of the event.
		/// </summary>
		/// <param name="event">The event to get the event kind from.</param>
		/// <returns>The kind of event.</returns>
		public static EventKind ToEventKind(this EventInfo @event)
		{
			// determine the event kind from the add accessor method
			// (remove accessor method would work as well, usually both methods have the same attributes resulting in the same event kind)
			Debug.Assert(@event.AddMethod.Attributes == @event.RemoveMethod.Attributes);
			return @event.AddMethod.ToEventKind();
		}

		/// <summary>
		/// Gets the kind of the event the accessor method belongs to.
		/// </summary>
		/// <param name="accessor">
		/// Event accessor method to get the event kind from (must be an add/remove accessor method).
		/// </param>
		/// <returns>The kind of event the specified accessor belongs to.</returns>
		/// <exception cref="ArgumentException">The method is not a add/remove accessor method.</exception>
		public static EventKind ToEventKind(this MethodInfo accessor)
		{
			// ensure the method is a add/remove accessor method
			if (!accessor.IsSpecialName || !accessor.Name.StartsWith("add_") && !accessor.Name.StartsWith("remove_"))
				throw new ArgumentException("The method is not a add/remove accessor method.");

			MethodAttributes attributes = accessor.Attributes;
			if ((attributes & MethodAttributes.Static) == MethodAttributes.Static) return EventKind.Static;
			if ((attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract) return EventKind.Abstract;
			if ((attributes & (MethodAttributes.Virtual | MethodAttributes.NewSlot)) == MethodAttributes.Virtual) return EventKind.Override;
			if ((attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual) return EventKind.Virtual;
			return EventKind.Normal;
		}

		#endregion

		#region Concerning Properties

		/// <summary>
		/// Gets the kind of the property.
		/// </summary>
		/// <param name="property">The property to get the property kind from (must have at least one accessor method).</param>
		/// <returns>The kind of property.</returns>
		/// <exception cref="ArgumentException">The property has neither a get accessor method nor a set accessor method.</exception>
		public static PropertyKind ToPropertyKind(this PropertyInfo property)
		{
			// determine the property kind from the get/set accessor method
			// (both accessor methods should have the same attributes, but one of the accessor methods may not be implemented)
			var accessor = property.GetMethod ?? property.SetMethod;
			if (accessor == null) throw new ArgumentException("The property has neither a get accessor method nor a set accessor method.");
			return accessor.ToPropertyKind();
		}

		/// <summary>
		/// Gets the property kind of the method the property accessor method belongs to.
		/// </summary>
		/// <param name="accessor">
		/// Property accessor method to get the property kind from (must be a get/set accessor method).
		/// </param>
		/// <returns>The kind of property the specified accessor belongs to.</returns>
		/// <exception cref="ArgumentException">The method is not a get/set accessor method.</exception>
		public static PropertyKind ToPropertyKind(this MethodInfo accessor)
		{
			// ensure the method is a get/set accessor method
			if (!accessor.IsSpecialName || !accessor.Name.StartsWith("get_") && !accessor.Name.StartsWith("set_"))
				throw new ArgumentException("The method is not a get/set accessor method.");

			MethodAttributes attributes = accessor.Attributes;
			if ((attributes & MethodAttributes.Static) == MethodAttributes.Static) return PropertyKind.Static;
			if ((attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract) return PropertyKind.Abstract;
			if ((attributes & (MethodAttributes.Virtual | MethodAttributes.NewSlot)) == MethodAttributes.Virtual) return PropertyKind.Override;
			if ((attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual) return PropertyKind.Virtual;
			return PropertyKind.Normal;
		}

		#endregion

		#region Concerning Methods

		/// <summary>
		/// Gets the <see cref="MethodAttributes"/> corresponding to the visibility.
		/// </summary>
		/// <param name="visibility">Visibility to get the <see cref="MethodAttributes"/> from.</param>
		/// <returns>The corresponding <see cref="MethodAttributes"/>.</returns>
		public static MethodAttributes ToMethodAttributes(this Visibility visibility)
		{
			switch (visibility)
			{
				case Visibility.Public:            return MethodAttributes.Public;
				case Visibility.Protected:         return MethodAttributes.Family;
				case Visibility.ProtectedInternal: return MethodAttributes.FamORAssem;
				case Visibility.Internal:          return MethodAttributes.Assembly;
				case Visibility.Private:           return MethodAttributes.Private;

				default: // should never occur
					throw new NotImplementedException($"The specified visibility ({visibility}) is not supported.");
			}
		}

		/// <summary>
		/// Gets the method's visibility.
		/// </summary>
		/// <param name="method">Method to get the visibility from.</param>
		/// <returns>Visibility of the method.</returns>
		public static Visibility ToVisibility(this MethodBase method)
		{
			switch (method.Attributes & MethodAttributes.MemberAccessMask)
			{
				case MethodAttributes.Public:
					return Visibility.Public;

				case MethodAttributes.Family:
					return Visibility.Protected;

				case MethodAttributes.FamORAssem:
					return Visibility.ProtectedInternal;

				case MethodAttributes.Assembly:
					return Visibility.Internal;

				case MethodAttributes.Private:
					return Visibility.Private;

				case MethodAttributes.FamANDAssem:
					throw new NotImplementedException($"The access modifier of the method ({method.Attributes & MethodAttributes.MemberAccessMask}) is not supported under C#.");

				default:
					throw new NotImplementedException("The visibility of the method is not supported."); // should never occur
			}
		}

		/// <summary>
		/// Gets the kind of property accessor methods from the property kind.
		/// </summary>
		/// <param name="kind">Kind of the property.</param>
		/// <returns>Kind of the property's accessor methods.</returns>
		public static MethodKind ToAccessorMethodKind(this PropertyKind kind)
		{
			switch (kind)
			{
				case PropertyKind.Static:   return MethodKind.Static;
				case PropertyKind.Normal:   return MethodKind.Normal;
				case PropertyKind.Virtual:  return MethodKind.Virtual;
				case PropertyKind.Abstract: return MethodKind.Abstract;
				case PropertyKind.Override: return MethodKind.Override;

				default: // should never occur
					throw new NotImplementedException($"The property kind ({kind}) is not supported.");
			}
		}

		/// <summary>
		/// Gets the method kind of the specified method.
		/// </summary>
		/// <param name="method">Method to get the method kind from.</param>
		/// <returns>The kind of the method.</returns>
		public static MethodKind ToMethodKind(this MethodInfo method)
		{
			MethodAttributes attributes = method.Attributes;
			if ((attributes & MethodAttributes.Static) == MethodAttributes.Static) return MethodKind.Static;
			if ((attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract) return MethodKind.Abstract;
			if ((attributes & (MethodAttributes.Virtual | MethodAttributes.NewSlot)) == MethodAttributes.Virtual) return MethodKind.Override;
			if ((attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual) return MethodKind.Virtual;
			return MethodKind.Normal;
		}

		/// <summary>
		/// Gets the <see cref="MethodAttributes"/> corresponding to the method kind.
		/// </summary>
		/// <param name="kind">Method kind to get the <see cref="MethodAttributes"/> from.</param>
		/// <returns>The corresponding <see cref="MethodAttributes"/>.</returns>
		public static MethodAttributes ToMethodAttributes(this MethodKind kind)
		{
			switch (kind)
			{
				case MethodKind.Static:   return MethodAttributes.Static;
				case MethodKind.Normal:   return 0;
				case MethodKind.Abstract: return MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Abstract;
				case MethodKind.Virtual:  return MethodAttributes.Virtual | MethodAttributes.NewSlot;
				case MethodKind.Override: return MethodAttributes.Virtual | MethodAttributes.ReuseSlot;

				default: // should never occur
					throw new NotImplementedException($"The method kind ({kind}) is not supported.");
			}
		}

		/// <summary>
		/// Gets the appropriate calling convention for the method kind.
		/// </summary>
		/// <param name="kind">Method kind to get the calling convention for.</param>
		/// <param name="isVArgs"><c>true</c> for variable argument lists; otherwise <c>false</c>.</param>
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
					throw new NotImplementedException($"The method kind ({kind}) is not supported.");
			}

			if (isVArgs) convention |= CallingConventions.VarArgs;

			return convention;
		}

		#endregion

		#region Public Type Check
		
		/// <summary>
		/// Checks whether the specified type is public and all nested types are public, too;
		/// otherwise an exception is thrown as the created type in a dynamically generated assembly will not be able to access it.
		/// </summary>
		/// <param name="typeToCheck">Type to check.</param>
		/// <exception cref="ArgumentException">The specified type itself or a type that is part of the specified type is not public.</exception>
		public static void EnsureTypeIsTotallyPublic(Type typeToCheck)
		{
			EnsureTypeIsTotallyPublic(typeToCheck, typeToCheck);
		}

		/// <summary>
		/// Checks whether the specified type is public and all nested types are public, too;
		/// otherwise an exception is thrown as the created type in a dynamically generated assembly will not be able to access it.
		/// </summary>
		/// <param name="typeToCheck">Type to check.</param>
		/// <param name="specifiedType">The original type that is the root of the check.</param>
		/// <exception cref="ArgumentException">The specified type itself or a type that is part of the specified type is not public.</exception>
		private static void EnsureTypeIsTotallyPublic(Type typeToCheck, Type specifiedType)
		{
			// in the initial call, the root type is null
			if (specifiedType == null) specifiedType = typeToCheck;

			// ensure that the declaring type is public as well
			if (typeToCheck.IsNested)
			{
				EnsureTypeIsTotallyPublic(typeToCheck.DeclaringType, specifiedType);
			}

			// check whether the type is public
			if (!typeToCheck.IsPublic && !typeToCheck.IsNestedPublic)
			{
				throw new ArgumentException(
					typeToCheck == specifiedType
						? $"The specified type ({specifiedType.FullName}) is not public."
						: $"The specified type ({specifiedType.FullName}) depends on a type ({typeToCheck.FullName}) that is not public.");
			}

			if (typeof(Delegate).IsAssignableFrom(typeToCheck))
			{
				// a delegate
				// => check return value type and parameter types
				MethodInfo method = typeToCheck.GetMethod("Invoke");
				Debug.Assert(method != null, nameof(method) + " != null");
				EnsureTypeIsTotallyPublic(method.ReturnType, specifiedType);
				foreach (ParameterInfo parameterInfo in method.GetParameters())
				{
					EnsureTypeIsTotallyPublic(parameterInfo.ParameterType, specifiedType);
				}

				// everything is fine...
				return;
			}

			// check base type
			if (typeToCheck.BaseType != null)
			{
				EnsureTypeIsTotallyPublic(typeToCheck.BaseType, specifiedType);
			}
		}

		#endregion

		#region Validating Names

		/// <summary>
		/// Checks whether the specified name is a valid type name and throws an exception if it violates the naming constraints.
		/// </summary>
		/// <param name="name">Type name to check.</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid type name.</exception>
		public static void EnsureNameIsValidTypeName(string name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name), "The type name must not be a null reference.");
			var tokens = name.Split('.');
			if (tokens.Any(x => !CodeGenerator.IsValidLanguageIndependentIdentifier(x)))
				throw new ArgumentException($"'{name}' is not a valid type name.");
		}

		/// <summary>
		/// Checks whether the specified name is a valid language independent identifier and throws an exception,
		/// if it violates the naming constraints.
		/// </summary>
		/// <param name="name">Identifier to check.</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		public static void EnsureNameIsValidLanguageIndependentIdentifier(string name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name), "The identifier must not be a null reference.");
			if (!CodeGenerator.IsValidLanguageIndependentIdentifier(name))
				throw new ArgumentException($"'{name}' is not a valid identifier.");
		}

		#endregion

		#region Emitting MSIL

		/// <summary>
		/// Emits the appropriate MSIL opcode to load an argument onto the evaluation stack.
		/// </summary>
		/// <param name="msilGenerator">IL generator to emit opcodes to.</param>
		/// <param name="argumentIndex">Index of the argument to load.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="argumentIndex"/> is negative.</exception>
		public static void EmitLoadArgument(ILGenerator msilGenerator, int argumentIndex)
		{
			if (argumentIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(argumentIndex), argumentIndex, "The argument index must not be negative.");

			if (argumentIndex == 0) msilGenerator.Emit(OpCodes.Ldarg_0);
			else if (argumentIndex == 1) msilGenerator.Emit(OpCodes.Ldarg_1);
			else if (argumentIndex == 2) msilGenerator.Emit(OpCodes.Ldarg_2);
			else if (argumentIndex == 3) msilGenerator.Emit(OpCodes.Ldarg_3);
			else if (argumentIndex <= 255) msilGenerator.Emit(OpCodes.Ldarg_S, (byte)argumentIndex);
			else msilGenerator.Emit(OpCodes.Ldarg, (short)argumentIndex);
		}

		/// <summary>
		/// Emits the appropriate MSIL opcodes to load a signed 32-bit integer constant onto the evaluation stack.
		/// </summary>
		/// <param name="msilGenerator">IL generator to emit opcodes to.</param>
		/// <param name="x">Constant to load.</param>
		public static void EmitLoadConstant(ILGenerator msilGenerator, int x)
		{
			switch (x)
			{
				case -1:
					msilGenerator.Emit(OpCodes.Ldc_I4_M1);
					break;

				case 0:
					msilGenerator.Emit(OpCodes.Ldc_I4_0);
					break;

				case 1:
					msilGenerator.Emit(OpCodes.Ldc_I4_1);
					break;

				case 2:
					msilGenerator.Emit(OpCodes.Ldc_I4_2);
					break;

				case 3:
					msilGenerator.Emit(OpCodes.Ldc_I4_3);
					break;

				case 4:
					msilGenerator.Emit(OpCodes.Ldc_I4_4);
					break;

				case 5:
					msilGenerator.Emit(OpCodes.Ldc_I4_5);
					break;

				case 6:
					msilGenerator.Emit(OpCodes.Ldc_I4_6);
					break;

				case 7:
					msilGenerator.Emit(OpCodes.Ldc_I4_7);
					break;

				case 8:
					msilGenerator.Emit(OpCodes.Ldc_I4_8);
					break;

				default:
					if (x >= sbyte.MinValue && x <= sbyte.MaxValue) msilGenerator.Emit(OpCodes.Ldc_I4_S, (sbyte)x);
					else msilGenerator.Emit(OpCodes.Ldc_I4, x);
					break;
			}
		}

		/// <summary>
		/// Emits the appropriate MSIL opcodes to load an unsigned 32-bit integer constant onto the evaluation stack.
		/// </summary>
		/// <param name="msilGenerator">IL generator to emit opcodes to.</param>
		/// <param name="x">Constant to load.</param>
		public static void EmitLoadConstant(ILGenerator msilGenerator, uint x)
		{
			switch (x)
			{
				case 0:
					msilGenerator.Emit(OpCodes.Ldc_I4_0);
					break;

				case 1:
					msilGenerator.Emit(OpCodes.Ldc_I4_1);
					break;

				case 2:
					msilGenerator.Emit(OpCodes.Ldc_I4_2);
					break;

				case 3:
					msilGenerator.Emit(OpCodes.Ldc_I4_3);
					break;

				case 4:
					msilGenerator.Emit(OpCodes.Ldc_I4_4);
					break;

				case 5:
					msilGenerator.Emit(OpCodes.Ldc_I4_5);
					break;

				case 6:
					msilGenerator.Emit(OpCodes.Ldc_I4_6);
					break;

				case 7:
					msilGenerator.Emit(OpCodes.Ldc_I4_7);
					break;

				case 8:
					msilGenerator.Emit(OpCodes.Ldc_I4_8);
					break;

				default:
					if (x <= (uint)sbyte.MaxValue)
					{
						msilGenerator.Emit(OpCodes.Ldc_I4_S, (sbyte)x);
					}
					else
					{
						unchecked
						{
							int value = (int)x;
							if (value == -1) msilGenerator.Emit(OpCodes.Ldc_I4_M1);
							else msilGenerator.Emit(OpCodes.Ldc_I4, value);
						}
					}

					break;
			}
		}

		/// <summary>
		/// Emits the appropriate MSIL opcodes to load a signed 64-bit integer constant onto the evaluation stack.
		/// </summary>
		/// <param name="msilGenerator">IL generator to emit opcodes to.</param>
		/// <param name="x">Constant to load.</param>
		public static void EmitLoadConstant(ILGenerator msilGenerator, long x)
		{
			switch (x)
			{
				case -1:
					msilGenerator.Emit(OpCodes.Ldc_I4_M1);
					break;

				case 0:
					msilGenerator.Emit(OpCodes.Ldc_I4_0);
					break;

				case 1:
					msilGenerator.Emit(OpCodes.Ldc_I4_1);
					break;

				case 2:
					msilGenerator.Emit(OpCodes.Ldc_I4_2);
					break;

				case 3:
					msilGenerator.Emit(OpCodes.Ldc_I4_3);
					break;

				case 4:
					msilGenerator.Emit(OpCodes.Ldc_I4_4);
					break;

				case 5:
					msilGenerator.Emit(OpCodes.Ldc_I4_5);
					break;

				case 6:
					msilGenerator.Emit(OpCodes.Ldc_I4_6);
					break;

				case 7:
					msilGenerator.Emit(OpCodes.Ldc_I4_7);
					break;

				case 8:
					msilGenerator.Emit(OpCodes.Ldc_I4_8);
					break;

				default:
					if (x >= sbyte.MinValue && x <= sbyte.MaxValue)
					{
						msilGenerator.Emit(OpCodes.Ldc_I4_S, (sbyte)x);
					}
					else if (x >= int.MinValue && x <= int.MaxValue)
					{
						msilGenerator.Emit(OpCodes.Ldc_I4, (int)x);
					}
					else
					{
						msilGenerator.Emit(OpCodes.Ldc_I8, x);
						return; // do not emit conversion opcode here!
					}

					break;
			}

			// there is an 32-bit integer on the evaluation stack now
			// => convert it to a 64-bit integer
			msilGenerator.Emit(OpCodes.Conv_I8);
		}

		/// <summary>
		/// Emits the appropriate MSIL opcodes to load an unsigned 32-bit integer constant onto the evaluation stack.
		/// </summary>
		/// <param name="msilGenerator">IL generator to emit opcodes to.</param>
		/// <param name="x">Constant to load.</param>
		public static void EmitLoadConstant(ILGenerator msilGenerator, ulong x)
		{
			switch (x)
			{
				case 0:
					msilGenerator.Emit(OpCodes.Ldc_I4_0);
					break;

				case 1:
					msilGenerator.Emit(OpCodes.Ldc_I4_1);
					break;

				case 2:
					msilGenerator.Emit(OpCodes.Ldc_I4_2);
					break;

				case 3:
					msilGenerator.Emit(OpCodes.Ldc_I4_3);
					break;

				case 4:
					msilGenerator.Emit(OpCodes.Ldc_I4_4);
					break;

				case 5:
					msilGenerator.Emit(OpCodes.Ldc_I4_5);
					break;

				case 6:
					msilGenerator.Emit(OpCodes.Ldc_I4_6);
					break;

				case 7:
					msilGenerator.Emit(OpCodes.Ldc_I4_7);
					break;

				case 8:
					msilGenerator.Emit(OpCodes.Ldc_I4_8);
					break;

				default:
					if (x <= (ulong)sbyte.MaxValue)
					{
						msilGenerator.Emit(OpCodes.Ldc_I4_S, (sbyte)x);
					}
					else if (x <= int.MaxValue)
					{
						msilGenerator.Emit(OpCodes.Ldc_I4, (int)x);
					}
					else
					{
						unchecked
						{
							long value = (long)x;

							if (value == -1)
							{
								msilGenerator.Emit(OpCodes.Ldc_I4_M1);
							}
							else
							{
								msilGenerator.Emit(OpCodes.Ldc_I8, value);
								return; // do not emit conversion opcode here!
							}
						}
					}

					break;
			}

			// there is an 32-bit integer on the evaluation stack now
			// => convert it to a 64-bit integer
			msilGenerator.Emit(OpCodes.Conv_I8);
		}

		/// <summary>
		/// Emits the appropriate opcode to load the default value of the specified type onto the evaluation stack.
		/// </summary>
		/// <param name="msilGenerator">IL generator to emit opcodes to.</param>
		/// <param name="type">Type whose default value is to load.</param>
		/// <param name="box">
		/// <c>true</c> to box value types;
		/// <c>false</c> to keep value types.
		/// </param>
		public static void EmitLoadDefaultValue(ILGenerator msilGenerator, Type type, bool box)
		{
			if (type.IsValueType)
			{
				if (type.IsEnum) type = type.GetEnumUnderlyingType();

				if (type == typeof(bool) || type == typeof(char) || type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint))
				{
					msilGenerator.Emit(OpCodes.Ldc_I4_0);
				}
				else if (type == typeof(long) || type == typeof(ulong))
				{
					msilGenerator.Emit(OpCodes.Ldc_I4_0);
					msilGenerator.Emit(OpCodes.Conv_I8);
				}
				else if (type == typeof(float))
				{
					msilGenerator.Emit(OpCodes.Ldc_I4_0);
					msilGenerator.Emit(OpCodes.Conv_R4);
				}
				else if (type == typeof(double))
				{
					msilGenerator.Emit(OpCodes.Ldc_I4_0);
					msilGenerator.Emit(OpCodes.Conv_R8);
				}
				else if (type == typeof(IntPtr) || type == typeof(UIntPtr))
				{
					// IntPtr and UIntPtr are primitive types with a platform dependent size
					// (4 byte on 32-bit platforms, 8 byte on 64-bit platforms)
					LocalBuilder builder = msilGenerator.DeclareLocal(type);
					msilGenerator.Emit(OpCodes.Ldloca, builder);
					msilGenerator.Emit(OpCodes.Initobj, type);
					msilGenerator.Emit(OpCodes.Ldloc_0);
				}
				else
				{
					Debug.Assert(!type.IsPrimitive, "Primitive types should have their own special implementation.");
					LocalBuilder builder = msilGenerator.DeclareLocal(type);
					msilGenerator.Emit(OpCodes.Ldloca, builder);
					msilGenerator.Emit(OpCodes.Initobj, type);
					msilGenerator.Emit(OpCodes.Ldloc_0);
				}

				if (box) msilGenerator.Emit(OpCodes.Box, type);
			}
			else
			{
				msilGenerator.Emit(OpCodes.Ldnull);
			}
		}

		#endregion
	}

}
