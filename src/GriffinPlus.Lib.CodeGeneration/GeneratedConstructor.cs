///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// A generated constructor.
	/// </summary>
	class GeneratedConstructor : IGeneratedConstructorInternal, IEquatable<GeneratedConstructor>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedConstructor"/> class.
		/// </summary>
		/// <param name="typeDefinition">The type definition the constructor belongs to.</param>
		/// <param name="visibility">The visibility of the constructor.</param>
		/// <param name="parameterTypes">Types of parameters of the constructor to create.</param>
		/// <param name="baseClassConstructorCallImplementation">
		/// Callback that implements the call to a base class constructor if the type in creation derives from another type;
		/// <c>null</c> to call the parameterless constructor of the base class.
		/// </param>
		/// <param name="implementConstructorCallback">
		/// Callback that emits additional code to execute after the constructor of the base class has run;
		/// <c>null</c> to skip emitting additional code.
		/// </param>
		internal GeneratedConstructor(
			TypeDefinition                                 typeDefinition,
			Visibility                                     visibility,
			Type[]                                         parameterTypes,
			ConstructorBaseClassCallImplementationCallback baseClassConstructorCallImplementation,
			ConstructorImplementationCallback              implementConstructorCallback)
		{
			if (parameterTypes == null) throw new ArgumentNullException(nameof(parameterTypes));
			if (parameterTypes.Any(x => x == null)) throw new ArgumentException("List of parameter types contains a null reference.", nameof(parameterTypes));
			foreach (Type type in parameterTypes) CodeGenHelpers.EnsureTypeIsTotallyPublic(type);

			TypeDefinition = typeDefinition ?? throw new ArgumentNullException(nameof(typeDefinition));
			Visibility = visibility;
			ParameterTypes = new List<Type>(parameterTypes);
			BaseClassConstructorCallImplementationCallback = baseClassConstructorCallImplementation;
			ImplementConstructorCallback = implementConstructorCallback;

			// add the constructor to the type builder
			MethodAttributes flags = Visibility.ToMethodAttributes() | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
			ConstructorBuilder = TypeDefinition.TypeBuilder.DefineConstructor(flags, CallingConventions.HasThis, ParameterTypes.ToArray());
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.ConstructorInfo"/> associated with the constructor.
		/// </summary>
		public ConstructorInfo ConstructorInfo => ConstructorBuilder;

		/// <summary>
		/// Gets the visibility of the constructor.
		/// </summary>
		public Visibility Visibility { get; }

		/// <summary>
		/// Gets the types of the constructor parameters.
		/// </summary>
		public IEnumerable<Type> ParameterTypes { get; }

		/// <summary>
		/// Gets the type definition the constructor belongs to.
		/// </summary>
		public TypeDefinition TypeDefinition { get; }

		/// <summary>
		/// Gets the <see cref="System.Reflection.Emit.ConstructorBuilder"/> associated with the constructor.
		/// </summary>
		public ConstructorBuilder ConstructorBuilder { get; }

		/// <summary>
		/// Gets the callback that adds code to call a constructor of the base class of the type in creation
		/// (<c>null</c> to call the parameterless constructor of the base class).
		/// </summary>
		internal ConstructorBaseClassCallImplementationCallback BaseClassConstructorCallImplementationCallback { get; }

		/// <summary>
		/// Gets the callback that adds code to call a constructor of the base class of the type in creation
		/// (<c>null</c> to call the parameterless constructor of the base class).
		/// </summary>
		ConstructorBaseClassCallImplementationCallback IGeneratedConstructorInternal.BaseClassConstructorCallImplementationCallback => BaseClassConstructorCallImplementationCallback;

		/// <summary>
		/// Gets the callback that emits additional code to execute after the constructor of the base class has run;
		/// <c>null</c> to skip emitting additional code.
		/// </summary>
		internal ConstructorImplementationCallback ImplementConstructorCallback { get; }

		/// <summary>
		/// Gets the callback that emits additional code to execute after the constructor of the base class has run;
		/// <c>null</c> to skip emitting additional code.
		/// </summary>
		ConstructorImplementationCallback IGeneratedConstructorInternal.ImplementConstructorCallback => ImplementConstructorCallback;

		/// <summary>
		/// Checks whether the current object equals the specified one.
		/// Two constructor definitions are equal if both have the same visibility and share the same parameter types.
		/// The implementation callback is _not_ significant.
		/// </summary>
		/// <param name="other">Object to compare with.</param>
		/// <returns>
		/// <c>true</c> if the current object and the specified object are equal;
		/// otherwise <c>false</c>.
		/// </returns>
		public bool Equals(GeneratedConstructor other)
		{
			// two constructors are equal if their signature is the same
			if (other == null) return false;
			return Visibility == other.Visibility && ParameterTypes.SequenceEqual(other.ParameterTypes);
		}

		/// <summary>
		/// Checks whether the current object equals the specified one
		/// Two constructor definitions are equal if both have the same visibility and share the same parameter types.
		/// The implementation callback is _not_ significant.
		/// </summary>
		/// <param name="obj">Object to compare with.</param>
		/// <returns>
		/// <c>true</c> if the current object and the specified object are equal;
		/// otherwise <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(GeneratedConstructor)) return false;
			return Equals(obj as GeneratedConstructor);
		}

		/// <summary>
		/// Gets the hash code of the object.
		/// </summary>
		/// <returns>Hash code of the object.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Visibility.GetHashCode();
				foreach (var type in ParameterTypes) hashCode = (hashCode * 397) ^ type.GetHashCode();
				return hashCode;
			}
		}
	}

}
