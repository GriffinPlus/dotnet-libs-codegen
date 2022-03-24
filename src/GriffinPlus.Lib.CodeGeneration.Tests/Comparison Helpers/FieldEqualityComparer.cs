///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	/// <summary>
	/// An equality comparer for <see cref="FieldInfo"/> that compares the most significant characteristics
	/// of a <see cref="FieldInfo"/> (field name, field type and field attributes).
	/// </summary>
	class FieldEqualityComparer : EqualityComparer<FieldInfo>
	{
		/// <summary>
		/// An immutable instance of the <see cref="FieldEqualityComparer"/> class.
		/// </summary>
		public static FieldEqualityComparer Instance { get; } = new FieldEqualityComparer();

		/// <summary>
		/// Checks whether the specified fields equal each other taking the equality criteria of the equality comparer into account.
		/// </summary>
		/// <param name="x">First field to compare.</param>
		/// <param name="y">Seconds field to compare.</param>
		/// <returns>
		/// <c>true</c> if the specified fields are equal;
		/// otherwise <c>false</c>.
		/// </returns>
		public override bool Equals(FieldInfo x, FieldInfo y)
		{
			if (x == null && y == null) return true;
			if (x == null || y == null) return false;
			if (x.Name != y.Name) return false;
			if (x.FieldType != y.FieldType) return false;
			if (x.Attributes != y.Attributes) return false;
			return true;
		}

		/// <summary>
		/// Gets a hash code for the specified field taking the equality criteria of the equality comparer into account.
		/// </summary>
		/// <param name="obj">Field to get the hash code for.</param>
		/// <returns>The hash code.</returns>
		public override int GetHashCode(FieldInfo obj)
		{
			return CalculateHashCode(obj);
		}

		/// <summary>
		/// Calculates a hash code for the specified field taking the equality criteria of the comparer into account.
		/// </summary>
		/// <param name="field">Field to get the hash code for.</param>
		/// <returns>The hash code.</returns>
		public static int CalculateHashCode(FieldInfo field)
		{
			unchecked
			{
				int hashCode = field.Name.GetHashCode();
				hashCode = (hashCode * 397) ^ field.FieldType.GetHashCode();
				hashCode = (hashCode * 397) ^ field.Attributes.GetHashCode();
				return hashCode;
			}
		}
	}

}
