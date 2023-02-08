///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Typed interface of a generated event.
	/// </summary>
	/// <typeparam name="T">Type of the event handler delegate.</typeparam>
	// ReSharper disable once UnusedTypeParameter
	public interface IGeneratedEvent<T> : IGeneratedEvent where T : Delegate { }

}
