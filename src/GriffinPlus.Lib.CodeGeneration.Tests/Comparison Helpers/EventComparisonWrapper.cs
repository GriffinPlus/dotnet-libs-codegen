///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	/// <summary>
	/// A wrapper that helps to compare <see cref="EventInfo"/> and <see cref="IGeneratedEvent"/> instances
	/// (wrapping is necessary as the <see cref="EventBuilder"/> in <see cref="IGeneratedEvent"/> does not derive from <see cref="EventInfo"/>).
	/// </summary>
	readonly struct EventComparisonWrapper : IEquatable<EventComparisonWrapper>
	{
		public string                  Name             { get; }
		public Type                    EventHandlerType { get; }
		public MethodComparisonWrapper AddAccessor      { get; }
		public MethodComparisonWrapper RemoveAccessor   { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EventComparisonWrapper"/> struct
		/// from an <see cref="EventInfo"/> object.
		/// </summary>
		/// <param name="info">A <see cref="EventInfo"/> object to initialize the wrapper with.</param>
		public EventComparisonWrapper(EventInfo info)
		{
			Name = info.Name;
			EventHandlerType = info.EventHandlerType;
			AddAccessor = new MethodComparisonWrapper(info.AddMethod);
			RemoveAccessor = new MethodComparisonWrapper(info.RemoveMethod);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EventComparisonWrapper"/> struct
		/// from an <see cref="IGeneratedEvent"/> compliant object.
		/// </summary>
		/// <param name="event">A <see cref="IGeneratedEvent"/> compliant object to initialize the wrapper with.</param>
		public EventComparisonWrapper(IGeneratedEvent @event)
		{
			Name = @event.Name;
			EventHandlerType = @event.EventHandlerType;
			AddAccessor = new MethodComparisonWrapper(@event.AddAccessor);
			RemoveAccessor = new MethodComparisonWrapper(@event.RemoveAccessor);
		}

		/// <summary>
		/// Checks whether the event wrapper equals the specified one.
		/// </summary>
		/// <param name="other">Wrapper to compare with.</param>
		/// <returns>
		/// <c>true</c> if the two instances are equal;<br/>
		/// otherwise <c>false</c>.
		/// </returns>
		public bool Equals(EventComparisonWrapper other)
		{
			if (Name != other.Name) return false;
			if (EventHandlerType != other.EventHandlerType) return false;
			return AddAccessor.Equals(other.AddAccessor) && RemoveAccessor.Equals(other.RemoveAccessor);
		}

		/// <summary>
		/// Checks whether the event wrapper equals the specified object.
		/// </summary>
		/// <param name="obj">Object to compare with.</param>
		/// <returns>
		/// <c>true</c> if the two instances are equal;<br/>
		/// otherwise <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj.GetType() == GetType() && Equals((EventComparisonWrapper)obj);
		}

		/// <summary>
		/// Gets the hash code of the wrapped method.
		/// </summary>
		/// <returns>The hash code.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Name != null ? Name.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ (EventHandlerType != null ? EventHandlerType.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ AddAccessor.GetHashCode();
				hashCode = (hashCode * 397) ^ RemoveAccessor.GetHashCode();
				return hashCode;
			}
		}
	}

}
