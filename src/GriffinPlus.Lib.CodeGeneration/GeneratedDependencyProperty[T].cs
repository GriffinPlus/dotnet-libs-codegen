﻿///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// A generated dependency property.
	/// </summary>
	class GeneratedDependencyProperty<T> : Member, IGeneratedDependencyProperty<T>
	{
		private readonly DependencyPropertyInitializer mInitializer;
		private readonly InitialValueInitializer       mInitialValueInitializer;
		private readonly ProvideValueCallback<T>       mTypedProvideInitialValueCallback;
		private readonly ProvideValueCallback          mUntypedProvideInitialValueCallback;

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedDependencyProperty{T}"/> class (without initial value).
		/// </summary>
		/// <param name="typeDefinition">The type definition the dependency property belongs to.</param>
		/// <param name="name">
		/// Name of the dependency property and the regular property providing access to the dependency property
		/// (<c>null</c> to create a random name).
		/// </param>
		/// <param name="isReadOnly">
		/// <c>true</c> if the dependency property is read-only;<br/>
		/// <c>false</c> if it is read-write.
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		internal GeneratedDependencyProperty(
			TypeDefinition typeDefinition,
			string         name,
			bool           isReadOnly) :
			base(typeDefinition)
		{
			IsReadOnly = isReadOnly;
			Initialize(name);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedDependencyProperty{T}"/> class (with initial value).
		/// </summary>
		/// <param name="typeDefinition">The type definition the dependency property belongs to.</param>
		/// <param name="name">
		/// Name of the dependency property and the regular property providing access to the dependency property
		/// (<c>null</c> to create a random name).
		/// </param>
		/// <param name="isReadOnly">
		/// <c>true</c> if the dependency property is read-only;<br/>
		/// <c>false</c> if it is read-write.
		/// </param>
		/// <param name="initialValue">Initial value of the field.</param>
		/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		internal GeneratedDependencyProperty(
			TypeDefinition typeDefinition,
			string         name,
			bool           isReadOnly,
			T              initialValue) :
			base(typeDefinition)
		{
			IsReadOnly = isReadOnly;

			// set up an initializer or factory callback pushing the initial value into the dependency property on initialization
			HasInitialValue = true;
			InitialValue = initialValue;
			if (InitialValueInitializers.TryGetInitializer(typeof(T), out InitialValueInitializer initializer)) mInitialValueInitializer = initializer;
			else mTypedProvideInitialValueCallback = () => InitialValue;

			// initialize common parts
			Initialize(name);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedDependencyProperty{T}"/> class
		/// (with initial value provided by a custom initializer).
		/// </summary>
		/// <param name="typeDefinition">The type definition the dependency property belongs to.</param>
		/// <param name="name">
		/// Name of the dependency property and the regular property providing access to the dependency property
		/// (<c>null</c> to create a random name).
		/// </param>
		/// <param name="isReadOnly">
		/// <c>true</c> if the dependency property is read-only;<br/>
		/// <c>false</c> if it is read-write.
		/// </param>
		/// <param name="initializer">
		/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
		/// value for the generated dependency property.
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> or <paramref name="initializer"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		internal GeneratedDependencyProperty(
			TypeDefinition                typeDefinition,
			string                        name,
			bool                          isReadOnly,
			DependencyPropertyInitializer initializer) :
			base(typeDefinition)
		{
			IsReadOnly = isReadOnly;
			mInitializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
			Initialize(name);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedDependencyProperty{T}"/> class
		/// (with initial value provided by a typed factory callback).
		/// </summary>
		/// <param name="typeDefinition">The type definition the dependency property belongs to.</param>
		/// <param name="name">
		/// Name of the dependency property and the regular property providing access to the dependency property
		/// (<c>null</c> to create a random name).
		/// </param>
		/// <param name="isReadOnly">
		/// <c>true</c> if the dependency property is read-only;<br/>
		/// <c>false</c> if it is read-write.
		/// </param>
		/// <param name="provideInitialValueCallback">Factory callback providing the initial value of the dependency property.</param>
		/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> or <paramref name="provideInitialValueCallback"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		internal GeneratedDependencyProperty(
			TypeDefinition          typeDefinition,
			string                  name,
			bool                    isReadOnly,
			ProvideValueCallback<T> provideInitialValueCallback) :
			base(typeDefinition)
		{
			IsReadOnly = isReadOnly;
			mTypedProvideInitialValueCallback = provideInitialValueCallback ?? throw new ArgumentNullException(nameof(provideInitialValueCallback));
			Initialize(name);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedDependencyProperty{T}"/> class
		/// (with initial value provided by an untyped factory callback).
		/// </summary>
		/// <param name="typeDefinition">The type definition the dependency property belongs to.</param>
		/// <param name="name">
		/// Name of the dependency property and the regular property providing access to the dependency property
		/// (<c>null</c> to create a random name).
		/// </param>
		/// <param name="isReadOnly">
		/// <c>true</c> if the dependency property is read-only;<br/>
		/// <c>false</c> if it is read-write.
		/// </param>
		/// <param name="provideInitialValueCallback">Factory callback providing the initial value of the dependency property.</param>
		/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> or <paramref name="provideInitialValueCallback"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		internal GeneratedDependencyProperty(
			TypeDefinition       typeDefinition,
			string               name,
			bool                 isReadOnly,
			ProvideValueCallback provideInitialValueCallback) :
			base(typeDefinition)
		{
			IsReadOnly = isReadOnly;
			mUntypedProvideInitialValueCallback = provideInitialValueCallback ?? throw new ArgumentNullException(nameof(provideInitialValueCallback));
			Initialize(name);
		}

		/// <summary>
		/// Performs common initializations during construction.
		/// </summary>
		/// <param name="name">Name of the dependency property (<c>null</c> to create a random name).</param>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		private void Initialize(string name)
		{
			// set the name of the dependency property and check whether it is a valid identifier
			Name = string.IsNullOrWhiteSpace(name) ? "DependencyProperty_" + Guid.NewGuid().ToString("N") : name;
			EnsureNameIsValidLanguageIndependentIdentifier(Name);

			// add the dependency property
			if (IsReadOnly)
			{
				DependencyPropertyField = TypeDefinition.AddStaticField<DependencyPropertyKey>(
					Name + "Property",
					Visibility.Public,
					ImplementReadOnly);
			}
			else
			{
				DependencyPropertyField = TypeDefinition.AddStaticField<DependencyProperty>(
					Name + "Property",
					Visibility.Public,
					ImplementReadWrite);
			}
		}

		/// <summary>
		/// Gets the type of the dependency property.
		/// </summary>
		public Type Type => typeof(T);

		/// <summary>
		/// Gets the name of the dependency property and the regular property providing access to the dependency property.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the property is read-only.
		/// </summary>
		public bool IsReadOnly { get; }

		/// <summary>
		/// Gets a value indicating whether the <see cref="InitialValue"/> property contains a valid initial value.
		/// </summary>
		public bool HasInitialValue { get; }

		/// <summary>
		/// Gets the initial value of the dependency property (if any).
		/// The <see cref="HasInitialValue"/> property determines whether this property contains a valid initial value.
		/// </summary>
		object IInitialValueProvider.InitialValue => InitialValue;

		/// <summary>
		/// Gets the initial value of the dependency property (if any).
		/// The <see cref="HasInitialValue"/> property determines whether this property contains a valid initial value.
		/// </summary>
		public T InitialValue { get; }

		/// <summary>
		/// Gets the static field storing the registered dependency property. Can be of type:<br/>
		/// - <see cref="DependencyProperty"/> (<see cref="IsReadOnly"/> is <c>false</c>)<br/>
		/// - <see cref="DependencyPropertyKey"/> (<see cref="IsReadOnly"/> is <c>true</c>).
		/// </summary>
		public IGeneratedField DependencyPropertyField { get; private set; }

		/// <summary>
		/// Gets the accessor property associated with the dependency property.
		/// (can be <c>null</c>, call <see cref="AddAccessorProperty"/> to add the accessor property).
		/// </summary>
		public IGeneratedProperty<T> AccessorProperty { get; private set; }

		/// <summary>
		/// Gets the accessor property associated with the dependency property.
		/// (can be <c>null</c>, call <see cref="AddAccessorProperty"/> to add the accessor property).
		/// </summary>
		IGeneratedProperty IGeneratedDependencyProperty.AccessorProperty => AccessorProperty;

		/// <summary>
		/// Adds a regular property accessing the dependency property.
		/// </summary>
		/// <param name="name">Name of the property (<c>null</c> to use the name of the dependency property).</param>
		/// <param name="getAccessorVisibility">Visibility of the 'get' accessor of the property to create.</param>
		/// <param name="setAccessorVisibility">Visibility of the 'set' accessor of the property to create.</param>
		/// <returns>The added accessor property.</returns>
		public IGeneratedProperty<T> AddAccessorProperty(
			string     name                  = null,
			Visibility getAccessorVisibility = Visibility.Public,
			Visibility setAccessorVisibility = Visibility.Public)
		{
			// abort if the accessor property was already added to the type definition
			if (AccessorProperty != null)
				throw new InvalidOperationException("The accessor property was already added.");

			// fall back to the name of the dependency property if name was not specified
			if (string.IsNullOrWhiteSpace(name)) name = Name;

			// check whether the name is a valid identifier
			if (name != null) EnsureNameIsValidLanguageIndependentIdentifier(name);

			// add the accessor property with get/set accessor
			AccessorProperty = TypeDefinition.AddProperty<T>(name, new PropertyImplementation_DependencyProperty(this));
			AccessorProperty.AddGetAccessor(getAccessorVisibility);
			AccessorProperty.AddSetAccessor(setAccessorVisibility);

			return AccessorProperty;
		}

		/// <summary>
		/// Adds a regular property accessing the dependency property.
		/// </summary>
		/// <param name="name">Name of the property (<c>null</c> to use the name of the dependency property).</param>
		/// <param name="getAccessorVisibility">Visibility of the 'get' accessor of the property to create.</param>
		/// <param name="setAccessorVisibility">Visibility of the 'set' accessor of the property to create.</param>
		/// <returns>The added accessor property.</returns>
		IGeneratedProperty IGeneratedDependencyProperty.AddAccessorProperty(
			string     name,
			Visibility getAccessorVisibility,
			Visibility setAccessorVisibility)
		{
			return AddAccessorProperty(name, getAccessorVisibility, setAccessorVisibility);
		}

		/// <summary>
		/// Implements setting the field backing the dependency property if the dependency property is read-write.
		/// </summary>
		/// <param name="field">Field backing the dependency property.</param>
		/// <param name="msilGenerator">MSIL generator to use.</param>
		private void ImplementReadWrite(IGeneratedField field, ILGenerator msilGenerator)
		{
			// get access to the Type.GetTypeFromHandle() method to convert a RuntimeTypeHandle to a Type object
			MethodInfo getTypeFromHandleMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), [typeof(RuntimeTypeHandle)]);
			Debug.Assert(getTypeFromHandleMethod != null, nameof(getTypeFromHandleMethod) + " != null");

			// push parameters for DependencyProperty.Register() onto the evaluation stack
			msilGenerator.Emit(OpCodes.Ldstr, Name);
			msilGenerator.Emit(OpCodes.Ldtoken, typeof(T));
			msilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);
			msilGenerator.Emit(OpCodes.Ldtoken, TypeDefinition.TypeBuilder);
			msilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);

			bool hasInitialValue = false;
			if (mInitializer != null || mInitialValueInitializer != null)
			{
				// let initializer add the code needed to provide the object to store in the property
				// (leaves an object on the evaluation stack)
				hasInitialValue = true;
				if (mInitializer != null) mInitializer(this, msilGenerator);
				else mInitialValueInitializer(this, msilGenerator);
				if (typeof(T).IsValueType) msilGenerator.Emit(OpCodes.Box, typeof(T));
			}
			else if (mTypedProvideInitialValueCallback != null)
			{
				// the dependency property has a factory method that provides the initial value
				hasInitialValue = true;

				// put external factory callback into the collection of objects to pass along with the generated type
				int externalObjectIndex = TypeDefinition.ExternalObjects.Count;
				TypeDefinition.ExternalObjects.Add(mTypedProvideInitialValueCallback);

				// emit code to call the factory callback when the type is constructed.
				msilGenerator.Emit(OpCodes.Ldtoken, TypeDefinition.TypeBuilder);
				msilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);
				MethodInfo codeGenExternalStorageGetMethod = typeof(CodeGenExternalStorage).GetMethod(nameof(CodeGenExternalStorage.Get));
				Debug.Assert(codeGenExternalStorageGetMethod != null, nameof(codeGenExternalStorageGetMethod) + " != null");
				msilGenerator.Emit(OpCodes.Call, codeGenExternalStorageGetMethod);
				msilGenerator.Emit(OpCodes.Ldc_I4, externalObjectIndex);
				msilGenerator.Emit(OpCodes.Ldelem, typeof(ProvideValueCallback<T>));
				MethodInfo invokeMethod = typeof(ProvideValueCallback<T>).GetMethod("Invoke");
				Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
				msilGenerator.Emit(OpCodes.Call, invokeMethod);
				if (typeof(T).IsValueType) msilGenerator.Emit(OpCodes.Box, typeof(T));
			}
			else if (mUntypedProvideInitialValueCallback != null)
			{
				// the dependency property has a factory method that provides the initial value
				hasInitialValue = true;

				// put external factory callback into the collection of objects to pass along with the generated type
				int externalObjectIndex = TypeDefinition.ExternalObjects.Count;
				TypeDefinition.ExternalObjects.Add(mUntypedProvideInitialValueCallback);

				// emit code to call the factory callback when the type is constructed.
				msilGenerator.Emit(OpCodes.Ldtoken, TypeDefinition.TypeBuilder);
				msilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);
				MethodInfo codeGenExternalStorageGetMethod = typeof(CodeGenExternalStorage).GetMethod(nameof(CodeGenExternalStorage.Get));
				Debug.Assert(codeGenExternalStorageGetMethod != null, nameof(codeGenExternalStorageGetMethod) + " != null");
				msilGenerator.Emit(OpCodes.Call, codeGenExternalStorageGetMethod);
				msilGenerator.Emit(OpCodes.Ldc_I4, externalObjectIndex);
				msilGenerator.Emit(OpCodes.Ldelem, typeof(ProvideValueCallback));
				MethodInfo invokeMethod = typeof(ProvideValueCallback).GetMethod("Invoke");
				Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
				msilGenerator.Emit(OpCodes.Call, invokeMethod);
			}

			if (hasInitialValue)
			{
				// the dependency property does have an initial value
				// => create PropertyMetadata object with an initial value
				ConstructorInfo propertyMetadataConstructor = typeof(PropertyMetadata).GetConstructor([typeof(object)]);
				Debug.Assert(propertyMetadataConstructor != null, nameof(propertyMetadataConstructor) + " != null");
				msilGenerator.Emit(OpCodes.Newobj, propertyMetadataConstructor);
			}
			else
			{
				// the dependency property does not have an initial value
				// => create PropertyMetadata object without an initial value
				ConstructorInfo propertyMetadataConstructor = typeof(PropertyMetadata).GetConstructor(Type.EmptyTypes);
				Debug.Assert(propertyMetadataConstructor != null, nameof(propertyMetadataConstructor) + " != null");
				msilGenerator.Emit(OpCodes.Newobj, propertyMetadataConstructor);
			}

			// create the dependency property using DependencyProperty.Register()
			// ----------------------------------------------------------------------------------------------------------------
			// call DependencyProperty.Register() method
			MethodInfo registerMethod = typeof(DependencyProperty).GetMethod(
				"Register",
				BindingFlags.Public | BindingFlags.Static,
				null,
				[typeof(string), typeof(Type), typeof(Type), typeof(PropertyMetadata)],
				null);
			Debug.Assert(registerMethod != null, nameof(registerMethod) + " != null");
			msilGenerator.Emit(OpCodes.Call, registerMethod);
		}

		/// <summary>
		/// Implements setting the field backing the dependency property if the dependency property is read-write.
		/// </summary>
		/// <param name="field">Field backing the dependency property.</param>
		/// <param name="msilGenerator">MSIL generator to use.</param>
		private void ImplementReadOnly(IGeneratedField field, ILGenerator msilGenerator)
		{
			// get access to the Type.GetTypeFromHandle() method to convert a RuntimeTypeHandle to a Type object
			MethodInfo getTypeFromHandleMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), [typeof(RuntimeTypeHandle)]);
			Debug.Assert(getTypeFromHandleMethod != null, nameof(getTypeFromHandleMethod) + " != null");

			// push parameters for DependencyProperty.RegisterReadOnly() onto the evaluation stack
			msilGenerator.Emit(OpCodes.Ldstr, Name);
			msilGenerator.Emit(OpCodes.Ldtoken, typeof(T));
			msilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);
			msilGenerator.Emit(OpCodes.Ldtoken, TypeDefinition.TypeBuilder);
			msilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);

			bool hasInitialValue = false;
			if (mInitializer != null || mInitialValueInitializer != null)
			{
				// let initializer add the code needed to provide the object to store in the property
				// (leaves an object on the evaluation stack)
				hasInitialValue = true;
				if (mInitializer != null) mInitializer(this, msilGenerator);
				else mInitialValueInitializer(this, msilGenerator);
				if (typeof(T).IsValueType) msilGenerator.Emit(OpCodes.Box, typeof(T));
			}
			else if (mTypedProvideInitialValueCallback != null)
			{
				// the dependency property has a factory method that provides the initial value
				hasInitialValue = true;

				// put external factory callback into the collection of objects to pass along with the generated type
				int externalObjectIndex = TypeDefinition.ExternalObjects.Count;
				TypeDefinition.ExternalObjects.Add(mTypedProvideInitialValueCallback);

				// emit code to call the factory callback when the type is constructed.
				msilGenerator.Emit(OpCodes.Ldtoken, TypeDefinition.TypeBuilder);
				msilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);
				MethodInfo codeGenExternalStorageGetMethod = typeof(CodeGenExternalStorage).GetMethod(nameof(CodeGenExternalStorage.Get));
				Debug.Assert(codeGenExternalStorageGetMethod != null, nameof(codeGenExternalStorageGetMethod) + " != null");
				msilGenerator.Emit(OpCodes.Call, codeGenExternalStorageGetMethod);
				msilGenerator.Emit(OpCodes.Ldc_I4, externalObjectIndex);
				msilGenerator.Emit(OpCodes.Ldelem, typeof(ProvideValueCallback<T>));
				MethodInfo invokeMethod = typeof(ProvideValueCallback<T>).GetMethod("Invoke");
				Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
				msilGenerator.Emit(OpCodes.Call, invokeMethod);
				if (typeof(T).IsValueType) msilGenerator.Emit(OpCodes.Box, typeof(T));
			}
			else if (mUntypedProvideInitialValueCallback != null)
			{
				// the dependency property has a factory method that provides the initial value
				hasInitialValue = true;

				// put external factory callback into the collection of objects to pass along with the generated type
				int externalObjectIndex = TypeDefinition.ExternalObjects.Count;
				TypeDefinition.ExternalObjects.Add(mUntypedProvideInitialValueCallback);

				// emit code to call the factory callback when the type is constructed.
				msilGenerator.Emit(OpCodes.Ldtoken, TypeDefinition.TypeBuilder);
				msilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);
				MethodInfo codeGenExternalStorageGetMethod = typeof(CodeGenExternalStorage).GetMethod(nameof(CodeGenExternalStorage.Get));
				Debug.Assert(codeGenExternalStorageGetMethod != null, nameof(codeGenExternalStorageGetMethod) + " != null");
				msilGenerator.Emit(OpCodes.Call, codeGenExternalStorageGetMethod);
				msilGenerator.Emit(OpCodes.Ldc_I4, externalObjectIndex);
				msilGenerator.Emit(OpCodes.Ldelem, typeof(ProvideValueCallback));
				MethodInfo invokeMethod = typeof(ProvideValueCallback).GetMethod("Invoke");
				Debug.Assert(invokeMethod != null, nameof(invokeMethod) + " != null");
				msilGenerator.Emit(OpCodes.Call, invokeMethod);
			}

			if (hasInitialValue)
			{
				// the dependency property does have an initial value
				// => create PropertyMetadata object with an initial value
				ConstructorInfo propertyMetadataConstructor = typeof(PropertyMetadata).GetConstructor([typeof(object)]);
				Debug.Assert(propertyMetadataConstructor != null, nameof(propertyMetadataConstructor) + " != null");
				msilGenerator.Emit(OpCodes.Newobj, propertyMetadataConstructor);
			}
			else
			{
				// the dependency property does not have an initial value
				// => create PropertyMetadata object without an initial value
				ConstructorInfo propertyMetadataConstructor = typeof(PropertyMetadata).GetConstructor(Type.EmptyTypes);
				Debug.Assert(propertyMetadataConstructor != null, nameof(propertyMetadataConstructor) + " != null");
				msilGenerator.Emit(OpCodes.Newobj, propertyMetadataConstructor);
			}

			// create the dependency property using DependencyProperty.RegisterReadOnly()
			// ----------------------------------------------------------------------------------------------------------------
			MethodInfo registerReadOnlyMethod = typeof(DependencyProperty).GetMethod(
				"RegisterReadOnly",
				BindingFlags.Public | BindingFlags.Static,
				null,
				[typeof(string), typeof(Type), typeof(Type), typeof(PropertyMetadata)],
				null);
			Debug.Assert(registerReadOnlyMethod != null, nameof(registerReadOnlyMethod) + " != null");
			msilGenerator.Emit(OpCodes.Call, registerReadOnlyMethod);
		}
	}

}

#elif NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
// Dependency properties are not supported on .NET Standard and .NET5/6/7/8 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif
