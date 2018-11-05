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
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Property implementation for accessing a dependency property.
	/// </summary>
	public class PropertyImplementation_DependencyProperty : IPropertyImplementation
	{
		private readonly GeneratedDependencyProperty mDependencyProperty;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyImplementation_DependencyProperty"/> class.
		/// </summary>
		/// <param name="property">Dependency property to create the accessor property for.</param>
		public PropertyImplementation_DependencyProperty(GeneratedDependencyProperty property)
		{
			mDependencyProperty = property;
		}

		/// <summary>
		/// Reviews the declaration of the property and adds additional type declarations, if necessary.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		/// <param name="property">The property to review.</param>
		public void Declare(CodeGenEngine engine, GeneratedProperty property)
		{

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
				MethodInfo getValueMethod = typeof(System.Windows.DependencyObject).GetMethod("GetValue", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Windows.DependencyProperty) }, null);
				getMsil.Emit(OpCodes.Ldarg_0);

				if (mDependencyProperty.IsReadOnly)
				{
					getMsil.Emit(OpCodes.Ldsfld, mDependencyProperty.DependencyPropertyField.FieldBuilder);
					PropertyInfo dependencyPropertyProperty = typeof(System.Windows.DependencyPropertyKey).GetProperty("DependencyProperty");
					getMsil.Emit(OpCodes.Call, dependencyPropertyProperty.GetGetMethod(false));
				}
				else
				{
					getMsil.Emit(OpCodes.Ldsfld, mDependencyProperty.DependencyPropertyField.FieldBuilder);
				}

				getMsil.Emit(OpCodes.Call, getValueMethod);
				if (property.Type.IsValueType) getMsil.Emit(OpCodes.Unbox_Any, property.Type);
				getMsil.Emit(OpCodes.Ret);
			}

			// implement set accessor
			// ---------------------------------------------------------------------------------------------------------------
			if (setMsil != null)
			{
				MethodInfo setValueMethod = mDependencyProperty.IsReadOnly ?
					typeof(System.Windows.DependencyObject).GetMethod("SetValue", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Windows.DependencyPropertyKey), typeof(object) }, null) :
					typeof(System.Windows.DependencyObject).GetMethod("SetValue", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Windows.DependencyProperty), typeof(object) }, null);

				setMsil.Emit(OpCodes.Ldarg_0);
				setMsil.Emit(OpCodes.Ldsfld, mDependencyProperty.DependencyPropertyField.FieldBuilder);
				if (property.Type.IsValueType) setMsil.Emit(OpCodes.Box, property.Type);
				setMsil.Emit(OpCodes.Call, setValueMethod);
			}
		}

		/// <summary>
		/// Is called when the property the implementation strategy is attached to is removed from the type in creation.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		public void OnRemoving(CodeGenEngine engine)
		{

		}

	}
}
