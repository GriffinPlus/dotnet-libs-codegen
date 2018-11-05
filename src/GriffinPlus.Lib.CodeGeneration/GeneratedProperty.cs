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
	/// A generated property.
	/// </summary>
	public class GeneratedProperty : Member, IProperty
	{
		#region Member Variables

		private string mName;
		private Type mType;
		private PropertyKind mKind;
		private PropertyBuilder mPropertyBuilder;
		private GeneratedMethod mGetAccessorMethod;
		private GeneratedMethod mSetAccessorMethod;
		private IPropertyImplementation mImplementation;
		#endregion

		#region Construction

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedProperty"/> class.
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="kind">Kind of property to generate.</param>
		/// <param name="type">Type of the property.</param>
		/// <param name="name">Name of the property (may be null).</param>
		/// <param name="implementation">Implementation strategy to use (may be null).</param>
		internal GeneratedProperty(
			CodeGenEngine engine,
			PropertyKind kind,
			Type type,
			string name,
			IPropertyImplementation implementation) :
				base(engine)
		{
			// check parameters
			if (type == null) throw new ArgumentNullException("type");

			// ensure that the specified type is public and all nested types are public, too
			// => otherwise the dynamically created assembly is not able to access it
			CodeGenHelpers.CheckTypeIsTotallyPublic(type);

			mName = name;
			mKind = kind;
			mType = type;

			if (mName == null || mName.Trim().Length == 0) {
				mName = "X" + Guid.NewGuid().ToString("N");
			}

			// declare the 'get' accessor method
			mGetAccessorMethod = engine.AddMethod(kind.ToMethodKind(), "get_" + name, type, Type.EmptyTypes, Visibility.Public);
			mGetAccessorMethod.AdditionalMethodAttributes = MethodAttributes.SpecialName | MethodAttributes.HideBySig;

			// declare the 'set' accessor method
			mSetAccessorMethod = engine.AddMethod(kind.ToMethodKind(), "set_" + name, typeof(void), new Type[] { type }, Visibility.Public);
			mSetAccessorMethod.AdditionalMethodAttributes = MethodAttributes.SpecialName | MethodAttributes.HideBySig;

			mImplementation = implementation;
			if (mImplementation != null) {
				mImplementation.Declare(engine, this);
			}
		}

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedProperty"/> class (for overrides).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="property">Inherited property to override.</param>
		/// <param name="implementation">Implementation strategy to use (may be null).</param>
		internal GeneratedProperty(
			CodeGenEngine engine,
			InheritedProperty property,
			IPropertyImplementation implementation) :
				base(engine)
		{
			// check parameters
			if (property == null) throw new ArgumentNullException("property");

			mName = property.Name;
			mKind = PropertyKind.Override;
			mType = property.Type;
			mGetAccessorMethod = engine.AddOverride(property.GetAccessor);
			mSetAccessorMethod = engine.AddOverride(property.SetAccessor);

			mImplementation = implementation;
			if (mImplementation != null) {
				mImplementation.Declare(engine, this);
			}

			// do not allow changes to overridden properties
			// (signature must match the signature of the inherited property)
			Freeze();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string Name
		{
			get { return mName; }
		}

		/// <summary>
		/// Gets or sets the type of the property.
		/// </summary>
		public Type Type
		{
			get { return mType; }
			set {
				CheckFrozen();
				if (value == null) throw new ArgumentNullException();
				CodeGenHelpers.CheckTypeIsTotallyPublic(value);
				mType = value;
			}
		}

		/// <summary>
		/// Gets a property kind indicating whether the property is static, virtual or abstract.
		/// </summary>
		public PropertyKind Kind
		{
			get { return mKind; }
			set {
				CheckFrozen();
				mKind = value;
			}
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.PropertyInfo"/> associated with the property.
		/// </summary>
		PropertyInfo IProperty.PropertyInfo
		{
			get { return mPropertyBuilder; }
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.Emit.PropertyBuilder"/> associated with the property.
		/// </summary>
		public PropertyBuilder PropertyBuilder
		{
			get { return mPropertyBuilder; }
		}

		/// <summary>
		/// Gets the 'get' accessor method.
		/// </summary>
		IMethod IProperty.GetAccessor
		{
			get { return mGetAccessorMethod; }
		}

		/// <summary>
		/// Gets the 'get' accessor method.
		/// </summary>
		public GeneratedMethod GetAccessor
		{
			get { return mGetAccessorMethod; }
		}

		/// <summary>
		/// Gets the 'set' accessor method.
		/// </summary>
		IMethod IProperty.SetAccessor
		{
			get { return mSetAccessorMethod; }
		}

		/// <summary>
		/// Gets the 'set' accessor method.
		/// </summary>
		public GeneratedMethod SetAccessor
		{
			get { return mSetAccessorMethod; }
		}

		#endregion

		#region Internal Management

		/// <summary>
		/// Is called when the property is about to be removed from the type in creation.
		/// </summary>
		internal void OnRemoving()
		{
			Debug.Assert(!IsFrozen);
			if (mImplementation != null) mImplementation.OnRemoving(Engine);
			Engine.RemoveMethod(mGetAccessorMethod);
			Engine.RemoveMethod(mSetAccessorMethod);
		}

		/// <summary>
		/// Adds the property and its accessor methods to the type builder.
		/// </summary>
		internal void AddToTypeBuilder()
		{
			Debug.Assert(IsFrozen);

			if (mPropertyBuilder == null)
			{
				mPropertyBuilder = Engine.TypeBuilder.DefineProperty(mName, PropertyAttributes.None, mType, Type.EmptyTypes);

				// create and link the 'get' accessor method
				mGetAccessorMethod.AddToTypeBuilder();
				if (mGetAccessorMethod.MethodBuilder != null) {
					mPropertyBuilder.SetGetMethod(mGetAccessorMethod.MethodBuilder);
				}

				// create and link the 'set' accessor method
				mSetAccessorMethod.AddToTypeBuilder();
				if (mSetAccessorMethod.MethodBuilder != null) {
					mPropertyBuilder.SetSetMethod(mSetAccessorMethod.MethodBuilder);
				}
			}
		}

		/// <summary>
		/// Calls the implementation strategy to add the MSIL for the get/set accessors.
		/// </summary>
		internal void Implement()
		{
			if (mImplementation != null) {
				mImplementation.Implement(Engine, this);
			}
		}

		#endregion
	}
}
