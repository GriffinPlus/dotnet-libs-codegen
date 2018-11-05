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

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// A method that provides an implementation returning an object on the evaluation stack to store in a generated field.
	/// </summary>
	/// <param name="msil">MSIL generator to use.</param>
	/// <param name="field">Field to implement the method for.</param>
	public delegate void FieldInitializer(ILGenerator msil, GeneratedField field);

	/// <summary>
	/// A generated field.
	/// </summary>
	public class GeneratedField : Member, IField
	{
		#region Member Variables

		private string mName;
		private Type mType;
		private bool mIsStatic;
		private Visibility mVisibility;
		private FieldBuilder mFieldBuilder;
		private readonly FieldInitializer mInitializer;
		private object mDefaultValue;

		#endregion

		#region Class Initialization

		private readonly static Dictionary<Type,FieldInitializer> sDefaultValueInitializers;

		/// <summary>
		/// Initializes the <see cref="GeneratedField"/> class.
		/// </summary>
		static GeneratedField()
		{
			sDefaultValueInitializers = new Dictionary<Type, FieldInitializer>();

			sDefaultValueInitializers.Add(typeof(SByte),   (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(SByte)field.mDefaultValue);  });
			sDefaultValueInitializers.Add(typeof(Byte),    (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(Byte)field.mDefaultValue);   });
			sDefaultValueInitializers.Add(typeof(Int16),   (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(Int16)field.mDefaultValue);  });
			sDefaultValueInitializers.Add(typeof(UInt16),  (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(UInt16)field.mDefaultValue); });
			sDefaultValueInitializers.Add(typeof(Int32),   (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)field.mDefaultValue);         });
			sDefaultValueInitializers.Add(typeof(UInt32),  (msil,field) => { msil.Emit(OpCodes.Ldc_I4, (Int32)(UInt32)field.mDefaultValue); });
			sDefaultValueInitializers.Add(typeof(Int64),   (msil,field) => { msil.Emit(OpCodes.Ldc_I8, (Int64)field.mDefaultValue);         });
			sDefaultValueInitializers.Add(typeof(UInt64),  (msil,field) => { msil.Emit(OpCodes.Ldc_I8, (Int64)(UInt64)field.mDefaultValue); });
			sDefaultValueInitializers.Add(typeof(Single),  (msil,field) => { msil.Emit(OpCodes.Ldc_R4, (Single)field.mDefaultValue);        });
			sDefaultValueInitializers.Add(typeof(Double),  (msil,field) => { msil.Emit(OpCodes.Ldc_R8, (Double)field.mDefaultValue);        });
		}

		#endregion

		#region Construction

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedField"/> class (without default value).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="isStatic">true to emit a static field; false to emit a member field.</param>
		/// <param name="type">Type of the field.</param>
		/// <param name="name">Name of the field (null to create an anonymous field).</param>
		/// <param name="visibility">Visibility of the field.</param>
		internal GeneratedField(CodeGenEngine engine, bool isStatic, Type type, string name, Visibility visibility) :
			this(engine, isStatic, type, name, visibility, null)
		{

		}

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedField"/> class (with default value).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="isStatic">true to emit a static field; false to emit a member field.</param>
		/// <param name="type">Type of the field.</param>
		/// <param name="name">Name of the field (null to create an anonymous field).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="defaultValue">Default value of the field.</param>
		internal GeneratedField(CodeGenEngine engine, bool isStatic, Type type, string name, Visibility visibility, object defaultValue) :
			base(engine)
		{
			// ensure that the specified type is public and all nested types are public, too
			// => otherwise the dynamically created assembly is not able to access it
			CodeGenHelpers.CheckTypeIsTotallyPublic(type);

			// ensure that the specified default value is of the same type as the field
			if (!type.IsAssignableFrom(defaultValue.GetType())) {
				string error = string.Format("Default value ({0}) is not assignable to field ({1}) of type ({2}).", defaultValue, name, type.FullName);
				throw new ArgumentException(error);
			}

			// determine the initializer that pushes the default value into the field at runtime
			if (!sDefaultValueInitializers.TryGetValue(type, out mInitializer)) {
				string error = string.Format("Default value ({0}) not supported for field ({1}).", defaultValue, name);
				throw new NotSupportedException(error);
			}

			mVisibility = visibility;
			mDefaultValue = defaultValue;
			mIsStatic = isStatic;
			mType = type;
			mName = name;

			if (string.IsNullOrWhiteSpace(mName)) {
				mName = (isStatic ? "s" : "m") + "X" + Guid.NewGuid().ToString("N");
			}
		}

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedField"/> class (with custom initializer).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="isStatic">true to emit a static field; false to emit a member field.</param>
		/// <param name="type">Type of the field.</param>
		/// <param name="name">Name of the field (null to create an anonymous field).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="initializer">Field initializer that emits code to intitialize the field.</param>
		internal GeneratedField(CodeGenEngine engine, bool isStatic, Type type, string name, Visibility visibility, FieldInitializer initializer) :
			base(engine)
		{
			// ensure that the specified type is public and all nested types are public, too
			// => otherwise the dynamically created assembly is not able to access it
			CodeGenHelpers.CheckTypeIsTotallyPublic(type);

			mVisibility = visibility;
			mInitializer = initializer;
			mDefaultValue = null;
			mIsStatic = isStatic;
			mType = type;
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
			get { return mType; }
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
		public object DefaultValue
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
		internal void AddToTypeBuilder()
		{
			Debug.Assert(IsFrozen);

			if (mFieldBuilder == null)
			{
				if (mVisibility != Visibility.NotSpecified)
				{
					FieldAttributes flags = mVisibility.ToFieldAttributes();
					if (mIsStatic) flags |= FieldAttributes.Static;
					mFieldBuilder = Engine.TypeBuilder.DefineField(mName, mType, flags);
				}
			}
		}

		/// <summary>
		/// Adds code to initialize the field with the specified default value (if any).
		/// </summary>
		/// <param name="msil">IL Generator attached to a constructor.</param>
		internal void ImplementFieldInitialization(ILGenerator msil)
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
		}

		#endregion

	}
}
