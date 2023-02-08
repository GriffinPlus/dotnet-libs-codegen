///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
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

		/// <summary>
		/// Adds a 'get' accessor method to the property.
		/// </summary>
		/// <param name="visibility">Visibility of the 'get' accessor to add.</param>
		/// <param name="getAccessorImplementationCallback">
		/// A callback that implements the 'get' accessor method of the property
		/// (<c>null</c> to let the implementation strategy implement the method if specified when adding the property).
		/// </param>
		/// <returns>The added 'get' accessor method.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="getAccessorImplementationCallback"/> is <c>null</c> and the property was created without an implementation strategy.
		/// </exception>
		/// <exception cref="InvalidOperationException">The 'get' accessor method was already added to the property.</exception>
		IGeneratedMethod AddGetAccessor(
			Visibility                             visibility                        = Visibility.Public,
			PropertyAccessorImplementationCallback getAccessorImplementationCallback = null);

		/// <summary>
		/// Adds a 'set' accessor method to the property.
		/// </summary>
		/// <param name="visibility">Visibility of the 'set' accessor to add.</param>
		/// <param name="setAccessorImplementationCallback">
		/// A callback that implements the 'set' accessor method of the property
		/// (<c>null</c> to let the implementation strategy implement the method if specified when adding the property).
		/// </param>
		/// <returns>The added 'set' accessor method.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="setAccessorImplementationCallback"/> is <c>null</c> and the property was created without an implementation strategy.
		/// </exception>
		/// <exception cref="InvalidOperationException">The 'set' accessor method was already added to the property.</exception>
		IGeneratedMethod AddSetAccessor(
			Visibility                             visibility                        = Visibility.Public,
			PropertyAccessorImplementationCallback setAccessorImplementationCallback = null);
	}

}
