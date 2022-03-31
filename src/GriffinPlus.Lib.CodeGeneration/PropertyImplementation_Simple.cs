///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Property implementation that simply backs the property with an anonymous field.
	/// </summary>
	public class PropertyImplementation_Simple : PropertyImplementation
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
		/// Implements the get accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the get accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the get accessor method to implement.</param>
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
		/// Implements the set accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the set accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the set accessor method to implement.</param>
		public override void ImplementSetAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedProperty property,
			ILGenerator        msilGenerator)
		{
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

			msilGenerator.Emit(OpCodes.Ret);
		}
	}

}
