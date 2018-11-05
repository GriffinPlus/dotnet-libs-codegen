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
using System.Linq;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// An inherited method.
	/// </summary>
	public class InheritedMethod : Member, IMethod
	{
		private MethodInfo mMethodInfo;
		private Visibility mAccessModifier;
		private Type[] mParameterTypes;

		/// <summary>
		/// Intializes a new instance of the <see cref="InheritedMethod"/> class.
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="method">Method the type in creation has inherited.</param>
		internal InheritedMethod(CodeGenEngine engine, MethodInfo method) : base(engine)
		{
			mMethodInfo = method;
			mAccessModifier = mMethodInfo.ToVisibility();
			mParameterTypes = mMethodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
			Freeze();
		}

		/// <summary>
		/// Gets the name of the method.
		/// </summary>
		public string Name
		{
			get { return mMethodInfo.Name; }
		}

		/// <summary>
		/// Gets the kind of the method.
		/// </summary>
		public MethodKind Kind
		{
			get { return mMethodInfo.ToMethodKind(); }
		}

		/// <summary>
		/// Gets the return type of the method.
		/// </summary>
		public Type ReturnType
		{
			get { return mMethodInfo.ReturnType; }
		}

		/// <summary>
		/// Gets the parameter types of the method.
		/// </summary>
		public Type[] ParameterTypes
		{
			get { return mParameterTypes; }
		}

		/// <summary>
		/// Gets the access modifier of the method.
		/// </summary>
		public Visibility Visibility
		{
			get { return mAccessModifier; }
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.MethodInfo" /> associated with the method.
		/// </summary>
		public MethodInfo MethodInfo
		{
			get { return mMethodInfo; }
		}

		/// <summary>
		/// Adds an override for the current method.
		/// </summary>
		/// <returns>The generated method.</returns>
		public GeneratedMethod Override()
		{
			return Engine.AddOverride(this);
		}
	}
}
