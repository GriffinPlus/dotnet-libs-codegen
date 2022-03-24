///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Typed interface of event implementation strategies that implement add/remove accessor methods and the event raiser method, if any.
	/// </summary>
	/// <typeparam name="T">Type of the event handler delegate.</typeparam>
	public interface IEventImplementation<T> : IEventImplementation where T : Delegate
	{
		/// <summary>
		/// Adds other fields, events, properties and methods to the definition of the type in creation.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">Event to implement.</param>
		void Declare(TypeDefinition typeDefinition, IGeneratedEvent<T> eventToImplement);

		/// <summary>
		/// Implements the add accessor method of the event.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">The event the add accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the add accessor method to implement.</param>
		void ImplementAddAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedEvent<T> eventToImplement,
			ILGenerator        msilGenerator);

		/// <summary>
		/// Implements the remove accessor method of the event.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">The event the remove accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the remove accessor method to implement.</param>
		void ImplementRemoveAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedEvent<T> eventToImplement,
			ILGenerator        msilGenerator);

		/// <summary>
		/// Implements the event raiser method of the event, if any.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">The event the raiser method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the event raiser method to implement.</param>
		void ImplementRaiserMethod(
			TypeDefinition     typeDefinition,
			IGeneratedEvent<T> eventToImplement,
			ILGenerator        msilGenerator);
	}

}
