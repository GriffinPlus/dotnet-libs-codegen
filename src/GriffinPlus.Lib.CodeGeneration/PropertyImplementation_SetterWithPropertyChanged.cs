///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Property implementation that backs the property with a field and calls the 'OnPropertyChanged' method when the
	/// property value changes. The 'OnPropertyChanged' method must be public, protected or protected internal and have
	/// the following signature: <c>void OnPropertyChanged(string name)</c>.
	/// </summary>
	public class PropertyImplementation_SetterWithPropertyChanged : PropertyImplementation
	{
		private IGeneratedField mBackingField;

		/// <summary>
		/// Adds additional fields, events, properties and methods to the type definition.
		/// </summary>
		/// <param name="typeDefinition">The type definition.</param>
		/// <param name="property">The property to implement.</param>
		public override void Declare(TypeDefinition typeDefinition, IGeneratedProperty property)
		{
			// add an anonymous field
			mBackingField = property.Kind == PropertyKind.Static
				                ? typeDefinition.AddStaticField(property.PropertyType, null, Visibility.Private)
				                : typeDefinition.AddField(property.PropertyType, null, Visibility.Private);
		}

		/// <summary>
		/// Implements the 'get' accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the 'get' accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the 'get' accessor method to implement.</param>
		public override void ImplementGetAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedProperty property,
			ILGenerator        msilGenerator)
		{
			if (property.Kind == PropertyKind.Static)
			{
				msilGenerator.Emit(OpCodes.Ldsfld, mBackingField.FieldBuilder);
			}
			else
			{
				msilGenerator.Emit(OpCodes.Ldarg_0);
				msilGenerator.Emit(OpCodes.Ldfld, mBackingField.FieldBuilder);
			}

			msilGenerator.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Implements the 'set' accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the 'set' accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the 'set' accessor method to implement.</param>
		public override void ImplementSetAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedProperty property,
			ILGenerator        msilGenerator)
		{
			MethodInfo equalsMethod = typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) });
			Debug.Assert(equalsMethod != null, nameof(equalsMethod) + " != null");

			Label endLabel = msilGenerator.DefineLabel();

			// jump to end if the value to set equals the backing field
			if (property.Kind == PropertyKind.Static)
			{
				msilGenerator.Emit(OpCodes.Ldsfld, mBackingField.FieldBuilder);
				if (property.PropertyType.IsValueType) msilGenerator.Emit(OpCodes.Box, property.PropertyType);
				msilGenerator.Emit(OpCodes.Ldarg_0);
				if (property.PropertyType.IsValueType) msilGenerator.Emit(OpCodes.Box, property.PropertyType);
			}
			else
			{
				msilGenerator.Emit(OpCodes.Ldarg_0);
				msilGenerator.Emit(OpCodes.Ldfld, mBackingField.FieldBuilder);
				if (property.PropertyType.IsValueType) msilGenerator.Emit(OpCodes.Box, property.PropertyType);
				msilGenerator.Emit(OpCodes.Ldarg_1);
				if (property.PropertyType.IsValueType) msilGenerator.Emit(OpCodes.Box, property.PropertyType);
			}

			msilGenerator.Emit(OpCodes.Call, equalsMethod);
			msilGenerator.Emit(OpCodes.Brtrue_S, endLabel);

			// update field
			if (property.Kind == PropertyKind.Static)
			{
				msilGenerator.Emit(OpCodes.Ldarg_0);
				msilGenerator.Emit(OpCodes.Stsfld, mBackingField.FieldBuilder);
			}
			else
			{
				msilGenerator.Emit(OpCodes.Ldarg_0);
				msilGenerator.Emit(OpCodes.Ldarg_1);
				msilGenerator.Emit(OpCodes.Stfld, mBackingField.FieldBuilder);
			}

			// call event raiser
			if (property.Kind != PropertyKind.Static)
			{
				IMethod raiserMethod = typeDefinition.GetMethod("OnPropertyChanged", new[] { typeof(string) });

				if (raiserMethod == null)
				{
					Debug.Assert(typeDefinition.TypeBuilder.BaseType != null, "engine.TypeBuilder.BaseType != null");
					string error = $"The class ({typeDefinition.TypeBuilder.FullName}) or its base class ({typeDefinition.TypeBuilder.BaseType.FullName}) does not define 'void OnPropertyChanged(string name)'.";
					throw new CodeGenException(error);
				}

				if (raiserMethod is GeneratedMethod generatedMethod)
				{
					msilGenerator.Emit(OpCodes.Ldarg_0);
					msilGenerator.Emit(OpCodes.Ldstr, property.Name);
					msilGenerator.Emit(OpCodes.Callvirt, generatedMethod.MethodBuilder);
					if (generatedMethod.ReturnType != typeof(void)) msilGenerator.Emit(OpCodes.Pop);
				}
				else if (raiserMethod is InheritedMethod inheritedMethod)
				{
					msilGenerator.Emit(OpCodes.Ldarg_0);
					msilGenerator.Emit(OpCodes.Ldstr, property.Name);
					msilGenerator.Emit(OpCodes.Callvirt, inheritedMethod.MethodInfo);
					if (inheritedMethod.ReturnType != typeof(void)) msilGenerator.Emit(OpCodes.Pop);
				}
				else
				{
					throw new CodeGenException("OnPropertyChanged(string) is neither generated nor inherited.");
				}
			}

			msilGenerator.MarkLabel(endLabel);
			msilGenerator.Emit(OpCodes.Ret);
		}
	}

}
