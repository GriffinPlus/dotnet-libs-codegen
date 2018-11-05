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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Event implementation that implements an event as .NET would do (a backing field stores a multicast delegate containing
	/// registered event handlers, adding and removing handlers is implemented thread-safe in a lockless way using interlocked
	/// operations).
	/// </summary>
	/// <remarks>
	/// The event raiser method is implemented depending on the event type due to performance reasons.
	/// If the event type is <see cref="System.EventHandler"/> or <see cref="System.EventHandler{TEventArgs}"/> with
	/// TEventArgs being <see cref="System.EventArgs"/> the event raiser method will have the signature <c>void OnEvent()</c>.
	/// If the event type is <see cref="System.EventHandler{TEventArgs}"/> with more specialized TEventArgs the event raiser
	/// method will have the signature <c>void OnEvent(SpecializedEventArgs e)</c>. Any other event type will produce an event
	/// raiser method that has the same parameters as the delegate.
	/// </remarks>
	public class EventImplementation_Standard : IEventImplementation
	{
		private GeneratedField mBackingField;

		/// <summary>
		/// Reviews the default declaration of the event and adds additional type declarations, if necessary.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		/// <param name="evnt">The event to review.</param>
		public void Declare(CodeGenEngine engine, GeneratedEvent evnt)
		{
			// declare the backing field storing event handlers
			mBackingField = engine.AddField(evnt.Type, null, evnt.Kind == EventKind.Static, evnt.Visibility, null);
		}

		/// <summary>
		/// Implements the event.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		/// <param name="evnt">The event to implement.</param>
		public void Implement(CodeGenEngine engine, GeneratedEvent evnt)
		{
			ImplementAccessor(true, evnt, mBackingField);
			ImplementAccessor(false, evnt, mBackingField);
			ImplementEventRaiser(evnt, mBackingField);
		}

		/// <summary>
		/// Implements the add or remove accessor of the event.
		/// </summary>
		/// <param name="isAdd">true to create an 'add' method; false to create a 'remove' method.</param>
		/// <param name="evnt">Event to implement.</param>
		/// <param name="backingField">Multicast delegate field backing the event.</param>
		private static void ImplementAccessor(bool isAdd, GeneratedEvent evnt, GeneratedField backingField)
		{
			Type backingFieldType = backingField.FieldBuilder.FieldType;

			// get the Delegate.Combine() method  when adding a handler and Delegate.Remove() when removing a handler
			MethodInfo delegateMethod = typeof(Delegate).GetMethod(isAdd ? "Combine" : "Remove", new[] { typeof(Delegate), typeof(Delegate) });

			// get the System.Threading.Interlocked.CompareExchange(ref object, object, object) method
			MethodInfo interlockedCompareExchangeGenericMethod = typeof(Interlocked)
				.GetMethods()
				.Single(m => m.Name == "CompareExchange" && m.GetGenericArguments().Length == 1);
			MethodInfo interlockedCompareExchangeMethod = interlockedCompareExchangeGenericMethod.MakeGenericMethod(backingFieldType);

			// emit code to combine the handler with the multicast delegate in the backing field respectively remove the handler from it
			ILGenerator msil = isAdd ? evnt.AddAccessor.MethodBuilder.GetILGenerator() : evnt.RemoveAccessor.MethodBuilder.GetILGenerator();
			LocalBuilder local0 = msil.DeclareLocal(backingFieldType);
			LocalBuilder local1 = msil.DeclareLocal(backingFieldType);
			LocalBuilder local2 = msil.DeclareLocal(backingFieldType);
			Label retryLabel = msil.DefineLabel();
			if (evnt.Kind == EventKind.Static)
			{
				msil.Emit(OpCodes.Ldsfld, backingField.FieldBuilder);
				msil.Emit(OpCodes.Stloc_0);
				msil.MarkLabel(retryLabel);
				msil.Emit(OpCodes.Ldloc_0);
				msil.Emit(OpCodes.Stloc_1);
				msil.Emit(OpCodes.Ldloc_1);
				msil.Emit(OpCodes.Ldarg_0);
				msil.EmitCall(OpCodes.Call, delegateMethod, null);
				msil.Emit(OpCodes.Castclass, backingFieldType);
				msil.Emit(OpCodes.Stloc_2);
				msil.Emit(OpCodes.Ldsflda, backingField.FieldBuilder);
				msil.Emit(OpCodes.Ldloc_2);
				msil.Emit(OpCodes.Ldloc_1);
				msil.Emit(OpCodes.Call, interlockedCompareExchangeMethod);
				msil.Emit(OpCodes.Stloc_0);
				msil.Emit(OpCodes.Ldloc_0);
				msil.Emit(OpCodes.Ldloc_1);
				msil.Emit(OpCodes.Bne_Un_S, retryLabel);
				msil.Emit(OpCodes.Ret);
			}
			else
			{
				msil.Emit(OpCodes.Ldarg_0);
				msil.Emit(OpCodes.Ldfld, backingField.FieldBuilder);
				msil.Emit(OpCodes.Stloc_0);
				msil.MarkLabel(retryLabel);
				msil.Emit(OpCodes.Ldloc_0);
				msil.Emit(OpCodes.Stloc_1);
				msil.Emit(OpCodes.Ldloc_1);
				msil.Emit(OpCodes.Ldarg_1);
				msil.EmitCall(OpCodes.Call, delegateMethod, null);
				msil.Emit(OpCodes.Castclass, backingFieldType);
				msil.Emit(OpCodes.Stloc_2);
				msil.Emit(OpCodes.Ldarg_0);
				msil.Emit(OpCodes.Ldflda, backingField.FieldBuilder);
				msil.Emit(OpCodes.Ldloc_2);
				msil.Emit(OpCodes.Ldloc_1);
				msil.Emit(OpCodes.Call, interlockedCompareExchangeMethod);
				msil.Emit(OpCodes.Stloc_0);
				msil.Emit(OpCodes.Ldloc_0);
				msil.Emit(OpCodes.Ldloc_1);
				msil.Emit(OpCodes.Bne_Un_S, retryLabel);
				msil.Emit(OpCodes.Ret);
			}
		}

		/// <summary>
		/// Adds the event raiser method.
		/// </summary>
		/// <param name="evnt">Event to implement.</param>
		/// <param name="backingField">Multicast delegate field backing the event.</param>
		private static void ImplementEventRaiser(GeneratedEvent evnt, GeneratedField backingField)
		{
			ILGenerator msil = evnt.Raiser.MethodBuilder.GetILGenerator();

			if (evnt.Type == typeof(EventHandler) || evnt.Type == typeof(EventHandler<EventArgs>))
			{
				// System.EventHandler
				// System.EventHandler<EventArgs>

				FieldInfo EventArgsEmpty = typeof(EventArgs).GetField("Empty");
				LocalBuilder handlerLocalBuilder = msil.DeclareLocal(backingField.FieldBuilder.FieldType);
				Label label = msil.DefineLabel();
				if (evnt.Kind == EventKind.Static)
				{
					msil.Emit(OpCodes.Ldsfld, backingField.FieldBuilder);
					msil.Emit(OpCodes.Stloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Brfalse_S, label);
					msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Ldnull);  // load sender (null)
					msil.Emit(OpCodes.Ldsfld, EventArgsEmpty); // load event arguments
				}
				else
				{
					msil.Emit(OpCodes.Ldarg_0);
					msil.Emit(OpCodes.Ldfld, backingField.FieldBuilder);
					msil.Emit(OpCodes.Stloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Brfalse_S, label);
					msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Ldarg_0); // load sender (this)
					msil.Emit(OpCodes.Ldsfld, EventArgsEmpty); // load event arguments
				}

				MethodInfo invokeMethod = backingField.FieldBuilder.FieldType.GetMethod("Invoke");
				msil.Emit(OpCodes.Callvirt, invokeMethod);
				msil.MarkLabel(label);
				msil.Emit(OpCodes.Ret);

				return;
			}
			else if (evnt.Type.IsGenericType && evnt.Type.GetGenericTypeDefinition() == typeof(EventHandler<>))
			{
				// EventHandler<T> with T derived from System.EventArgs

				LocalBuilder handlerLocalBuilder = msil.DeclareLocal(backingField.FieldBuilder.FieldType);
				Label label = msil.DefineLabel();
				if (evnt.Kind == EventKind.Static)
				{
					msil.Emit(OpCodes.Ldsfld, backingField.FieldBuilder);
					msil.Emit(OpCodes.Stloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Brfalse_S, label);
					msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Ldnull);  // load sender (null)
					msil.Emit(OpCodes.Ldarg_0); // load event arguments
				}
				else
				{
					msil.Emit(OpCodes.Ldarg_0);
					msil.Emit(OpCodes.Ldfld, backingField.FieldBuilder);
					msil.Emit(OpCodes.Stloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Brfalse_S, label);
					msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
					msil.Emit(OpCodes.Ldarg_0); // load sender (this)
					msil.Emit(OpCodes.Ldarg_1); // load event arguments
				}

				MethodInfo invokeMethod = backingField.FieldBuilder.FieldType.GetMethod("Invoke");
				msil.Emit(OpCodes.Callvirt, invokeMethod);
				msil.MarkLabel(label);
				msil.Emit(OpCodes.Ret);

				return;
			}
			else if (typeof(Delegate).IsAssignableFrom(evnt.Type))
			{
				MethodInfo invokeMethod = backingField.FieldBuilder.FieldType.GetMethod("Invoke");
				LocalBuilder handlerLocalBuilder = msil.DeclareLocal(backingField.FieldBuilder.FieldType);
				Label label = msil.DefineLabel();

				if (evnt.Kind == EventKind.Static) {
					msil.Emit(OpCodes.Ldsfld, backingField.FieldBuilder);
				} else {
					msil.Emit(OpCodes.Ldarg_0);
					msil.Emit(OpCodes.Ldfld, backingField.FieldBuilder);
				}

				msil.Emit(OpCodes.Stloc, handlerLocalBuilder);
				msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				msil.Emit(OpCodes.Brfalse_S, label);
				msil.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				int argumentOffset = evnt.Kind == EventKind.Static ? 0 : 1;
				for (int i = 0; i < evnt.Raiser.ParameterTypes.Length; i++) {
					CodeGenHelpers.EmitLoadArgument(msil, argumentOffset + i);
				}
				msil.Emit(OpCodes.Callvirt, invokeMethod);
				msil.MarkLabel(label);
				msil.Emit(OpCodes.Ret);
				return;
			}

			throw new NotSupportedException("The event type is not supported.");
		}

		/// <summary>
		/// Is called when the event the implementation strategy is attached to is removed from the type in creation.
		/// </summary>
		/// <param name="engine">The <see cref="CodeGenEngine"/> assembling the type in creation.</param>
		public void OnRemoving(CodeGenEngine engine)
		{
			engine.RemoveField(mBackingField);
		}
	}
}
