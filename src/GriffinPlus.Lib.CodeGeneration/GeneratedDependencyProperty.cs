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
	/// A method that provides an implementation returning an object on the evaluation stack to use as the default value
	/// for the generated dependency property (object on the stack must be a reference type, so box value types before
	/// returning!)
	/// </summary>
	/// <param name="msil">MSIL generator to use.</param>
	/// <param name="dp">Dependency property to implement the method for.</param>
	public delegate void DependencyPropertyInitializer(ILGenerator msil, GeneratedDependencyProperty dp);

	/// <summary>
	/// A generated dependency property.
	/// </summary>
	public class GeneratedDependencyProperty : Member, IDependencyProperty
	{
		#region Member Variables

		private readonly string mName;
		private readonly bool mIsReadOnly;
		private readonly IGeneratedFieldInternal mDependencyPropertyField;
		private readonly GeneratedProperty mAccessorProperty;
		private readonly IPropertyImplementation mImplementation;

		#endregion

		#region Construction

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedProperty"/> class
		/// (associates a standard <see cref="System.Windows.PropertyMetadata"/> object with a default value with the dependency property).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="name">Name of the property (may be null).</param>
		/// <param name="type">Type of the property.</param>
		/// <param name="isReadOnly">
		/// true, if the dependency property is read-only;
		/// false, if it is read-write.
		/// </param>
		/// <param name="initializer">A method that provides code creating the default value of the dependency property (may be null to use the type's default value).</param>
		internal GeneratedDependencyProperty(
			CodeGenEngine engine,
			string name,
			Type type,
			bool isReadOnly,
			DependencyPropertyInitializer initializer = null) :
				base(engine)
		{
			mName = name;
			mIsReadOnly = isReadOnly;

			// add the dependency property
			if (mIsReadOnly)
			{
				mDependencyPropertyField = (IGeneratedFieldInternal)engine.AddStaticField<System.Windows.DependencyPropertyKey>(
					name + "Property",
					Visibility.Public,
					(msil, field) =>
					{
						MethodInfo registerReadOnlyMethod = typeof(System.Windows.DependencyProperty).GetMethod(
							"RegisterReadOnly",
							BindingFlags.Static,
							null,
							new Type[] { typeof(string), typeof(Type), typeof(Type), typeof(System.Windows.PropertyMetadata) },
							null);

						msil.Emit(OpCodes.Ldstr, mName);
						msil.Emit(OpCodes.Ldtoken, type);
						msil.Emit(OpCodes.Ldtoken, engine.TypeBuilder);

						// create PropertyMetadata object
						ConstructorInfo propertyMetadataConstructor = typeof(System.Windows.PropertyMetadata).GetConstructor(new Type[] { typeof(object) });
						if (initializer != null) initializer(msil, this);
						else CodeGenHelpers.EmitLoadDefaultValue(msil, type, true);
						msil.Emit(OpCodes.Newobj, propertyMetadataConstructor);

						// call DependencyProperty.RegisterReadOnly() method
						msil.Emit(OpCodes.Call, registerReadOnlyMethod);
					});
			}
			else
			{
				mDependencyPropertyField = (IGeneratedFieldInternal)engine.AddStaticField<System.Windows.DependencyProperty>(
					name + "Property",
					Visibility.Public,
					(msil, field) =>
					{
						MethodInfo registerMethod = typeof(System.Windows.DependencyProperty).GetMethod(
							"Register",
							BindingFlags.Public | BindingFlags.Static,
							null,
							new Type[] { typeof(string), typeof(Type), typeof(Type), typeof(System.Windows.PropertyMetadata) },
							null);

						msil.Emit(OpCodes.Ldstr, mName);
						msil.Emit(OpCodes.Ldtoken, type);
						msil.Emit(OpCodes.Ldtoken, engine.TypeBuilder);

						// create PropertyMetadata object
						ConstructorInfo propertyMetadataConstructor = typeof(System.Windows.PropertyMetadata).GetConstructor(new Type[] { typeof(object) });
						if (initializer != null) initializer(msil, this);
						else CodeGenHelpers.EmitLoadDefaultValue(msil, type, true);
						msil.Emit(OpCodes.Newobj, propertyMetadataConstructor);

						// call DependencyProperty.Register() method
						msil.Emit(OpCodes.Call, registerMethod);
					});
			}

			// add the accessor property
			mImplementation = new PropertyImplementation_DependencyProperty(this);
			mAccessorProperty = engine.AddProperty(
				name,
				type,
				PropertyKind.Normal,
				mImplementation);

			mAccessorProperty.GetAccessor.Visibility = CodeGeneration.Visibility.Public;
			mAccessorProperty.SetAccessor.Visibility = CodeGeneration.Visibility.Internal;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the type of the dependency property.
		/// </summary>
		public Type Type
		{
			get { return mAccessorProperty.Type; }
		}

		/// <summary>
		/// Gets the name of the dependency property.
		/// </summary>
		public string Name
		{
			get { return mName; }
		}

		/// <summary>
		/// Gets a value indicating whether the property is read-only (true) or read-write (false).
		/// </summary>
		public bool IsReadOnly
		{
			get { return mIsReadOnly; }
		}

		/// <summary>
		/// Gets the accessor property associated with the dependency property.
		/// </summary>
		public GeneratedProperty AccessorProperty
		{
			get { return mAccessorProperty; }
		}

		/// <summary>
		/// Gets the accessor property associated with the dependency property.
		/// </summary>
		IProperty IDependencyProperty.AccessorProperty
		{
			get { return mAccessorProperty; }
		}

		/// <summary>
		/// Gets the static field storing the registered dependency property.
		/// </summary>
		public IGeneratedField DependencyPropertyField
		{
			get { return mDependencyPropertyField; }
		}

		#endregion

		#region Internal Management

		/// <summary>
		/// Is called when the event is about to be removed from the type in creation.
		/// </summary>
		internal void OnRemoving()
		{
			Engine.RemoveField(mDependencyPropertyField);
			Engine.RemoveProperty(mAccessorProperty);
		}

		/// <summary>
		/// Adds the property and its accessor methods to the type builder.
		/// </summary>
		internal void AddToTypeBuilder()
		{
			Debug.Assert(IsFrozen);
			mDependencyPropertyField.AddToTypeBuilder();
			mAccessorProperty.AddToTypeBuilder();
		}

		#endregion

	}
}
