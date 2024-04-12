///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Typed interface of an inherited property.
/// </summary>
/// <typeparam name="T">Type of the property.</typeparam>
public interface IInheritedProperty<T> : IInheritedProperty
{
	/// <summary>
	/// Adds an override for the current property. The <see cref="IProperty.Kind"/> property must be
	/// <see cref="PropertyKind.Abstract"/>, <see cref="PropertyKind.Virtual"/> or <see cref="PropertyKind.Override"/>.
	/// </summary>
	/// <param name="implementation">Implementation strategy that implements the 'get'/'set' accessor methods of the property.</param>
	/// <returns>The generated property.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
	new IGeneratedProperty<T> Override(IPropertyImplementation implementation);

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
	new IGeneratedProperty<T> Override(
		PropertyAccessorImplementationCallback getAccessorImplementationCallback,
		PropertyAccessorImplementationCallback setAccessorImplementationCallback);
}
