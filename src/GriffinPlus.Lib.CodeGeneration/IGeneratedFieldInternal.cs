///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Internal interface of a generated field.
	/// </summary>
	interface IGeneratedFieldInternal : IGeneratedField
	{
		/// <summary>
		/// Adds code to initialize the field with the specified default value (if any).
		/// </summary>
		/// <param name="createdType">The created type.</param>
		/// <param name="msilGenerator">IL Generator attached to a constructor.</param>
		void ImplementFieldInitialization(Type createdType, ILGenerator msilGenerator);
	}

}
