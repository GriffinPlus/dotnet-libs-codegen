///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Event kind specifying whether an event (respectively its add/remove accessor) is static, normal, virtual, abstract or an override.
	/// </summary>
	public enum EventKind
	{
		/// <summary>
		/// The event is static.
		/// </summary>
		Static,

		/// <summary>
		/// The event is a normal instance event, i.e. not abstract, virtual or an override.
		/// </summary>
		Normal,

		/// <summary>
		/// The event is virtual.
		/// </summary>
		Virtual,

		/// <summary>
		/// The event is abstract.
		/// </summary>
		Abstract,

		/// <summary>
		/// The event  is an override of an inherited abstract or virtual event.
		/// </summary>
		Override
	}

}
