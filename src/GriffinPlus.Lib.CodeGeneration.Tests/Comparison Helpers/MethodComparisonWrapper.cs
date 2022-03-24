///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	/// <summary>
	/// A wrapper that helps to compare <see cref="MethodInfo"/> and <see cref="IGeneratedMethod"/> instances
	/// (wrapping is necessary as the <see cref="MethodBuilder"/> in <see cref="IGeneratedMethod"/> is not functionally
	/// equivalent to <see cref="MethodInfo"/>, although it derives from it).
	/// </summary>
	readonly struct MethodComparisonWrapper : IEquatable<MethodComparisonWrapper>
	{
		public string     Name           { get; }
		public MethodKind Kind           { get; }
		public Visibility Visibility     { get; }
		public Type       ReturnType     { get; }
		public Type[]     ParameterTypes { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodComparisonWrapper"/> struct
		/// from a <see cref="MethodInfo"/> object.
		/// </summary>
		/// <param name="info">A <see cref="MethodInfo"/> object to initialize the wrapper with.</param>
		public MethodComparisonWrapper(MethodInfo info)
		{
			if (info != null)
			{
				Name = info.Name;
				Kind = info.ToMethodKind();
				Visibility = info.ToVisibility();
				ReturnType = info.ReturnType;
				ParameterTypes = info.GetParameters().Select(x => x.ParameterType).ToArray();
			}
			else
			{
				Name = null; // indicates "empty"
				Kind = MethodKind.Normal;
				Visibility = Visibility.Public;
				ReturnType = typeof(void);
				ParameterTypes = Type.EmptyTypes;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodComparisonWrapper"/> struct
		/// from an <see cref="IGeneratedMethod"/> compliant object.
		/// </summary>
		/// <param name="method">A <see cref="IGeneratedMethod"/> compliant object to initialize the wrapper with.</param>
		public MethodComparisonWrapper(IGeneratedMethod method)
		{
			if (method != null)
			{
				Name = method.Name;
				Kind = method.Kind;
				Visibility = method.Visibility;
				ReturnType = method.ReturnType;
				ParameterTypes = method.ParameterTypes.ToArray();
			}
			else
			{
				Name = null; // indicates "empty"
				Kind = MethodKind.Normal;
				Visibility = Visibility.Public;
				ReturnType = typeof(void);
				ParameterTypes = Type.EmptyTypes;
			}
		}

		/// <summary>
		/// Checks whether the method wrapper equals the specified one.
		/// </summary>
		/// <param name="other">Wrapper to compare with.</param>
		/// <returns>
		/// <c>true</c> if the two instances are equal; otherwise <c>false</c>.
		/// </returns>
		public bool Equals(MethodComparisonWrapper other)
		{
			if (Name != other.Name) return false;
			if (Kind != other.Kind) return false;
			if (Visibility != other.Visibility) return false;
			if (ReturnType != other.ReturnType) return false;
			if (!ParameterTypes.SequenceEqual(other.ParameterTypes)) return false;
			return true;
		}

		/// <summary>
		/// Checks whether the method wrapper equals the specified object.
		/// </summary>
		/// <param name="obj">Object to compare with.</param>
		/// <returns>
		/// <c>true</c> if the two instances are equal; otherwise <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != GetType()) return false;
			return Equals((MethodComparisonWrapper)obj);
		}

		/// <summary>
		/// Gets the hash code of the wrapped method.
		/// </summary>
		/// <returns>The hash code.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Name != null ? Name.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ Kind.GetHashCode();
				hashCode = (hashCode * 397) ^ Visibility.GetHashCode();
				hashCode = (hashCode * 397) ^ (ReturnType != null ? ReturnType.GetHashCode() : 0);
				foreach (var type in ParameterTypes)
				{
					hashCode = (hashCode * 397) ^ (type != null ? type.GetHashCode() : 0);
				}

				return hashCode;
			}
		}
	}

}
