///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	/// <summary>
	/// A wrapper that helps to compare <see cref="PropertyInfo"/> and <see cref="IGeneratedProperty"/> instances
	/// (wrapping is necessary as the <see cref="PropertyBuilder"/> in <see cref="IGeneratedProperty"/> is not functionally
	/// equivalent to <see cref="PropertyInfo"/>, although it derives from it).
	/// </summary>
	readonly struct PropertyComparisonWrapper
	{
		public string                  Name         { get; }
		public Type                    PropertyType { get; }
		public MethodComparisonWrapper GetAccessor  { get; }
		public MethodComparisonWrapper SetAccessor  { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyComparisonWrapper"/> struct.
		/// </summary>
		/// <param name="info">A <see cref="PropertyInfo"/> object to initialize the wrapper with.</param>
		public PropertyComparisonWrapper(PropertyInfo info)
		{
			Name = info.Name;
			PropertyType = info.PropertyType;
			GetAccessor = new MethodComparisonWrapper(info.GetMethod);
			SetAccessor = new MethodComparisonWrapper(info.SetMethod);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyComparisonWrapper"/> struct.
		/// </summary>
		/// <param name="property">A <see cref="IGeneratedProperty"/> compliant object to initialize the wrapper with.</param>
		public PropertyComparisonWrapper(IGeneratedProperty property)
		{
			Name = property.Name;
			PropertyType = property.PropertyType;
			GetAccessor = new MethodComparisonWrapper(property.GetAccessor);
			SetAccessor = new MethodComparisonWrapper(property.SetAccessor);
		}

		/// <summary>
		/// Checks whether the property wrapper equals the specified one.
		/// </summary>
		/// <param name="other">Wrapper to compare with.</param>
		/// <returns>
		/// <c>true</c> if the two instances are equal; otherwise <c>false</c>.
		/// </returns>
		public bool Equals(PropertyComparisonWrapper other)
		{
			if (Name != other.Name) return false;
			if (PropertyType != other.PropertyType) return false;
			if (!GetAccessor.Equals(other.GetAccessor)) return false;
			if (!SetAccessor.Equals(other.SetAccessor)) return false;
			return true;
		}

		/// <summary>
		/// Checks whether the property wrapper equals the specified object.
		/// </summary>
		/// <param name="obj">Object to compare with.</param>
		/// <returns>
		/// <c>true</c> if the two instances are equal; otherwise <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != GetType()) return false;
			return Equals((PropertyComparisonWrapper)obj);
		}

		/// <summary>
		/// Gets the hash code of the property.
		/// </summary>
		/// <returns>The hash code.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Name != null ? Name.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ (PropertyType != null ? PropertyType.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ GetAccessor.GetHashCode();
				hashCode = (hashCode * 397) ^ SetAccessor.GetHashCode();
				return hashCode;
			}
		}
	}

}
