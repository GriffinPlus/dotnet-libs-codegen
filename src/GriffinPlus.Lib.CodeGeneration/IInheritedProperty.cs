///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Untyped interface of an inherited property.
	/// </summary>
	public interface IInheritedProperty : IProperty
	{
		/// <summary>
		/// Gets the 'get' accessor method
		/// (<c>null</c> if the property does not have a 'get' accessor).
		/// </summary>
		new IInheritedMethod GetAccessor { get; }

		/// <summary>
		/// Gets the 'set' accessor method
		/// (<c>null</c> if the property does not have a 'set' accessor).
		/// </summary>
		new IInheritedMethod SetAccessor { get; }
	}

}
