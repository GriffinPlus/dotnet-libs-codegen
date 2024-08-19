///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Untyped interface of a generated event.
/// </summary>
public interface IGeneratedEvent : IEvent, IGeneratedMember
{
	/// <summary>
	/// Gets the implementation strategy used to implement the event
	/// (<c>null</c>, if implementation callbacks are used).
	/// </summary>
	IEventImplementation Implementation { get; }

	/// <summary>
	/// Gets the <see cref="System.Reflection.Emit.EventBuilder"/> associated with the event.
	/// </summary>
	EventBuilder EventBuilder { get; }

	/// <summary>
	/// Gets the 'add' accessor method.
	/// </summary>
	new IGeneratedMethod AddAccessor { get; }

	/// <summary>
	/// Gets the 'remove' accessor method.
	/// </summary>
	new IGeneratedMethod RemoveAccessor { get; }
}
