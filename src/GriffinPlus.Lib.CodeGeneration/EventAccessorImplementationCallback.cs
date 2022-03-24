///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// A callback that implements the add/remove accessor method of the specified event.
	/// </summary>
	/// <param name="event">The event the accessor method to implement belongs to.</param>
	/// <param name="msilGenerator">MSIL generator attached to the add/remove accessor method to implement.</param>
	public delegate void EventAccessorImplementationCallback<T>(IGeneratedEvent<T> @event, ILGenerator msilGenerator) where T : Delegate;

}
