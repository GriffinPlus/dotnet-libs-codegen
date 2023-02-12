﻿///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration.Demo.DuckCasting
{

	/// <summary>
	/// Interface of a dog.
	/// </summary>
	public interface IDog : IAnimal
	{
		/// <summary>
		/// Makes 'Woof!'.
		/// </summary>
		void Bark();
	}

}
