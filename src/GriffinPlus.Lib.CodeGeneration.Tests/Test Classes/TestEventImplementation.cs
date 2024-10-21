///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Event implementation that implements an event as .NET would do (a backing field stores a multicast delegate containing
/// registered event handlers, adding and removing handlers is implemented thread-safe in a lockless way using interlocked
/// operations). Optionally an event raiser method can be added that takes care of firing the event.
/// </summary>
public class TestEventImplementation : EventImplementation
{
	private          IGeneratedMethod mEventRaiserMethod;
	private readonly bool             mAddEventRaiserMethod;
	private readonly string           mEventRaiserMethodName;
	private readonly Visibility       mEventRaiserMethodVisibility;

	/// <summary>
	/// Initializes a new instance of the <see cref="TestEventImplementation"/> class.
	/// An event raiser method is not added.
	/// </summary>
	public TestEventImplementation() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="TestEventImplementation"/> class.
	/// An event raiser method is added and implemented depending on the event type.
	/// If the event type is <see cref="EventHandler"/> or <see cref="System.EventHandler{TEventArgs}"/> with
	/// <c>TEventArgs</c> being <see cref="EventArgs"/> the event raiser method will have the signature <c>void OnEvent()</c>.
	/// If the event type is <see cref="EventHandler{TEventArgs}"/> with more specialized <c>TEventArgs</c> (e.g. <c>SpecializedEventArgs</c>)
	/// the event raiser method will have the signature <c>void OnEvent(SpecializedEventArgs e)</c>. Any other event type will
	/// produce an event raiser method that has the same return type and parameters as the delegate.
	/// </summary>
	/// <param name="eventRaiserMethodName">Name of the event raiser method (<c>null</c> to use 'On' + the name of the event).</param>
	/// <param name="eventRaiserMethodVisibility">Visibility of the event raiser method.</param>
	public TestEventImplementation(
		string     eventRaiserMethodName       = null,
		Visibility eventRaiserMethodVisibility = Visibility.Protected)
	{
		mAddEventRaiserMethod = true;
		mEventRaiserMethodName = eventRaiserMethodName;
		mEventRaiserMethodVisibility = eventRaiserMethodVisibility;
	}

	/// <summary>
	/// Gets the event raiser method (<c>null</c>, if no event raiser method is generated).
	/// </summary>
	public override IGeneratedMethod EventRaiserMethod => mEventRaiserMethod;

	/// <summary>
	/// Gets the field backing the event.
	/// </summary>
	public IGeneratedField BackingField { get; private set; }

	/// <summary>
	/// Adds other fields, events, properties and methods to the <see cref="TypeDefinition"/>.
	/// </summary>
	/// <param name="typeDefinition">Definition of the type in creation.</param>
	/// <param name="eventToImplement">Event to implement.</param>
	public override void Declare(TypeDefinition typeDefinition, IGeneratedEvent eventToImplement)
	{
		// declare the backing field storing event handlers
		BackingField = eventToImplement.Kind == EventKind.Static
			               ? typeDefinition.AddStaticField(eventToImplement.EventHandlerType, null, eventToImplement.Visibility)
			               : typeDefinition.AddField(eventToImplement.EventHandlerType, null, eventToImplement.Visibility);

		// abort if event raiser method should not be declared
		if (!mAddEventRaiserMethod)
			return;

		// determine the name of the event raiser method
		string methodName = string.IsNullOrWhiteSpace(mEventRaiserMethodName) ? "On" + eventToImplement.Name : mEventRaiserMethodName;
		CodeGenHelpers.EnsureNameIsValidLanguageIndependentIdentifier(methodName);

		// determine the return type and the parameter types of the event raiser method
		Type returnType;
		Type[] parameterTypes;
		if (eventToImplement.EventHandlerType == typeof(EventHandler) || eventToImplement.EventHandlerType == typeof(EventHandler<EventArgs>))
		{
			// System.EventHandler
			// System.EventHandler<EventArgs>
			returnType = typeof(void);
			parameterTypes = Type.EmptyTypes;
		}
		else if (eventToImplement.EventHandlerType.IsGenericType && eventToImplement.EventHandlerType.GetGenericTypeDefinition() == typeof(EventHandler<>))
		{
			// EventHandler<T> with T derived from System.EventArgs
			returnType = typeof(void);
			parameterTypes = eventToImplement.EventHandlerType.GetGenericArguments();
		}
		else
		{
			// some other delegate type
			MethodInfo invokeMethod = eventToImplement.EventHandlerType.GetMethod("Invoke");
			Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
			returnType = invokeMethod.ReturnType;
			parameterTypes = invokeMethod.GetParameters().Select(x => x.ParameterType).ToArray();
		}

		// add the event raiser method to the type definition
		MethodKind kind = eventToImplement.Kind == EventKind.Static ? MethodKind.Static : MethodKind.Normal;
		mEventRaiserMethod = typeDefinition.AddMethod(
			kind,
			methodName,
			returnType,
			parameterTypes,
			mEventRaiserMethodVisibility,
			(_, msilGenerator) => ImplementRaiserMethod(typeDefinition, eventToImplement, msilGenerator));
	}

	/// <summary>
	/// Implements the add accessor method of the event.
	/// </summary>
	/// <param name="typeDefinition">Definition of the type in creation.</param>
	/// <param name="eventToImplement">The event the add accessor method to implement belongs to.</param>
	/// <param name="msilGenerator">MSIL generator attached to the add accessor method to implement.</param>
	public override void ImplementAddAccessorMethod(
		TypeDefinition  typeDefinition,
		IGeneratedEvent eventToImplement,
		ILGenerator     msilGenerator)
	{
		ImplementAccessor(true, eventToImplement);
	}

	/// <summary>
	/// Implements the remove accessor method of the event.
	/// </summary>
	/// <param name="typeDefinition">Definition of the type in creation.</param>
	/// <param name="eventToImplement">The event the remove accessor method to implement belongs to.</param>
	/// <param name="msilGenerator">MSIL generator attached to the remove accessor method to implement.</param>
	public override void ImplementRemoveAccessorMethod(
		TypeDefinition  typeDefinition,
		IGeneratedEvent eventToImplement,
		ILGenerator     msilGenerator)
	{
		ImplementAccessor(false, eventToImplement);
	}

	/// <summary>
	/// Implements the add/remove accessor of the event.
	/// </summary>
	/// <param name="isAdd">
	/// <c>true</c> to implement the 'add' accessor method;<br/>
	/// <c>false</c> to implement the 'remove' accessor method.
	/// </param>
	/// <param name="eventToImplement">Event to implement.</param>
	internal void ImplementAccessor(bool isAdd, IGeneratedEvent eventToImplement)
	{
		Type backingFieldType = BackingField.FieldBuilder.FieldType;

		// get the Delegate.Combine() method  when adding a handler and Delegate.Remove() when removing a handler
		MethodInfo delegateMethod = typeof(Delegate).GetMethod(isAdd ? "Combine" : "Remove", [typeof(Delegate), typeof(Delegate)]);
		Debug.Assert(delegateMethod != null, nameof(delegateMethod) + " != null");

		// get the System.Threading.Interlocked.CompareExchange(ref object, object, object) method
		MethodInfo interlockedCompareExchangeGenericMethod = typeof(Interlocked).GetMethods().Single(m => m.Name == "CompareExchange" && m.GetGenericArguments().Length == 1);
		MethodInfo interlockedCompareExchangeMethod = interlockedCompareExchangeGenericMethod.MakeGenericMethod(backingFieldType);

		// emit code to combine the handler with the multicast delegate in the backing field respectively remove the handler from it
		ILGenerator msilGenerator = isAdd ? eventToImplement.AddAccessor.MethodBuilder.GetILGenerator() : eventToImplement.RemoveAccessor.MethodBuilder.GetILGenerator();
		msilGenerator.DeclareLocal(backingFieldType); // local 0
		msilGenerator.DeclareLocal(backingFieldType); // local 1
		msilGenerator.DeclareLocal(backingFieldType); // local 2
		Label retryLabel = msilGenerator.DefineLabel();
		if (eventToImplement.Kind == EventKind.Static)
		{
			msilGenerator.Emit(OpCodes.Ldsfld, BackingField.FieldBuilder);
			msilGenerator.Emit(OpCodes.Stloc_0);
			msilGenerator.MarkLabel(retryLabel);
			msilGenerator.Emit(OpCodes.Ldloc_0);
			msilGenerator.Emit(OpCodes.Stloc_1);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Ldarg_0);
			msilGenerator.EmitCall(OpCodes.Call, delegateMethod, null);
			msilGenerator.Emit(OpCodes.Castclass, backingFieldType);
			msilGenerator.Emit(OpCodes.Stloc_2);
			msilGenerator.Emit(OpCodes.Ldsflda, BackingField.FieldBuilder);
			msilGenerator.Emit(OpCodes.Ldloc_2);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Call, interlockedCompareExchangeMethod);
			msilGenerator.Emit(OpCodes.Stloc_0);
			msilGenerator.Emit(OpCodes.Ldloc_0);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Bne_Un_S, retryLabel);
			msilGenerator.Emit(OpCodes.Ret);
		}
		else
		{
			msilGenerator.Emit(OpCodes.Ldarg_0);
			msilGenerator.Emit(OpCodes.Ldfld, BackingField.FieldBuilder);
			msilGenerator.Emit(OpCodes.Stloc_0);
			msilGenerator.MarkLabel(retryLabel);
			msilGenerator.Emit(OpCodes.Ldloc_0);
			msilGenerator.Emit(OpCodes.Stloc_1);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Ldarg_1);
			msilGenerator.EmitCall(OpCodes.Call, delegateMethod, null);
			msilGenerator.Emit(OpCodes.Castclass, backingFieldType);
			msilGenerator.Emit(OpCodes.Stloc_2);
			msilGenerator.Emit(OpCodes.Ldarg_0);
			msilGenerator.Emit(OpCodes.Ldflda, BackingField.FieldBuilder);
			msilGenerator.Emit(OpCodes.Ldloc_2);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Call, interlockedCompareExchangeMethod);
			msilGenerator.Emit(OpCodes.Stloc_0);
			msilGenerator.Emit(OpCodes.Ldloc_0);
			msilGenerator.Emit(OpCodes.Ldloc_1);
			msilGenerator.Emit(OpCodes.Bne_Un_S, retryLabel);
			msilGenerator.Emit(OpCodes.Ret);
		}
	}

	/// <summary>
	/// Implements the event raiser method.
	/// </summary>
	/// <param name="typeDefinition">Definition of the type in creation.</param>
	/// <param name="eventToImplement">The event the raiser method to implement belongs to.</param>
	/// <param name="msilGenerator">MSIL generator attached to the event raiser method to implement.</param>
	public override void ImplementRaiserMethod(
		TypeDefinition  typeDefinition,
		IGeneratedEvent eventToImplement,
		ILGenerator     msilGenerator)
	{
		// handle events of type System.EventHandler and System.EventHandler<EventArgs>
		// (the event raiser will have the signature: void OnEvent())
		if (eventToImplement.EventHandlerType == typeof(EventHandler) || eventToImplement.EventHandlerType == typeof(EventHandler<EventArgs>))
		{
			FieldInfo eventArgsEmpty = typeof(EventArgs).GetField("Empty");
			Debug.Assert(eventArgsEmpty != null);
			LocalBuilder handlerLocalBuilder = msilGenerator.DeclareLocal(BackingField.FieldBuilder.FieldType);
			Label label = msilGenerator.DefineLabel();

			if (eventToImplement.Kind == EventKind.Static)
			{
				msilGenerator.Emit(OpCodes.Ldsfld, BackingField.FieldBuilder);
				msilGenerator.Emit(OpCodes.Stloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Brfalse_S, label);
				msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Ldnull);                 // load sender (null)
				msilGenerator.Emit(OpCodes.Ldsfld, eventArgsEmpty); // load event arguments
			}
			else
			{
				msilGenerator.Emit(OpCodes.Ldarg_0);
				msilGenerator.Emit(OpCodes.Ldfld, BackingField.FieldBuilder);
				msilGenerator.Emit(OpCodes.Stloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Brfalse_S, label);
				msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Ldarg_0); // load sender (this)
				if (typeDefinition.TypeBuilder.IsValueType)
				{
					msilGenerator.Emit(OpCodes.Ldobj, typeDefinition.TypeBuilder);
					msilGenerator.Emit(OpCodes.Box, typeDefinition.TypeBuilder);
				}

				msilGenerator.Emit(OpCodes.Ldsfld, eventArgsEmpty); // load event arguments
			}

			MethodInfo invokeMethod = BackingField.FieldBuilder.FieldType.GetMethod("Invoke");
			Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
			msilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
			msilGenerator.MarkLabel(label);
			msilGenerator.Emit(OpCodes.Ret);

			return;
		}

		// handle events of type EventHandler<TEventArgs> with TEventArgs deriving from System.EventArgs
		// (the event raiser will have the signature: void OnEvent(TEventArgs))
		if (eventToImplement.EventHandlerType.IsGenericType && eventToImplement.EventHandlerType.GetGenericTypeDefinition() == typeof(EventHandler<>))
		{
			LocalBuilder handlerLocalBuilder = msilGenerator.DeclareLocal(BackingField.FieldBuilder.FieldType);
			Label label = msilGenerator.DefineLabel();
			if (eventToImplement.Kind == EventKind.Static)
			{
				msilGenerator.Emit(OpCodes.Ldsfld, BackingField.FieldBuilder);
				msilGenerator.Emit(OpCodes.Stloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Brfalse_S, label);
				msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Ldnull);  // load sender (null)
				msilGenerator.Emit(OpCodes.Ldarg_0); // load event arguments
			}
			else
			{
				msilGenerator.Emit(OpCodes.Ldarg_0);
				msilGenerator.Emit(OpCodes.Ldfld, BackingField.FieldBuilder);
				msilGenerator.Emit(OpCodes.Stloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Brfalse_S, label);
				msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
				msilGenerator.Emit(OpCodes.Ldarg_0); // load sender (this)
				if (typeDefinition.TypeBuilder.IsValueType)
				{
					msilGenerator.Emit(OpCodes.Ldobj, typeDefinition.TypeBuilder);
					msilGenerator.Emit(OpCodes.Box, typeDefinition.TypeBuilder);
				}

				msilGenerator.Emit(OpCodes.Ldarg_1); // load event arguments
			}

			MethodInfo invokeMethod = BackingField.FieldBuilder.FieldType.GetMethod("Invoke");
			Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
			msilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
			msilGenerator.MarkLabel(label);
			msilGenerator.Emit(OpCodes.Ret);

			return;
		}

		// handle events of other delegate types
		// (the event raiser will have the same parameter types as the event's delegate)
		// ReSharper disable once InvertIf
		if (typeof(Delegate).IsAssignableFrom(eventToImplement.EventHandlerType))
		{
			LocalBuilder handlerLocalBuilder = msilGenerator.DeclareLocal(BackingField.FieldBuilder.FieldType);
			Label label = msilGenerator.DefineLabel();

			if (eventToImplement.Kind == EventKind.Static)
			{
				msilGenerator.Emit(OpCodes.Ldsfld, BackingField.FieldBuilder);
			}
			else
			{
				msilGenerator.Emit(OpCodes.Ldarg_0);
				msilGenerator.Emit(OpCodes.Ldfld, BackingField.FieldBuilder);
			}

			msilGenerator.Emit(OpCodes.Stloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
			msilGenerator.Emit(OpCodes.Brfalse_S, label);
			msilGenerator.Emit(OpCodes.Ldloc, handlerLocalBuilder);
			int argumentOffset = eventToImplement.Kind == EventKind.Static ? 0 : 1;
			int count = EventRaiserMethod.ParameterTypes.Count;
			for (int i = 0; i < count; i++) CodeGenHelpers.EmitLoadArgument(msilGenerator, argumentOffset + i);
			MethodInfo invokeMethod = BackingField.FieldBuilder.FieldType.GetMethod("Invoke");
			Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
			msilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
			msilGenerator.Emit(OpCodes.Ret);
			msilGenerator.MarkLabel(label);
			if (invokeMethod.ReturnType != typeof(void)) CodeGenHelpers.EmitLoadDefaultValue(msilGenerator, invokeMethod.ReturnType, false);
			msilGenerator.Emit(OpCodes.Ret);
			return;
		}

		// should never occur as all event handlers are delegates at last
		throw new NotImplementedException($"The event type ({eventToImplement.EventHandlerType.FullName}) is not supported.");
	}
}
