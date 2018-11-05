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
	/// An inherited field.
	/// </summary>
	public class InheritedField : Member, IField
	{
		private readonly FieldInfo mFieldInfo;

		/// <summary>
		/// Intializes a new instance of the <see cref="InheritedField"/> class.
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="field">Field the type in creation has inherited.</param>
		internal InheritedField(CodeGenEngine engine, FieldInfo field) :
			base(engine)
		{
			mFieldInfo = field;
			Freeze();
		}

		/// <summary>
		/// Gets the name of the field.
		/// </summary>
		public string Name
		{
			get { return mFieldInfo.Name; }
		}

		/// <summary>
		/// Gets the type of the field.
		/// </summary>
		public Type Type
		{
			get { return mFieldInfo.FieldType; }
		}

		/// <summary>
		/// Gets a value indicating whether the field is class variable (true) or a member variable (false).
		/// </summary>
		public bool IsStatic
		{
			get { return mFieldInfo.IsStatic; }
		}

		/// <summary>
		/// Gets the access modifier of the field.
		/// </summary>
		public Visibility Visibility
		{
			get { return mFieldInfo.ToVisibility(); }
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.FieldInfo"/> associated with the field.
		/// </summary>
		public FieldInfo FieldInfo
		{
			get { return mFieldInfo; }
		}
	}
}
