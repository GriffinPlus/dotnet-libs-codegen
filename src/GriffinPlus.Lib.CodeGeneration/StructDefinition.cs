///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Definition of a struct to create dynamically.
	/// </summary>
	public sealed class StructDefinition : TypeDefinition
	{
		/// <summary>
		/// Initializes a new definition of a struct.
		/// </summary>
		/// <param name="name">Name of the struct to create (<c>null</c> to create a random name).</param>
		public StructDefinition(string name = null) : base(null, true, name) { }

		/// <summary>
		/// Initializes a new definition of a struct
		/// (associates the type definition with the specified module definition, for internal use only).
		/// </summary>
		/// <param name="module">Module definition to associate the class definition with.</param>
		/// <param name="name">Name of the struct to create (<c>null</c> to create a random name).</param>
		internal StructDefinition(ModuleDefinition module, string name) : base(module, true, name) { }
	}

}
