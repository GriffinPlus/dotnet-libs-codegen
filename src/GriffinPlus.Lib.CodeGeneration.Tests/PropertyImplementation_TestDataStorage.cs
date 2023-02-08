///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using Xunit;

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	/// <summary>
	/// Property implementation that backs the property with test data stored in <see cref="TestDataStorage"/>.
	/// </summary>
	public class PropertyImplementation_TestDataStorage : PropertyImplementation
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyImplementation_TestDataStorage"/> class
		/// using the specified object in the <see cref="TestDataStorage"/> to back the property.
		/// </summary>
		/// <param name="handle">Handle of the test data object in the <see cref="TestDataStorage"/>.</param>
		public PropertyImplementation_TestDataStorage(int handle)
		{
			Handle = handle;
		}

		/// <summary>
		/// Gets the handle of the test data object in the <see cref="TestDataStorage"/> backing the property.
		/// </summary>
		public int Handle { get; }

		/// <summary>
		/// Gets a value indicating whether the <see cref="Declare"/> method was called.
		/// </summary>
		public bool DeclareWasCalled { get; set; }

		/// <summary>
		/// Adds additional fields, events, properties and methods to the type definition.
		/// </summary>
		/// <param name="typeDefinition">The type definition.</param>
		/// <param name="property">The property to implement.</param>
		public override void Declare(TypeDefinition typeDefinition, IGeneratedProperty property)
		{
			Assert.NotNull(typeDefinition);
			Assert.NotNull(property);
			DeclareWasCalled = true;
		}

		/// <summary>
		/// Implements the 'get' accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the 'get' accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the 'get' accessor method to implement.</param>
		public override void ImplementGetAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedProperty property,
			ILGenerator        msilGenerator)
		{
			MethodInfo testDataStorage_get = typeof(TestDataStorage).GetMethod(nameof(TestDataStorage.Get));
			Debug.Assert(testDataStorage_get != null, nameof(testDataStorage_get) + " != null");
			msilGenerator.Emit(OpCodes.Ldc_I4, Handle);
			msilGenerator.Emit(OpCodes.Call, testDataStorage_get);
			msilGenerator.Emit(OpCodes.Unbox_Any, property.PropertyType);
			msilGenerator.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Implements the 'set' accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the 'set' accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the 'set' accessor method to implement.</param>
		public override void ImplementSetAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedProperty property,
			ILGenerator        msilGenerator)
		{
			msilGenerator.Emit(OpCodes.Ldc_I4, Handle);
			msilGenerator.Emit(property.Kind == PropertyKind.Static ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
			if (property.PropertyType.IsValueType) msilGenerator.Emit(OpCodes.Box, property.PropertyType);
			MethodInfo testDataStorage_set = typeof(TestDataStorage).GetMethod(nameof(TestDataStorage.Set));
			Debug.Assert(testDataStorage_set != null, nameof(testDataStorage_set) + " != null");
			msilGenerator.Emit(OpCodes.Call, testDataStorage_set);
			msilGenerator.Emit(OpCodes.Ret);
		}
	}

}
