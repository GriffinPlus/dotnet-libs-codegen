///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// An equality comparer for <see cref="ConstructorInfo"/> that compares the most significant characteristics
/// of a <see cref="ConstructorInfo"/> (method attributes and parameter types).
/// </summary>
class ConstructorEqualityComparer : EqualityComparer<ConstructorInfo>
{
	/// <summary>
	/// An immutable instance of the <see cref="ConstructorEqualityComparer"/> class.
	/// </summary>
	public static ConstructorEqualityComparer Instance { get; } = new();

	/// <summary>
	/// Checks whether the specified constructors equal each other taking the equality
	/// criteria of the equality comparer into account.
	/// </summary>
	/// <param name="x">First constructor to compare.</param>
	/// <param name="y">Seconds constructor to compare.</param>
	/// <returns>
	/// <c>true</c> if the specified constructors are equal;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public override bool Equals(ConstructorInfo x, ConstructorInfo y)
	{
		if (x == null && y == null) return true;
		if (x == null || y == null) return false;
		if (x.Attributes != y.Attributes) return false;
		IEnumerable<Type> xParameters = x.GetParameters().Select(parameter => parameter.ParameterType);
		IEnumerable<Type> yParameters = y.GetParameters().Select(parameter => parameter.ParameterType);
		return xParameters.SequenceEqual(yParameters);
	}

	/// <summary>
	/// Gets a hash code for the specified constructor taking the equality criteria of the equality comparer into account.
	/// </summary>
	/// <param name="obj">Constructor to get the hash code for.</param>
	/// <returns>The hash code.</returns>
	public override int GetHashCode(ConstructorInfo obj)
	{
		return CalculateHashCode(obj);
	}

	/// <summary>
	/// Calculates a hash code for the specified constructor taking the equality criteria of the comparer into account.
	/// </summary>
	/// <param name="constructor">Constructor to get the hash code for.</param>
	/// <returns>The hash code.</returns>
	public static int CalculateHashCode(ConstructorInfo constructor)
	{
		unchecked
		{
			int hashCode = constructor.Attributes.GetHashCode();

			return constructor
				.GetParameters()
				.Select(parameter => parameter.ParameterType)
				.Aggregate(
					hashCode,
					(current, type) => (current * 397) ^ type.GetHashCode());
		}
	}
}
