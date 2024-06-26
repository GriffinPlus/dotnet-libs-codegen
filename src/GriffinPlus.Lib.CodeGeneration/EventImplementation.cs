﻿///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Base class for event implementation strategies that implement add/remove accessor methods and the event raiser method, if any.
/// </summary>
public abstract class EventImplementation : IEventImplementation
{
	/// <summary>
	/// Gets the event raiser method (<c>null</c>, if no event raiser method was added).
	/// </summary>
	public abstract IGeneratedMethod EventRaiserMethod { get; }

	/// <summary>
	/// Adds other fields, events, properties and methods to the definition of the type in creation.
	/// </summary>
	/// <param name="typeDefinition">Definition of the type in creation.</param>
	/// <param name="eventToImplement">Event to implement.</param>
	public abstract void Declare(TypeDefinition typeDefinition, IGeneratedEvent eventToImplement);

	/// <summary>
	/// Implements the add accessor method of the event.
	/// </summary>
	/// <param name="typeDefinition">Definition of the type in creation.</param>
	/// <param name="eventToImplement">The event the add accessor method to implement belongs to.</param>
	/// <param name="msilGenerator">MSIL generator attached to the add accessor method to implement.</param>
	public abstract void ImplementAddAccessorMethod(
		TypeDefinition  typeDefinition,
		IGeneratedEvent eventToImplement,
		ILGenerator     msilGenerator);

	/// <summary>
	/// Implements the remove accessor method of the event.
	/// </summary>
	/// <param name="typeDefinition">Definition of the type in creation.</param>
	/// <param name="eventToImplement">The event the remove accessor method to implement belongs to.</param>
	/// <param name="msilGenerator">MSIL generator attached to the remove accessor method to implement.</param>
	public abstract void ImplementRemoveAccessorMethod(
		TypeDefinition  typeDefinition,
		IGeneratedEvent eventToImplement,
		ILGenerator     msilGenerator);

	/// <summary>
	/// Implements the raiser method of the event.
	/// </summary>
	/// <param name="typeDefinition">Definition of the type in creation.</param>
	/// <param name="eventToImplement">The event the raiser method to implement belongs to.</param>
	/// <param name="msilGenerator">MSIL generator attached to the event raiser method to implement.</param>
	public abstract void ImplementRaiserMethod(
		TypeDefinition  typeDefinition,
		IGeneratedEvent eventToImplement,
		ILGenerator     msilGenerator);
}
