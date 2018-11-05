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
	/// Interface for inherited and generated methods.
	/// </summary>
	public interface IMethod
	{
		/// <summary>
		/// Gets the name of the method.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the kind of the method.
		/// </summary>
		MethodKind Kind { get; }

		/// <summary>
		/// Gets the return type of the method.
		/// </summary>
		Type ReturnType { get; }

		/// <summary>
		/// Gets the parameter types of the method.
		/// </summary>
		Type[] ParameterTypes { get; }

		/// <summary>
		/// Gets the access modifier of the method.
		/// </summary>
		Visibility Visibility { get; }

		/// <summary>
		/// Gets the <see cref="System.Reflection.MethodInfo" /> associated with the method.
		/// </summary>
		MethodInfo MethodInfo { get; }
	}
}