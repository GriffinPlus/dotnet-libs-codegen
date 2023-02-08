///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if NET461 || (NET5_0 || NET6_0 || NET7_0 ) && WINDOWS
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Property implementation for accessing a dependency property.
	/// </summary>
	public class PropertyImplementation_DependencyProperty : PropertyImplementation
	{
		private readonly IGeneratedDependencyProperty mDependencyProperty;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyImplementation_DependencyProperty"/> class.
		/// </summary>
		/// <param name="property">Dependency property to create the accessor property for.</param>
		public PropertyImplementation_DependencyProperty(IGeneratedDependencyProperty property)
		{
			mDependencyProperty = property;
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
			MethodInfo getValueMethod = typeof(DependencyObject)
				.GetMethod(
					"GetValue",
					BindingFlags.Public | BindingFlags.Instance,
					null,
					new[] { typeof(DependencyProperty) },
					null);

			Debug.Assert(getValueMethod != null, nameof(getValueMethod) + " != null");

			msilGenerator.Emit(OpCodes.Ldarg_0);

			if (mDependencyProperty.IsReadOnly)
			{
				msilGenerator.Emit(OpCodes.Ldsfld, mDependencyProperty.DependencyPropertyField.FieldBuilder);
				PropertyInfo dependencyPropertyProperty = typeof(DependencyPropertyKey).GetProperty("DependencyProperty");
				Debug.Assert(dependencyPropertyProperty != null, nameof(dependencyPropertyProperty) + " != null");
				msilGenerator.Emit(OpCodes.Call, dependencyPropertyProperty.GetGetMethod(false));
			}
			else
			{
				msilGenerator.Emit(OpCodes.Ldsfld, mDependencyProperty.DependencyPropertyField.FieldBuilder);
			}

			msilGenerator.Emit(OpCodes.Call, getValueMethod);
			if (property.PropertyType.IsValueType) msilGenerator.Emit(OpCodes.Unbox_Any, property.PropertyType);
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
			MethodInfo setValueMethod = typeof(DependencyObject)
				.GetMethod(
					"SetValue",
					BindingFlags.Public | BindingFlags.Instance,
					null,
					new[]
					{
						mDependencyProperty.IsReadOnly
							? typeof(DependencyPropertyKey)
							: typeof(DependencyProperty),
						typeof(object)
					},
					null);

			Debug.Assert(setValueMethod != null, nameof(setValueMethod) + " != null");

			msilGenerator.Emit(OpCodes.Ldarg_0);
			msilGenerator.Emit(OpCodes.Ldsfld, mDependencyProperty.DependencyPropertyField.FieldBuilder);
			if (property.PropertyType.IsValueType) msilGenerator.Emit(OpCodes.Box, property.PropertyType);
			msilGenerator.Emit(OpCodes.Call, setValueMethod);
		}
	}

}

#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0
// Dependency properties are not supported on .NET Standard and .NET5/6/7 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif
