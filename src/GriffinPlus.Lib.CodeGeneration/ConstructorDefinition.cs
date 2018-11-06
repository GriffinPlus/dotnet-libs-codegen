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
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Delegate for a callback that implements a call to the base class constructor, if the type in creation derived from another type.
	/// </summary>
	/// <param name="constructorDefinition">Definition of the constructor that needs to emit code for calling a constructor of its base class.</param>
	/// <param name="typeBuilder">Type builder creating the requested type.</param>
	/// <param name="msil">IL code generator to use.</param>
	public delegate void ImplementBaseClassConstructorCallCallback(ConstructorDefinition constructorDefinition, TypeBuilder typeBuilder, ILGenerator msil);

	/// <summary>
	/// Information about a constructor to create.
	/// </summary>
	public partial class ConstructorDefinition : IEquatable<ConstructorDefinition>
	{
		private readonly static ConstructorDefinition sDefaultConstructor = new ConstructorDefinition(Visibility.Public, Type.EmptyTypes, null);
		private readonly static SignatureEqualityComparer sSignatureEqualityComparer = new SignatureEqualityComparer();
		private readonly Visibility mAccessModifier;
		private readonly List<Type> mParameterTypes;
		private readonly int mHashCode;
		private readonly ImplementBaseClassConstructorCallCallback mImplementBaseClassConstructorCallCallback;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConstructorDefinition"/> class.
		/// </summary>
		/// <param name="accessModifier">Access modifier determining the visibility of the constructor.</param>
		/// <param name="parameterTypes">Types of the parameters of the constructor to create.</param>
		/// <param name="implementBaseClassConstructorCall">
		/// Callback that implements the call to a base class constructor, if the type under construction derives from another type;
		/// null to call the parameterless constructor of the base class.
		/// </param>
		public ConstructorDefinition(Visibility accessModifier, Type[] parameterTypes, ImplementBaseClassConstructorCallCallback implementBaseClassConstructorCall = null)
		{
			if (parameterTypes == null) throw new ArgumentNullException(nameof(parameterTypes));

			mAccessModifier = accessModifier;
			mParameterTypes = new List<Type>(parameterTypes);
			mImplementBaseClassConstructorCallCallback = implementBaseClassConstructorCall;
			
			// calculate the hash code of the constructor definition
			// => use the hash codes of the constructor parameter types as these types identify the constructor uniquely
			mHashCode = mAccessModifier.GetHashCode();
			foreach (var type in mParameterTypes)
			{
				mHashCode ^= type.GetHashCode();
			}
		}

		/// <summary>
		/// Gets the definition of a default constructor that calls the default constructor of the base class,
		/// if the type in creation derives from another type. The default constructor is public. If some other
		/// visibility is needed, you need to create a definition on your own.
		/// </summary>
		public static ConstructorDefinition Default
		{
			get { return sDefaultConstructor; }
		}

		/// <summary>
		/// Gets an equality comparer that compares the parameters of the constructor only.
		/// It does not take the visibility or the implementation callback into account.
		/// </summary>
		public static EqualityComparer<ConstructorDefinition> SignatureEquality
		{
			get { return sSignatureEqualityComparer; }
		}

		/// <summary>
		/// Gets the visibility of the constructor.
		/// </summary>
		public Visibility AccessModifier
		{
			get { return mAccessModifier; }
		}

		/// <summary>
		/// Gets the types of the constructor parameters.
		/// </summary>
		public IEnumerable<Type> ParameterTypes
		{
			get { return mParameterTypes; }
		}

		/// <summary>
		/// Gets the callback method that adds code to call a constructor of the base class of the type in creation
		/// (null to call the parameterless constructor of the base class)
		/// </summary>
		public ImplementBaseClassConstructorCallCallback ImplementBaseClassConstructorCallCallback
		{
			get { return mImplementBaseClassConstructorCallCallback; }
		}

		/// <summary>
		/// Checks whether the current object equals the specified one.
		/// Two constructor definitions are equal, if both have the same visibility and share the same parameter types
		/// The implementation callback is _not_ significant.
		/// </summary>
		/// <param name="other">Object to compare with.</param>
		/// <returns>true, if the current object and the specified object are equal; otherwise false.</returns>
		public bool Equals(ConstructorDefinition other)
		{
			// two constructors are equal, if their signature is the same
			if (other == null) return false;
			return mAccessModifier == other.mAccessModifier && mParameterTypes.SequenceEqual(other.mParameterTypes);
		}

		/// <summary>
		/// Checks whether the current object equals the specified one
		/// Two constructor definitions are equal, if both have the same visibility and share the same parameter types
		/// The implementation callback is _not_ significant.
		/// </summary>
		/// <param name="obj">Object to compare with.</param>
		/// <returns>true, if the current object and the specified object are equal; otherwise false.</returns>
		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(ConstructorDefinition)) return false;
			return Equals(obj as ConstructorDefinition);
		}

		/// <summary>
		/// Gets the hash code of the object.
		/// </summary>
		/// <returns>Hash code of the object.</returns>
		public override int GetHashCode()
		{
			return mHashCode;
		}
	}
}
