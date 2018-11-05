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

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// An inherited property.
	/// </summary>
	public class InheritedProperty : Member, IProperty
	{
		private PropertyInfo mPropertyInfo;
		private InheritedMethod mGetAccessorMethod;
		private InheritedMethod mSetAccessorMethod;

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedProperty"/> class.
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="property">Property the type in creation has inherited.</param>
		internal InheritedProperty(CodeGenEngine engine, PropertyInfo property) :
			base(engine)
		{
			mPropertyInfo = property;

			// init get accessor method
			MethodInfo getAccessor = mPropertyInfo.GetGetMethod(true);
			if (getAccessor != null) {
				mGetAccessorMethod = new InheritedMethod(engine, getAccessor);
			}

			// init set accessor method
			MethodInfo setAccessor = mPropertyInfo.GetSetMethod(true);
			if (setAccessor != null) {
				mSetAccessorMethod = new InheritedMethod(engine, setAccessor);
			}

			Freeze();
		}

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string Name
		{
			get { return mPropertyInfo.Name; }
		}

		/// <summary>
		/// Gets the type of the property.
		/// </summary>
		public Type Type
		{
			get { return mPropertyInfo.PropertyType; }
		}

		/// <summary>
		/// Gets a property kind indicating whether the property is static, virtual or abstract.
		/// </summary>
		public PropertyKind Kind
		{
			get {
				if (mGetAccessorMethod != null) return mGetAccessorMethod.MethodInfo.ToPropertyKind();
				if (mSetAccessorMethod != null) return mSetAccessorMethod.MethodInfo.ToPropertyKind();
				throw new CodeGenException("Neither a 'get' nor a 'set' accessor is defined on the type."); // should never occur...
			}
		}

		/// <summary>
		/// Gets the 'get' accessor method.
		/// </summary>
		public InheritedMethod GetAccessor
		{
			get { return mGetAccessorMethod; }
		}

		/// <summary>
		/// Gets the 'get' accessor method.
		/// </summary>
		IMethod IProperty.GetAccessor
		{
			get { return mGetAccessorMethod; }
		}

		/// <summary>
		/// Gets the 'set' accessor method.
		/// </summary>
		public InheritedMethod SetAccessor
		{
			get { return mSetAccessorMethod; }
		}

		/// <summary>
		/// Gets the 'set' accessor method.
		/// </summary>
		IMethod IProperty.SetAccessor
		{
			get { return mSetAccessorMethod; }
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.PropertyInfo"/> associated with the property.
		/// </summary>
		public PropertyInfo PropertyInfo
		{
			get { return mPropertyInfo; }
		}

		/// <summary>
		/// Adds an override for the current property.
		/// </summary>
		/// <param name="implementation">
		/// Implementation strategy to use (may be null to skip implementation and add it lateron using
		/// the <see cref="GeneratedProperty.PropertyBuilder"/> property).
		/// </param>
		/// <returns>The generated property.</returns>
		public GeneratedProperty Override(IPropertyImplementation implementation)
		{
			return Engine.AddOverride(this, implementation);
		}

	}
}

