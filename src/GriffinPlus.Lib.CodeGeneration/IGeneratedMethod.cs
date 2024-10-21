///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Interface of a generated method.
/// </summary>
public interface IGeneratedMethod : IMethod, IGeneratedMember
{
	/// <summary>
	/// Gets the additional attributes of the method.
	/// </summary>
	MethodAttributes AdditionalAttributes { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.MethodBuilder"/> associated with the method.
	/// </summary>
	MethodBuilder MethodBuilder { get; }

	/// <summary>
	/// Gets the implementation strategy used to implement the method
	/// (<c>null</c>, if implementation callbacks are used).
	/// </summary>
	IMethodImplementation Implementation { get; }
}
