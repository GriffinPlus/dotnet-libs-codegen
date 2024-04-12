///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// An equality comparer only taking the signature of the constructor (its parameter types) into account.
/// </summary>
public class ConstructorSignatureEqualityComparer : EqualityComparer<IConstructor>
{
	/// <summary>
	/// Gets an equality comparer that compares the parameters of the constructor only.
	/// It does not take the visibility or the implementation callback into account.
	/// </summary>
	public static ConstructorSignatureEqualityComparer Instance { get; } = new();

	/// <summary>
	/// Checks whether the specified constructor definitions equal each other taking the equality
	/// criteria of the equality comparer into account.
	/// </summary>
	/// <param name="x">First constructor definition to compare.</param>
	/// <param name="y">Seconds constructor definition to compare.</param>
	/// <returns>
	/// <c>true</c> if the specified constructor definitions are equal;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public override bool Equals(IConstructor x, IConstructor y)
	{
		if (x == null && y == null) return true;
		if (x == null || y == null) return false;
		return x.ParameterTypes.SequenceEqual(y.ParameterTypes);
	}

	/// <summary>
	/// Gets a hash code for the specified constructor definition taking the equality criteria of the
	/// equality comparer into account.
	/// </summary>
	/// <param name="obj">Constructor definition to get the hash code for.</param>
	/// <returns>The hash code.</returns>
	public override int GetHashCode(IConstructor obj)
	{
		return CalculateHashCode(obj);
	}

	/// <summary>
	/// Calculates a hash code for the specified constructor definition taking the equality criteria of the
	/// comparer into account.
	/// </summary>
	/// <param name="generatedConstructor">Constructor definition to get the hash code for.</param>
	/// <returns>The hash code.</returns>
	public static int CalculateHashCode(IConstructor generatedConstructor)
	{
		unchecked
		{
			return generatedConstructor
				.ParameterTypes
				.Aggregate(
					0,
					(current, type) => (current * 397) ^ type.GetHashCode());
		}
	}
}
