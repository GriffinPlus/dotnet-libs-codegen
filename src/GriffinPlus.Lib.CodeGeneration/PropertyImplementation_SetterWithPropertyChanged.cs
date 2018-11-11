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

using GriffinPlus.Lib.Logging;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Property implementation that backs the property with a field and calls the 'OnPropertyChanged' method when the
	/// property value changes. The 'OnPropertyChanged' method must be public, protected or protected internal and have
	/// the following signature: <c>void OnPropertyChanged(string name)</c>.
	/// </summary>
	public class PropertyImplementation_SetterWithPropertyChanged : IPropertyImplementation
	{
		private static LogWriter sLog = Log.GetWriter(typeof(PropertyImplementation_SetterWithPropertyChanged));
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
				MethodInfo equalsMethod = typeof(object).GetMethod("Equals", new Type[] { typeof(object), typeof(object) });
				Label endLabel = setMsil.DefineLabel();

				// jump to end, if the value to set equals the backing field
				if (property.Kind == PropertyKind.Static)
				{
					setMsil.Emit(OpCodes.Ldsfld, mBackingField.FieldBuilder);
					if (property.Type.IsValueType) setMsil.Emit(OpCodes.Box, property.Type);
					setMsil.Emit(OpCodes.Ldarg_0);
					if (property.Type.IsValueType) setMsil.Emit(OpCodes.Box, property.Type);
				}
				else
				{
					setMsil.Emit(OpCodes.Ldarg_0);
					setMsil.Emit(OpCodes.Ldfld, mBackingField.FieldBuilder);
					if (property.Type.IsValueType) setMsil.Emit(OpCodes.Box, property.Type);
					setMsil.Emit(OpCodes.Ldarg_1);
					if (property.Type.IsValueType) setMsil.Emit(OpCodes.Box, property.Type);
				}
				setMsil.Emit(OpCodes.Call, equalsMethod);
				setMsil.Emit(OpCodes.Brtrue_S, endLabel);

				// update field
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

				// call event raiser
				if (property.Kind != PropertyKind.Static)
				{
					IMethod raiserMethod = engine.GetMethod("OnPropertyChanged", new Type[] { typeof(string) });

					if (raiserMethod == null)
					{
						string error = string.Format(
							"The class ({0}) or its base class ({1}) does not define 'void OnPropertyChanged(string name)'.",
							engine.TypeBuilder.FullName, engine.TypeBuilder.BaseType.FullName);
						sLog.Write(LogLevel.Error, error);
						throw new CodeGenException(error);
					}

					if (raiserMethod is GeneratedMethod)
					{
						GeneratedMethod method = raiserMethod as GeneratedMethod;
						setMsil.Emit(OpCodes.Ldarg_0);
						setMsil.Emit(OpCodes.Ldstr, property.Name);
						setMsil.Emit(OpCodes.Callvirt, method.MethodBuilder);
						if (method.ReturnType != typeof(void)) setMsil.Emit(OpCodes.Pop);
					}
					else if (raiserMethod is InheritedMethod)
					{
						InheritedMethod method = raiserMethod as InheritedMethod;
						setMsil.Emit(OpCodes.Ldarg_0);
						setMsil.Emit(OpCodes.Ldstr, property.Name);
						setMsil.Emit(OpCodes.Callvirt, method.MethodInfo);
						if (method.ReturnType != typeof(void)) setMsil.Emit(OpCodes.Pop);
					}
					else
					{
						throw new NotImplementedException("Method is neither generated nor inherited.");
					}
				}

				setMsil.MarkLabel(endLabel);
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
