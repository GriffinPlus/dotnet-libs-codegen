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
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// An inherited event.
	/// </summary>
	public class InheritedEvent : Member, IEvent
	{
		private readonly EventInfo mEventInfo;
		private readonly InheritedMethod mAddAccessor;
		private readonly InheritedMethod mRemoveAccessor;

		/// <summary>
		/// Intializes a new instance of the <see cref="InheritedEvent"/> class.
		/// </summary>
		/// <param name="engine">The code generation engine.</param>
		/// <param name="eventInfo">Event the type in creation has inherited.</param>
		internal InheritedEvent(CodeGenEngine engine, EventInfo eventInfo) :
			base(engine)
		{
			mEventInfo = eventInfo;
			mAddAccessor = new InheritedMethod(engine, mEventInfo.GetAddMethod(true));
			mRemoveAccessor = new InheritedMethod(engine, mEventInfo.GetRemoveMethod(true));
			Freeze();
		}

		/// <summary>
		/// Gets the name of the event.
		/// </summary>
		public string Name
		{
			get { return mEventInfo.Name; }
		}

		/// <summary>
		/// Gets the type of the event.
		/// </summary>
		public Type Type
		{
			get { return mEventInfo.EventHandlerType; }
		}

		/// <summary>
		/// Gets the kind of the event.
		/// </summary>
		public EventKind Kind
		{
			get { return mEventInfo.GetAddMethod(true).ToEventKind(); }
		}

		/// <summary>
		/// Gets the access modifier of the event.
		/// </summary>
		public Visibility Visibility
		{
			get { return mEventInfo.GetAddMethod(true).ToVisibility(); }
		}

		/// <summary>
		/// Gets the <see cref="System.Reflection.EventInfo"/> associated with the event.
		/// </summary>
		public EventInfo EventInfo
		{
			get { return mEventInfo; }
		}

		/// <summary>
		/// Gets the 'add' accessor method.
		/// </summary>
		IMethod IEvent.AddAccessor
		{
			get { return mAddAccessor; }
		}

		/// <summary>
		/// Gets the 'add' accessor method.
		/// </summary>
		public InheritedMethod AddAccessor
		{
			get { return mAddAccessor; }
		}

		/// <summary>
		/// Gets the 'remove' accessor method.
		/// </summary>
		IMethod IEvent.RemoveAccessor
		{
			get { return mRemoveAccessor; }
		}

		/// <summary>
		/// Gets the 'remove' accessor method.
		/// </summary>
		public InheritedMethod RemoveAccessor
		{
			get { return mRemoveAccessor; }
		}
	}
}
