///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Typed interface of a generated dependency property.
	/// </summary>
	/// <typeparam name="T">Type of the dependency property.</typeparam>
	public interface IGeneratedDependencyProperty<T> : IGeneratedDependencyProperty
	{
		/// <summary>
		/// Gets the initial value of the dependency property (if any).
		/// The <see cref="IInitialValueProvider.HasInitialValue"/> property determines whether this property contains a valid initial value.
		/// </summary>
		new T InitialValue { get; }

		/// <summary>
		/// Gets the accessor property associated with the dependency property.
		/// (may be <c>null</c>, call <see cref="AddAccessorProperty"/> to add the accessor property).
		/// </summary>
		new IGeneratedProperty<T> AccessorProperty { get; }

		/// <summary>
		/// Adds a regular property accessing the dependency property.
		/// </summary>
		/// <param name="name">Name of the property (<c>null</c> to use the name of the dependency property).</param>
		/// <param name="getAccessorVisibility">Visibility of the 'get' accessor of the property to create.</param>
		/// <param name="setAccessorVisibility">Visibility of the 'set' accessor of the property to create.</param>
		/// <returns>The added accessor property.</returns>
		new IGeneratedProperty<T> AddAccessorProperty(
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
