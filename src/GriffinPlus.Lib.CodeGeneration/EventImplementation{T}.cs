///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Base class for event implementation strategies that implement add/remove accessor methods and the event raiser method, if any.
	/// </summary>
	/// <typeparam name="T">Type of the event handler delegate.</typeparam>
	public abstract class EventImplementation<T> : IEventImplementation<T> where T : Delegate
	{
		/// <summary>
		/// Gets the event raiser method (may be <c>null</c> if no event raiser method was added).
		/// </summary>
		public abstract IGeneratedMethod EventRaiserMethod { get; }

		/// <summary>
		/// Adds other fields, events, properties and methods to the definition of the type in creation.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">Event to implement.</param>
		public abstract void Declare(TypeDefinition typeDefinition, IGeneratedEvent<T> eventToImplement);

		/// <summary>
		/// Adds other fields, events, properties and methods to the definition of the type in creation.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">Event to implement.</param>
		void IEventImplementation.Declare(TypeDefinition typeDefinition, IGeneratedEvent eventToImplement)
		{
			Declare(typeDefinition, (IGeneratedEvent<T>)eventToImplement);
		}

		/// <summary>
		/// Implements the add accessor method of the event.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">The event the add accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the add accessor method to implement.</param>
		public abstract void ImplementAddAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedEvent<T> eventToImplement,
			ILGenerator        msilGenerator);

		/// <summary>
		/// Implements the add accessor method of the event.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">The event the add accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the add accessor method to implement.</param>
		void IEventImplementation.ImplementAddAccessorMethod(
			TypeDefinition  typeDefinition,
			IGeneratedEvent eventToImplement,
			ILGenerator     msilGenerator)
		{
			ImplementAddAccessorMethod(typeDefinition, (IGeneratedEvent<T>)eventToImplement, msilGenerator);
		}

		/// <summary>
		/// Implements the remove accessor method of the event.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">The event the remove accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the remove accessor method to implement.</param>
		public abstract void ImplementRemoveAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedEvent<T> eventToImplement,
			ILGenerator        msilGenerator);

		/// <summary>
		/// Implements the remove accessor method of the event.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">The event the remove accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the remove accessor method to implement.</param>
		void IEventImplementation.ImplementRemoveAccessorMethod(
			TypeDefinition  typeDefinition,
			IGeneratedEvent eventToImplement,
			ILGenerator     msilGenerator)
		{
			ImplementRemoveAccessorMethod(typeDefinition, (IGeneratedEvent<T>)eventToImplement, msilGenerator);
		}

		/// <summary>
		/// Implements the remove accessor method of the event.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">The event the raiser method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the event raiser method to implement.</param>
		public abstract void ImplementRaiserMethod(
			TypeDefinition     typeDefinition,
			IGeneratedEvent<T> eventToImplement,
			ILGenerator        msilGenerator);

		/// <summary>
		/// Implements the remove accessor method of the event.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="eventToImplement">The event the raiser method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the event raiser method to implement.</param>
		void IEventImplementation.ImplementRaiserMethod(
			TypeDefinition  typeDefinition,
			IGeneratedEvent eventToImplement,
			ILGenerator     msilGenerator)
		{
			ImplementRaiserMethod(typeDefinition, (IGeneratedEvent<T>)eventToImplement, msilGenerator);
		}
	}

}
