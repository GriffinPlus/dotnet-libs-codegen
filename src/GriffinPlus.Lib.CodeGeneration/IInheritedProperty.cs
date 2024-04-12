///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Untyped interface of an inherited property.
/// </summary>
public interface IInheritedProperty : IProperty
{
	/// <summary>
	/// Gets the 'get' accessor method
	/// (<c>null</c>, if the property does not have a 'get' accessor).
	/// </summary>
	new IInheritedMethod GetAccessor { get; }

	/// <summary>
	/// Gets the 'set' accessor method
	/// (<c>null</c>, if the property does not have a 'set' accessor).
	/// </summary>
	new IInheritedMethod SetAccessor { get; }

	/// <summary>
	/// Adds an override for the current property.
	/// </summary>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessor methods of the property.</param>
	/// <returns>The generated property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	IGeneratedProperty Override(IPropertyImplementation implementation);

	/// <summary>
	/// Adds an override for the current property. The <see cref="IProperty.Kind"/> property must be
	/// <see cref="PropertyKind.Abstract"/>, <see cref="PropertyKind.Virtual"/> or <see cref="PropertyKind.Override"/>.
	/// </summary>
	/// <param name="getAccessorImplementationCallback">A callback that implements the 'get' accessor method of the property.</param>
	/// <param name="setAccessorImplementationCallback">A callback that implements the 'set' accessor method of the property.</param>
	/// <returns>The generated property.</returns>
	/// <exception cref="ArgumentNullException">
	/// The property has a 'get' accessor, but <paramref name="getAccessorImplementationCallback"/> is <c>null</c>.<br/>
	/// -or-<br/>
	/// The property has a 'set' accessor, but <paramref name="setAccessorImplementationCallback"/> is <c>null</c>.
	/// </exception>
	IGeneratedProperty Override(
		PropertyAccessorImplementationCallback getAccessorImplementationCallback,
		PropertyAccessorImplementationCallback setAccessorImplementationCallback);
}
