///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Untyped interface of a generated property.
	/// </summary>
	public interface IGeneratedProperty : IProperty
	{
		/// <summary>
		/// Gets the <see cref="System.Reflection.Emit.PropertyBuilder"/> associated with the field.
		/// </summary>
		PropertyBuilder PropertyBuilder { get; }

		/// <summary>
		/// Gets the 'get' accessor method
		/// (may be <c>null</c> if the property does not have a 'get' accessor)
		/// </summary>
		new IGeneratedMethod GetAccessor { get; }

		/// <summary>
		/// Gets the 'set' accessor method
		/// (may be <c>null</c> if the property does not have a 'set' accessor)
		/// </summary>
		new IGeneratedMethod SetAccessor { get; }

		/// <summary>
		/// Gets the implementation strategy used to implement the property
		/// (may be <c>null</c> if implementation callbacks are used).
		/// </summary>
		IPropertyImplementation Implementation { get; }
	}

}
