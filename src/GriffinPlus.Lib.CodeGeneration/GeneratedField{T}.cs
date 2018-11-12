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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// A generated field.
	/// </summary>
	public class GeneratedField<T> : Member, IGeneratedField, IGeneratedFieldInternal
	{
		#region Member Variables

		private string mName;
		private bool mIsStatic;
		private Visibility mVisibility;
		private FieldBuilder mFieldBuilder;
		private readonly FieldInitializer mInitializer;
		private readonly Func<T> mInitialValueFactory;
		private T mDefaultValue;

		#endregion

		#region Construction

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedField{T}"/> class (without default value).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="isStatic">true to emit a static field; false to emit a member field.</param>
		/// <param name="name">Name of the field (null to create an anonymous field).</param>
		/// <param name="visibility">Visibility of the field.</param>
		internal GeneratedField(CodeGenEngine engine, bool isStatic, string name, Visibility visibility) :
			this(engine, isStatic, name, visibility, null)
		{

		}

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedField{T}"/> class (with default value).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="isStatic">true to emit a static field; false to emit a member field.</param>
		/// <param name="name">Name of the field (null to create an anonymous field).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="defaultValue">Default value of the field.</param>
		internal GeneratedField(CodeGenEngine engine, bool isStatic, string name, Visibility visibility, T defaultValue) :
			base(engine)
		{
			// ensure that the specified type is public and all nested types are public, too
			// => otherwise the dynamically created assembly is not able to access it
			CodeGenHelpers.CheckTypeIsTotallyPublic(typeof(T));

			// ensure that the specified default value is of the same type as the field
			if (!typeof(T).IsAssignableFrom(defaultValue.GetType())) {
				string error = string.Format("Default value ({0}) is not assignable to field ({1}) of type ({2}).", defaultValue, name, typeof(T).FullName);
				throw new ArgumentException(error);
			}

			// determine the initializer that pushes the default value into the field at runtime
			if (!FieldInitializers.Default.TryGetValue(typeof(T), out mInitializer)) {
				string error = string.Format("Default value ({0}) not supported for field ({1}).", defaultValue, name);
				throw new NotSupportedException(error);
			}

			mVisibility = visibility;
			mInitialValueFactory = null;
			mDefaultValue = defaultValue;
			mIsStatic = isStatic;
			mName = name;

			if (string.IsNullOrWhiteSpace(mName)) {
				mName = (isStatic ? "s" : "m") + "X" + Guid.NewGuid().ToString("N");
			}
		}

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedField{T}"/> class (with factory callback).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="isStatic">true to emit a static field; false to emit a member field.</param>
		/// <param name="name">Name of the field (null to create an anonymous field).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="factory">Factory method that creates the object to assign to the field when the type is initialized.</param>
		internal GeneratedField(CodeGenEngine engine, bool isStatic, string name, Visibility visibility, Func<T> factory) :
			base(engine)
		{
			// ensure that the specified type is public and all nested types are public, too
			// => otherwise the dynamically created assembly is not able to access it
			CodeGenHelpers.CheckTypeIsTotallyPublic(typeof(T));

			mVisibility = visibility;
			mInitializer = null;
			mInitialValueFactory = factory;
			mDefaultValue = default(T);
			mIsStatic = isStatic;
			mName = name;

			if (string.IsNullOrWhiteSpace(mName)) {
				mName = (isStatic ? "s" : "m") + "X" + Guid.NewGuid().ToString("N");
			}
		}

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedField{T}"/> class (with custom initializer).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="isStatic">true to emit a static field; false to emit a member field.</param>
		/// <param name="name">Name of the field (null to create an anonymous field).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="initializer">Field initializer that emits code to intitialize the field.</param>
		internal GeneratedField(CodeGenEngine engine, bool isStatic, string name, Visibility visibility, FieldInitializer initializer) :
			base(engine)
		{
			// ensure that the specified type is public and all nested types are public, too
			// => otherwise the dynamically created assembly is not able to access it
			CodeGenHelpers.CheckTypeIsTotallyPublic(typeof(T));

			mVisibility = visibility;
			mInitializer = initializer;
			mInitialValueFactory = null;
			mDefaultValue = default(T);
			mIsStatic = isStatic;
			mName = name;

			if (string.IsNullOrWhiteSpace(mName)) {
				mName = (isStatic ? "s" : "m") + "X" + Guid.NewGuid().ToString("N");
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the name of the field.
		/// </summary>
		public string Name
		{
			get { return mName; }
		}

		/// <summary>
		/// Gets the type of the field.
		/// </summary>
		public Type Type
		{
			get { return typeof(T); }
		}

		/// <summary>
		/// Gets a value indicating whether the field is class variable (true) or a member variable (false).
		/// </summary>
		public bool IsStatic
		{
			get { return mIsStatic; }
		}

		/// <summary>
		/// Gets the access modifier of the field.
		/// </summary>
		public Visibility Visibility
		{
			get { return mVisibility; }
		}

		/// <summary>
		/// Gets a default value of the field (if any).
		/// </summary>
		object IGeneratedField.DefaultValue
		{
			get { return mDefaultValue; }
		}

		/// <summary>
		/// Gets a default value of the field (if any).
		/// </summary>
		public T DefaultValue
		{
			get { return mDefaultValue; }
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.FieldInfo"/> associated with the field.
		/// </summary>
		FieldInfo IField.FieldInfo
		{
			get { return mFieldBuilder; }
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.Emit.FieldBuilder"/> associated with the field.
		/// </summary>
		public FieldBuilder FieldBuilder
		{
			get { return mFieldBuilder; }
		}

		#endregion

		#region Internal Management

		/// <summary>
		/// Adds the method to the type builder.
		/// </summary>
		void IGeneratedFieldInternal.AddToTypeBuilder()
		{
			Debug.Assert(IsFrozen);

			if (mFieldBuilder == null)
			{
				if (mVisibility != Visibility.NotSpecified)
				{
					FieldAttributes flags = mVisibility.ToFieldAttributes();
					if (mIsStatic) flags |= FieldAttributes.Static;
					mFieldBuilder = Engine.TypeBuilder.DefineField(mName, typeof(T), flags);
				}
			}
		}

		/// <summary>
		/// Adds code to initialize the field with the specified default value (if any).
		/// </summary>
		/// <param name="msil">IL Generator attached to a constructor.</param>
		void IGeneratedFieldInternal.ImplementFieldInitialization(ILGenerator msil)
		{
			if (mInitializer != null)
			{
				// add loading the 'this' reference onto the evaluation stack (needed when storing to the field lateron)
				if (!mIsStatic) msil.Emit(OpCodes.Ldarg_0);

				// let initializer add the code needed to provide the object to store in the field
				mInitializer(msil, this); // leaves an object on the evaluation stack

				// add storing the object on the evaluation stack to the field
				if (mIsStatic)  msil.Emit(OpCodes.Stsfld, mFieldBuilder);
				else            msil.Emit(OpCodes.Stfld, mFieldBuilder);
			}
			else if (mInitialValueFactory != null)
			{
				// the field has a factory method that provides the initial value

				// add loading the 'this' reference onto the evaluation stack (needed when storing to the field lateron)
				if (!mIsStatic) msil.Emit(OpCodes.Ldarg_0);

				// put external factory callback into the collection of objects to pass along with the generated type
				int externalObjectIndex = Engine.ExternalObjects.Count;
				Engine.ExternalObjects.Add(mInitialValueFactory);

				// emit code to call the factory callback when the type is constructed.
				msil.Emit(OpCodes.Ldtoken, Engine.TypeBuilder);
				msil.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) }));
				msil.Emit(OpCodes.Call, typeof(CodeGenExternalStorage).GetMethod("Get"));
				msil.Emit(OpCodes.Ldc_I4, externalObjectIndex);
				msil.Emit(OpCodes.Ldelem, typeof(Func<T>));
				var invokeMethod = typeof(Func<T>).GetMethod("Invoke");
				msil.Emit(OpCodes.Call, invokeMethod);

				// add storing the object on the evaluation stack to the field
				if (mIsStatic)  msil.Emit(OpCodes.Stsfld, mFieldBuilder);
				else            msil.Emit(OpCodes.Stfld, mFieldBuilder);
			}
		}

		#endregion

	}
}
