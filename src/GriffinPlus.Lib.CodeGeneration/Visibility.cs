///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Visibility of type members (constructors, fields, events, properties and methods).
	/// </summary>
	public enum Visibility
	{
		/// <summary>
		/// The associated member is 'public', i.e. it can be accessed from anywhere.
		/// </summary>
		Public,

		/// <summary>
		/// The associated member is 'protected', i.e. it can be accessed from within the declaring type or types deriving from it.
		/// </summary>
		Protected,

		/// <summary>
		/// The associated member is 'protected internal', i.e. it can be accessed from within the declaring type, types deriving from it or from within the same
		/// assembly.
		/// </summary>
		ProtectedInternal,

		/// <summary>
		/// The associated member is 'internal', i.e. it can be accessed from within the same assembly only.
		/// </summary>
		Internal,

		/// <summary>
		/// The associated member is 'private', i.e. it can be accessed from within the declaring type only.
		/// </summary>
		Private
	}

}
