///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration.Demo.DuckCasting
{

	/// <summary>
	/// Base class to use for the duck casting example.
	/// </summary>
	public class Chimera
	{
		/// <summary>
		/// Gets the name of the animal.
		/// </summary>
		public string Name => "Chimera";

		/// <summary>
		/// Makes 'Quack!'.
		/// </summary>
		public void Quack()
		{
			Console.WriteLine("Quack!");
		}

		/// <summary>
		/// Makes 'Woof!'.
		/// </summary>
		public void Bark()
		{
			Console.WriteLine("Woof!");
		}

		/// <summary>
		/// Makes 'Meow!'.
		/// </summary>
		public void Meow()
		{
			Console.WriteLine("Meow!");
		}

		/// <summary>
		/// Just walk.
		/// </summary>
		public void Walk()
		{
			Console.WriteLine("Walking!");
		}
	}

}
