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
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Diagnostics;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// A generated method.
	/// </summary>
	public class GeneratedMethod : Member, IMethod
	{
		#region Member Variables

		private readonly string mName;
		private MethodKind mKind;
		private Type mReturnType;
		private List<Type> mParameterTypes;
		private MethodBuilder mMethodBuilder;
		private Visibility mVisibility;
		private CallingConventions mCallingConvention;
		private MethodAttributes mMethodAttributes;
		private MethodAttributes mAdditionalMethodAttributes;

		#endregion

		#region Construction

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedMethod"/> class.
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="kind">Kind of method determining whether the method is static, abstract, virtual or an override.</param>
		/// <param name="name">Name of the method.</param>
		/// <param name="returnType">Return type of the method.</param>
		/// <param name="parameterTypes">Types of the method parameters.</param>
		/// <param name="visibility">Visibility of the method.</param>
		internal GeneratedMethod(
			CodeGenEngine engine,
			MethodKind kind,
			string name,
			Type returnType,
			Type[] parameterTypes,
			Visibility visibility) :
				base(engine)
		{
			// check parameters
			if (name != null) CheckIdentifier(name);
			if (returnType == null) throw new ArgumentNullException(nameof(returnType));
			CodeGenHelpers.CheckTypeIsTotallyPublic(returnType);
			if (parameterTypes == null) throw new ArgumentNullException(nameof(parameterTypes));
			if (parameterTypes.Any(x => x == null)) throw new ArgumentException("List of parameter types contains a null reference.");
			foreach (Type type in parameterTypes) CodeGenHelpers.CheckTypeIsTotallyPublic(type);

			// generate random name, if no name was specified
			if (name == null || name.Trim().Length == 0) {
				name = "X" + Guid.NewGuid().ToString("N");
			}

			mKind = kind;
			mName = name;
			mReturnType = returnType;
			mParameterTypes = new List<Type>(parameterTypes);
			mVisibility = visibility;
			mCallingConvention = kind.ToCallingConvention(false);
			UpdateEffectiveMethodAttributes();
		}

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedMethod"/> class (for overrides).
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="method">Inherited method to override.</param>
		internal GeneratedMethod(CodeGenEngine engine, InheritedMethod method) :
			base(engine)
		{
			if (method == null) throw new ArgumentNullException(nameof(method));

			mName = method.Name;
			mKind = MethodKind.Override;
			mReturnType = method.ReturnType;
			mParameterTypes = new List<Type>(method.ParameterTypes);
			mVisibility = method.Visibility;
			mCallingConvention = method.MethodInfo.CallingConvention;
			mMethodAttributes = method.MethodInfo.Attributes;
			mMethodAttributes &= ~MethodAttributes.Abstract;
			mMethodAttributes &= ~MethodAttributes.NewSlot;
			mMethodAttributes |= MethodAttributes.ReuseSlot;

			// do not allow changes to overridden methods
			// (signature must match the signature of the inherited method)
			Freeze(); 
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the name of the method.
		/// </summary>
		public string Name
		{
			get { return mName; }
		}

		/// <summary>
		/// Gets or sets the kind of the method.
		/// </summary>
		public MethodKind Kind
		{
			get { return mKind; }
			set {
				CheckFrozen();
				mKind = value;
				UpdateEffectiveMethodAttributes();
			}
		}

		/// <summary>
		/// Gets the return type of the method.
		/// </summary>
		public Type ReturnType
		{
			get { return mMethodBuilder.ReturnType; }
			set {
				CheckFrozen();
				CodeGenHelpers.CheckTypeIsTotallyPublic(value);
				mReturnType = value;
			}
		}

		/// <summary>
		/// Gets the parameter types of the method.
		/// </summary>
		public Type[] ParameterTypes
		{
			get { return mParameterTypes.ToArray(); }
			set {
				CheckFrozen();
				if (value == null) throw new ArgumentNullException();
				if (value.Any(x => x == null)) throw new ArgumentException("List of parameter types contains a null reference.");
				foreach (Type type in value) CodeGenHelpers.CheckTypeIsTotallyPublic(type);
				mParameterTypes = new List<Type>(value);
			}
		}

		/// <summary>
		/// Gets the access modifier of the method.
		/// </summary>
		public Visibility Visibility
		{
			get { return mVisibility; }
			set {
				CheckFrozen();
				mVisibility = value;
				UpdateEffectiveMethodAttributes();
			}
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.MethodInfo" /> associated with the method.
		/// </summary>
		MethodInfo IMethod.MethodInfo
		{
			get { return mMethodBuilder; }
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.Emit.MethodBuilder" /> associated with the method.
		/// </summary>
		public MethodBuilder MethodBuilder
		{
			get { return mMethodBuilder; }
		}

		/// <summary>
		/// Gets the method attributes.
		/// </summary>
		public MethodAttributes MethodAttributes
		{
			get { return mMethodAttributes; }
		}

		/// <summary>
		/// Gets or sets method attributes that are or'ed with the calculated method attributes.
		/// </summary>
		public MethodAttributes AdditionalMethodAttributes
		{
			get { return mAdditionalMethodAttributes; }
			internal set {
				CheckFrozen();
				mAdditionalMethodAttributes = value;
				UpdateEffectiveMethodAttributes();
			}
		}

		/// <summary>
		/// Gets or sets the calling convention.
		/// </summary>
		public CallingConventions CallingConvention
		{
			get { return mCallingConvention; }
			set {
				CheckFrozen();
				mCallingConvention = value;
			}
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Updates the <see cref="mMethodAttributes"/> field.
		/// </summary>
		private void UpdateEffectiveMethodAttributes()
		{
			mMethodAttributes = mVisibility.ToMethodAttributes() | mKind.ToMethodAttributes() | mAdditionalMethodAttributes;
		}

		#endregion

		#region Internal Management

		/// <summary>
		/// Adds the method to the type builder.
		/// </summary>
		internal void AddToTypeBuilder()
		{
			Debug.Assert(IsFrozen);

			if (mMethodBuilder == null)
			{
				if (mVisibility != Visibility.NotSpecified)
				{
					mMethodBuilder = Engine.TypeBuilder.DefineMethod(
						mName,
						mMethodAttributes,
						mCallingConvention,
						mReturnType,
						mParameterTypes.ToArray());
				}
			}
		}

		#endregion

	}
}
