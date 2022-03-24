///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Untyped interface of an inherited event.
	/// </summary>
	public interface IInheritedEvent : IEvent
	{
		/// <summary>
		/// Gets the 'add' accessor method.
		/// </summary>
		new IInheritedMethod AddAccessor { get; }

		/// <summary>
		/// Gets the 'remove' accessor method.
		/// </summary>
		new IInheritedMethod RemoveAccessor { get; }

		/// <summary>
		/// Gets the <see cref="System.Reflection.EventInfo"/> associated with the event.
		/// </summary>
		EventInfo EventInfo { get; }
	}

}
