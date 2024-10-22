///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Xunit;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
// ReSharper disable RedundantTypeArgumentsOfMethod

namespace GriffinPlus.Lib.CodeGeneration.Tests;

using static Helpers;

/// <summary>
/// Common tests around the <see cref="TypeDefinition"/> class.
/// </summary>
public abstract class TypeDefinitionTests_Common<TDefinition> where TDefinition : TypeDefinition
{
	/// <summary>
	/// Creates a new type definition instance with a random name to test.
	/// </summary>
	/// <param name="name">Name of the type to create (<c>null</c> to create a random name).</param>
	/// <param name="attributes">
	/// Attributes of the type to create
	/// (only flags that are part of <see cref="ClassAttributes"/> and <see cref="StructAttributes"/> are valid).
	/// </param>
	/// <returns>The created type definition instance.</returns>
	public abstract TDefinition CreateTypeDefinition(string name = null, TypeAttributes attributes = 0);

	#region Construction

	/// <summary>
	/// Tests creating a new instance of the type definition using the constructor of classes
	/// deriving from <see cref="TypeDefinition"/>.
	/// </summary>
	[Theory]
	[InlineData("MyType")] // custom name
	[InlineData(null)]     // automatically generated name
	public void Create(string name)
	{
		// create a new type definition
		TDefinition definition = CreateTypeDefinition(name);

		// check whether the type definition has been initialized correctly
		CheckDefinitionAfterConstruction(
			definition,                // type definition to check
			name,                      // name of the type (as specified to the constructor, may be null)
			definition.BaseClassType); // base type the type to create should derive from
	}

	/// <summary>
	/// Checks the state of the type definition directly after construction.
	/// </summary>
	/// <param name="definition">Type definition to check.</param>
	/// <param name="typeName">
	/// Name of the type (as specified to the constructor, may be <c>null</c> to generate a random name).
	/// </param>
	/// <param name="baseType">The type to create derives from.</param>
	protected static void CheckDefinitionAfterConstruction(
		TypeDefinition definition,
		string         typeName,
		Type           baseType)
	{
		// the type definition should have an initialized type builder backing the definition
		Assert.NotNull(definition.TypeBuilder);

		// the type definition should reflect the expected base type
		Assert.NotNull(definition.BaseClassType);
		Assert.Same(baseType, definition.BaseClassType);

		// the type definition should reflect the expected type name
		if (typeName != null)
		{
			// name of the type to create was specified explicitly
			Assert.Equal(typeName, definition.TypeName);
		}
		else
		{
			// name of the type to create was not specified explicitly
			if (definition.BaseClassType == typeof(object) || definition.BaseClassType == typeof(ValueType))
			{
				// the type does not explicitly derive from some other type
				// => the type definition generates a random name for the type to create
				Assert.StartsWith("DynamicType_", definition.TypeName);
			}
			else
			{
				// the type explicitly derives from some other type
				// => the type definition uses the name of this type for the type to create
				Assert.Equal(definition.BaseClassType.FullName, definition.TypeName);
			}
		}

		// the type definition should not contain any generated members, yet
		Assert.Empty(definition.Constructors);
		Assert.Empty(definition.ImplementedInterfaces);
		Assert.Empty(definition.GeneratedEvents);
		Assert.Empty(definition.GeneratedFields);
		Assert.Empty(definition.GeneratedMethods);
		Assert.Empty(definition.GeneratedProperties);
#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
		Assert.Empty(definition.GeneratedDependencyProperties);
#elif NETCOREAPP2_2 || NETCOREAPP3_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
		// Dependency properties are not supported on .NET Core 2/3 and .NET 5/6/7/8 without Windows extensions
#else
#error Unhandled Target Framework.
#endif

		// the type definition should return the expected base class constructors
		CheckBaseClassConstructors(definition.BaseClassType, definition.BaseClassConstructors);

		// the type definition should return the expected inherited members
		// exposed by the appropriate properties
		CheckInheritedEvents(definition.BaseClassType, false, definition.InheritedEvents);
		CheckInheritedFields(definition.BaseClassType, false, definition.InheritedFields);
		CheckInheritedMethods(definition.BaseClassType, false, definition.InheritedMethods);
		CheckInheritedProperties(definition.BaseClassType, false, definition.InheritedProperties);

		// the type definition should return the expected inherited members
		// exposed by the appropriate methods
		foreach (bool includeHidden in new[] { false, true })
		{
			CheckInheritedEvents(definition.BaseClassType, includeHidden, definition.GetInheritedEvents(includeHidden));
			CheckInheritedFields(definition.BaseClassType, includeHidden, definition.GetInheritedFields(includeHidden));
			CheckInheritedMethods(definition.BaseClassType, includeHidden, definition.GetInheritedMethods(includeHidden));
			CheckInheritedProperties(definition.BaseClassType, includeHidden, definition.GetInheritedProperties(includeHidden));
		}
	}

	/// <summary>
	/// Checks whether the specified constructors reflect the expected set of base class constructors.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="constructors">Constructors to check.</param>
	private static void CheckBaseClassConstructors(
		Type                      baseType,
		IEnumerable<IConstructor> constructors)
	{
		HashSet<ConstructorInfo> baseClassConstructors = GetConstructorsAccessibleFromDerivedType(baseType);
		foreach (IConstructor constructor in constructors)
		{
			// the constructor event should be in the set of expected constructors
			Assert.Contains(constructor.ConstructorInfo, baseClassConstructors);

			// remove the constructor from the set to determine whether all constructors have been covered at the end
			baseClassConstructors.Remove(constructor.ConstructorInfo);
		}

		// all expected constructors should have been removed from the set now
		// (otherwise an expected constructor has not been covered)
		Assert.Empty(baseClassConstructors);
	}

	/// <summary>
	/// Checks whether the specified inherited events reflect the expected set of inherited events.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="includeHidden">
	/// <c>true</c> to include hidden events;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="eventsToCheck">Inherited events to check.</param>
	private static void CheckInheritedEvents(
		Type                         baseType,
		bool                         includeHidden,
		IEnumerable<IInheritedEvent> eventsToCheck)
	{
		HashSet<EventInfo> inheritedEvents = GetEventsAccessibleFromDerivedType(baseType, includeHidden);
		foreach (IInheritedEvent @event in eventsToCheck)
		{
			// the event should be in the set of expected events
			Assert.Contains(@event.EventInfo, inheritedEvents);

			// remove the event from the set to determine whether all events have been covered at the end
			inheritedEvents.Remove(@event.EventInfo);
		}

		// all expected events should have been removed from the set now
		// (otherwise an expected event has not been covered)
		Assert.Empty(inheritedEvents);
	}

	/// <summary>
	/// Checks whether the specified inherited fields reflect the expected set of inherited fields.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="includeHidden">
	/// <c>true</c> to include hidden fields;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="fieldsToCheck">Inherited fields to check.</param>
	private static void CheckInheritedFields(
		Type                         baseType,
		bool                         includeHidden,
		IEnumerable<IInheritedField> fieldsToCheck)
	{
		HashSet<FieldInfo> inheritedFields = GetFieldsAccessibleFromDerivedType(baseType, includeHidden);
		foreach (IInheritedField field in fieldsToCheck)
		{
			// the field should be in the set of expected fields
			Assert.Contains(field.FieldInfo, inheritedFields);

			// remove the field from the set to determine whether all fields have been covered at the end
			inheritedFields.Remove(field.FieldInfo);
		}

		// all expected fields should have been removed from the set now
		// (otherwise an expected field has not been covered)
		Assert.Empty(inheritedFields);
	}

	/// <summary>
	/// Checks whether the specified inherited methods reflect the expected set of inherited methods.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="includeHidden">
	/// <c>true</c> to include hidden methods;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="methodsToCheck">Inherited methods to check.</param>
	private static void CheckInheritedMethods(
		Type                          baseType,
		bool                          includeHidden,
		IEnumerable<IInheritedMethod> methodsToCheck)
	{
		HashSet<MethodInfo> inheritedMethods = GetMethodsAccessibleFromDerivedType(baseType, includeHidden);
		foreach (IInheritedMethod method in methodsToCheck)
		{
			// the method should be in the set of expected methods
			Assert.Contains(method.MethodInfo, inheritedMethods);

			// remove the method from the set to determine whether all methods have been covered at the end
			inheritedMethods.Remove(method.MethodInfo);
		}

		// all expected methods should have been removed from the set now
		// (otherwise an expected method has not been covered)
		Assert.Empty(inheritedMethods);
	}

	/// <summary>
	/// Checks whether the specified inherited properties reflect the expected set of inherited properties.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="includeHidden">
	/// <c>true</c> to include hidden properties;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="propertiesToCheck">Inherited properties to check.</param>
	private static void CheckInheritedProperties(
		Type                            baseType,
		bool                            includeHidden,
		IEnumerable<IInheritedProperty> propertiesToCheck)
	{
		HashSet<PropertyInfo> inheritedProperties = GetPropertiesAccessibleFromDerivedType(baseType, includeHidden);
		foreach (IInheritedProperty property in propertiesToCheck)
		{
			// the property should be in the set of expected properties
			Assert.Contains(property.PropertyInfo, inheritedProperties);

			// remove the property from the set to determine whether all properties have been covered at the end
			inheritedProperties.Remove(property.PropertyInfo);
		}

		// all expected properties should have been removed from the set now
		// (otherwise an expected property has not been covered)
		Assert.Empty(inheritedProperties);
	}

	#endregion

	#region CreateType()

	/// <summary>
	/// Tests creating a new type definition and the type from it.
	/// </summary>
	[Theory]
	[InlineData("MyType")] // custom name
	[InlineData(null)]     // automatically generated name
	public void CreateType(string name)
	{
		TDefinition definition = CreateTypeDefinition(name);
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
	}

	#endregion

	#region Adding Implemented Interface (TODO)

	[Fact]
	private void AddImplementedInterface() { }

	#endregion // Adding Implemented Interface

	#region Adding Fields

	#region Test Data

	/// <summary>
	/// Names to test with when adding fields to the type definition.
	/// </summary>
	private static IEnumerable<string> FieldNames
	{
		get
		{
			yield return "Field";
			yield return null;
		}
	}

	/// <summary>
	/// Test data for tests targeting
	/// <see cref="TypeDefinition.AddField{T}(string,Visibility)"/>,
	/// <see cref="TypeDefinition.AddField(Type,string,Visibility)"/>,
	/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility)"/> and
	/// <see cref="TypeDefinition.AddStaticField(Type,string,Visibility)"/>.
	/// </summary>
	public static IEnumerable<object[]> AddFieldTestData_WithDefaultValue
	{
		get
		{
			// ------------------------------------------------------------------------------------
			// different field names
			// ------------------------------------------------------------------------------------

			foreach (string name in FieldNames)
			{
				yield return
				[
					name,
					Visibility.Public,
					typeof(int),
					0
				];
			}

			// ------------------------------------------------------------------------------------
			// different visibilities
			// ------------------------------------------------------------------------------------

			foreach (Visibility visibility in Visibilities)
			{
				// skip test case covered above
				if (visibility == Visibility.Public)
					continue;

				yield return
				[
					null,
					visibility,
					typeof(int),
					0
				];
			}

			// ------------------------------------------------------------------------------------
			// different types
			// ------------------------------------------------------------------------------------

			//// value type (covered above)
			//yield return
			//[
			//	null,
			//	Visibility.Public,
			//	typeof(int),
			//	0
			//];

			// reference type
			yield return
			[
				null,
				Visibility.Public,
				typeof(string),
				null
			];
		}
	}

	/// <summary>
	/// Test data for tests targeting the following methods:<br/>
	/// - <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/><br/>
	/// - <see cref="TypeDefinition.AddField(Type,string,Visibility,object)"/><br/>
	/// - <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/><br/>
	/// - <see cref="TypeDefinition.AddStaticField(Type,string,Visibility,object)"/><br/>
	/// with different field types.
	/// </summary>
	public static IEnumerable<object[]> AddFieldTestData_InitialValue
	{
		get
		{
			// ------------------------------------------------------------------------------------
			// different field names
			// ------------------------------------------------------------------------------------

			foreach (string name in FieldNames)
			{
				// skip test case covered below...
				if (name == null)
					continue;

				yield return [name, Visibility.Public, typeof(int), 0];
			}

			// ------------------------------------------------------------------------------------
			// different visibilities
			// ------------------------------------------------------------------------------------

			foreach (Visibility visibility in Visibilities)
			{
				// skip test case covered below
				if (visibility == Visibility.Public)
					continue;

				yield return [null, visibility, typeof(int), 0];
			}

			// ------------------------------------------------------------------------------------
			// different types and initial values
			// ------------------------------------------------------------------------------------

			// System.Boolean
			yield return [null, Visibility.Public, typeof(bool), false]; // should emit OpCodes.Ldc_I4_0
			yield return [null, Visibility.Public, typeof(bool), true];  // should emit OpCodes.Ldc_I4_1

			// System.Char
			// (field initializers have optimizations for small integers)
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(char), (char)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(char), (char)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(char), char.MaxValue];                        // should emit OpCodes.Ldc_I4

			// System.SByte
			// (field initializers have optimizations for small integers)
			yield return [null, Visibility.Public, typeof(sbyte), (sbyte)-1];                             // should emit OpCodes.Ldc_I4_M1
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(sbyte), (sbyte)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(sbyte), sbyte.MinValue];                        // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(sbyte), sbyte.MaxValue];                        // should emit OpCodes.Ldc_I4_S

			// System.Byte
			// (field initializers have optimizations for small integers)
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(byte), (byte)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(byte), (byte)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(byte), byte.MaxValue];                        // should emit OpCodes.Ldc_I4

			// System.Int16
			// (field initializers have optimizations for small integers)
			yield return [null, Visibility.Public, typeof(short), (short)-1];                             // should emit OpCodes.Ldc_I4_M1
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(short), (short)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(short), (short)sbyte.MinValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(short), (short)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(short), short.MinValue];                        // should emit OpCodes.Ldc_I4
			yield return [null, Visibility.Public, typeof(short), short.MaxValue];                        // should emit OpCodes.Ldc_I4

			// System.UInt16
			// (field initializers have optimizations for small integers)
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(ushort), (ushort)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(ushort), (ushort)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(ushort), ushort.MaxValue];                        // should emit OpCodes.Ldc_I4

			// System.Int32
			// (field initializers have optimizations for small integers)
			yield return [null, Visibility.Public, typeof(int), -1];                             // should emit OpCodes.Ldc_I4_M1
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(int), i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(int), (int)sbyte.MinValue];            // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(int), (int)sbyte.MaxValue];            // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(int), int.MinValue];                   // should emit OpCodes.Ldc_I4
			yield return [null, Visibility.Public, typeof(int), int.MaxValue];                   // should emit OpCodes.Ldc_I4

			// System.UInt32
			// (field initializers have optimizations for small integers)
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(uint), (uint)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(uint), (uint)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(uint), uint.MaxValue];                        // should emit OpCodes.Ldc_I4

			// System.Int64
			// (field initializers have optimizations for small integers)
			yield return [null, Visibility.Public, typeof(long), (long)-1];                             // should emit OpCodes.Ldc_I4_M1 followed by OpCodes.Conv_I8
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(long), (long)i]; // should emit OpCodes.Ldc_I4_{0..8} followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(long), (long)sbyte.MinValue];                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(long), (long)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(long), (long)int.MinValue];                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(long), (long)int.MaxValue];                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(long), long.MinValue];                        // should emit OpCodes.Ldc_I8
			yield return [null, Visibility.Public, typeof(long), long.MaxValue];                        // should emit OpCodes.Ldc_I8

			// System.UInt64
			// (field initializers have optimizations for small integers)
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(ulong), (ulong)i]; // should emit OpCodes.Ldc_I4_{0..8} followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(ulong), (ulong)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(ulong), (ulong)int.MaxValue];                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(ulong), ulong.MaxValue];                        // should emit OpCodes.Ldc_I8

			// enumeration with underlying type System.SByte
			// (field initializers have optimizations for small integers)
			yield return [null, Visibility.Public, typeof(TestEnumS8), (TestEnumS8)(-1)];                           // should emit OpCodes.Ldc_I4_M1
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(TestEnumS8), (TestEnumS8)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(TestEnumS8), (TestEnumS8)sbyte.MinValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(TestEnumS8), (TestEnumS8)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S

			// enumeration with underlying type System.Byte
			// (field initializers have optimizations for small integers)
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(TestEnumU8), (TestEnumU8)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(TestEnumU8), (TestEnumU8)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(TestEnumU8), (TestEnumU8)byte.MaxValue];                  // should emit OpCodes.Ldc_I4

			// enumeration with underlying type System.Int16
			// (field initializers have optimizations for small integers)
			yield return [null, Visibility.Public, typeof(TestEnumS16), (TestEnumS16)(-1)];                           // should emit OpCodes.Ldc_I4_M1
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(TestEnumS16), (TestEnumS16)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(TestEnumS16), (TestEnumS16)sbyte.MinValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(TestEnumS16), (TestEnumS16)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(TestEnumS16), (TestEnumS16)short.MinValue];                 // should emit OpCodes.Ldc_I4
			yield return [null, Visibility.Public, typeof(TestEnumS16), (TestEnumS16)short.MaxValue];                 // should emit OpCodes.Ldc_I4

			// enumeration with underlying type System.UInt16
			// (field initializers have optimizations for small integers)
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(TestEnumU16), (TestEnumU16)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(TestEnumU16), (TestEnumU16)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(TestEnumU16), (TestEnumU16)ushort.MaxValue];                // should emit OpCodes.Ldc_I4

			// enumeration with underlying type System.Int32
			// (field initializers have optimizations for small integers)
			yield return [null, Visibility.Public, typeof(TestEnumS32), (TestEnumS32)(-1)];                           // should emit OpCodes.Ldc_I4_M1
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(TestEnumS32), (TestEnumS32)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(TestEnumS32), (TestEnumS32)sbyte.MinValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(TestEnumS32), (TestEnumS32)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(TestEnumS32), (TestEnumS32)int.MinValue];                   // should emit OpCodes.Ldc_I4
			yield return [null, Visibility.Public, typeof(TestEnumS32), (TestEnumS32)int.MaxValue];                   // should emit OpCodes.Ldc_I4

			// enumeration with underlying type System.UInt32
			// (field initializers have optimizations for small integers)
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(TestEnumU32), (TestEnumU32)i]; // should emit OpCodes.Ldc_I4_{0..8}
			yield return [null, Visibility.Public, typeof(TestEnumU32), (TestEnumU32)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S
			yield return [null, Visibility.Public, typeof(TestEnumU32), (TestEnumU32)uint.MaxValue];                  // should emit OpCodes.Ldc_I4

			// enumeration with underlying type System.Int64
			// (field initializers have optimizations for small integers)
			yield return [null, Visibility.Public, typeof(TestEnumS64), (TestEnumS64)(-1)];                           // should emit OpCodes.Ldc_I4_M1 followed by OpCodes.Conv_I8
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(TestEnumS64), (TestEnumS64)i]; // should emit OpCodes.Ldc_I4_{0..8} followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(TestEnumS64), (TestEnumS64)sbyte.MinValue];                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(TestEnumS64), (TestEnumS64)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(TestEnumS64), (TestEnumS64)int.MinValue];                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(TestEnumS64), (TestEnumS64)int.MaxValue];                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(TestEnumS64), (TestEnumS64)long.MinValue];                  // should emit OpCodes.Ldc_I8
			yield return [null, Visibility.Public, typeof(TestEnumS64), (TestEnumS64)long.MaxValue];                  // should emit OpCodes.Ldc_I8

			// enumeration with underlying type System.UInt64
			// (field initializers have optimizations for small integers)
			for (int i = 0; i <= 8; i++) yield return [null, Visibility.Public, typeof(TestEnumU64), (TestEnumU64)i]; // should emit OpCodes.Ldc_I4_{0..8} followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(TestEnumU64), (TestEnumU64)sbyte.MaxValue];                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(TestEnumU64), (TestEnumU64)int.MaxValue];                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
			yield return [null, Visibility.Public, typeof(TestEnumU64), (TestEnumU64)ulong.MaxValue];                 // should emit OpCodes.Ldc_I8

			// System.Single
			yield return [null, Visibility.Public, typeof(float), 0.5f]; // should emit OpCodes.Ldc_R4

			// System.Double
			yield return [null, Visibility.Public, typeof(double), 0.5]; // should emit OpCodes.Ldc_R8

			// System.String
			yield return [null, Visibility.Public, typeof(string), "just-a-string"];
			yield return [null, Visibility.Public, typeof(string), null];

			// System.DateTime
			yield return [null, Visibility.Public, typeof(DateTime), DateTime.Now];
		}
	}

	/// <summary>
	/// Test data for tests targeting...<br/>
	/// - <see cref="TypeDefinition.AddField{T}(string,Visibility,FieldInitializer)"/><br/>
	/// - <see cref="TypeDefinition.AddField(Type,string,Visibility,FieldInitializer)"/><br/>
	/// - <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,FieldInitializer)"/><br/>
	/// - <see cref="TypeDefinition.AddStaticField(Type,string,Visibility,FieldInitializer)"/>
	/// </summary>
	public static IEnumerable<object[]> AddFieldTestData_WithInitializer
	{
		get
		{
			foreach (string name in new[] { "Field", null })
			foreach (Visibility visibility in Visibilities)
			{
				// value type
				yield return
				[
					name,
					visibility,
					typeof(int),
					new FieldInitializer((_, msilGenerator) => { msilGenerator.Emit(OpCodes.Ldc_I4, 100); }),
					100
				];

				// reference type
				yield return
				[
					name,
					visibility,
					typeof(string),
					new FieldInitializer((_, msilGenerator) => { msilGenerator.Emit(OpCodes.Ldstr, "just-a-string"); }),
					"just-a-string"
				];

				// reference type (null)
				yield return
				[
					name,
					visibility,
					typeof(string),
					new FieldInitializer((_, msilGenerator) => { msilGenerator.Emit(OpCodes.Ldnull); }),
					null
				];
			}
		}
	}

	/// <summary>
	/// Test data for tests targeting...<br/>
	/// - <see cref="TypeDefinition.AddField{T}(string,Visibility,ProvideValueCallback{T})"/><br/>
	/// - <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,ProvideValueCallback{T})"/>
	/// </summary>
	public static IEnumerable<object[]> AddFieldTestData_WithTypedInitialValueCallback
	{
		get
		{
			foreach (string name in new[] { "Field", null })
			foreach (Visibility visibility in Visibilities)
			{
				// value type
				yield return
				[
					name,
					visibility,
					typeof(int),
					new ProvideValueCallback<int>(() => 100),
					100
				];

				// reference type
				yield return
				[
					name,
					visibility,
					typeof(string),
					new ProvideValueCallback<string>(() => "just-a-string"),
					"just-a-string"
				];

				// reference type (null)
				yield return
				[
					name,
					visibility,
					typeof(string),
					new ProvideValueCallback<string>(() => null),
					null
				];
			}
		}
	}

	/// <summary>
	/// Test data for tests targeting...<br/>
	/// - <see cref="TypeDefinition.AddField(Type,string,Visibility,ProvideValueCallback)"/><br/>
	/// - <see cref="TypeDefinition.AddStaticField(Type,string,Visibility,ProvideValueCallback)"/>
	/// </summary>
	public static IEnumerable<object[]> AddFieldTestData_WithUntypedInitialValueCallback
	{
		get
		{
			foreach (string name in new[] { "Field", null })
			foreach (Visibility visibility in Visibilities)
			{
				// value type
				yield return
				[
					name,
					visibility,
					typeof(int),
					new ProvideValueCallback(() => 100),
					100
				];

				// reference type
				yield return
				[
					name,
					visibility,
					typeof(string),
					new ProvideValueCallback(() => "just-a-string"),
					"just-a-string"
				];

				// reference type (null)
				yield return
				[
					name,
					visibility,
					typeof(string),
					new ProvideValueCallback(() => null),
					null
				];
			}
		}
	}

	#endregion

	#region AddField<T>(string name, Visibility visibility)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddField{T}(string,Visibility)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="expectedInitialValue">The expected initial value of the field.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithDefaultValue))]
	public void AddFieldT_WithDefaultValue(
		string     name,
		Visibility visibility,
		Type       fieldType,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addFieldMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddField))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(fieldType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(Visibility)]));
		var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, [name, visibility]);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddField(Type type, string name, Visibility visibility)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddField(Type,string,Visibility)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="expectedInitialValue">The expected initial value of the field.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithDefaultValue))]
	public void AddField_WithDefaultValue(
		string     name,
		Visibility visibility,
		Type       fieldType,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		IGeneratedField addedField = definition.AddField(fieldType, name, visibility);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddStaticField<T>(string name, Visibility visibility)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticField{T}(string,Visibility)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="expectedInitialValue">The expected initial value of the field.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithDefaultValue))]
	public void AddStaticFieldT_WithDefaultValue(
		string     name,
		Visibility visibility,
		Type       fieldType,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addFieldMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddStaticField))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(fieldType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(Visibility)]));
		var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, [name, visibility]);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		// (the instance is not required for accessing the static field, but tests whether the type can be created successfully)
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);
		Assert.NotNull(instance);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(null);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddStaticField(Type type, string name, Visibility visibility)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticField(Type,string,Visibility)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="expectedInitialValue">The expected initial value of the field.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithDefaultValue))]
	public void AddStaticField_WithDefaultValue(
		string     name,
		Visibility visibility,
		Type       fieldType,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		IGeneratedField addedField = definition.AddStaticField(fieldType, name, visibility);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		// (the instance is not required for accessing the static field, but tests whether the type can be created successfully)
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);
		Assert.NotNull(instance);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(null);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddField<T>(string name, Visibility visibility, T initialValue)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="initialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_InitialValue))]
	public void AddFieldT_WithInitialValue(
		string     name,
		Visibility visibility,
		Type       fieldType,
		object     initialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addFieldMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddField))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(fieldType))
			.Single(method => method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual([typeof(string), typeof(Visibility), fieldType]));
		var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, [name, visibility, initialValue]);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(initialValue, fieldValue);
	}

	#endregion

	#region AddField(Type type, string name, Visibility visibility, object initialValue)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="initialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_InitialValue))]
	public void AddField_WithInitialValue(
		string     name,
		Visibility visibility,
		Type       fieldType,
		object     initialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		IGeneratedField addedField = definition.AddField(fieldType, name, visibility, initialValue);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(initialValue, fieldValue);
	}

	#endregion

	#region AddStaticField<T>(string name, Visibility visibility, T initialValue)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="initialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_InitialValue))]
	public void AddStaticFieldT_WithInitialValue(
		string     name,
		Visibility visibility,
		Type       fieldType,
		object     initialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addFieldMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddStaticField))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(fieldType))
			.Single(method => method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual([typeof(string), typeof(Visibility), fieldType]));
		var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, [name, visibility, initialValue]);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		// (the instance is not required for accessing the static field, but tests whether the type can be created successfully)
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);
		Assert.NotNull(instance);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(null);
		Assert.Equal(initialValue, fieldValue);
	}

	#endregion

	#region AddStaticField(Type type, string name, Visibility visibility, object initialValue)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticField(Type,string,Visibility,object)"/> method.
	/// </summary>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="initialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_InitialValue))]
	public void AddStaticField_WithInitialValue(
		string     name,
		Visibility visibility,
		Type       fieldType,
		object     initialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		IGeneratedField addedField = definition.AddStaticField(fieldType, name, visibility, initialValue);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(initialValue, fieldValue);
	}

	#endregion

	#region AddField<T>(string name, Visibility visibility, FieldInitializer initializer)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddField{T}(string,Visibility,FieldInitializer)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="initializer">Field initializer to use when initializing the field (must be of type <see cref="FieldInitializer"/>).</param>
	/// <param name="expectedInitialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithInitializer))]
	public void AddFieldT_WithInitialValueInitializer(
		string     name,
		Visibility visibility,
		Type       fieldType,
		Delegate   initializer,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addFieldMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddField))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(fieldType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(Visibility), typeof(FieldInitializer)]));
		var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, [name, visibility, initializer]);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddField(Type type, string name, Visibility visibility, FieldInitializer initializer)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddField(Type,string,Visibility,FieldInitializer)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="initializer">Field initializer to use when initializing the field (must be of type <see cref="FieldInitializer"/>).</param>
	/// <param name="expectedInitialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithInitializer))]
	public void AddField_WithInitialValueInitializer(
		string     name,
		Visibility visibility,
		Type       fieldType,
		Delegate   initializer,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		IGeneratedField addedField = definition.AddField(fieldType, name, visibility, (FieldInitializer)initializer);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddStaticField<T>(string name, Visibility visibility, FieldInitializer<T> initializer)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,FieldInitializer)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="initializer">Field initializer to use when initializing the field (must be of type <see cref="FieldInitializer"/>).</param>
	/// <param name="expectedInitialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithInitializer))]
	public void AddStaticFieldT_WithInitialValueInitializer(
		string     name,
		Visibility visibility,
		Type       fieldType,
		Delegate   initializer,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addFieldMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddStaticField))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(fieldType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(Visibility), typeof(FieldInitializer)]));
		var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, [name, visibility, initializer]);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		// (the instance is not required for accessing the static field, but tests whether the type can be created successfully)
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);
		Assert.NotNull(instance);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(null);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddStaticField(Type type, string name, Visibility visibility, FieldInitializer initializer)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticField(Type,string,Visibility,FieldInitializer)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="initializer">Field initializer to use when initializing the field (must be of type <see cref="FieldInitializer"/>).</param>
	/// <param name="expectedInitialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithInitializer))]
	public void AddStaticField_WithInitialValueInitializer(
		string     name,
		Visibility visibility,
		Type       fieldType,
		Delegate   initializer,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		IGeneratedField addedField = definition.AddStaticField(fieldType, name, visibility, (FieldInitializer)initializer);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
		FieldInfo field = type.GetField(addedField.Name, bindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddField<T>(string name, Visibility visibility, ProvideValueCallback<T> provideInitialValueCallback)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddField{T}(string,Visibility,ProvideValueCallback{T})"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="provideInitialValueCallback">
	/// Callback that provides the initial value of the field (must be of type
	/// <see cref="ProvideValueCallback{T}"/>).
	/// </param>
	/// <param name="expectedInitialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithTypedInitialValueCallback))]
	public void AddFieldT_WithInitialValueCallback(
		string     name,
		Visibility visibility,
		Type       fieldType,
		Delegate   provideInitialValueCallback,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addFieldMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddField))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(fieldType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(Visibility), typeof(ProvideValueCallback<>).MakeGenericType(fieldType)]));
		var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, [name, visibility, provideInitialValueCallback]);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddField(Type type, string name, Visibility visibility, ProvideValueCallback provideInitialValueCallback)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddField(Type,string,Visibility,ProvideValueCallback)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="provideInitialValueCallback">
	/// Callback that provides the initial value of the field (must be of type <see cref="ProvideValueCallback"/>).
	/// </param>
	/// <param name="expectedInitialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithUntypedInitialValueCallback))]
	public void AddField_WithInitialValueCallback(
		string     name,
		Visibility visibility,
		Type       fieldType,
		Delegate   provideInitialValueCallback,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		IGeneratedField addedField = definition.AddField(fieldType, name, visibility, (ProvideValueCallback)provideInitialValueCallback);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddStaticField<T>(string name, Visibility visibility, ProvideValueCallback<T> provideInitialValueCallback)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,ProvideValueCallback{T})"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="provideInitialValueCallback">
	/// Callback that provides the initial value of the field (must be of type
	/// <see cref="ProvideValueCallback{T}"/>).
	/// </param>
	/// <param name="expectedInitialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithTypedInitialValueCallback))]
	public void AddStaticFieldT_WithInitialValueCallback(
		string     name,
		Visibility visibility,
		Type       fieldType,
		Delegate   provideInitialValueCallback,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addFieldMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddStaticField))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(fieldType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(Visibility), typeof(ProvideValueCallback<>).MakeGenericType(fieldType)]));
		var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, [name, visibility, provideInitialValueCallback]);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		// (the instance is not required for accessing the static field, but tests whether the type can be created successfully)
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);
		Assert.NotNull(instance);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(null);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#region AddStaticField(Type type, string name, Visibility visibility, ProvideValueCallback provideInitialValueCallback)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticField(Type,string,Visibility,ProvideValueCallback)"/> method.
	/// </summary>
	/// <param name="name">Name of the field to add.</param>
	/// <param name="visibility">Visibility of the field to add.</param>
	/// <param name="fieldType">Type of the field to add.</param>
	/// <param name="provideInitialValueCallback">
	/// Callback that provides the initial value of the field (must be of type <see cref="ProvideValueCallback"/>).
	/// </param>
	/// <param name="expectedInitialValue">The initial value of the field to set.</param>
	[Theory]
	[MemberData(nameof(AddFieldTestData_WithUntypedInitialValueCallback))]
	public void AddStaticField_WithInitialValueCallback(
		string     name,
		Visibility visibility,
		Type       fieldType,
		Delegate   provideInitialValueCallback,
		object     expectedInitialValue)
	{
		// create a new type definition and add the field
		TDefinition definition = CreateTypeDefinition();
		IGeneratedField addedField = definition.AddStaticField(fieldType, name, visibility, (ProvideValueCallback)provideInitialValueCallback);
		Assert.NotNull(addedField);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// check whether the field has the expected initial value
		FieldInfo field = type.GetField(addedField.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(field);
		object fieldValue = field.GetValue(instance);
		Assert.Equal(expectedInitialValue, fieldValue);
	}

	#endregion

	#endregion // Adding Fields

	#region Adding Events

	#region Test Data

	/// <summary>
	/// Names to test with when adding events to the type definition.
	/// </summary>
	public static IEnumerable<string> EventNames
	{
		get
		{
			yield return "Event";
			yield return null;
		}
	}

	/// <summary>
	/// Test data for tests targeting the following methods:<br/>
	/// - <see cref="TypeDefinition.AddEvent{T}(string,Visibility,IEventImplementation)"/><br/>
	/// - <see cref="TypeDefinition.AddEvent(Type,string,Visibility,IEventImplementation)"/><br/>
	/// - <see cref="TypeDefinition.AddStaticEvent{T}(string,Visibility,IEventImplementation)"/><br/>
	/// - <see cref="TypeDefinition.AddStaticEvent(Type,string,Visibility,IEventImplementation)"/><br/>
	/// - <see cref="ClassDefinition.AddVirtualEvent{T}(string,Visibility,IEventImplementation)"/><br/>
	/// - <see cref="ClassDefinition.AddVirtualEvent(Type,string,Visibility,IEventImplementation)"/><br/>
	/// using <see cref="TestEventImplementation"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	public static IEnumerable<object[]> AddEventTestData_WithImplementationStrategy
	{
		get
		{
			// ------------------------------------------------------------------------------------
			// different event names
			// (do not add an event raiser)
			// ------------------------------------------------------------------------------------

			foreach (string name in EventNames)
			{
				// System.EventHandler
				yield return
				[
					name,                 // name of the event
					Visibility.Public,    // visibility of the event
					typeof(EventHandler), // event handler type
					false,                // determines whether to add an event raiser method
					null,                 // name of the event raiser (null to create a random name)
					Visibility.Public,    // visibility of the event raiser method
					null,                 // expected return type of the generated event raiser method
					null                  // expected parameter types of the generated event raiser method
				];
			}

			// ------------------------------------------------------------------------------------
			// different visibilities
			// (do not add an event raiser)
			// ------------------------------------------------------------------------------------

			foreach (Visibility visibility in Visibilities)
			{
				// skip 'public' as covered above
				if (visibility == Visibility.Public)
					continue;

				// System.EventHandler
				yield return
				[
					null,                 // name of the event (null to use a random name)
					visibility,           // visibility of the event
					typeof(EventHandler), // event handler type
					false,                // determines whether to add an event raiser method
					null,                 // name of the event raiser (null to create a random name)
					Visibility.Public,    // visibility of the event raiser method
					null,                 // expected return type of the generated event raiser method
					null                  // expected parameter types of the generated event raiser method
				];
			}

			// ------------------------------------------------------------------------------------
			// add event raiser method or not
			// ------------------------------------------------------------------------------------

			//// covered above...
			//// --------------------------------------------------------------------------
			//// System.EventHandler
			//yield return
			//[
			//	null,                 // name of the event
			//	Visibility.Public,    // visibility of the event
			//	typeof(EventHandler), // event handler type
			//	false,                // determines whether to add an event raiser method
			//	null,                 // name of the event raiser (null to create a random name)
			//	Visibility.Public,    // visibility of the event raiser method
			//	null,                 // expected return type of the generated event raiser method
			//	null                  // expected parameter types of the generated event raiser method
			//];
			//// --------------------------------------------------------------------------

			// System.EventHandler
			// (create event raiser with different visibilities)
			foreach (Visibility visibility in Visibilities)
			{
				// skip 'public' as this case is handled above
				if (visibility == Visibility.Public)
					continue;

				yield return
				[
					null,                 // name of the event
					Visibility.Public,    // visibility of the event
					typeof(EventHandler), // event handler type
					true,                 // determines whether to add an event raiser method
					null,                 // name of the event raiser (null to create a random name)
					visibility,           // visibility of the event raiser method
					typeof(void),         // expected return type of the generated event raiser method
					Type.EmptyTypes       // expected parameter types of the generated event raiser method
				];
			}

			// System.EventHandler
			// (create event raiser with a specific name)
			yield return
			[
				null,                 // name of the event
				Visibility.Public,    // visibility of the event
				typeof(EventHandler), // event handler type
				true,                 // determines whether to add an event raiser method
				"XYZ",                // name of the event raiser (null to create a random name)
				Visibility.Public,    // visibility of the event raiser method
				typeof(void),         // expected return type of the generated event raiser method
				Type.EmptyTypes       // expected parameter types of the generated event raiser method
			];
		}
	}

	/// <summary>
	/// Test data for tests targeting the following methods:<br/>
	/// - <see cref="TypeDefinition.AddEvent{T}(string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/><br/>
	/// - <see cref="TypeDefinition.AddEvent(Type,string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/><br/>
	/// - <see cref="TypeDefinition.AddStaticEvent{T}(string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/><br/>
	/// - <see cref="TypeDefinition.AddStaticEvent(Type,string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/><br/>
	/// - <see cref="ClassDefinition.AddVirtualEvent{T}(string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/><br/>
	/// - <see cref="ClassDefinition.AddVirtualEvent(Type,string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/><br/>
	/// using parts of <see cref="TestEventImplementation"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	public static IEnumerable<object[]> AddEventTestData_WithImplementationCallbacks
	{
		get
		{
			foreach (string name in new[] { "Event", null })
			foreach (Visibility visibility in Visibilities)
			{
				// always:
				// event type: System.EventHandler
				// event raiser: void OnEvent())
				yield return
				[
					name,                // name of the event
					visibility,          // visibility of the event
					typeof(EventHandler) // event handler type
				];
			}
		}
	}

	#endregion

	#region AddEvent<T>(string name, Visibility visibility, IEventImplementation implementation)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddEvent{T}(string,Visibility,IEventImplementation)"/> method
	/// using <see cref="TestEventImplementation"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	/// <param name="letStrategyAddEventRaiserMethod">
	/// <c>true</c> to let the strategy add the event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
	/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
	/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
	/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationStrategy))]
	public void AddEventT_WithImplementationStrategy(
		string     name,
		Visibility visibility,
		Type       eventHandlerType,
		bool       letStrategyAddEventRaiserMethod,
		string     eventRaiserName,
		Visibility eventRaiserVisibility,
		Type       expectedEventRaiserReturnType,
		Type[]     expectedEventRaiserParameterTypes)
	{
		TestAddEvent_WithImplementationStrategy(
			name: name,
			kind: EventKind.Normal,
			visibility: visibility,
			letStrategyAddEventRaiserMethod: letStrategyAddEventRaiserMethod,
			eventRaiserName: eventRaiserName,
			eventRaiserVisibility: eventRaiserVisibility,
			eventHandlerType: eventHandlerType,
			expectedEventRaiserReturnType: expectedEventRaiserReturnType,
			expectedEventRaiserParameterTypes: expectedEventRaiserParameterTypes,
			addEventCallback: (definition, implementation) =>
			{
				// get the AddEvent(...) method to test
				MethodInfo addEventMethod = typeof(TypeDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(TypeDefinition.AddEvent))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(eventHandlerType))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual([typeof(string), typeof(Visibility), typeof(IEventImplementation)]));

				// add the event to the type definition
				var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(definition, [name, visibility, implementation]);
				Assert.NotNull(addedEvent);
				Assert.Same(implementation, addedEvent.Implementation);

				return addedEvent;
			});
	}

	#endregion

	#region AddEvent(Type type, string name, Visibility visibility, IEventImplementation implementation)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddEvent(Type,string,Visibility,IEventImplementation)"/> method
	/// using <see cref="TestEventImplementation"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	/// <param name="letStrategyAddEventRaiserMethod">
	/// <c>true</c> to let the strategy add the event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
	/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
	/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
	/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationStrategy))]
	public void AddEvent_WithImplementationStrategy(
		string     name,
		Visibility visibility,
		Type       eventHandlerType,
		bool       letStrategyAddEventRaiserMethod,
		string     eventRaiserName,
		Visibility eventRaiserVisibility,
		Type       expectedEventRaiserReturnType,
		Type[]     expectedEventRaiserParameterTypes)
	{
		TestAddEvent_WithImplementationStrategy(
			name: name,
			kind: EventKind.Normal,
			eventHandlerType: eventHandlerType,
			visibility: visibility,
			letStrategyAddEventRaiserMethod: letStrategyAddEventRaiserMethod,
			eventRaiserName: eventRaiserName,
			eventRaiserVisibility: eventRaiserVisibility,
			expectedEventRaiserReturnType: expectedEventRaiserReturnType,
			expectedEventRaiserParameterTypes: expectedEventRaiserParameterTypes,
			addEventCallback: (definition, implementation) =>
			{
				IGeneratedEvent addedEvent = definition.AddEvent(eventHandlerType, name, visibility, implementation);
				Assert.NotNull(addedEvent);
				Assert.Same(implementation, addedEvent.Implementation);

				return addedEvent;
			});
	}

	#endregion

	#region AddEvent<T>(string name, Visibility visibility, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddEvent{T}(string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/> method
	/// using a callback to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationCallbacks))]
	public void AddEventT_WithImplementationCallbacks(
		string     name,
		Visibility visibility,
		Type       eventHandlerType)
	{
		TestAddEvent_WithImplementationCallbacks(
			name: name,
			kind: EventKind.Normal,
			eventHandlerType: eventHandlerType,
			visibility: visibility,
			addEventCallback: (definition, implementation) =>
			{
				// prepare callback to add the 'add' accessor and the 'remove' accessor
				// (the callbacks implement the standard event behavior to allow re-using test code for the 'standard' event implementation strategy)
				void ImplementAddAccessorCallback(IGeneratedEvent    @event, ILGenerator msilGenerator) => implementation.ImplementAddAccessorMethod(definition, @event, msilGenerator);
				void ImplementRemoveAccessorCallback(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementRemoveAccessorMethod(definition, @event, msilGenerator);

				// get the AddEvent<T>(...) method to test
				MethodInfo addEventMethod = typeof(TypeDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(TypeDefinition.AddEvent))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(eventHandlerType))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual([typeof(string), typeof(Visibility), typeof(EventAccessorImplementationCallback), typeof(EventAccessorImplementationCallback)]));

				// invoke the method to add the event to the type definition
				var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
					definition,
					[
						name,
						visibility,
						(EventAccessorImplementationCallback)ImplementAddAccessorCallback,
						(EventAccessorImplementationCallback)ImplementRemoveAccessorCallback
					]);

				Assert.NotNull(addedEvent);
				return addedEvent;
			});
	}

	#endregion

	#region AddEvent(Type type, string name, Visibility visibility, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddEvent(Type,string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/>
	/// method
	/// using a callback to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationCallbacks))]
	public void AddEvent_WithImplementationCallbacks(
		string     name,
		Visibility visibility,
		Type       eventHandlerType)
	{
		TestAddEvent_WithImplementationCallbacks(
			name: name,
			kind: EventKind.Normal,
			eventHandlerType: eventHandlerType,
			visibility: visibility,
			addEventCallback: (definition, implementation) =>
			{
				// prepare callback to add the 'add' accessor and the 'remove' accessor
				// (the callbacks implement the standard event behavior to allow re-using test code for the 'standard' event implementation strategy)
				void ImplementAddAccessorCallback(IGeneratedEvent    @event, ILGenerator msilGenerator) => implementation.ImplementAddAccessorMethod(definition, @event, msilGenerator);
				void ImplementRemoveAccessorCallback(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementRemoveAccessorMethod(definition, @event, msilGenerator);

				// the event to the type definition
				IGeneratedEvent addedEvent = definition.AddEvent(
					eventHandlerType,
					name,
					visibility,
					ImplementAddAccessorCallback,
					ImplementRemoveAccessorCallback);

				Assert.NotNull(addedEvent);
				return addedEvent;
			});
	}

	#endregion

	#region AddStaticEvent<T>(string name, Visibility visibility, IEventImplementation implementation)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticEvent{T}(string,Visibility,IEventImplementation)"/> method
	/// using <see cref="TestEventImplementation"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	/// <param name="letStrategyAddEventRaiserMethod">
	/// <c>true</c> to let the strategy add the event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
	/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
	/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
	/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationStrategy))]
	public void AddStaticEventT_WithImplementationStrategy_Standard(
		string     name,
		Visibility visibility,
		Type       eventHandlerType,
		bool       letStrategyAddEventRaiserMethod,
		string     eventRaiserName,
		Visibility eventRaiserVisibility,
		Type       expectedEventRaiserReturnType,
		Type[]     expectedEventRaiserParameterTypes)
	{
		TestAddEvent_WithImplementationStrategy(
			name: name,
			kind: EventKind.Static,
			visibility: visibility,
			letStrategyAddEventRaiserMethod: letStrategyAddEventRaiserMethod,
			eventRaiserName: eventRaiserName,
			eventRaiserVisibility: eventRaiserVisibility,
			eventHandlerType: eventHandlerType,
			expectedEventRaiserReturnType: expectedEventRaiserReturnType,
			expectedEventRaiserParameterTypes: expectedEventRaiserParameterTypes,
			addEventCallback: (definition, implementation) =>
			{
				// get the AddEvent(...) method to test
				MethodInfo addEventMethod = typeof(TypeDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(TypeDefinition.AddStaticEvent))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(eventHandlerType))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual([typeof(string), typeof(Visibility), typeof(IEventImplementation)]));

				// add the event to the type definition
				var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(definition, [name, visibility, implementation]);
				Assert.NotNull(addedEvent);
				Assert.Same(implementation, addedEvent.Implementation);

				return addedEvent;
			});
	}

	#endregion

	#region AddStaticEvent(Type type, string name, Visibility visibility, IEventImplementation implementation)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticEvent(Type,string,Visibility,IEventImplementation)"/> method
	/// using <see cref="TestEventImplementation"/> to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	/// <param name="letStrategyAddEventRaiserMethod">
	/// <c>true</c> to let the strategy add the event raiser method;<br/>
	/// otherwise <c>false</c>.
	/// </param>
	/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
	/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
	/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
	/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationStrategy))]
	public void AddStaticEvent_WithImplementationStrategy(
		string     name,
		Visibility visibility,
		Type       eventHandlerType,
		bool       letStrategyAddEventRaiserMethod,
		string     eventRaiserName,
		Visibility eventRaiserVisibility,
		Type       expectedEventRaiserReturnType,
		Type[]     expectedEventRaiserParameterTypes)
	{
		TestAddEvent_WithImplementationStrategy(
			name: name,
			kind: EventKind.Static,
			eventHandlerType: eventHandlerType,
			visibility: visibility,
			letStrategyAddEventRaiserMethod: letStrategyAddEventRaiserMethod,
			eventRaiserName: eventRaiserName,
			eventRaiserVisibility: eventRaiserVisibility,
			expectedEventRaiserReturnType: expectedEventRaiserReturnType,
			expectedEventRaiserParameterTypes: expectedEventRaiserParameterTypes,
			addEventCallback: (definition, implementation) =>
			{
				IGeneratedEvent addedEvent = definition.AddStaticEvent(eventHandlerType, name, visibility, implementation);
				Assert.NotNull(addedEvent);
				Assert.Same(implementation, addedEvent.Implementation);

				return addedEvent;
			});
	}

	#endregion

	#region AddStaticEvent<T>(string name, Visibility visibility, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticEvent{T}(string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/>
	/// method
	/// using a callback to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationCallbacks))]
	public void AddStaticEventT_WithImplementationCallbacks(
		string     name,
		Visibility visibility,
		Type       eventHandlerType)
	{
		TestAddEvent_WithImplementationCallbacks(
			name: name,
			kind: EventKind.Static,
			eventHandlerType: eventHandlerType,
			visibility: visibility,
			addEventCallback: (definition, implementation) =>
			{
				// prepare callback to add the 'add' accessor and the 'remove' accessor
				// (the callbacks implement the standard event behavior to allow re-using test code for the 'standard' event implementation strategy)
				void ImplementAddAccessorCallback(IGeneratedEvent    @event, ILGenerator msilGenerator) => implementation.ImplementAddAccessorMethod(definition, @event, msilGenerator);
				void ImplementRemoveAccessorCallback(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementRemoveAccessorMethod(definition, @event, msilGenerator);

				// get the AddEvent<T>(...) method to test
				MethodInfo addEventMethod = typeof(TypeDefinition)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(method => method.Name == nameof(TypeDefinition.AddStaticEvent))
					.Where(method => method.GetGenericArguments().Length == 1)
					.Select(method => method.MakeGenericMethod(eventHandlerType))
					.Single(
						method => method
							.GetParameters()
							.Select(parameter => parameter.ParameterType)
							.SequenceEqual([typeof(string), typeof(Visibility), typeof(EventAccessorImplementationCallback), typeof(EventAccessorImplementationCallback)]));

				// invoke the method to add the event to the type definition
				var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
					definition,
					[
						name,
						visibility,
						(EventAccessorImplementationCallback)ImplementAddAccessorCallback,
						(EventAccessorImplementationCallback)ImplementRemoveAccessorCallback
					]);

				Assert.NotNull(addedEvent);
				return addedEvent;
			});
	}

	#endregion

	#region AddStaticEvent(Type type, string name, Visibility visibility, EventAccessorImplementationCallback addAccessorImplementationCallback, EventAccessorImplementationCallback removeAccessorImplementationCallback)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticEvent(Type,string,Visibility,EventAccessorImplementationCallback,EventAccessorImplementationCallback)"/>
	/// method
	/// using a callback to implement add/remove accessors and the event raiser method.
	/// </summary>
	/// <param name="name">Name of the event to add.</param>
	/// <param name="visibility">Visibility of the event to add.</param>
	/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
	[Theory]
	[MemberData(nameof(AddEventTestData_WithImplementationCallbacks))]
	public void AddStaticEvent_WithImplementationCallbacks(
		string     name,
		Visibility visibility,
		Type       eventHandlerType)
	{
		TestAddEvent_WithImplementationCallbacks(
			name: name,
			kind: EventKind.Static,
			eventHandlerType: eventHandlerType,
			visibility: visibility,
			addEventCallback: (definition, implementation) =>
			{
				// prepare callback to add the 'add' accessor and the 'remove' accessor
				// (the callbacks implement the standard event behavior to allow re-using test code for the 'standard' event implementation strategy)
				void ImplementAddAccessorCallback(IGeneratedEvent    @event, ILGenerator msilGenerator) => implementation.ImplementAddAccessorMethod(definition, @event, msilGenerator);
				void ImplementRemoveAccessorCallback(IGeneratedEvent @event, ILGenerator msilGenerator) => implementation.ImplementRemoveAccessorMethod(definition, @event, msilGenerator);

				// the event to the type definition
				IGeneratedEvent addedEvent = definition.AddStaticEvent(
					eventHandlerType,
					name,
					visibility,
					ImplementAddAccessorCallback,
					ImplementRemoveAccessorCallback);

				Assert.NotNull(addedEvent);
				return addedEvent;
			});
	}

	#endregion

	#region Common Test Code

	internal void TestAddEvent_WithImplementationStrategy(
		string                                                   name,
		EventKind                                                kind,
		Type                                                     eventHandlerType,
		Visibility                                               visibility,
		bool                                                     letStrategyAddEventRaiserMethod,
		string                                                   eventRaiserName,
		Visibility                                               eventRaiserVisibility,
		Type                                                     expectedEventRaiserReturnType,
		Type[]                                                   expectedEventRaiserParameterTypes,
		Func<TDefinition, IEventImplementation, IGeneratedEvent> addEventCallback)
	{
		// create a new type definition
		TDefinition definition = CreateTypeDefinition();

		// create an instance of the implementation strategy
		TestEventImplementation implementation = letStrategyAddEventRaiserMethod
			                                         ? new TestEventImplementation(eventRaiserName, eventRaiserVisibility)
			                                         : new TestEventImplementation();

		// add the event
		IGeneratedEvent addedEvent = addEventCallback(definition, implementation);
		string actualName = AssertEventName(name, addedEvent.Name);
		Assert.NotNull(addedEvent);
		Assert.Equal(kind, addedEvent.Kind);
		Assert.Equal(visibility, addedEvent.Visibility);
		Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
		Assert.Same(implementation, addedEvent.Implementation);

		// add an event raiser method, if the strategy did not add one
		if (!letStrategyAddEventRaiserMethod)
		{
			Assert.Null(expectedEventRaiserReturnType);
			Assert.Null(expectedEventRaiserParameterTypes);
			eventRaiserName ??= "FireMyEvent";
			expectedEventRaiserReturnType = typeof(void);
			expectedEventRaiserParameterTypes = Type.EmptyTypes;
			definition.AddMethod(
				kind == EventKind.Static ? MethodKind.Static : MethodKind.Normal,
				eventRaiserName,
				expectedEventRaiserReturnType,
				expectedEventRaiserParameterTypes,
				eventRaiserVisibility,
				(_, msilGenerator) => implementation.ImplementRaiserMethod(
					definition,
					addedEvent,
					msilGenerator));
		}

		// determine the event raiser method
		string expectedEventRaiserName = eventRaiserName ?? "On" + actualName;
		IGeneratedMethod eventRaiserMethod = definition.GeneratedMethods.SingleOrDefault(method => method.Name == expectedEventRaiserName);
		Assert.NotNull(eventRaiserMethod);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the implementation of the event
		TestEventImplementation(
			definition,
			instance,
			actualName,
			kind,
			visibility,
			eventHandlerType,
			true,
			expectedEventRaiserName,
			expectedEventRaiserReturnType,
			expectedEventRaiserParameterTypes);
	}

	internal void TestAddEvent_WithImplementationCallbacks(
		string                                                      name,
		EventKind                                                   kind,
		Type                                                        eventHandlerType,
		Visibility                                                  visibility,
		Func<TDefinition, TestEventImplementation, IGeneratedEvent> addEventCallback)
	{
		// create a new type definition
		TDefinition definition = CreateTypeDefinition();

		// create an instance of the implementation strategy
		// (it is not used as a whole, but parts of it => eliminates duplicate test code)
		var implementation = new TestEventImplementation();

		// add the event
		IGeneratedEvent addedEvent = addEventCallback(definition, implementation);
		string actualName = AssertEventName(name, addedEvent.Name);
		Assert.NotNull(addedEvent);
		Assert.Equal(kind, addedEvent.Kind);
		Assert.Equal(visibility, addedEvent.Visibility);
		Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
		Assert.Null(addedEvent.Implementation);

		// declare backing field
		implementation.Declare(definition, addedEvent);

		// add an event raiser method
		// (should always just be: public void FireMyEvent();
		Type eventRaiserReturnType = typeof(void);
		Type[] eventRaiserParameterTypes = Type.EmptyTypes;
		string eventRaiserName = "FireMyEvent";
		IGeneratedMethod eventRaiserMethod = definition.AddMethod(
			kind == EventKind.Static ? MethodKind.Static : MethodKind.Normal,
			eventRaiserName,
			eventRaiserReturnType,
			eventRaiserParameterTypes,
			Visibility.Public,
			(_, msilGenerator) => implementation.ImplementRaiserMethod(definition, addedEvent, msilGenerator));

		// determine the actual name of the event raiser method
		string actualEventRaiserName = AssertMethodName(eventRaiserName, eventRaiserMethod.Name);

		// create the defined type, check the result against the definition and create an instance of that type
		Type type = definition.CreateType();
		CheckTypeAgainstDefinition(type, definition);
		object instance = Activator.CreateInstance(type);

		// test the implementation of the event
		TestEventImplementation(
			definition,
			instance,
			actualName,
			kind,
			visibility,
			eventHandlerType,
			true,
			actualEventRaiserName,
			eventRaiserReturnType,
			eventRaiserParameterTypes);
	}

	#endregion

	#endregion // Adding Events

	#region Adding Properties

	#region Test Data

	/// <summary>
	/// Names to test with when adding properties to the type definition.
	/// </summary>
	protected static IEnumerable<string> PropertyNames
	{
		get
		{
			yield return "Property";
			yield return null;
		}
	}

	/// <summary>
	/// Test data for tests targeting adding new properties.
	/// </summary>
	public static IEnumerable<object[]> AddPropertyTestData
	{
		get
		{
			// ------------------------------------------------------------------------------------
			// different method names
			// ------------------------------------------------------------------------------------
			foreach (string name in PropertyNames)
			{
				yield return
				[
					name,                    // name
					typeof(int),             // property type
					Visibility.Public,       // visibility
					new object[] { 1, 2, 3 } // test objects
				];
			}

			// ------------------------------------------------------------------------------------
			// different visibilities
			// ------------------------------------------------------------------------------------

			foreach (Visibility visibility in Visibilities)
			{
				yield return
				[
					null,                       // name (random)
					typeof(long),               // property type (type does not matter, but 'long' would cause a duplicate test case)
					visibility,                 // visibility
					new object[] { 1L, 2L, 3L } // test objects
				];
			}

			// ------------------------------------------------------------------------------------
			// different property types
			// ------------------------------------------------------------------------------------

			// value (covered above)
			//yield return
			//[
			//	null,                       // name (random)
			//	typeof(int),                // property type
			//	Visibility.Public,          // visibility
			//  new object[] { 1L, 2L, 3L } // test objects
			//];

			// reference
			yield return
			[
				null,                          // name (random)
				typeof(string),                // property type
				Visibility.Public,             // visibility
				new object[] { "A", "B", "C" } // test objects
			];
		}
	}

	#endregion

	#region AddProperty<T>(string name)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddProperty{T}(string)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddPropertyT_WithoutImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property (actually one property with get/set accessors,
		// one property with get accessor only, one property with set accessor only and one property without a get/set accessor)
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_getSet" : null]);
		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_getOnly" : null]);
		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_setOnly" : null]);
		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_none" : null]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Normal,
			propertyType,
			accessorVisibility,
			null,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddProperty(Type type, string name)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddProperty(Type,string)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddProperty_WithoutImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddProperty))
			.Single(
				method => !method.IsGenericMethod && method
					          .GetParameters()
					          .Select(parameter => parameter.ParameterType)
					          .SequenceEqual([typeof(Type), typeof(string)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_getSet" : null]);
		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_getOnly" : null]);
		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_setOnly" : null]);
		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_none" : null]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Normal,
			propertyType,
			accessorVisibility,
			null,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddProperty<T>(string name, IPropertyImplementation implementation)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddProperty{T}(string,IPropertyImplementation)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddPropertyT_WithImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property (actually one property with get/set accessors,
		// one property with get accessor only, one property with set accessor only and one property without a get/set accessor)
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(IPropertyImplementation)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		var implementation = new PropertyImplementation_TestDataStorage(handle);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_getSet" : null,
				implementation
			]);

		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_getOnly" : null,
				implementation
			]);

		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_setOnly" : null,
				implementation
			]);

		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_none" : null,
				implementation
			]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Normal,
			propertyType,
			accessorVisibility,
			implementation,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddProperty(Type type, string name, IPropertyImplementation implementation)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddProperty(Type,string,IPropertyImplementation)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddProperty_WithImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddProperty))
			.Single(
				method => !method.IsGenericMethod && method
					          .GetParameters()
					          .Select(parameter => parameter.ParameterType)
					          .SequenceEqual([typeof(Type), typeof(string), typeof(IPropertyImplementation)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		var implementation = new PropertyImplementation_TestDataStorage(handle);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_getSet" : null,
				implementation
			]);

		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_getOnly" : null,
				implementation
			]);

		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_setOnly" : null,
				implementation
			]);

		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_none" : null,
				implementation
			]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Normal,
			propertyType,
			accessorVisibility,
			implementation,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddStaticProperty<T>(string name)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticProperty{T}(string)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddStaticPropertyT_WithoutImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property (actually one property with get/set accessors,
		// one property with get accessor only, one property with set accessor only and one property without a get/set accessor)
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddStaticProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_getSet" : null]);
		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_getOnly" : null]);
		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_setOnly" : null]);
		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [name != null ? name + "_none" : null]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Static,
			propertyType,
			accessorVisibility,
			null,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddStaticProperty(Type type, string name)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticProperty(Type,string)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddStaticProperty_WithoutImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddStaticProperty))
			.Single(
				method => !method.IsGenericMethod && method
					          .GetParameters()
					          .Select(parameter => parameter.ParameterType)
					          .SequenceEqual([typeof(Type), typeof(string)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_getSet" : null]);
		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_getOnly" : null]);
		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_setOnly" : null]);
		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(definition, [propertyType, name != null ? name + "_none" : null]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Static,
			propertyType,
			accessorVisibility,
			null,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddStaticProperty<T>(string name, IPropertyImplementation implementation)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticProperty{T}(string,IPropertyImplementation)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddStaticPropertyT_WithImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property (actually one property with get/set accessors,
		// one property with get accessor only, one property with set accessor only and one property without a get/set accessor)
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddStaticProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(IPropertyImplementation)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		var implementation = new PropertyImplementation_TestDataStorage(handle);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_getSet" : null,
				implementation
			]);

		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_getOnly" : null,
				implementation
			]);

		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_setOnly" : null,
				implementation
			]);

		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				name != null ? name + "_none" : null,
				implementation
			]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Static,
			propertyType,
			accessorVisibility,
			implementation,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region AddStaticProperty(Type type, string name, IPropertyImplementation implementation)

	/// <summary>
	/// Tests the <see cref="TypeDefinition.AddStaticProperty(Type,string, IPropertyImplementation)"/> method.
	/// </summary>
	/// <param name="name">Name of the property to add.</param>
	/// <param name="propertyType">Type of the property to add.</param>
	/// <param name="accessorVisibility">Visibility the 'get'/'set' accessor should have.</param>
	/// <param name="testObjects">Test values to use when playing with accessor methods.</param>
	[Theory]
	[MemberData(nameof(AddPropertyTestData))]
	public void AddStaticProperty_WithImplementationStrategy(
		string     name,
		Type       propertyType,
		Visibility accessorVisibility,
		object[]   testObjects)
	{
		// create a new type definition and add the property
		TDefinition definition = CreateTypeDefinition();
		MethodInfo addPropertyMethod = typeof(TypeDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(TypeDefinition.AddStaticProperty))
			.Single(
				method => !method.IsGenericMethod && method
					          .GetParameters()
					          .Select(parameter => parameter.ParameterType)
					          .SequenceEqual([typeof(Type), typeof(string), typeof(IPropertyImplementation)]));

		using var storage = new TestDataStorage();
		int handle = storage.Add(testObjects[0]);
		var implementation = new PropertyImplementation_TestDataStorage(handle);

		var addedProperty_getSet = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_getSet" : null,
				implementation
			]);

		var addedProperty_getOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_getOnly" : null,
				implementation
			]);

		var addedProperty_setOnly = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_setOnly" : null,
				implementation
			]);

		var addedProperty_none = (IGeneratedProperty)addPropertyMethod.Invoke(
			definition,
			[
				propertyType,
				name != null ? name + "_none" : null,
				implementation
			]);

		// add accessor methods and test the property
		AddProperty_CommonPart(
			definition,
			PropertyKind.Static,
			propertyType,
			accessorVisibility,
			implementation,
			handle,
			testObjects,
			addedProperty_getSet,
			addedProperty_getOnly,
			addedProperty_setOnly,
			addedProperty_none);
	}

	#endregion

	#region Common Test Code

	/// <summary>
	/// Checks whether the specified properties have the expected initial state, implements the accessor methods using the <see cref="TestDataStorage"/>
	/// and tests whether the implementation works as expected.
	/// </summary>
	/// <param name="definition">The definition of the generated type containing the properties to test.</param>
	/// <param name="expectedPropertyKind">The expected kind of the properties to test.</param>
	/// <param name="expectedPropertyType">The expected type of the properties to test.</param>
	/// <param name="accessorVisibility">Visibility of the accessor methods to implement.</param>
	/// <param name="implementation">Implementation strategy the property should use to implement (<c>null</c> to implement using callbacks).</param>
	/// <param name="handle">Handle of the test object in the <see cref="TestDataStorage"/> backing the property.</param>
	/// <param name="testObjects">Test objects to use when playing with the 'get'/'set' accessor methods.</param>
	/// <param name="addedProperty_getSet">The added property to test (with 'get'/'set' accessor).</param>
	/// <param name="addedProperty_getOnly">The added property to test (with 'get' accessor only).</param>
	/// <param name="addedProperty_setOnly">The added property to test (with 'set' accessor only).</param>
	/// <param name="addedProperty_none">The added property to test (without 'get'/'set' accessor).</param>
	internal static void AddProperty_CommonPart(
		TDefinition             definition,
		PropertyKind            expectedPropertyKind,
		Type                    expectedPropertyType,
		Visibility              accessorVisibility,
		IPropertyImplementation implementation,
		int                     handle,
		IReadOnlyList<object>   testObjects,
		IGeneratedProperty      addedProperty_getSet,
		IGeneratedProperty      addedProperty_getOnly,
		IGeneratedProperty      addedProperty_setOnly,
		IGeneratedProperty      addedProperty_none)
	{
		// the added properties should not be null
		Assert.NotNull(addedProperty_getSet);
		Assert.NotNull(addedProperty_getOnly);
		Assert.NotNull(addedProperty_setOnly);
		Assert.NotNull(addedProperty_none);

		// check whether the added properties are in the expected state
		TestAddedProperty(addedProperty_getSet, expectedPropertyKind, expectedPropertyType, implementation);
		TestAddedProperty(addedProperty_getOnly, expectedPropertyKind, expectedPropertyType, implementation);
		TestAddedProperty(addedProperty_setOnly, expectedPropertyKind, expectedPropertyType, implementation);
		TestAddedProperty(addedProperty_none, expectedPropertyKind, expectedPropertyType, implementation);

		// implement get/set accessor methods (abstract properties are declared only)
		// (the actual data is stored in the test data storage, so it can be inspected more easily)
		if (implementation != null || expectedPropertyKind == PropertyKind.Abstract)
		{
			addedProperty_getSet.AddGetAccessor(accessorVisibility, getAccessorImplementationCallback: null);
			addedProperty_getSet.AddSetAccessor(accessorVisibility, setAccessorImplementationCallback: null);
			addedProperty_getOnly.AddGetAccessor(accessorVisibility, getAccessorImplementationCallback: null);
			addedProperty_setOnly.AddSetAccessor(accessorVisibility, setAccessorImplementationCallback: null);
		}
		else
		{
			addedProperty_getSet.AddGetAccessor(accessorVisibility, (p,  g) => EmitPropertyGetAccessorWithTestDataStorageCallback(p, handle, g));
			addedProperty_getSet.AddSetAccessor(accessorVisibility, (p,  g) => EmitPropertySetAccessorWithTestDataStorageCallback(p, handle, g));
			addedProperty_getOnly.AddGetAccessor(accessorVisibility, (p, g) => EmitPropertyGetAccessorWithTestDataStorageCallback(p, handle, g));
			addedProperty_setOnly.AddSetAccessor(accessorVisibility, (p, g) => EmitPropertySetAccessorWithTestDataStorageCallback(p, handle, g));
		}

		// check whether the accessor methods in the generated property have been set accordingly
		Assert.NotNull(addedProperty_getOnly.GetAccessor);
		Assert.Null(addedProperty_getOnly.SetAccessor);
		Assert.Null(addedProperty_setOnly.GetAccessor);
		Assert.NotNull(addedProperty_setOnly.SetAccessor);
		Assert.NotNull(addedProperty_getSet.GetAccessor);
		Assert.NotNull(addedProperty_getSet.SetAccessor);

		// create the defined type, check the result against the definition
		Type generatedType = definition.CreateType();
		CheckTypeAgainstDefinition(generatedType, definition);

		// create an instance of that generated type and test the property implementation,
		// if the property has an implementation
		if (expectedPropertyKind != PropertyKind.Abstract)
		{
			// test the property implementation
			object instance = Activator.CreateInstance(generatedType);
			TestPropertyImplementation(addedProperty_getSet, instance, testObjects, handle);
			TestPropertyImplementation(addedProperty_getOnly, instance, testObjects, handle);
			TestPropertyImplementation(addedProperty_setOnly, instance, testObjects, handle);
			TestPropertyImplementation(addedProperty_none, instance, testObjects, handle);
		}
	}

	/// <summary>
	/// Tests the state of a newly added property.
	/// </summary>
	/// <param name="generatedProperty">The property to test.</param>
	/// <param name="expectedPropertyKind">The expected property kind.</param>
	/// <param name="expectedPropertyType">The expected property type.</param>
	/// <param name="expectedImplementation">The expected property implementation (<c>null</c>).</param>
	private static void TestAddedProperty(
		IGeneratedProperty      generatedProperty,
		PropertyKind            expectedPropertyKind,
		Type                    expectedPropertyType,
		IPropertyImplementation expectedImplementation)
	{
		// check whether the property is of the expected property kind
		Assert.Equal(expectedPropertyKind, generatedProperty.Kind);

		// check whether the property is of the expected type
		Assert.Equal(expectedPropertyType, generatedProperty.PropertyType);

		// check whether the property has an initialized property builder
		Assert.NotNull(generatedProperty.PropertyBuilder);

		// check whether the property has the expected implementation strategy
		Assert.Same(expectedImplementation, generatedProperty.Implementation);

		// newly added properties should not have accessor methods, yet
		Assert.Null(generatedProperty.GetAccessor);
		Assert.Null(generatedProperty.SetAccessor);
	}

	/// <summary>
	/// Tests a property that has been implemented to use the <see cref="TestDataStorage"/> as backing storage.
	/// </summary>
	/// <param name="generatedProperty">The property to test.</param>
	/// <param name="instance">Instance of the dynamically created type that contains the property.</param>
	/// <param name="testObjects">Test objects to use when playing with the 'get'/'set' accessors.</param>
	/// <param name="testDataHandle">Handle of the test data field in the backing storage.</param>
	private static void TestPropertyImplementation(
		IProperty             generatedProperty,
		object                instance,
		IReadOnlyList<object> testObjects,
		int                   testDataHandle)
	{
		Type generatedType = instance.GetType();
		PropertyInfo property = generatedType.GetProperty(generatedProperty.Name, ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(property);

		// reset instance if the property is static to make getting/setting them below work as expected
		if (generatedProperty.Kind == PropertyKind.Static)
			instance = null;

		if (generatedProperty.Kind != PropertyKind.Abstract)
		{
			// reset test data in the backing storage
			TestDataStorage.Set(testDataHandle, testObjects[0]);

			// check whether the get accessor returns the expected initial value
			if (property.CanRead)
				Assert.Equal(testObjects[0], property.GetValue(instance));

			// check whether the set accessor modifies the test data in the backing storage
			if (property.CanWrite)
			{
				// property has a set accessor
				// => use it to modify the data in the backing storage
				property.SetValue(instance, testObjects[1]);
				Assert.Equal(testObjects[1], TestDataStorage.Get(testDataHandle));
			}
			else
			{
				// property does not have a set accessor
				// => directly change data in the backing storage
				TestDataStorage.Set(testDataHandle, testObjects[1]);
			}

			// check whether the get accessor returns the changed value
			if (property.CanRead)
				Assert.Equal(testObjects[1], property.GetValue(instance));
		}
	}

	#endregion

	#endregion // Adding Properties

	#region Adding Methods

	/// <summary>
	/// Names to test with when adding methods to the type definition.
	/// </summary>
	public static IEnumerable<string> MethodNames
	{
		get
		{
			yield return "Method";
			yield return null;
		}
	}

	/// <summary>
	/// Method kinds to test with when adding methods to the type definition.
	/// </summary>
	public static IEnumerable<MethodKind> MethodKinds => Enum.GetValues(typeof(MethodKind)).Cast<MethodKind>();

	/// <summary>
	/// Test data for tests targeting adding new methods.
	/// </summary>
	public static IEnumerable<object[]> AddMethodTestData
	{
		get
		{
			// ------------------------------------------------------------------------------------
			// different method names
			// ------------------------------------------------------------------------------------
			foreach (string name in MethodNames)
			{
				Type type = typeof(int);

				CreateMethodTestData(
					type,
					2,
					out Type[] parameterTypes,
					out object[] testArguments,
					out object expectedTestResult);

				yield return
				[
					name,              // name
					type,              // return type
					parameterTypes,    // parameter types
					Visibility.Public, // visibility
					testArguments,     // test arguments
					expectedTestResult // expected test result
				];
			}

			// ------------------------------------------------------------------------------------
			// different visibilities
			// ------------------------------------------------------------------------------------

			foreach (Visibility visibility in Visibilities)
			{
				Type type = typeof(long);

				CreateMethodTestData(
					type,
					2,
					out Type[] parameterTypes,
					out object[] testArguments,
					out object expectedTestResult);

				yield return
				[
					null,              // name
					type,              // return type
					parameterTypes,    // parameter types
					visibility,        // visibility
					testArguments,     // test arguments
					expectedTestResult // expected test result
				];
			}

			// ------------------------------------------------------------------------------------
			// different types
			// ------------------------------------------------------------------------------------

			// value (covered above)
			//{
			//	Type type = typeof(int);

			//	CreateMethodTestData(
			//		type,
			//		2,
			//		out Type[] parameterTypes,
			//		out object[] testArguments,
			//		out object expectedTestResult);

			//	yield return
			//	[
			//		null,              // name
			//		type,              // return type
			//		parameterTypes,    // parameter types
			//		Visibility.Public, // visibility
			//		testArguments,     // test arguments
			//		expectedTestResult // expected test result
			//	];
			//}

			// reference
			{
				Type type = typeof(string);

				CreateMethodTestData(
					type,
					2,
					out Type[] parameterTypes,
					out object[] testArguments,
					out object expectedTestResult);

				yield return
				[
					null,              // name
					type,              // return type
					parameterTypes,    // parameter types
					Visibility.Public, // visibility
					testArguments,     // test arguments
					expectedTestResult // expected test result
				];
			}
		}
	}

	/// <summary>
	/// Test data for tests targeting adding new methods.
	/// </summary>
	public static IEnumerable<object[]> AddMethodWithKindTestData
	{
		get
		{
			// use existing test data to test adding 'normal' methods
			foreach (object[] data in AddMethodTestData)
			{
				yield return
				[
					MethodKind.Normal, // method kind
					data[0],           // name
					data[1],           // return type
					data[2],           // parameter types
					data[3],           // visibility
					data[4],           // test arguments
					data[5]            // expected test result
				];
			}

			// ------------------------------------------------------------------------------------
			// different method kinds
			// (skip adding normal methods (covered above))
			// ------------------------------------------------------------------------------------

			foreach (MethodKind kind in MethodKinds.Where(x => x != MethodKind.Normal && x != MethodKind.Override))
			{
				Type type = typeof(int);

				CreateMethodTestData(
					type,
					2,
					out Type[] parameterTypes,
					out object[] testArguments,
					out object expectedTestResult);

				yield return
				[
					kind,              // method kind
					null,              // name
					type,              // return type
					parameterTypes,    // parameter types
					Visibility.Public, // visibility
					testArguments,     // test arguments
					expectedTestResult // expected test result
				];
			}
		}
	}

	#region AddMethod(MethodKind kind, string name, Type returnType, Type[] parameterTypes, Visibility visibility, IMethodImplementation implementation, MethodAttributes additionalMethodAttributes = 0)

	/// <summary>
	/// Tests the
	/// <see cref="TypeDefinition.AddMethod(MethodKind,string,Type,Type[],Visibility,IMethodImplementation,MethodAttributes)"/>
	/// method.
	/// </summary>
	/// <param name="kind">Kind of the method to add.</param>
	/// <param name="name">Name of the method to add.</param>
	/// <param name="returnType">Return type of the method to add.</param>
	/// <param name="parameterTypes">Types of parameters of the method to add.</param>
	/// <param name="visibility">Visibility of the method to add.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodWithKindTestData))]
	public void AddMethod_WithKind_WithImplementationStrategy(
		MethodKind kind,
		string     name,
		Type       returnType,
		Type[]     parameterTypes,
		Visibility visibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		// create a new type definition
		TypeDefinition definition = kind != MethodKind.Abstract
			                            ? CreateTypeDefinition(null)
			                            : CreateTypeDefinition(null, TypeAttributes.Abstract);

		// test the method
		TestAddMethod(
			definition,
			kind,
			name,
			returnType,
			parameterTypes,
			visibility,
			testArguments,
			expectedTestResult,
			() =>
			{
				var implementation = new TestMethodImplementation();
				IGeneratedMethod addedMethodDefinition = definition.AddMethod(
					kind,
					name,
					returnType,
					parameterTypes,
					visibility,
					kind != MethodKind.Abstract ? implementation : null);

				if (kind != MethodKind.Abstract)
				{
					Assert.Same(implementation, addedMethodDefinition.Implementation);
				}
				else
				{
					Assert.Null(addedMethodDefinition.Implementation);
				}

				return addedMethodDefinition;
			});
	}

	#endregion

	#region AddMethod(MethodKind kind, string name, Type returnType, Type[] parameterTypes, Visibility visibility, MethodImplementationCallback implementationCallback, MethodAttributes additionalMethodAttributes = 0)

	/// <summary>
	/// Tests the
	/// <see cref="TypeDefinition.AddMethod(MethodKind,string,Type,Type[],Visibility,MethodImplementationCallback,MethodAttributes)"/>
	/// method.
	/// </summary>
	/// <param name="kind">Kind of the method to add.</param>
	/// <param name="name">Name of the method to add.</param>
	/// <param name="returnType">Return type of the method to add.</param>
	/// <param name="parameterTypes">Types of parameters of the method to add.</param>
	/// <param name="visibility">Visibility of the method to add.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodWithKindTestData))]
	public void AddMethod_WithKind_WithImplementationCallback(
		MethodKind kind,
		string     name,
		Type       returnType,
		Type[]     parameterTypes,
		Visibility visibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		// create a new type definition
		TypeDefinition definition = kind != MethodKind.Abstract
			                            ? CreateTypeDefinition(null)
			                            : CreateTypeDefinition(null, TypeAttributes.Abstract);

		// test the method
		TestAddMethod(
			definition,
			kind,
			name,
			returnType,
			parameterTypes,
			visibility,
			testArguments,
			expectedTestResult,
			() =>
			{
				IGeneratedMethod addedMethodDefinition = definition.AddMethod(
					kind,
					name,
					returnType,
					parameterTypes,
					visibility,
					kind != MethodKind.Abstract ? TestMethodImplementation.Callback : null);
				Assert.Null(addedMethodDefinition.Implementation);
				return addedMethodDefinition;
			});
	}

	#endregion

	#region AddMethod(string name, Type returnType, Type[] parameterTypes, Visibility visibility, IMethodImplementation implementation, MethodAttributes additionalMethodAttributes = 0)

	/// <summary>
	/// Tests the
	/// <see cref="TypeDefinition.AddMethod(string,Type,Type[],Visibility,IMethodImplementation,MethodAttributes)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the method to add.</param>
	/// <param name="returnType">Return type of the method to add.</param>
	/// <param name="parameterTypes">Types of parameters of the method to add.</param>
	/// <param name="visibility">Visibility of the method to add.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodTestData))]
	public void AddMethod_WithImplementationStrategy(
		string     name,
		Type       returnType,
		Type[]     parameterTypes,
		Visibility visibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		// create a new type definition
		TypeDefinition definition = CreateTypeDefinition(null);

		// test the method
		TestAddMethod(
			definition,
			MethodKind.Normal,
			name,
			returnType,
			parameterTypes,
			visibility,
			testArguments,
			expectedTestResult,
			() =>
			{
				var implementation = new TestMethodImplementation();
				IGeneratedMethod addedMethodDefinition = definition.AddMethod(
					MethodKind.Normal,
					name,
					returnType,
					parameterTypes,
					visibility,
					implementation);

				Assert.Same(implementation, addedMethodDefinition.Implementation);
				return addedMethodDefinition;
			});
	}

	#endregion

	#region AddMethod(string name, Type returnType, Type[] parameterTypes, Visibility visibility, MethodImplementationCallback implementationCallback, MethodAttributes additionalMethodAttributes = 0)

	/// <summary>
	/// Tests the
	/// <see cref="TypeDefinition.AddMethod(string,Type,Type[],Visibility,MethodImplementationCallback,MethodAttributes)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the method to add.</param>
	/// <param name="returnType">Return type of the method to add.</param>
	/// <param name="parameterTypes">Types of parameters of the method to add.</param>
	/// <param name="visibility">Visibility of the method to add.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodTestData))]
	public void AddMethod_WithImplementationCallback(
		string     name,
		Type       returnType,
		Type[]     parameterTypes,
		Visibility visibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		// create a new type definition
		TypeDefinition definition = CreateTypeDefinition(null);

		// test the method
		TestAddMethod(
			definition,
			MethodKind.Normal,
			name,
			returnType,
			parameterTypes,
			visibility,
			testArguments,
			expectedTestResult,
			() =>
			{
				var implementation = new TestMethodImplementation();
				IGeneratedMethod addedMethodDefinition = definition.AddMethod(
					MethodKind.Normal,
					name,
					returnType,
					parameterTypes,
					visibility,
					TestMethodImplementation.Callback);

				Assert.Null(addedMethodDefinition.Implementation);
				return addedMethodDefinition;
			});
	}

	#endregion

	#region AddStaticMethod(string name, Type returnType, Type[] parameterTypes, Visibility visibility, IMethodImplementation implementation, MethodAttributes additionalMethodAttributes = 0)

	/// <summary>
	/// Tests the
	/// <see cref="TypeDefinition.AddStaticMethod(string,Type,Type[],Visibility,IMethodImplementation,MethodAttributes)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the method to add.</param>
	/// <param name="returnType">Return type of the method to add.</param>
	/// <param name="parameterTypes">Types of parameters of the method to add.</param>
	/// <param name="visibility">Visibility of the method to add.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodTestData))]
	public void AddStaticMethod_WithImplementationStrategy(
		string     name,
		Type       returnType,
		Type[]     parameterTypes,
		Visibility visibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		// create a new type definition
		TypeDefinition definition = CreateTypeDefinition(null);

		// test the method
		TestAddMethod(
			definition,
			MethodKind.Static,
			name,
			returnType,
			parameterTypes,
			visibility,
			testArguments,
			expectedTestResult,
			() =>
			{
				var implementation = new TestMethodImplementation();
				IGeneratedMethod addedMethodDefinition = definition.AddMethod(
					MethodKind.Static,
					name,
					returnType,
					parameterTypes,
					visibility,
					implementation);

				Assert.Same(implementation, addedMethodDefinition.Implementation);
				return addedMethodDefinition;
			});
	}

	#endregion

	#region AddMethod(string name, Type returnType, Type[] parameterTypes, Visibility visibility, MethodImplementationCallback implementationCallback, MethodAttributes additionalMethodAttributes = 0)

	/// <summary>
	/// Tests the
	/// <see cref="TypeDefinition.AddStaticMethod(string,Type,Type[],Visibility,MethodImplementationCallback,MethodAttributes)"/>
	/// method.
	/// </summary>
	/// <param name="name">Name of the method to add.</param>
	/// <param name="returnType">Return type of the method to add.</param>
	/// <param name="parameterTypes">Types of parameters of the method to add.</param>
	/// <param name="visibility">Visibility of the method to add.</param>
	/// <param name="testArguments">Arguments to pass to the method when testing it.</param>
	/// <param name="expectedTestResult">Expected result returned by the method when testing it.</param>
	[Theory]
	[MemberData(nameof(AddMethodTestData))]
	public void AddStaticMethod_WithImplementationCallback(
		string     name,
		Type       returnType,
		Type[]     parameterTypes,
		Visibility visibility,
		object[]   testArguments,
		object     expectedTestResult)
	{
		// create a new type definition
		TypeDefinition definition = CreateTypeDefinition(null);

		// test the method
		TestAddMethod(
			definition,
			MethodKind.Static,
			name,
			returnType,
			parameterTypes,
			visibility,
			testArguments,
			expectedTestResult,
			() =>
			{
				IGeneratedMethod addedMethodDefinition = definition.AddMethod(
					MethodKind.Static,
					name,
					returnType,
					parameterTypes,
					visibility,
					TestMethodImplementation.Callback);

				Assert.Null(addedMethodDefinition.Implementation);
				return addedMethodDefinition;
			});
	}

	#endregion

	#endregion // Adding Methods
}
