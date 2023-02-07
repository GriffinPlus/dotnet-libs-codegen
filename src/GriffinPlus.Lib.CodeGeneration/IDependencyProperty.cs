///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if NET461 || (NET5_0 || NET6_0 || NET7_0) && WINDOWS
using System;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Interface of inherited and generated dependency properties.
	/// </summary>
	public interface IDependencyProperty
	{
		/// <summary>
		/// Gets the type of the property.
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a value indicating whether the dependency property is read-only (true) or read-write (false).
		/// </summary>
		bool IsReadOnly { get; }
	}

}

#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0
// Dependency properties are not supported on .NET Standard and .NET5/6/7 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif
