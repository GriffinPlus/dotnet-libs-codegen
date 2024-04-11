///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
using System.Windows;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Untyped interface of a generated dependency property.
	/// </summary>
	public interface IGeneratedDependencyProperty : IDependencyProperty, IInitialValueProvider
	{
		/// <summary>
		/// Gets the static field storing the registered dependency property (can be of type <see cref="DependencyProperty"/>
		/// (<see cref="IDependencyProperty.IsReadOnly"/> is <c>false</c>) or <see cref="DependencyPropertyKey"/> (<see cref="IDependencyProperty.IsReadOnly"/>
		/// is <c>true</c>).
		/// </summary>
		IGeneratedField DependencyPropertyField { get; }

		/// <summary>
		/// Gets the accessor property associated with the dependency property.
		/// (may be <c>null</c>, call <see cref="AddAccessorProperty"/> to add the accessor property).
		/// </summary>
		IGeneratedProperty AccessorProperty { get; }

		/// <summary>
		/// Adds a regular property accessing the dependency property.
		/// </summary>
		/// <param name="name">Name of the property (<c>null</c> to use the name of the dependency property).</param>
		/// <param name="getAccessorVisibility">Visibility of the 'get' accessor of the property to create.</param>
		/// <param name="setAccessorVisibility">Visibility of the 'set' accessor of the property to create.</param>
		/// <returns>The added accessor property.</returns>
		IGeneratedProperty AddAccessorProperty(
			string     name = null,
			Visibility getAccessorVisibility = Visibility.Public,
			Visibility setAccessorVisibility = Visibility.Public);
	}

}

#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
// Dependency properties are not supported on .NET Standard and .NET5/6/7/8 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif
