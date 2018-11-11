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
	/// Property implementation that simply backs the property with a field.
	/// </summary>
	public class PropertyImplementation_Simple : IPropertyImplementation
	{
		private IGeneratedField mBackingField;

		/// <summary>
		/// Reviews the declaration of the property and adds additional type declarations, if necessary.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		/// <param name="property">The property to review.</param>
		public void Declare(CodeGenEngine engine, GeneratedProperty property)
		{
			// add an anonymous field
			mBackingField = engine.AddField(property.Type, null, property.Kind == PropertyKind.Static, Visibility.Private);
		}

		/// <summary>
		/// Implements the event.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		/// <param name="property">The property to implement.</param>
		public void Implement(CodeGenEngine engine, GeneratedProperty property)
		{
			ILGenerator getMsil = property.GetAccessor.MethodBuilder?.GetILGenerator();
			ILGenerator setMsil = property.SetAccessor.MethodBuilder?.GetILGenerator();

			// implement get accessor
			// ---------------------------------------------------------------------------------------------------------------
			if (getMsil != null)
			{
				if (property.Kind == PropertyKind.Static)
				{
					getMsil.Emit(OpCodes.Ldsfld, mBackingField.FieldBuilder);
				}
				else
				{
					getMsil.Emit(OpCodes.Ldarg_0);
					getMsil.Emit(OpCodes.Ldfld, mBackingField.FieldBuilder);
				}

				getMsil.Emit(OpCodes.Ret);
			}

			// implement set accessor
			// ---------------------------------------------------------------------------------------------------------------
			if (setMsil != null)
			{
				if (property.Kind == PropertyKind.Static)
				{
					setMsil.Emit(OpCodes.Ldarg_0);
					setMsil.Emit(OpCodes.Stsfld, mBackingField.FieldBuilder);
				}
				else
				{
					setMsil.Emit(OpCodes.Ldarg_0);
					setMsil.Emit(OpCodes.Ldarg_1);
					setMsil.Emit(OpCodes.Stfld, mBackingField.FieldBuilder);
				}

				setMsil.Emit(OpCodes.Ret);
			}
		}

		/// <summary>
		/// Is called when the property the implementation strategy is attached to is removed from the type in creation.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		public void OnRemoving(CodeGenEngine engine)
		{
			engine.RemoveField(mBackingField);
		}

	}
}
