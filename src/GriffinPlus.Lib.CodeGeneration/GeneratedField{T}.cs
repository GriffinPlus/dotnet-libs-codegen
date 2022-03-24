///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// A generated field.
	/// </summary>
	class GeneratedField<T> : Member, IGeneratedField<T>, IGeneratedFieldInternal
	{
		private readonly FieldInitializer<T>     mInitializer;
		private readonly InitialValueInitializer mInitialValueInitializer;
		private readonly ProvideValueCallback<T> mProvideInitialValueCallback;

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedField{T}"/> class (without initial value).
		/// </summary>
		/// <param name="typeDefinition">The code generation engine.</param>
		/// <param name="isStatic"><c>true</c> to create a static field; <c>false</c> to create an instance field.</param>
		/// <param name="name">Name of the field (may be <c>null</c> to create a random name).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		internal GeneratedField(
			TypeDefinition typeDefinition,
			bool           isStatic,
			string         name,
			Visibility     visibility) :
			base(typeDefinition)
		{
			Initialize(isStatic, name, visibility);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedField{T}"/> class (with initial value).
		/// </summary>
		/// <param name="typeDefinition">The type definition the field belongs to.</param>
		/// <param name="isStatic"><c>true</c> to create a static field; <c>false</c> to create an instance field.</param>
		/// <param name="name">Name of the field (may be <c>null</c> to create a random name).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="initialValue">Initial value of the field.</param>
		/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		internal GeneratedField(
			TypeDefinition typeDefinition,
			bool           isStatic,
			string         name,
			Visibility     visibility,
			T              initialValue) :
			base(typeDefinition)
		{
			// set up an initializer or factory callback pushing the initial value into the dependency property on initialization
			HasInitialValue = true;
			InitialValue = initialValue;
			if (InitialValueInitializers.TryGetInitializer(typeof(T), out var initializer)) mInitialValueInitializer = initializer;
			else mProvideInitialValueCallback = () => initialValue;

			// initialize common parts
			Initialize(isStatic, name, visibility);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedField{T}"/> class (with factory callback for an initial value).
		/// </summary>
		/// <param name="typeDefinition">The type definition the field belongs to.</param>
		/// <param name="isStatic"><c>true</c> to create a static field; <c>false</c> to create an instance field.</param>
		/// <param name="name">Name of the field (may be <c>null</c> to create a random name).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="initializer">
		/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
		/// value for the generated field.
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> or <paramref name="initializer"/> is <c>null.</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		internal GeneratedField(
			TypeDefinition      typeDefinition,
			bool                isStatic,
			string              name,
			Visibility          visibility,
			FieldInitializer<T> initializer) :
			base(typeDefinition)
		{
			mInitializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
			Initialize(isStatic, name, visibility);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratedField{T}"/> class (with factory callback for an initial value).
		/// </summary>
		/// <param name="typeDefinition">The type definition the field belongs to.</param>
		/// <param name="isStatic"><c>true</c> to create a static field; <c>false</c> to create an instance field.</param>
		/// <param name="name">Name of the field (may be <c>null</c> to create a random name).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="provideInitialValueCallback">Factory callback providing the initial value of the field.</param>
		/// <exception cref="ArgumentNullException"><paramref name="typeDefinition"/> or <paramref name="provideInitialValueCallback"/> is <c>null.</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		internal GeneratedField(
			TypeDefinition          typeDefinition,
			bool                    isStatic,
			string                  name,
			Visibility              visibility,
			ProvideValueCallback<T> provideInitialValueCallback) :
			base(typeDefinition)
		{
			Visibility = visibility;
			mProvideInitialValueCallback = provideInitialValueCallback ?? throw new ArgumentNullException(nameof(provideInitialValueCallback));
			Initialize(isStatic, name, visibility);
		}

		/// <summary>
		/// Performs common initializations during construction.
		/// </summary>
		/// <param name="isStatic"><c>true</c> to create a static field; <c>false</c> to create an instance field.</param>
		/// <param name="name">Name of the field (may be <c>null</c> to create a random name).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <exception cref="ArgumentException"><paramref name="name"/> is not a valid language independent identifier.</exception>
		private void Initialize(bool isStatic, string name, Visibility visibility)
		{
			IsStatic = isStatic;
			Visibility = visibility;

			// set the name of the field and check whether it is a valid identifier
			Name = string.IsNullOrWhiteSpace(name) ? (IsStatic ? "StaticField_" : "Field_") + Guid.NewGuid().ToString("N") : name;
			EnsureNameIsValidLanguageIndependentIdentifier(Name);

			// ensure that the specified type is public and all nested types are public, too
			// => otherwise the dynamically created assembly is not able to access it
			CodeGenHelpers.EnsureTypeIsTotallyPublic(typeof(T));

			// create the field builder
			FieldAttributes flags = Visibility.ToFieldAttributes();
			if (IsStatic) flags |= FieldAttributes.Static;
			FieldBuilder = TypeDefinition.TypeBuilder.DefineField(Name, typeof(T), flags);
		}

		/// <summary>
		/// Gets the name of the field.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the type of the field.
		/// </summary>
		public Type Type => typeof(T);

		/// <summary>
		/// Gets a value indicating whether the field is class variable (<c>true</c>) or an instance variable (<c>false</c>).
		/// </summary>
		public bool IsStatic { get; private set; }

		/// <summary>
		/// Gets the visibility of the field.
		/// </summary>
		public Visibility Visibility { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the <see cref="InitialValue"/> property contains a valid initial value.
		/// </summary>
		public bool HasInitialValue { get; }

		/// <summary>
		/// Gets the initial value of the field (if any).
		/// The <see cref="HasInitialValue"/> property determines whether this property contains a valid initial value.
		/// </summary>
		object IInitialValueProvider.InitialValue => InitialValue;

		/// <summary>
		/// Gets the initial value of the field (if any).
		/// The <see cref="HasInitialValue"/> property determines whether this property contains a valid initial value.
		/// </summary>
		public T InitialValue { get; }

		/// <summary>
		/// Gets the <see cref="System.Reflection.FieldInfo"/> associated with the field.
		/// </summary>
		FieldInfo IField.FieldInfo => FieldBuilder;

		/// <summary>
		/// Gets the <see cref="System.Reflection.Emit.FieldBuilder"/> associated with the field.
		/// </summary>
		public FieldBuilder FieldBuilder { get; private set; }

		/// <summary>
		/// Adds code to initialize the field with the specified initial value (if any).
		/// </summary>
		/// <param name="createdType">The created type.</param>
		/// <param name="msilGenerator">IL Generator attached to a constructor.</param>
		void IGeneratedFieldInternal.ImplementFieldInitialization(Type createdType, ILGenerator msilGenerator)
		{
			if (mInitializer != null || mInitialValueInitializer != null)
			{
				// add loading the 'this' reference onto the evaluation stack (needed when storing to the field later on)
				if (!IsStatic) msilGenerator.Emit(OpCodes.Ldarg_0);

				// let initializer add the code needed to provide the object to store in the field
				// (leaves an object on the evaluation stack)
				if (mInitializer != null) mInitializer(this, msilGenerator);
				else mInitialValueInitializer(this, msilGenerator);

				// add storing the object on the evaluation stack to the field
				msilGenerator.Emit(IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, FieldBuilder);
			}
			else if (mProvideInitialValueCallback != null)
			{
				// the field has a factory method that provides the initial value

				// add loading the 'this' reference onto the evaluation stack (needed when storing to the field later on)
				if (!IsStatic) msilGenerator.Emit(OpCodes.Ldarg_0);

				// put external factory callback into the collection of objects to pass along with the generated type
				int externalObjectIndex = TypeDefinition.ExternalObjects.Count;
				TypeDefinition.ExternalObjects.Add(mProvideInitialValueCallback);

				// emit code to call the factory callback when the type is constructed.
				msilGenerator.Emit(OpCodes.Ldtoken, createdType);
				var type_getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) });
				Debug.Assert(type_getTypeFromHandle != null, nameof(type_getTypeFromHandle) + " != null");
				msilGenerator.Emit(OpCodes.Call, type_getTypeFromHandle);
				var codeGenExternalStorage_get = typeof(CodeGenExternalStorage).GetMethod(nameof(CodeGenExternalStorage.Get));
				Debug.Assert(codeGenExternalStorage_get != null, nameof(codeGenExternalStorage_get) + " != null");
				msilGenerator.Emit(OpCodes.Call, codeGenExternalStorage_get);
				msilGenerator.Emit(OpCodes.Ldc_I4, externalObjectIndex);
				msilGenerator.Emit(OpCodes.Ldelem, typeof(Func<T>));
				var func_invoke = typeof(Func<T>).GetMethod("Invoke");
				Debug.Assert(func_invoke != null, nameof(func_invoke) + " != null");
				msilGenerator.Emit(OpCodes.Call, func_invoke);

				// add storing the object on the evaluation stack to the field
				msilGenerator.Emit(IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, FieldBuilder);
			}
		}
	}

}
