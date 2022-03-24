///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Interface of a generated method.
	/// </summary>
	public interface IGeneratedMethod : IMethod
	{
		/// <summary>
		/// Gets the <see cref="System.Reflection.Emit.MethodBuilder"/> associated with the method.
		/// </summary>
		MethodBuilder MethodBuilder { get; }
	}

}
