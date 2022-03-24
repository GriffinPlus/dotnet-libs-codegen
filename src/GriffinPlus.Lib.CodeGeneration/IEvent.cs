///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Interface of inherited and generated events.
	/// </summary>
	public interface IEvent
	{
		/// <summary>
		/// Gets the name of the event.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the type of the event handler.
		/// </summary>
		Type EventHandlerType { get; }

		/// <summary>
		/// Gets the kind of the event.
		/// </summary>
		EventKind Kind { get; }

		/// <summary>
		/// Gets the access modifier of the event.
		/// </summary>
		Visibility Visibility { get; }

		/// <summary>
		/// Gets the 'add' accessor method.
		/// </summary>
		IMethod AddAccessor { get; }

		/// <summary>
		/// Gets the 'remove' accessor method.
		/// </summary>
		IMethod RemoveAccessor { get; }
	}

}
