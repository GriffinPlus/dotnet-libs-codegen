///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Xunit;

// ReSharper disable ConvertToLocalFunction
// ReSharper disable RedundantTypeArgumentsOfMethod

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	/// <summary>
	/// Common tests around the <see cref="TypeDefinition"/> class.
	/// </summary>
	public abstract class TypeDefinitionTests<TDefinition> where TDefinition : TypeDefinition
	{
		/// <summary>
		/// Creates a new type definition instance with a random name to test.
		/// </summary>
		/// <param name="name">Name of the type to create (may be <c>null</c> to create a random name).</param>
		/// <returns>The created type definition instance.</returns>
		public abstract TDefinition CreateTypeDefinition(string name = null);

		#region Common Test Data

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
		/// All supported visibilities.
		/// </summary>
		private static IEnumerable<Visibility> Visibilities
		{
			get
			{
				yield return Visibility.Public;
				yield return Visibility.Protected;
				yield return Visibility.ProtectedInternal;
				yield return Visibility.Internal;
				yield return Visibility.Private;
			}
		}

		#endregion

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
			var definition = CreateTypeDefinition(name);

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
		/// <param name="baseType">The type the type to create derives from.</param>
		protected static void CheckDefinitionAfterConstruction(TypeDefinition definition, string typeName, Type baseType)
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
#if NET461 || NET5_0 && WINDOWS
			Assert.Empty(definition.GeneratedDependencyProperties);
#elif NETCOREAPP3_1_OR_GREATER
			// Dependency properties are not supported on .NET Core
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
			var baseClassConstructors = GetConstructorsAccessibleFromDerivedType(baseType);
			foreach (var constructor in constructors)
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
		/// <param name="includeHidden"><c>true</c> to include hidden events; otherwise <c>false</c>.</param>
		/// <param name="eventsToCheck">Inherited events to check.</param>
		private static void CheckInheritedEvents(
			Type                         baseType,
			bool                         includeHidden,
			IEnumerable<IInheritedEvent> eventsToCheck)
		{
			var inheritedEvents = GetEventsAccessibleFromDerivedType(baseType, includeHidden);
			foreach (var @event in eventsToCheck)
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
		/// <param name="includeHidden"><c>true</c> to include hidden fields; otherwise <c>false</c>.</param>
		/// <param name="fieldsToCheck">Inherited fields to check.</param>
		private static void CheckInheritedFields(
			Type                         baseType,
			bool                         includeHidden,
			IEnumerable<IInheritedField> fieldsToCheck)
		{
			var inheritedFields = GetFieldsAccessibleFromDerivedType(baseType, includeHidden);
			foreach (var field in fieldsToCheck)
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
		/// <param name="includeHidden"><c>true</c> to include hidden methods; otherwise <c>false</c>.</param>
		/// <param name="methodsToCheck">Inherited methods to check.</param>
		private static void CheckInheritedMethods(
			Type                          baseType,
			bool                          includeHidden,
			IEnumerable<IInheritedMethod> methodsToCheck)
		{
			var inheritedMethods = GetMethodsAccessibleFromDerivedType(baseType, includeHidden);
			foreach (var method in methodsToCheck)
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
		/// <param name="includeHidden"><c>true</c> to include hidden properties; otherwise <c>false</c>.</param>
		/// <param name="propertiesToCheck">Inherited properties to check.</param>
		private static void CheckInheritedProperties(
			Type                            baseType,
			bool                            includeHidden,
			IEnumerable<IInheritedProperty> propertiesToCheck)
		{
			var inheritedProperties = GetPropertiesAccessibleFromDerivedType(baseType, includeHidden);
			foreach (var property in propertiesToCheck)
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
			var definition = CreateTypeDefinition(name);
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
		}

		#endregion

		#region Adding Fields

		#region Test Data for Tests targeting AddField<T>(...) and AddStaticField<T>(...)

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility)"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_NoInitialValue
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					yield return new object[] { name, visibility, typeof(int), 0 };       // value type
					yield return new object[] { name, visibility, typeof(string), null }; // reference type
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="bool"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Boolean
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.Boolean
					yield return new object[] { name, visibility, false }; // should emit OpCodes.Ldc_I4_0
					yield return new object[] { name, visibility, true };  // should emit OpCodes.Ldc_I4_1
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="char"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Char
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.Char
					// (field initializers have optimizations for small integers)
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (char)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (char)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, char.MaxValue };                        // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="sbyte"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_SByte
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.SByte
					// (field initializers have optimizations for small integers)
					yield return new object[] { name, visibility, (sbyte)-1 };                             // should emit OpCodes.Ldc_I4_M1
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (sbyte)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, sbyte.MinValue };                        // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, sbyte.MaxValue };                        // should emit OpCodes.Ldc_I4_S
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="byte"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Byte
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.Byte
					// (field initializers have optimizations for small integers)
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (byte)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (byte)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, byte.MaxValue };                        // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="short"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Int16
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.Int16
					// (field initializers have optimizations for small integers)
					yield return new object[] { name, visibility, (short)-1 };                             // should emit OpCodes.Ldc_I4_M1
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (short)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (short)sbyte.MinValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (short)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, short.MinValue };                        // should emit OpCodes.Ldc_I4
					yield return new object[] { name, visibility, short.MaxValue };                        // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="ushort"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_UInt16
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.UInt16
					// (field initializers have optimizations for small integers)
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (ushort)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (ushort)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, ushort.MaxValue };                        // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="int"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Int32
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.Int32
					// (field initializers have optimizations for small integers)
					yield return new object[] { name, visibility, -1 };                             // should emit OpCodes.Ldc_I4_M1
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (int)sbyte.MinValue };            // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (int)sbyte.MaxValue };            // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, int.MinValue };                   // should emit OpCodes.Ldc_I4
					yield return new object[] { name, visibility, int.MaxValue };                   // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="uint"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_UInt32
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.UInt32
					// (field initializers have optimizations for small integers)
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (uint)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (uint)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, uint.MaxValue };                        // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="long"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Int64
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.Int64
					// (field initializers have optimizations for small integers)
					yield return new object[] { name, visibility, (long)-1 };                             // should emit OpCodes.Ldc_I4_M1 followed by OpCodes.Conv_I8
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (long)i }; // should emit OpCodes.Ldc_I4_{0..8} followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (long)sbyte.MinValue };                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (long)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (long)int.MinValue };                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (long)int.MaxValue };                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, long.MinValue };                        // should emit OpCodes.Ldc_I8
					yield return new object[] { name, visibility, long.MaxValue };                        // should emit OpCodes.Ldc_I8
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="ulong"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_UInt64
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.UInt64
					// (field initializers have optimizations for small integers)
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (ulong)i }; // should emit OpCodes.Ldc_I4_{0..8} followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (ulong)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (ulong)int.MaxValue };                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, ulong.MaxValue };                        // should emit OpCodes.Ldc_I8
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="TestEnumS8"/>,
		/// an enumeration type backed by <see cref="sbyte"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Enum_SByte
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// enumeration with underlying type System.SByte
					// (field initializers have optimizations for small integers)
					yield return new object[] { name, visibility, (TestEnumS8)(-1) };                           // should emit OpCodes.Ldc_I4_M1
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (TestEnumS8)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (TestEnumS8)sbyte.MinValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (TestEnumS8)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="TestEnumU8"/>,
		/// an enumeration type backed by <see cref="byte"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Enum_Byte
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// enumeration with underlying type System.Byte
					// (field initializers have optimizations for small integers)
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (TestEnumU8)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (TestEnumU8)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (TestEnumU8)byte.MaxValue };                  // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="TestEnumS16"/>,
		/// an enumeration type backed by <see cref="short"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Enum_Int16
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// enumeration with underlying type System.Int16
					// (field initializers have optimizations for small integers)
					yield return new object[] { name, visibility, (TestEnumS16)(-1) };                           // should emit OpCodes.Ldc_I4_M1
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (TestEnumS16)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (TestEnumS16)sbyte.MinValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (TestEnumS16)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (TestEnumS16)short.MinValue };                 // should emit OpCodes.Ldc_I4
					yield return new object[] { name, visibility, (TestEnumS16)short.MaxValue };                 // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="TestEnumU16"/>,
		/// an enumeration type backed by <see cref="ushort"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Enum_UInt16
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// enumeration with underlying type System.UInt16
					// (field initializers have optimizations for small integers)
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (TestEnumU16)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (TestEnumU16)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (TestEnumU16)ushort.MaxValue };                // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="TestEnumS32"/>,
		/// an enumeration type backed by <see cref="int"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Enum_Int32
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// enumeration with underlying type System.Int32
					// (field initializers have optimizations for small integers)
					yield return new object[] { name, visibility, (TestEnumS32)(-1) };                           // should emit OpCodes.Ldc_I4_M1
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (TestEnumS32)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (TestEnumS32)sbyte.MinValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (TestEnumS32)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (TestEnumS32)int.MinValue };                   // should emit OpCodes.Ldc_I4
					yield return new object[] { name, visibility, (TestEnumS32)int.MaxValue };                   // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="TestEnumU32"/>,
		/// an enumeration type backed by <see cref="uint"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Enum_UInt32
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// enumeration with underlying type System.UInt32
					// (field initializers have optimizations for small integers)
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (TestEnumU32)i }; // should emit OpCodes.Ldc_I4_{0..8}
					yield return new object[] { name, visibility, (TestEnumU32)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S
					yield return new object[] { name, visibility, (TestEnumU32)uint.MaxValue };                  // should emit OpCodes.Ldc_I4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="TestEnumS64"/>,
		/// an enumeration type backed by <see cref="long"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Enum_Int64
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// enumeration with underlying type System.Int64
					// (field initializers have optimizations for small integers)
					yield return new object[] { name, visibility, (TestEnumS64)(-1) };                           // should emit OpCodes.Ldc_I4_M1 followed by OpCodes.Conv_I8
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (TestEnumS64)i }; // should emit OpCodes.Ldc_I4_{0..8} followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (TestEnumS64)sbyte.MinValue };                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (TestEnumS64)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (TestEnumS64)int.MinValue };                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (TestEnumS64)int.MaxValue };                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (TestEnumS64)long.MinValue };                  // should emit OpCodes.Ldc_I8
					yield return new object[] { name, visibility, (TestEnumS64)long.MaxValue };                  // should emit OpCodes.Ldc_I8
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="TestEnumU64"/>,
		/// an enumeration type backed by <see cref="ulong"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Enum_UInt64
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// enumeration with underlying type System.UInt64
					// (field initializers have optimizations for small integers)
					for (int i = 0; i <= 8; i++) yield return new object[] { name, visibility, (TestEnumU64)i }; // should emit OpCodes.Ldc_I4_{0..8} followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (TestEnumU64)sbyte.MaxValue };                 // should emit OpCodes.Ldc_I4_S followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (TestEnumU64)int.MaxValue };                   // should emit OpCodes.Ldc_I4 followed by OpCodes.Conv_I8
					yield return new object[] { name, visibility, (TestEnumU64)ulong.MaxValue };                 // should emit OpCodes.Ldc_I8
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="float"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Single
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.Single
					yield return new object[] { name, visibility, 0.5f }; // should emit OpCodes.Ldc_R4
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="double"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_Double
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.Double
					yield return new object[] { name, visibility, 0.5 }; // should emit OpCodes.Ldc_R8
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="string"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_String
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.String
					yield return new object[] { name, visibility, "just-a-string" };
					yield return new object[] { name, visibility, null };
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> with field type <see cref="DateTime"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_InitialValue_DateTime
		{
			get
			{
				foreach (string name in FieldNames)
				foreach (Visibility visibility in Visibilities)
				{
					// System.DateTime
					// (should result in using a factory callback)
					yield return new object[] { name, visibility, DateTime.Now };
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,FieldInitializer{T})"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,FieldInitializer{T})"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_WithInitializer
		{
			get
			{
				foreach (string name in new[] { "Field", null })
				foreach (Visibility visibility in Visibilities)
				{
					// value type
					yield return new object[]
					{
						name,
						visibility,
						typeof(int),
						new FieldInitializer<int>((field, msilGenerator) => { msilGenerator.Emit(OpCodes.Ldc_I4, 100); }),
						100
					};

					// reference type
					yield return new object[]
					{
						name,
						visibility,
						typeof(string),
						new FieldInitializer<string>((field, msilGenerator) => { msilGenerator.Emit(OpCodes.Ldstr, "just-a-string"); }),
						"just-a-string"
					};
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddField{T}(string,Visibility,ProvideValueCallback{T})"/> and
		/// <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,ProvideValueCallback{T})"/>.
		/// </summary>
		public static IEnumerable<object[]> AddFieldTestData_WithInitialValueCallback
		{
			get
			{
				foreach (string name in new[] { "Field", null })
				foreach (Visibility visibility in Visibilities)
				{
					// value type
					yield return new object[]
					{
						name,
						visibility,
						typeof(int),
						new ProvideValueCallback<int>(() => 100),
						100
					};

					// reference type
					yield return new object[]
					{
						name,
						visibility,
						typeof(string),
						new ProvideValueCallback<string>(() => "just-a-string"),
						"just-a-string"
					};
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
		[MemberData(nameof(AddFieldTestData_NoInitialValue))]
		public void AddFieldT_WithDefaultValue(
			string     name,
			Visibility visibility,
			Type       fieldType,
			object     expectedInitialValue)
		{
			// create a new type definition and add the field
			var definition = CreateTypeDefinition();
			var addFieldMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddField))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(fieldType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility) }));
			var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, new object[] { name, visibility });

			// create the defined type, check the result against the definition and create an instance of that type
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);

			// check whether the field has the expected expected initial value
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			var field = type.GetField(addedField.Name, bindingFlags);
			Assert.NotNull(field);
			var fieldValue = field.GetValue(instance);
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
		[MemberData(nameof(AddFieldTestData_NoInitialValue))]
		public void AddStaticFieldT_WithDefaultValue(
			string     name,
			Visibility visibility,
			Type       fieldType,
			object     expectedInitialValue)
		{
			// create a new type definition and add the field
			var definition = CreateTypeDefinition();
			var addFieldMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddStaticField))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(fieldType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility) }));
			var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, new object[] { name, visibility });

			// create the defined type, check the result against the definition and create an instance of that type
			// (the instance is not required for accessing the static field, but tests whether the type can be created successfully)
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);
			Assert.NotNull(instance);

			// check whether the field has the expected initial value
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
			var field = type.GetField(addedField.Name, bindingFlags);
			Assert.NotNull(field);
			var fieldValue = field.GetValue(null);
			Assert.Equal(expectedInitialValue, fieldValue);
		}

		#endregion

		#region AddField<T>(string name, Visibility visibility, T initialValue)

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Boolean))]
		public void AddFieldT_WithInitialValue_Boolean(string name, Visibility visibility, bool initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Char))]
		public void AddFieldT_WithInitialValue_Char(string name, Visibility visibility, char initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_SByte))]
		public void AddFieldT_WithInitialValue_SByte(string name, Visibility visibility, sbyte initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Byte))]
		public void AddFieldT_WithInitialValue_Byte(string name, Visibility visibility, byte initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Int16))]
		public void AddFieldT_WithInitialValue_Int16(string name, Visibility visibility, short initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_UInt16))]
		public void AddFieldT_WithInitialValue_UInt16(string name, Visibility visibility, ushort initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Int32))]
		public void AddFieldT_WithInitialValue_Int32(string name, Visibility visibility, int initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_UInt32))]
		public void AddFieldT_WithInitialValue_UInt32(string name, Visibility visibility, uint initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Int64))]
		public void AddFieldT_WithInitialValue_Int64(string name, Visibility visibility, long initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_UInt64))]
		public void AddFieldT_WithInitialValue_UInt64(string name, Visibility visibility, ulong initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_SByte))]
		public void AddFieldT_WithInitialValue_Enum_Byte(string name, Visibility visibility, TestEnumS8 initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_Byte))]
		public void AddFieldT_WithInitialValue_Enum_SByte(string name, Visibility visibility, TestEnumU8 initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_Int16))]
		public void AddFieldT_WithInitialValue_Enum_Int16(string name, Visibility visibility, TestEnumS16 initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_UInt16))]
		public void AddFieldT_WithInitialValue_Enum_UInt16(string name, Visibility visibility, TestEnumU16 initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_Int32))]
		public void AddFieldT_WithInitialValue_Enum_Int32(string name, Visibility visibility, TestEnumS32 initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_UInt32))]
		public void AddFieldT_WithInitialValue_Enum_UInt32(string name, Visibility visibility, TestEnumU32 initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_Int64))]
		public void AddFieldT_WithInitialValue_Enum_Int64(string name, Visibility visibility, TestEnumS64 initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_UInt64))]
		public void AddFieldT_WithInitialValue_Enum_UInt64(string name, Visibility visibility, TestEnumU64 initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Single))]
		public void AddFieldT_WithInitialValue_Single(string name, Visibility visibility, float initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Double))]
		public void AddFieldT_WithInitialValue_Double(string name, Visibility visibility, double initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_String))]
		public void AddFieldT_WithInitialValue_String(string name, Visibility visibility, string initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_DateTime))]
		public void AddFieldT_WithInitialValue_DateTime(string name, Visibility visibility, DateTime initialValue) => AddFieldT_WithInitialValue(name, visibility, initialValue);

		/// <summary>
		/// Tests the <see cref="TypeDefinition.AddField{T}(string,Visibility,T)"/> method
		/// (common part, type specific tests methods run the tests).
		/// </summary>
		/// <typeparam name="TFieldType">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add.</param>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="initialValue">The initial value of the field to set.</param>
		private void AddFieldT_WithInitialValue<TFieldType>(
			string     name,
			Visibility visibility,
			TFieldType initialValue)
		{
			// create a new type definition and add the field
			var definition = CreateTypeDefinition();
			var addFieldMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddField))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(typeof(TFieldType)))
				.Single(method => method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(TFieldType) }));
			var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, new object[] { name, visibility, initialValue });

			// create the defined type, check the result against the definition and create an instance of that type
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);

			// check whether the field has the expected initial value
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			var field = type.GetField(addedField.Name, bindingFlags);
			Assert.NotNull(field);
			var fieldValue = field.GetValue(instance);
			Assert.Equal(initialValue, fieldValue);
		}

		#endregion

		#region AddStaticField<T>(string name, Visibility visibility, T initialValue)

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Boolean))]
		public void AddStaticFieldT_WithInitialValue_Boolean(string name, Visibility visibility, bool initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Char))]
		public void AddStaticFieldT_WithInitialValue_Char(string name, Visibility visibility, char initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_SByte))]
		public void AddStaticFieldT_WithInitialValue_SByte(string name, Visibility visibility, sbyte initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Byte))]
		public void AddStaticFieldT_WithInitialValue_Byte(string name, Visibility visibility, byte initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Int16))]
		public void AddStaticFieldT_WithInitialValue_Int16(string name, Visibility visibility, short initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_UInt16))]
		public void AddStaticFieldT_WithInitialValue_UInt16(string name, Visibility visibility, ushort initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Int32))]
		public void AddStaticFieldT_WithInitialValue_Int32(string name, Visibility visibility, int initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_UInt32))]
		public void AddStaticFieldT_WithInitialValue_UInt32(string name, Visibility visibility, uint initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Int64))]
		public void AddStaticFieldT_WithInitialValue_Int64(string name, Visibility visibility, long initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_UInt64))]
		public void AddStaticFieldT_WithInitialValue_UInt64(string name, Visibility visibility, ulong initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_SByte))]
		public void AddStaticFieldT_WithInitialValue_Enum_SByte(string name, Visibility visibility, TestEnumS8 initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_Byte))]
		public void AddStaticFieldT_WithInitialValue_Enum_Byte(string name, Visibility visibility, TestEnumU8 initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_Int16))]
		public void AddStaticFieldT_WithInitialValue_Enum_Int16(string name, Visibility visibility, TestEnumS16 initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_UInt16))]
		public void AddStaticFieldT_WithInitialValue_Enum_UInt16(string name, Visibility visibility, TestEnumU16 initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_Int32))]
		public void AddStaticFieldT_WithInitialValue_Enum_Int32(string name, Visibility visibility, TestEnumS32 initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_UInt32))]
		public void AddStaticFieldT_WithInitialValue_Enum_UInt32(string name, Visibility visibility, TestEnumU32 initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_Int64))]
		public void AddStaticFieldT_WithInitialValue_Enum_Int64(string name, Visibility visibility, TestEnumS64 initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Enum_UInt64))]
		public void AddStaticFieldT_WithInitialValue_Enum_UInt64(string name, Visibility visibility, TestEnumU64 initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Single))]
		public void AddStaticFieldT_WithInitialValue_Single(string name, Visibility visibility, float initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_Double))]
		public void AddStaticFieldT_WithInitialValue_Double(string name, Visibility visibility, double initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_String))]
		public void AddStaticFieldT_WithInitialValue_String(string name, Visibility visibility, string initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		[Theory]
		[MemberData(nameof(AddFieldTestData_InitialValue_DateTime))]
		public void AddStaticFieldT_WithInitialValue_DateTime(string name, Visibility visibility, DateTime initialValue) => AddStaticFieldT_WithInitialValue(name, visibility, initialValue);

		/// <summary>
		/// Tests the <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,T)"/> method
		/// (common part, type specific tests methods run the tests).
		/// </summary>
		/// <typeparam name="TFieldType">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add.</param>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="initialValue">The initial value of the field to set.</param>
		private void AddStaticFieldT_WithInitialValue<TFieldType>(
			string     name,
			Visibility visibility,
			TFieldType initialValue)
		{
			// create a new type definition and add the field
			var definition = CreateTypeDefinition();
			var addFieldMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddStaticField))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(typeof(TFieldType)))
				.Single(method => method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(TFieldType) }));
			var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, new object[] { name, visibility, initialValue });

			// create the defined type, check the result against the definition and create an instance of that type
			// (the instance is not required for accessing the static field, but tests whether the type can be created successfully)
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);
			Assert.NotNull(instance);

			// check whether the field has the expected initial value
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
			var field = type.GetField(addedField.Name, bindingFlags);
			Assert.NotNull(field);
			var fieldValue = field.GetValue(null);
			Assert.Equal(initialValue, fieldValue);
		}

		#endregion

		#region AddField<T>(string name, Visibility visibility, FieldInitializer<T> initializer)

		/// <summary>
		/// Tests the <see cref="TypeDefinition.AddField{T}(string,Visibility,FieldInitializer{T})"/> method.
		/// </summary>
		/// <param name="name">Name of the field to add.</param>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="initializer">Field initializer to use when initializing the field (must be of type <see cref="FieldInitializer{T}"/>).</param>
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
			var definition = CreateTypeDefinition();
			var addFieldMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddField))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(fieldType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(FieldInitializer<>).MakeGenericType(fieldType) }));
			var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, new object[] { name, visibility, initializer });

			// create the defined type, check the result against the definition and create an instance of that type
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);

			// check whether the field has the expected initial value
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			var field = type.GetField(addedField.Name, bindingFlags);
			Assert.NotNull(field);
			var fieldValue = field.GetValue(instance);
			Assert.Equal(expectedInitialValue, fieldValue);
		}

		#endregion

		#region AddStaticField<T>(string name, Visibility visibility, FieldInitializer<T> initializer)

		/// <summary>
		/// Tests the <see cref="TypeDefinition.AddStaticField{T}(string,Visibility,FieldInitializer{T})"/> method.
		/// </summary>
		/// <param name="name">Name of the field to add.</param>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="initializer">Field initializer to use when initializing the field (must be of type <see cref="FieldInitializer{T}"/>).</param>
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
			var definition = CreateTypeDefinition();
			var addFieldMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddStaticField))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(fieldType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(FieldInitializer<>).MakeGenericType(fieldType) }));
			var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, new object[] { name, visibility, initializer });

			// create the defined type, check the result against the definition and create an instance of that type
			// (the instance is not required for accessing the static field, but tests whether the type can be created successfully)
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);
			Assert.NotNull(instance);

			// check whether the field has the expected initial value
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
			var field = type.GetField(addedField.Name, bindingFlags);
			Assert.NotNull(field);
			var fieldValue = field.GetValue(null);
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
		[MemberData(nameof(AddFieldTestData_WithInitialValueCallback))]
		public void AddFieldT_WithInitialValueCallback(
			string     name,
			Visibility visibility,
			Type       fieldType,
			Delegate   provideInitialValueCallback,
			object     expectedInitialValue)
		{
			// create a new type definition and add the field
			var definition = CreateTypeDefinition();
			var addFieldMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddField))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(fieldType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(ProvideValueCallback<>).MakeGenericType(fieldType) }));
			var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, new object[] { name, visibility, provideInitialValueCallback });

			// create the defined type, check the result against the definition and create an instance of that type
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);

			// check whether the field has the expected initial value
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			var field = type.GetField(addedField.Name, bindingFlags);
			Assert.NotNull(field);
			var fieldValue = field.GetValue(instance);
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
		[MemberData(nameof(AddFieldTestData_WithInitialValueCallback))]
		public void AddStaticFieldT_WithInitialValueCallback(
			string     name,
			Visibility visibility,
			Type       fieldType,
			Delegate   provideInitialValueCallback,
			object     expectedInitialValue)
		{
			// create a new type definition and add the field
			var definition = CreateTypeDefinition();
			var addFieldMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddStaticField))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(fieldType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(ProvideValueCallback<>).MakeGenericType(fieldType) }));
			var addedField = (IGeneratedField)addFieldMethod.Invoke(definition, new object[] { name, visibility, provideInitialValueCallback });

			// create the defined type, check the result against the definition and create an instance of that type
			// (the instance is not required for accessing the static field, but tests whether the type can be created successfully)
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);
			Assert.NotNull(instance);

			// check whether the field has the expected initial value
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
			var field = type.GetField(addedField.Name, bindingFlags);
			Assert.NotNull(field);
			var fieldValue = field.GetValue(null);
			Assert.Equal(expectedInitialValue, fieldValue);
		}

		#endregion

		#endregion // Adding Fields

		#region Adding Events (TODO, override test case missing)

		#region Test Data

		/// <summary>
		/// Some specialized event arguments for testing purposes.
		/// </summary>
		public class SpecializedEventArgs : EventArgs
		{
			public new static readonly SpecializedEventArgs Empty = new SpecializedEventArgs();
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddAbstractEvent{T}(string,Visibility)"/>.
		/// </summary>
		public static IEnumerable<object[]> AddAbstractEventTestData
		{
			get
			{
				foreach (string name in new[] { "Event", null })
				foreach (Visibility visibility in Visibilities)
				{
					// System.EventHandler
					yield return new object[]
					{
						name,                // name of the event
						visibility,          // visibility of the event
						typeof(EventHandler) // event handler type
					};

					// System.EventHandler<EventArgs>
					yield return new object[]
					{
						name,                           // name of the event
						visibility,                     // visibility of the event
						typeof(EventHandler<EventArgs>) // event handler type
					};

					// System.EventHandler<SpecializedEventArgs>
					yield return new object[]
					{
						name,                                      // name of the event
						visibility,                                // visibility of the event
						typeof(EventHandler<SpecializedEventArgs>) // event handler type
					};

					// System.Action
					yield return new object[]
					{
						name,          // name of the event
						visibility,    // visibility of the event
						typeof(Action) // event handler type
					};

					// System.Action<int>
					yield return new object[]
					{
						name,               // name of the event
						visibility,         // visibility of the event
						typeof(Action<int>) // event handler type
					};

					// System.Func<int>
					yield return new object[]
					{
						name,              // name of the event
						visibility,        // visibility of the event
						typeof(Func<long>) // event handler type
					};

					// System.Func<int,long>
					yield return new object[]
					{
						name,                   // name of the event
						visibility,             // visibility of the event
						typeof(Func<int, long>) // event handler type
					};
				}
			}
		}

		/// <summary>
		/// Test data for tests targeting <see cref="TypeDefinition.AddEvent{T}(string,Visibility,IEventImplementation{T})"/>,
		/// <see cref="TypeDefinition.AddVirtualEvent{T}(string,Visibility,IEventImplementation{T})"/> and
		/// <see cref="TypeDefinition.AddEventOverride{T}(IInheritedEvent{T},IEventImplementation{T})"/> using
		/// <see cref="EventImplementation_Standard{T}"/> to implement add/remove accessors and the event raiser method.
		/// </summary>
		public static IEnumerable<object[]> AddEventTestData_WithImplementationStrategy_Standard
		{
			get
			{
				foreach (string name in new[] { "Event", null })
				foreach (Visibility visibility in Visibilities)
				foreach (bool addRaiser in new[] { false, true })
				{
					// cycle through event raiser visibilities only if adding a raiser method,
					// use public visibility only if not adding a raiser method (no effect)
					var eventRaiserVisibilities = addRaiser ? Visibilities : new[] { Visibility.Public };

					foreach (var eventRaiserVisibility in eventRaiserVisibilities)
					{
						// System.EventHandler
						// (event raiser should be like: void OnEvent())
						yield return new object[]
						{
							name,                              // name of the event
							visibility,                        // visibility of the event
							typeof(EventHandler),              // event handler type
							addRaiser,                         // determines whether to add an event raiser method
							addRaiser ? "XYZ" : null,          // name of the event raiser (null to create a random name)
							eventRaiserVisibility,             // visibility of the event raiser method
							addRaiser ? typeof(void) : null,   // expected return type of the generated event raiser method
							addRaiser ? Type.EmptyTypes : null // expected parameter types of the generated event raiser method 
						};

						// System.EventHandler<EventArgs>
						// (event raiser should be like: void OnEvent())
						yield return new object[]
						{
							name,                              // name of the event
							visibility,                        // visibility of the event
							typeof(EventHandler<EventArgs>),   // event handler type
							addRaiser,                         // determines whether to add an event raiser method
							addRaiser ? "XYZ" : null,          // name of the event raiser (null to create a random name)
							eventRaiserVisibility,             // visibility of the event raiser method
							addRaiser ? typeof(void) : null,   // expected return type of the generated event raiser method
							addRaiser ? Type.EmptyTypes : null // expected parameter types of the generated event raiser method 
						};

						// System.EventHandler<SpecializedEventArgs>
						// (event raiser should be like: void OnEvent(SpecializedEventArgs e))
						yield return new object[]
						{
							name,                                                     // name of the event
							visibility,                                               // visibility of the event
							typeof(EventHandler<SpecializedEventArgs>),               // event handler type
							addRaiser,                                                // determines whether to add an event raiser method
							addRaiser ? "XYZ" : null,                                 // name of the event raiser (null to create a random name)
							eventRaiserVisibility,                                    // visibility of the event raiser method
							addRaiser ? typeof(void) : null,                          // expected return type of the generated event raiser method
							addRaiser ? new[] { typeof(SpecializedEventArgs) } : null // expected parameter types of the generated event raiser method 
						};

						// System.Action
						// (event raiser should be like: void OnEvent())
						yield return new object[]
						{
							name,                              // name of the event
							visibility,                        // visibility of the event
							typeof(Action),                    // event handler type
							addRaiser,                         // determines whether to add an event raiser method
							addRaiser ? "XYZ" : null,          // name of the event raiser (null to create a random name)
							eventRaiserVisibility,             // visibility of the event raiser method
							addRaiser ? typeof(void) : null,   // expected return type of the generated event raiser method
							addRaiser ? Type.EmptyTypes : null // expected parameter types of the generated event raiser method 
						};

						// System.Action<int>
						// (event raiser should be like: void OnEvent(int i))
						yield return new object[]
						{
							name,                                    // name of the event
							visibility,                              // visibility of the event
							typeof(Action<int>),                     // event handler type
							addRaiser,                               // determines whether to add an event raiser method
							addRaiser ? "XYZ" : null,                // name of the event raiser (null to create a random name)
							eventRaiserVisibility,                   // visibility of the event raiser method
							addRaiser ? typeof(void) : null,         // expected return type of the generated event raiser method
							addRaiser ? new[] { typeof(int) } : null // expected parameter types of the generated event raiser method 
						};

						// System.Func<int>
						// (event raiser should be like: int OnEvent())
						yield return new object[]
						{
							name,                              // name of the event
							visibility,                        // visibility of the event
							typeof(Func<long>),                // event handler type
							addRaiser,                         // determines whether to add an event raiser method
							addRaiser ? "XYZ" : null,          // name of the event raiser (null to create a random name)
							eventRaiserVisibility,             // visibility of the event raiser method
							addRaiser ? typeof(long) : null,   // expected return type of the generated event raiser method
							addRaiser ? Type.EmptyTypes : null // expected parameter types of the generated event raiser method 
						};

						// System.Func<int,long>
						// (event raiser should be like: long OnEvent(int i))
						yield return new object[]
						{
							name,                                    // name of the event
							visibility,                              // visibility of the event
							typeof(Func<int, long>),                 // event handler type
							addRaiser,                               // determines whether to add an event raiser method
							addRaiser ? "XYZ" : null,                // name of the event raiser (null to create a random name)
							eventRaiserVisibility,                   // visibility of the event raiser method
							addRaiser ? typeof(long) : null,         // expected return type of the generated event raiser method
							addRaiser ? new[] { typeof(int) } : null // expected parameter types of the generated event raiser method 
						};
					}
				}
			}
		}

		#endregion

		#region AddAbstractEvent<T>(string name, Visibility visibility) --- TODO!

		/*
		/// <summary>
		/// Tests the <see cref="TypeDefinition.AddAbstractEvent{T}(string,Visibility)"/> method.
		/// </summary>
		/// <param name="name">Name of the event to add.</param>
		/// <param name="visibility">Visibility of the event to add.</param>
		/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
		[Theory]
		[MemberData(nameof(AddAbstractEventTestData))]
		public void AddAbstractEvent(string name, Visibility visibility, Type eventHandlerType)
		{
			// create a new type definition
			var definition = CreateTypeDefinition();

			// get the AddAbstractEvent(...) method to test
			var addEventMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddAbstractEvent))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(eventHandlerType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility) }));

			// invoke the method to add the event to the type definition
			var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
				definition,
				new object[] { name, visibility });
			Assert.Equal(EventKind.Abstract, addedEvent.Kind);
			Assert.Equal(visibility, addedEvent.Visibility);
			Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
			Assert.Null(addedEvent.Implementation);

			// create the defined type and check the result against the definition
			// (creating an instance of that type is not possible as it contains an abstract member)
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
		}
		*/

		#endregion

		#region AddEvent<T>(string name, Visibility visibility, IEventImplementation{T} implementation)

		/// <summary>
		/// Tests the <see cref="TypeDefinition.AddEvent{T}(string,Visibility,IEventImplementation{T})"/> method
		/// using <see cref="EventImplementation_Standard{T}"/> to implement add/remove accessors and the event raiser method.
		/// </summary>
		/// <param name="name">Name of the event to add.</param>
		/// <param name="visibility">Visibility of the event to add.</param>
		/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
		/// <param name="addEventRaiserMethod"><c>true</c> to add the event raiser method; otherwise <c>false</c>.</param>
		/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
		/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
		/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
		/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
		[Theory]
		[MemberData(nameof(AddEventTestData_WithImplementationStrategy_Standard))]
		public void AddEvent_WithImplementationStrategy_Standard(
			string     name,
			Visibility visibility,
			Type       eventHandlerType,
			bool       addEventRaiserMethod,
			string     eventRaiserName,
			Visibility eventRaiserVisibility,
			Type       expectedEventRaiserReturnType,
			Type[]     expectedEventRaiserParameterTypes)
		{
			// create a new type definition
			var definition = CreateTypeDefinition();

			// create an instance of the implementation strategy
			var implementationType = typeof(EventImplementation_Standard<>).MakeGenericType(eventHandlerType);
			var implementation = addEventRaiserMethod
				                     ? (IEventImplementation)Activator.CreateInstance(implementationType, eventRaiserName, eventRaiserVisibility)
				                     : (IEventImplementation)Activator.CreateInstance(implementationType);

			// get the AddEvent(...) method to test
			var addEventMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddEvent))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(eventHandlerType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(IEventImplementation<>).MakeGenericType(eventHandlerType) }));

			// invoke the method to add the event to the type definition
			var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
				definition,
				new object[] { name, visibility, implementation });
			Assert.Equal(EventKind.Normal, addedEvent.Kind);
			Assert.Equal(visibility, addedEvent.Visibility);
			Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
			Assert.Same(implementation, addedEvent.Implementation);

			// create the defined type, check the result against the definition and create an instance of that type
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);

			// test the implementation of the event
			TestEventImplementation_Standard(
				definition,
				instance,
				false,
				addedEvent.Name,
				eventHandlerType,
				addEventRaiserMethod,
				eventRaiserName,
				expectedEventRaiserReturnType,
				expectedEventRaiserParameterTypes);
		}

		#endregion

		#region AddVirtualEvent<T>(string name, Visibility visibility, IEventImplementation{T} implementation)

		/// <summary>
		/// Tests the <see cref="TypeDefinition.AddVirtualEvent{T}(string,Visibility,IEventImplementation{T})"/> method
		/// using <see cref="EventImplementation_Standard{T}"/> to implement add/remove accessors and the event raiser method.
		/// </summary>
		/// <param name="name">Name of the event to add.</param>
		/// <param name="visibility">Visibility of the event to add.</param>
		/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
		/// <param name="addEventRaiserMethod"><c>true</c> to add the event raiser method; otherwise <c>false</c>.</param>
		/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
		/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
		/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
		/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
		[Theory]
		[MemberData(nameof(AddEventTestData_WithImplementationStrategy_Standard))]
		public void AddVirtualEvent_WithImplementationStrategy_Standard(
			string     name,
			Visibility visibility,
			Type       eventHandlerType,
			bool       addEventRaiserMethod,
			string     eventRaiserName,
			Visibility eventRaiserVisibility,
			Type       expectedEventRaiserReturnType,
			Type[]     expectedEventRaiserParameterTypes)
		{
			// create a new type definition
			var definition = CreateTypeDefinition();

			// create an instance of the implementation strategy
			var implementationType = typeof(EventImplementation_Standard<>).MakeGenericType(eventHandlerType);
			var implementation = addEventRaiserMethod
				                     ? (IEventImplementation)Activator.CreateInstance(implementationType, eventRaiserName, eventRaiserVisibility)
				                     : (IEventImplementation)Activator.CreateInstance(implementationType);

			// get the AddEvent(...) method to test
			var addEventMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddVirtualEvent))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(eventHandlerType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(IEventImplementation<>).MakeGenericType(eventHandlerType) }));

			// invoke the method to add the event to the type definition
			var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
				definition,
				new object[] { name, visibility, implementation });
			Assert.Equal(EventKind.Virtual, addedEvent.Kind);
			Assert.Equal(visibility, addedEvent.Visibility);
			Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
			Assert.Same(implementation, addedEvent.Implementation);

			// create the defined type, check the result against the definition and create an instance of that type
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);

			// test the implementation of the event
			TestEventImplementation_Standard(
				definition,
				instance,
				false,
				addedEvent.Name,
				eventHandlerType,
				addEventRaiserMethod,
				eventRaiserName,
				expectedEventRaiserReturnType,
				expectedEventRaiserParameterTypes);
		}

		#endregion

		#region AddEventOverride<T>(IInheritedEvent<T> eventToOverride, IEventImplementation{T} implementation) --- TODO!

		/// <summary>
		/// Tests the <see cref="TypeDefinition.AddEventOverride{T}(IInheritedEvent{T},IEventImplementation{T})"/> method
		/// using <see cref="EventImplementation_Standard{T}"/> to implement add/remove accessors and the event raiser method.
		/// </summary>
		public void AddEventOverride_WithImplementationStrategy_Standard()
		{
			// TODO: Implement...
		}

		#endregion

		#region AddStaticEvent<T>(string name, Visibility visibility, IEventImplementation{T} implementation)

		/// <summary>
		/// Tests the <see cref="TypeDefinition.AddStaticEvent{T}(string,Visibility,IEventImplementation{T})"/> method
		/// using <see cref="EventImplementation_Standard{T}"/> to implement add/remove accessors and the event raiser method.
		/// </summary>
		/// <param name="name">Name of the event to add.</param>
		/// <param name="visibility">Visibility of the event to add.</param>
		/// <param name="eventHandlerType">Type of the event handler associated with the event.</param>
		/// <param name="addEventRaiserMethod"><c>true</c> to add the event raiser method; otherwise <c>false</c>.</param>
		/// <param name="eventRaiserName">Name of the event raiser (<c>null</c> to generate a name automatically).</param>
		/// <param name="eventRaiserVisibility">Visibility of the event raiser method.</param>
		/// <param name="expectedEventRaiserReturnType">The expected return type of the generated event raiser method.</param>
		/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the generated event raiser method.</param>
		[Theory]
		[MemberData(nameof(AddEventTestData_WithImplementationStrategy_Standard))]
		public void AddStaticEvent_WithImplementationStrategy_Standard(
			string     name,
			Visibility visibility,
			Type       eventHandlerType,
			bool       addEventRaiserMethod,
			string     eventRaiserName,
			Visibility eventRaiserVisibility,
			Type       expectedEventRaiserReturnType,
			Type[]     expectedEventRaiserParameterTypes)
		{
			// create a new type definition
			var definition = CreateTypeDefinition();

			// create an instance of the implementation strategy
			var implementationType = typeof(EventImplementation_Standard<>).MakeGenericType(eventHandlerType);
			var implementation = addEventRaiserMethod
				                     ? (IEventImplementation)Activator.CreateInstance(implementationType, eventRaiserName, eventRaiserVisibility)
				                     : (IEventImplementation)Activator.CreateInstance(implementationType);

			// get the AddEvent(...) method to test
			var addEventMethod = typeof(TypeDefinition)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(method => method.Name == nameof(TypeDefinition.AddStaticEvent))
				.Where(method => method.GetGenericArguments().Length == 1)
				.Select(method => method.MakeGenericMethod(eventHandlerType))
				.Single(
					method => method
						.GetParameters()
						.Select(parameter => parameter.ParameterType)
						.SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(IEventImplementation<>).MakeGenericType(eventHandlerType) }));

			// invoke the method to add the event to the type definition
			var addedEvent = (IGeneratedEvent)addEventMethod.Invoke(
				definition,
				new object[] { name, visibility, implementation });
			Assert.Equal(EventKind.Static, addedEvent.Kind);
			Assert.Equal(visibility, addedEvent.Visibility);
			Assert.Equal(eventHandlerType, addedEvent.EventHandlerType);
			Assert.Same(implementation, addedEvent.Implementation);

			// create the defined type, check the result against the definition and create an instance of that type
			Type type = definition.CreateType();
			CheckTypeAgainstDefinition(type, definition);
			object instance = Activator.CreateInstance(type);

			// test the implementation of the event
			TestEventImplementation_Standard(
				definition,
				instance,
				true,
				addedEvent.Name,
				eventHandlerType,
				addEventRaiserMethod,
				eventRaiserName,
				expectedEventRaiserReturnType,
				expectedEventRaiserParameterTypes);
		}

		#endregion

		#region Common Event Test Code

		/// <summary>
		/// Tests an event that has been implemented using the <see cref="EventImplementation_Standard{T}"/> implementation strategy.
		/// </summary>
		/// <param name="definition">Type definition the event to test belongs to.</param>
		/// <param name="instance">Instance of the dynamically created type that contains the event.</param>
		/// <param name="isStaticEvent">
		/// <c>true</c> if the event to test is a static event;
		/// <c>false</c> if the event to test is an instance event.
		/// </param>
		/// <param name="eventName">Name of the added event.</param>
		/// <param name="eventHandlerType">
		/// Type of the event handler. For the sake of simplicity only the following event handler types are supported:
		/// <see cref="EventHandler"/> (should generate a raiser method without return value and arguments),
		/// <see cref="EventHandler{TEventArgs}"/> with <see cref="EventArgs"/> (should generate a raiser method without return value and arguments),
		/// <see cref="EventHandler{TEventArgs}"/> with <see cref="SpecializedEventArgs"/> (should generate a raiser method without return value taking
		/// <see cref="SpecializedEventArgs"/> as argument),
		/// <see cref="Action"/> (should generate a raiser method without return value and arguments) and
		/// <see cref="Action{Int32}"/> (should generate a raiser method without return value taking <see cref="Int32"/> as argument) and
		/// <see cref="Func{Int64}"/> (should generate a raiser method with return value of type <see cref="Int64"/>, but without arguments) and
		/// <see cref="Func{Int32,Int64}"/> (should generate a raiser method with return value of type <see cref="Int64"/> taking <see cref="Int32"/> as
		/// argument).
		/// </param>
		/// <param name="strategyAddsEventRaiserMethod">
		/// <c>true</c> if the implementation strategy should have added an event raiser method;
		/// otherwise <c>false</c>.
		/// </param>
		/// <param name="eventRaiserName">
		/// Name of the added event raiser method
		/// (may be <c>null</c> to let the implementation strategy choose a name).
		/// </param>
		/// <param name="expectedEventRaiserReturnType">The expected return type of the event raiser method, if any.</param>
		/// <param name="expectedEventRaiserParameterTypes">The expected parameter types of the event raiser method, if any.</param>
		protected static void TestEventImplementation_Standard(
			TypeDefinition definition,
			object         instance,
			bool           isStaticEvent,
			string         eventName,
			Type           eventHandlerType,
			bool           strategyAddsEventRaiserMethod,
			string         eventRaiserName,
			Type           expectedEventRaiserReturnType,
			Type[]         expectedEventRaiserParameterTypes)
		{
			// get the type of the generated instance
			Type generatedType = instance.GetType();

			// get generated event accessor methods
			var bindingFlags = (isStaticEvent ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public | BindingFlags.NonPublic;
			var addAccessorMethod = generatedType.GetMethod("add_" + eventName, bindingFlags);
			var removeAccessorMethod = generatedType.GetMethod("remove_" + eventName, bindingFlags);
			Assert.NotNull(addAccessorMethod);
			Assert.NotNull(removeAccessorMethod);

			// abort if the implementation strategy is not expected to have added an event raiser method
			if (!strategyAddsEventRaiserMethod)
			{
				Assert.Empty(definition.GeneratedMethods);
				return;
			}

			// the implementation strategy should have added an event raiser method

			// determine the name of the event raiser method the implementation strategy should have added
			string expectedEventRaiserName = eventRaiserName ?? "On" + eventName;

			// check whether the implementation strategy has added the event raiser method to the type definition
			Assert.Single(definition.GeneratedMethods);
			var eventRaiserMethodDefinition = definition.GeneratedMethods.Single();
			Assert.Equal(expectedEventRaiserName, eventRaiserMethodDefinition.Name);
			Assert.Equal(expectedEventRaiserReturnType, eventRaiserMethodDefinition.ReturnType);
			Assert.Equal(expectedEventRaiserParameterTypes, eventRaiserMethodDefinition.ParameterTypes);

			// get the event raiser method
			var eventRaiserMethod = generatedType.GetMethods(bindingFlags).SingleOrDefault(x => x.Name == expectedEventRaiserName);
			Assert.NotNull(eventRaiserMethod);

			// prepare an event handler to register with the event
			Delegate handler = null;
			bool handlerWasCalled = false;
			object[] eventRaiserArguments = null;
			object expectedReturnValue = null;
			if (eventHandlerType == typeof(EventHandler))
			{
				// set up an event handler to test with
				eventRaiserArguments = Array.Empty<object>();
				handler = new EventHandler(
					(sender, e) =>
					{
						if (isStaticEvent)
						{
							Assert.Null(sender);
						}
						else
						{
							if (definition.TypeBuilder.IsValueType) Assert.Equal(instance, sender);
							else Assert.Same(instance, sender);
						}

						Assert.Same(EventArgs.Empty, e);
						handlerWasCalled = true;
					});
			}
			else if (eventHandlerType == typeof(EventHandler<EventArgs>))
			{
				// set up an event handler to test with
				eventRaiserArguments = Array.Empty<object>();
				handler = new EventHandler<EventArgs>(
					(sender, e) =>
					{
						if (isStaticEvent)
						{
							Assert.Null(sender);
						}
						else
						{
							if (definition.TypeBuilder.IsValueType) Assert.Equal(instance, sender);
							else Assert.Same(instance, sender);
						}

						Assert.Same(EventArgs.Empty, e);
						handlerWasCalled = true;
					});
			}
			else if (eventHandlerType == typeof(EventHandler<SpecializedEventArgs>))
			{
				// set up an event handler to test with
				eventRaiserArguments = new object[] { SpecializedEventArgs.Empty };
				handler = new EventHandler<SpecializedEventArgs>(
					(sender, e) =>
					{
						if (isStaticEvent)
						{
							Assert.Null(sender);
						}
						else
						{
							if (definition.TypeBuilder.IsValueType) Assert.Equal(instance, sender);
							else Assert.Same(instance, sender);
						}

						Assert.Same(SpecializedEventArgs.Empty, e);
						handlerWasCalled = true;
					});
			}
			else if (eventHandlerType == typeof(Action))
			{
				// set up an event handler to test with
				eventRaiserArguments = Array.Empty<object>();
				handler = new Action(() => { handlerWasCalled = true; });
			}
			else if (eventHandlerType == typeof(Action<int>))
			{
				// set up an event handler to test with
				const int testValue = 42;
				eventRaiserArguments = new object[] { testValue };
				handler = new Action<int>(
					value =>
					{
						Assert.Equal(testValue, value);
						handlerWasCalled = true;
					});
			}
			else if (eventHandlerType == typeof(Func<long>))
			{
				// set up an event handler to test with
				const long handlerReturnValue = 100;
				eventRaiserArguments = Array.Empty<object>();
				expectedReturnValue = handlerReturnValue;
				handler = new Func<long>(
					() =>
					{
						handlerWasCalled = true;
						return handlerReturnValue;
					});
			}
			else if (eventHandlerType == typeof(Func<int, long>))
			{
				// set up an event handler to test with
				const int handlerArgument = 42;
				const long handlerReturnValue = 100;
				eventRaiserArguments = new object[] { handlerArgument };
				expectedReturnValue = handlerReturnValue;
				handler = new Func<int, long>(
					value =>
					{
						Assert.Equal(handlerArgument, value);
						handlerWasCalled = true;
						return handlerReturnValue;
					});
			}
			else
			{
				Debug.Fail("Unhandled test case.");
			}

			// add event handler to the event and raise it
			// => the handler should be called
			handlerWasCalled = false;
			addAccessorMethod.Invoke(instance, new object[] { handler });
			object actualHandlerReturnValue = eventRaiserMethod.Invoke(instance, eventRaiserArguments);
			Assert.True(handlerWasCalled);
			Assert.Equal(expectedReturnValue, actualHandlerReturnValue);

			// remove the event handler from the event and raise it
			// => the handler should not be called any more
			handlerWasCalled = false;
			removeAccessorMethod.Invoke(instance, new object[] { handler });
			eventRaiserMethod.Invoke(instance, eventRaiserArguments);
			Assert.False(handlerWasCalled);
		}

		#endregion

		#endregion // Adding Events

		#region Adding Properties (TODO, all test cases missing)

		#endregion // Adding Properties

		#region Adding Dependency Properties (TODO, all test cases missing)

		#endregion // Adding Dependency Properties

		#region Adding Methods (TODO, all test cases missing)

		#endregion // Adding Methods

		#region Helper: Checking Created Type against the Definition

		/// <summary>
		/// Checks the specified type against the type definition and determines whether the type was created correctly.
		/// This checks member declaration, but not their implementation as this would need to dig into the msil stream.
		/// </summary>
		/// <param name="type">Type to check.</param>
		/// <param name="definition">Definition the type should comply with.</param>
		protected static void CheckTypeAgainstDefinition(Type type, TypeDefinition definition)
		{
			// check whether the full name of the type equals the defined type name
			Assert.Equal(definition.TypeName, type.FullName);

			// check whether the expected constructors have been declared
			// (TODO: add checking for static constructor, requires pimping up the type definition)
			var expectedConstructors = new HashSet<ConstructorInfo>(definition.Constructors.Select(x => x.ConstructorInfo));
			const BindingFlags constructorBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			var actualConstructors = new HashSet<ConstructorInfo>(type.GetConstructors(constructorBindingFlags));
			Assert.Equal(expectedConstructors, actualConstructors, ConstructorEqualityComparer.Instance);

			// prepare binding flags for lookups using reflection (include all members that are declared by the specified type)
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

			// check whether the expected fields have been declared
			var expectedFields = new HashSet<FieldInfo>(definition.GeneratedFields.Select(x => x.FieldInfo));
			var actualFields = new HashSet<FieldInfo>(type.GetFields(bindingFlags));
			Assert.Equal(expectedFields, actualFields, FieldEqualityComparer.Instance);

			// check whether the expected events have been declared
			// (the EventComparisonWrapper class handles the comparison on its own, no external comparer needed)
			var expectedEvents = new HashSet<EventComparisonWrapper>(definition.GeneratedEvents.Select(x => new EventComparisonWrapper(x)));
			var actualEvents = new HashSet<EventComparisonWrapper>(type.GetEvents(bindingFlags).Select(x => new EventComparisonWrapper(x)));
			Assert.Equal(expectedEvents, actualEvents);

			// check whether the expected properties have been declared
			var expectedProperties = new HashSet<PropertyComparisonWrapper>(definition.GeneratedProperties.Select(x => new PropertyComparisonWrapper(x)));
			var actualProperties = new HashSet<PropertyComparisonWrapper>(type.GetProperties(bindingFlags).Select(x => new PropertyComparisonWrapper(x)));
			Assert.Equal(expectedProperties, actualProperties);

			// check whether the expected methods have been declared
			var expectedMethods = new HashSet<MethodComparisonWrapper>(definition.GeneratedMethods.Select(x => new MethodComparisonWrapper(x)));
			var actualMethods = new HashSet<MethodComparisonWrapper>(type.GetMethods(bindingFlags).Where(x => !x.IsSpecialName).Select(x => new MethodComparisonWrapper(x)));
			Assert.Equal(expectedMethods, actualMethods);
		}

		#endregion

		#region Helper: Getting Members Accessible from Derived Type (Constructors, Events, Fields, Methods and Properties)

		/// <summary>
		/// Gets constructors of the specified type that can be accessed by a type deriving from that type.
		/// </summary>
		/// <param name="type">Type to inspect.</param>
		/// <returns>The constructors of the specified type that can be accessed by a type deriving from that type.</returns>
		protected static HashSet<ConstructorInfo> GetConstructorsAccessibleFromDerivedType(Type type)
		{
			HashSet<ConstructorInfo> constructorInfos = new HashSet<ConstructorInfo>();
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			foreach (ConstructorInfo constructorInfo in type.GetConstructors(flags))
			{
				// skip constructor if it is private or internal
				// (cannot be accessed by a derived type defined in another assembly)
				if (constructorInfo.IsPrivate || constructorInfo.IsAssembly) continue;

				// keep constructor
				constructorInfos.Add(constructorInfo);
			}

			return constructorInfos;
		}

		/// <summary>
		/// Gets the events that can be accessed by a type deriving from the specified type.
		/// </summary>
		/// <param name="type">Type to inspect.</param>
		/// <param name="includeHidden"><c>true</c> to include hidden events; otherwise <c>false</c>.</param>
		/// <returns>The events that can be accessed by a type deriving from the specified type.</returns>
		protected static HashSet<EventInfo> GetEventsAccessibleFromDerivedType(Type type, bool includeHidden)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
			HashSet<EventInfo> eventInfos = new HashSet<EventInfo>();
			Type typeToInspect = type;
			while (typeToInspect != null)
			{
				foreach (EventInfo eventInfo in typeToInspect.GetEvents(flags))
				{
					// skip event if it is 'private' or 'internal'
					// (cannot be accessed by a derived type defined in another assembly)
					MethodInfo addMethod = eventInfo.GetAddMethod(true);
					MethodInfo removeMethod = eventInfo.GetRemoveMethod(true);
					if (addMethod.IsPrivate || addMethod.IsAssembly) continue;
					if (removeMethod.IsPrivate || removeMethod.IsAssembly) continue;

					// skip event if it is virtual and already in the set of events (also covers abstract, virtual and overridden events)
					// => only the most specific implementation gets into the returned set of events
					if ((addMethod.IsVirtual || removeMethod.IsVirtual) && eventInfos.Any(x => HasSameSignature(x, eventInfo)))
						continue;

					// skip event if an event with the same signature is already in the set of events
					// and hidden events should not be returned
					if (!includeHidden && eventInfos.Any(x => HasSameSignature(x, eventInfo)))
						continue;

					// the event is accessible from a derived class in some other assembly
					eventInfos.Add(eventInfo);
				}

				typeToInspect = typeToInspect.BaseType;
			}

			return eventInfos;
		}

		/// <summary>
		/// Gets the fields that can be accessed by a type deriving from the specified type.
		/// </summary>
		/// <param name="type">Type to inspect.</param>
		/// <param name="includeHidden"><c>true</c> to include hidden fields; otherwise <c>false</c>.</param>
		/// <returns>The fields that can be accessed by a type deriving from the specified type.</returns>
		protected static HashSet<FieldInfo> GetFieldsAccessibleFromDerivedType(Type type, bool includeHidden)
		{
			// all fields that are neither private nor internal are accessible to derived types
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
			HashSet<FieldInfo> fieldInfos = new HashSet<FieldInfo>();
			Type typeToInspect = type;
			while (typeToInspect != null)
			{
				foreach (FieldInfo fieldInfo in typeToInspect.GetFields(flags))
				{
					// skip field if it is 'private' or 'internal'
					// (cannot be accessed by a derived type defined in another assembly)
					if (fieldInfo.IsPrivate || fieldInfo.IsAssembly) continue;

					// skip field if a field with the same signature is already in the set of fields
					// and hidden fields should not be returned
					if (!includeHidden && fieldInfos.Any(x => HasSameSignature(x, fieldInfo)))
						continue;

					// the field is accessible from a derived class in some other assembly
					fieldInfos.Add(fieldInfo);
				}

				typeToInspect = typeToInspect.BaseType;
			}

			return fieldInfos;
		}

		/// <summary>
		/// Gets the methods that can be accessed by a type deriving from the specified type.
		/// </summary>
		/// <param name="type">Type to inspect.</param>
		/// <param name="includeHidden"><c>true</c> to include hidden methods; otherwise <c>false</c>.</param>
		/// <returns>The methods that can be accessed by a type deriving from the specified type.</returns>
		protected static HashSet<MethodInfo> GetMethodsAccessibleFromDerivedType(Type type, bool includeHidden)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
			HashSet<MethodInfo> methodInfos = new HashSet<MethodInfo>();
			Type typeToInspect = type;
			while (typeToInspect != null)
			{
				foreach (MethodInfo methodInfo in typeToInspect.GetMethods(flags))
				{
					// skip method if it is 'private' or 'internal'
					// (cannot be accessed by a derived type defined in another assembly)
					if (methodInfo.IsPrivate || methodInfo.IsAssembly) continue;

					// skip methods with a special name as these methods usually cannot be called by the user directly
					// (add/remove accessor methods of events and get/set accessor methods of properties)
					if (methodInfo.IsSpecialName) continue;

					// skip method if it is virtual and already in the set of methods (also covers abstract, virtual and overridden events)
					// => only the most specific implementation gets into the returned set of methods
					if (methodInfo.IsVirtual && methodInfos.Any(x => HasSameSignature(x, methodInfo)))
						continue;

					// skip property if a method with the same signature is already in the set of methods
					// and hidden methods should not be returned
					if (!includeHidden && methodInfos.Any(x => HasSameSignature(x, methodInfo)))
						continue;

					// the method is accessible from a derived class in some other assembly
					methodInfos.Add(methodInfo);
				}

				typeToInspect = typeToInspect.BaseType;
			}

			return methodInfos;
		}

		/// <summary>
		/// Gets the properties that can be accessed by a type deriving from the specified type.
		/// </summary>
		/// <param name="type">Type to inspect.</param>
		/// <param name="includeHidden"><c>true</c> to include hidden properties; otherwise <c>false</c>.</param>
		/// <returns>The properties that can be accessed by a type deriving from the specified type.</returns>
		protected static HashSet<PropertyInfo> GetPropertiesAccessibleFromDerivedType(Type type, bool includeHidden)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
			HashSet<PropertyInfo> propertyInfos = new HashSet<PropertyInfo>();
			Type typeToInspect = type;
			while (typeToInspect != null)
			{
				foreach (PropertyInfo propertyInfo in typeToInspect.GetProperties(flags))
				{
					// skip accessors that are 'private' or 'internal'
					// (cannot be accessed by a derived type defined in another assembly)
					int callableAccessorCount = 0;

					// check visibility of the 'get' accessor
					MethodInfo getAccessor = propertyInfo.GetGetMethod(true);
					if (getAccessor != null && !getAccessor.IsPrivate && !getAccessor.IsAssembly)
						callableAccessorCount++;

					// check visibility of the 'set' accessor
					MethodInfo setAccessor = propertyInfo.GetSetMethod(true);
					if (setAccessor != null && !setAccessor.IsPrivate && !setAccessor.IsAssembly)
						callableAccessorCount++;

					// skip property if neither a get accessor method nor a set accessor method are accessible
					if (callableAccessorCount == 0) continue;

					// skip property if it is already in the set of properties and its accessor methods are virtual
					// (the check for virtual also covers abstract, virtual and override methods)
					// => only the most specific implementation gets into the returned set of properties
					if (propertyInfos.Any(x => HasSameSignature(x, propertyInfo)))
					{
						if (getAccessor != null && getAccessor.IsVirtual) continue;
						if (setAccessor != null && setAccessor.IsVirtual) continue;
					}

					// skip property if a property with the same signature is already in the set of properties
					// and hidden properties should not be returned
					if (!includeHidden && propertyInfos.Any(x => HasSameSignature(x, propertyInfo)))
						continue;

					// the property is accessible from a derived class in some other assembly
					propertyInfos.Add(propertyInfo);
				}

				typeToInspect = typeToInspect.BaseType;
			}

			return propertyInfos;
		}

		/// <summary>
		/// Checks whether the signatures (name + field type) of the specified fields are the same.
		/// </summary>
		/// <param name="x">Field to compare.</param>
		/// <param name="y">Field to compare to.</param>
		/// <returns>
		/// <c>true</c> if the specified fields have the same signature;
		/// otherwise <c>false</c>.
		/// </returns>
		private static bool HasSameSignature(FieldInfo x, FieldInfo y)
		{
			if (x.Name != y.Name) return false;
			return x.FieldType == y.FieldType;
		}

		/// <summary>
		/// Checks whether the signatures (name + event handler type) of the specified events are the same.
		/// </summary>
		/// <param name="x">Event to compare.</param>
		/// <param name="y">Event to compare to.</param>
		/// <returns>
		/// <c>true</c> if the specified events have the same signature;
		/// otherwise <c>false</c>.
		/// </returns>
		private static bool HasSameSignature(EventInfo x, EventInfo y)
		{
			if (x.Name != y.Name) return false;
			return x.EventHandlerType == y.EventHandlerType;
		}

		/// <summary>
		/// Checks whether the signatures (name + property type) of the specified properties are the same.
		/// </summary>
		/// <param name="x">Property to compare.</param>
		/// <param name="y">Property to compare to.</param>
		/// <returns>
		/// <c>true</c> if the specified properties have the same signature;
		/// otherwise <c>false</c>.
		/// </returns>
		private static bool HasSameSignature(PropertyInfo x, PropertyInfo y)
		{
			if (x.Name != y.Name) return false;
			return x.PropertyType == y.PropertyType;
		}

		/// <summary>
		/// Checks whether the signatures (name + return type + parameter types) of the specified methods are the same.
		/// </summary>
		/// <param name="x">Method to compare.</param>
		/// <param name="y">Method to compare to.</param>
		/// <returns>
		/// <c>true</c> if the specified methods have the same signature;
		/// otherwise <c>false</c>.
		/// </returns>
		private static bool HasSameSignature(MethodInfo x, MethodInfo y)
		{
			if (x.Name != y.Name) return false;
			return x.GetParameters().Select(z => z.ParameterType).SequenceEqual(y.GetParameters().Select(z => z.ParameterType));
		}

		#endregion
	}

}
