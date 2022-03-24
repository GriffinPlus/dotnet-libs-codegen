///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Property kind specifying whether a property is static, virtual, abstract or an override of a virtual/abstract property.
	/// </summary>
	public enum PropertyKind
	{
		/// <summary>
		/// The property is a static.
		/// </summary>
		Static,

		/// <summary>
		/// The property is a normal instance property, i.e. not abstract, virtual or an override.
		/// </summary>
		Normal,

		/// <summary>
		/// The property is virtual.
		/// </summary>
		Virtual,

		/// <summary>
		/// The property is abstract.
		/// </summary>
		Abstract,

		/// <summary>
		/// The property is an override of an inherited virtual or abstract property.
		/// </summary>
		Override
	}

}
