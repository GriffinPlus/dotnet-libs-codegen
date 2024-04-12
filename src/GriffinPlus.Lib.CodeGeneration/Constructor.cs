///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// A constructor.
/// </summary>
[DebuggerDisplay("Declaring Type = {ConstructorInfo.DeclaringType.FullName}, Constructor Info = {ConstructorInfo}")]
class Constructor : IConstructor, IEquatable<Constructor>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Constructor"/> class.
	/// </summary>
	/// <param name="constructorInfo">The <see cref="ConstructorInfo"/> describing the constructor.</param>
	internal Constructor(ConstructorInfo constructorInfo)
	{
		ConstructorInfo = constructorInfo;
		foreach (Type type in ParameterTypes) CodeGenHelpers.EnsureTypeIsTotallyPublic(type);
	}

	/// <summary>
	/// Gets the <see cref="System.Reflection.ConstructorInfo"/> associated with the constructor.
	/// </summary>
	public ConstructorInfo ConstructorInfo { get; }

	/// <summary>
	/// Gets the visibility of the constructor.
	/// </summary>
	public Visibility Visibility => ConstructorInfo.ToVisibility();

	/// <summary>
	/// Gets the types of the constructor parameters.
	/// </summary>
	public IEnumerable<Type> ParameterTypes => ConstructorInfo.GetParameters().Select(x => x.ParameterType).ToArray();

	/// <summary>
	/// Checks whether the current object equals the specified one.
	/// </summary>
	/// <param name="other">Object to compare with.</param>
	/// <returns>
	/// <c>true</c> if the current object and the specified object are equal;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public bool Equals(Constructor other)
	{
		// two constructors are equal if their signature is the same
		if (other == null) return false;
		return ConstructorInfo == other.ConstructorInfo;
	}

	/// <summary>
	/// Checks whether the current object equals the specified one.
	/// </summary>
	/// <param name="obj">Object to compare with.</param>
	/// <returns>
	/// <c>true</c> if the current object and the specified object are equal;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public override bool Equals(object obj)
	{
		if (obj == null || obj.GetType() != typeof(Constructor)) return false;
		return Equals(obj as Constructor);
	}

	/// <summary>
	/// Gets the hash code of the constructor.
	/// </summary>
	/// <returns>Hash code of the constructor.</returns>
	public override int GetHashCode()
	{
		return ConstructorInfo.GetHashCode();
	}
}
