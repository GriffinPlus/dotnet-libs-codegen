///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Collection of predefined property implementation strategies.
	/// </summary>
	public static class PropertyImplementations
	{
		/// <summary>
		/// Property implementation that simply backs the property with an anonymous field.
		/// </summary>
		public static PropertyImplementation_Simple Simple => new PropertyImplementation_Simple();

		/// <summary>
		/// Property implementation that backs the property with a field and calls the 'OnPropertyChanged' method when the
		/// property value changes. The 'OnPropertyChanged' method must be public, protected or protected internal and have
		/// the following signature: <c>void OnPropertyChanged(string name)</c>.
		/// </summary>
		public static PropertyImplementation_SetterWithPropertyChanged SetterWithPropertyChanged => new PropertyImplementation_SetterWithPropertyChanged();

#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
		/// <summary>
		/// Property implementation that provides access to the specified dependency property.
		/// </summary>
		/// <param name="dp">Dependency property to access via the property.</param>
		public static PropertyImplementation_DependencyProperty DependencyPropertyAccessor(IGeneratedDependencyProperty dp)
		{
			return new PropertyImplementation_DependencyProperty(dp);
		}
#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
		// Dependency properties are not supported on .NET Standard and .NET5/6/7/8 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif
	}

}
