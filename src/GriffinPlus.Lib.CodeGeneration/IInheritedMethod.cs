///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Interface of an inherited method.
	/// </summary>
	public interface IInheritedMethod : IMethod
	{
		/// <summary>
		/// Adds an override for the current method. The <see cref="IMethod.Kind"/> property must be
		/// <see cref="MethodKind.Abstract"/>, <see cref="MethodKind.Virtual"/> or <see cref="MethodKind.Override"/>.
		/// </summary>
		/// <param name="implementation">Implementation strategy that implements the method.</param>
		/// <returns>The generated method.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
		IGeneratedMethod Override<T>(IMethodImplementation implementation);

		/// <summary>
		/// Adds an override for the current method. The <see cref="IMethod.Kind"/> property must be
		/// <see cref="MethodKind.Abstract"/>, <see cref="MethodKind.Virtual"/> or <see cref="MethodKind.Override"/>.
		/// </summary>
		/// <param name="implementationCallback">A callback that implements the method.</param>
		/// <returns>The generated method.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="implementationCallback"/> is <c>null</c>.</exception>
		IGeneratedMethod Override<T>(MethodImplementationCallback implementationCallback);
	}

}
