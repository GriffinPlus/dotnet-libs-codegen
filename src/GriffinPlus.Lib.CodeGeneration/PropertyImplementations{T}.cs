///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Collection of predefined property implementation strategies.
	/// </summary>
	/// <typeparam name="T">Type of the property to implement.</typeparam>
	public static class PropertyImplementations<T>
	{
		/// <summary>
		/// Property implementation that simply backs the property with an anonymous field.
		/// </summary>
		public static PropertyImplementation_Simple<T> Simple => new PropertyImplementation_Simple<T>();

		/// <summary>
		/// Property implementation that backs the property with a field and calls the 'OnPropertyChanged' method when the
		/// property value changes. The 'OnPropertyChanged' method must be public, protected or protected internal and have
		/// the following signature: <c>void OnPropertyChanged(string name)</c>.
		/// </summary>
		public static PropertyImplementation_SetterWithPropertyChanged<T> SetterWithPropertyChanged => new PropertyImplementation_SetterWithPropertyChanged<T>();

#if NET461 || NET5_0 && WINDOWS
		/// <summary>
		/// Property implementation that provides access to the specified dependency property.
		/// </summary>
		/// <param name="dp">Dependency property to access via the property.</param>
		public static PropertyImplementation_DependencyProperty<T> DependencyPropertyAccessor(IGeneratedDependencyProperty<T> dp)
		{
			return new PropertyImplementation_DependencyProperty<T>(dp);
		}
#elif NETSTANDARD2_0 || NETSTANDARD2_1 // Dependency properties are not supported on .NET Standard...
#else
#error Unhandled Target Framework.
#endif
	}

}
