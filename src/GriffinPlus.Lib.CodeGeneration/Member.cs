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

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Base class for fields, events, properties and methods in the code generation engine.
	/// </summary>
	public class Member : IMember
	{
		private readonly CodeGenEngine mEngine;
		private bool mIsFrozen;

		/// <summary>
		/// Intializes a new instance of the <see cref="Member"/> class.
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		internal Member(CodeGenEngine engine)
		{
			mEngine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		/// <summary>
		/// Gets the code generation engine.
		/// </summary>
		internal CodeGenEngine Engine
		{
			get { return mEngine; }
		}

		/// <summary>
		/// Gets a value indicating whether the member is frozen.
		/// </summary>
		public bool IsFrozen
		{
			get { return mIsFrozen; }
		}

		/// <summary>
		/// Freezes the member, so it cannot be modified lateron.
		/// </summary>
		public virtual void Freeze()
		{
			mIsFrozen = true;
		}

		/// <summary>
		/// Throws an exception, if the member is frozen.
		/// </summary>
		protected void CheckFrozen()
		{
			if (mIsFrozen) throw new InvalidOperationException("The member is frozen and must not be modified.");
		}

		/// <summary>
		/// Checks whether the specified identifier is valid and throws an exception, if it violates the naming constraints.
		/// </summary>
		/// <param name="identifier">Identifier to check.</param>
		internal static void CheckIdentifier(string identifier)
		{
			if (identifier == null) throw new ArgumentNullException(nameof(identifier), "The identifier must not be a null reference.");
			if (!System.CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(identifier)) {
				string error = string.Format("'{0}' is not a valid identifier.", identifier);
				throw new ArgumentException(error);
			}
		}
	}
}
