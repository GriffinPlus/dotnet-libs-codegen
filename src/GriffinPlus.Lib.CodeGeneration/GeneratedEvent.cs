///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://griffin.plus)
//
// Copyright 2018 Sascha Falk <sascha@falk-online.eu>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed
// on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for
// the specific language governing permissions and limitations under the License.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// A generated event.
	/// </summary>
	public class GeneratedEvent : Member, IEvent
	{
		#region Member Variables

		private string mName;
		private Type mType;
		private EventKind mKind;
		private GeneratedMethod mAddAccessorMethod;
		private GeneratedMethod mRemoveAccessorMethod;
		private GeneratedMethod mRaiserMethod;
		private EventBuilder mEventBuilder;
		private IEventImplementation mImplementation;

		#endregion

		#region Construction

		/// <summary>
		/// Intializes a new instance of the <see cref="GeneratedEvent"/> class.
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="kind">Kind of the event.</param>
		/// <param name="eventType">Type of the event (must be a delegate type).</param>
		/// <param name="eventName">Name of the event (may be null).</param>
		/// <param name="visibility">Access modifier defining the visibility of the event.</param>
		/// <param name="eventRaiserName">Name of the event raiser method (may be null).</param>
		/// <param name="implementation">Implementation strategy to use (may be null).</param>
		internal GeneratedEvent(
			CodeGenEngine engine,
			EventKind kind,
			Type eventType,
			string eventName,
			Visibility visibility,
			string eventRaiserName,
			IEventImplementation implementation) :
				base(engine)
		{
			if (!typeof(Delegate).IsAssignableFrom(eventType))
			{
				string error = string.Format("The specified type ({0}) is not a delegate type and can therefore not be used for an event.", eventType.FullName);
				throw new ArgumentException(error);
			}

			// ensure that the specified event handler type is public and all nested types are public, too
			// => otherwise the dynamically created assembly is not able to access it
			CodeGenHelpers.CheckTypeIsTotallyPublic(eventType);

			mKind = kind;
			mType = eventType;
			mName = eventName;

			if (mName == null || mName.Trim().Length == 0)
			{
				mName = "X" + Guid.NewGuid().ToString("N");
			}

			if (string.IsNullOrWhiteSpace(eventRaiserName))
			{
				eventRaiserName = "On" + mName;
			}

			// add 'add' accessor method
			mAddAccessorMethod = Engine.AddMethod(mKind.ToMethodKind(), "add_" + mName, typeof(void), new Type[] { mType }, visibility);
			mAddAccessorMethod.AdditionalMethodAttributes = MethodAttributes.SpecialName | MethodAttributes.HideBySig;

			// add 'remove' accessor method
			mRemoveAccessorMethod = Engine.AddMethod(mKind.ToMethodKind(), "remove_" + mName, typeof(void), new Type[] { mType }, visibility);
			mRemoveAccessorMethod.AdditionalMethodAttributes = MethodAttributes.SpecialName | MethodAttributes.HideBySig;

			// add event raiser method
			MethodKind raiserMethodKind = mKind == EventKind.Static ? MethodKind.Static : MethodKind.Normal;
			Type[] raiserParameterTypes;
			if (mType == typeof(EventHandler) || mType == typeof(EventHandler<EventArgs>))
			{
				// System.EventHandler
				// System.EventHandler<EventArgs>
				raiserParameterTypes = Type.EmptyTypes;
			}
			else if (mType.IsGenericType && mType.GetGenericTypeDefinition() == typeof(EventHandler<>))
			{
				// EventHandler<T> with T derived from System.EventArgs
				raiserParameterTypes = mType.GetGenericArguments();
			}
			else if (typeof(Delegate).IsAssignableFrom(mType))
			{
				MethodInfo invokeMethod = mType.GetMethod("Invoke");
				raiserParameterTypes = invokeMethod.GetParameters().Select(x => x.ParameterType).ToArray();
			}
			else
			{
				throw new NotSupportedException("The event type is not supported.");
			}
			mRaiserMethod = Engine.AddMethod(raiserMethodKind, eventRaiserName, typeof(void), raiserParameterTypes, Visibility.Protected);

			// call implementation strategy
			mImplementation = implementation;
			if (mImplementation != null)
			{
				mImplementation.Declare(engine, this);
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the name of the event.
		/// </summary>
		public string Name
		{
			get { return mName; }
		}

		/// <summary>
		/// Gets the type of the event.
		/// </summary>
		public Type Type
		{
			get { return mType; }
		}

		/// <summary>
		/// Gets the kind of the event.
		/// </summary>
		public EventKind Kind
		{
			get { return mKind; }
		}

		/// <summary>
		/// Gets or sets the visibility of the event.
		/// </summary>
		public Visibility Visibility
		{
			get
			{
				Debug.Assert(mAddAccessorMethod.Visibility == mRemoveAccessorMethod.Visibility);
				return mAddAccessorMethod.Visibility;
			}
			set
			{
				CheckFrozen();
				mAddAccessorMethod.Visibility = value;
				mRemoveAccessorMethod.Visibility = value;
			}
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.Emit.EventBuilder"/> that was used to create the event.
		/// </summary>
		public EventBuilder EventBuilder
		{
			get { return mEventBuilder; }
		}

		/// <summary>
		/// Gets the 'add' accessor method.
		/// </summary>
		IMethod IEvent.AddAccessor
		{
			get { return mAddAccessorMethod; }
		}

		/// <summary>
		/// Gets the 'add' accessor method.
		/// </summary>
		public GeneratedMethod AddAccessor
		{
			get { return mAddAccessorMethod; }
		}

		/// <summary>
		/// Gets the 'remove' accessor method.
		/// </summary>
		IMethod IEvent.RemoveAccessor
		{
			get { return mRemoveAccessorMethod; }
		}

		/// <summary>
		/// Gets the 'remove' accessor method.
		/// </summary>
		public GeneratedMethod RemoveAccessor
		{
			get { return mRemoveAccessorMethod; }
		}

		/// <summary>
		/// Gets the event raiser method.
		/// </summary>
		public GeneratedMethod Raiser
		{
			get { return mRaiserMethod; }
		}

		#endregion

		#region Internal Management

		/// <summary>
		/// Is called when the event is about to be removed from the type in creation.
		/// </summary>
		internal void OnRemoving()
		{
			if (mImplementation != null) mImplementation.OnRemoving(Engine);
			Engine.RemoveMethod(mAddAccessorMethod);
			Engine.RemoveMethod(mRemoveAccessorMethod);
			Engine.RemoveMethod(mRaiserMethod);
		}

		/// <summary>
		/// Adds the property and its accessor methods to the type builder.
		/// </summary>
		internal void AddToTypeBuilder()
		{
			Debug.Assert(IsFrozen);

			if (mEventBuilder == null)
			{
				mAddAccessorMethod.AddToTypeBuilder();
				mRemoveAccessorMethod.AddToTypeBuilder();
				mRaiserMethod.AddToTypeBuilder();

				mEventBuilder = Engine.TypeBuilder.DefineEvent(mName, EventAttributes.None, mType);
				mEventBuilder.SetAddOnMethod(mAddAccessorMethod.MethodBuilder);
				mEventBuilder.SetRemoveOnMethod(mRemoveAccessorMethod.MethodBuilder);
			}
		}

		/// <summary>
		/// Adds code to implement the add/remove accessor methods.
		/// </summary>
		internal void Implement()
		{
			if (mImplementation != null)
			{
				mImplementation.Implement(Engine, this);
			}
		}

		#endregion

	}
}

