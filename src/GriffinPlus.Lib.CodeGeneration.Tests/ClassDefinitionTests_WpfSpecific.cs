///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if NET461 || NET48 || (NET5_0 || NET6_0 || NET7_0 || NET8_0) && WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows;

using Xunit;

// ReSharper disable SuggestBaseTypeForParameter

namespace GriffinPlus.Lib.CodeGeneration.Tests;

using static Helpers;

/// <summary>
/// Common tests around the <see cref="ClassDefinition"/> class.
/// </summary>
public class ClassDefinitionTests_WpfSpecific
{
	#region Common Test Data

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

	#region Adding Dependency Properties

	#region Test Data

	/// <summary>
	/// Test data for tests targeting
	/// <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool)"/> and
	/// <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool)"/>.
	/// </summary>
	public static IEnumerable<object[]> AddDependencyPropertyTestData_WithDefaultValue
	{
		get
		{
			// check whether a specified name is handled properly
			yield return
			[
				"MyDependencyProperty", // name
				typeof(int),            // property type
				false,                  // property is read-write
				Visibility.Public,      // 'get' accessor visibility
				Visibility.Public,      // 'set' accessor visibility
				0,                      // expected initial value
				1                       // value to set
			];

			// check whether accessor property visibilities are handled properly
			foreach (Visibility getAccessorVisibility in Visibilities)
			foreach (Visibility setAccessorVisibility in Visibilities.Reverse())
			{
				yield return
				[
					null,                  // name
					typeof(int),           // property type
					false,                 // property is read-write
					getAccessorVisibility, // 'get' accessor visibility
					setAccessorVisibility, // 'set' accessor visibility
					0,                     // expected initial value
					1                      // value to set
				];
			}

			// check whether value types and reference types of read-only and read-write
			// dependency properties are handled properly
			foreach (bool isReadOnly in new[] { false, true })
			{
				// value type
				yield return
				[
					null,              // name
					typeof(int),       // property type
					isReadOnly,        // read-write (false) or read-only (true)
					Visibility.Public, // 'get' accessor visibility
					Visibility.Public, // 'set' accessor visibility
					0,                 // expected initial value
					1                  // value to set
				];

				// reference type
				yield return
				[
					null,              // name
					typeof(string),    // property type
					isReadOnly,        // read-write (false) or read-only (true)
					Visibility.Public, // 'get' accessor visibility
					Visibility.Public, // 'set' accessor visibility
					null,              // expected initial value
					"Just a string."   // value to set
				];
			}
		}
	}

	/// <summary>
	/// Test data for tests targeting
	/// <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,T)"/> and
	/// <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,object)"/>.
	/// </summary>
	public static IEnumerable<object[]> AddDependencyPropertyTestData_WithInitialValue
	{
		get
		{
			// check whether a specified name is handled properly
			yield return
			[
				"MyDependencyProperty", // name
				typeof(int),            // property type (specific initializer is available)
				false,                  // property is read-write
				Visibility.Public,      // 'get' accessor visibility
				Visibility.Public,      // 'set' accessor visibility
				0,                      // expected initial value
				1                       // value to set
			];

			// check whether accessor property visibilities are handled properly
			foreach (Visibility getAccessorVisibility in Visibilities)
			foreach (Visibility setAccessorVisibility in Visibilities.Reverse())
			{
				yield return
				[
					null,                  // name
					typeof(int),           // property type (specific initializer is available)
					false,                 // property is read-write
					getAccessorVisibility, // 'get' accessor visibility
					setAccessorVisibility, // 'set' accessor visibility
					0,                     // expected initial value
					1                      // value to set
				];
			}

			// check whether value types and reference types of read-only and read-write
			// dependency properties are handled properly
			foreach (bool isReadOnly in new[] { false, true })
			{
				// value type (specific initializer is available)
				yield return
				[
					null,              // name
					typeof(int),       // property type
					isReadOnly,        // read-write (false) or read-only (true)
					Visibility.Public, // 'get' accessor visibility
					Visibility.Public, // 'set' accessor visibility
					100,               // expected initial value
					200                // value to set
				];

				// value type (specific initializer is not available)
				yield return
				[
					null,                               // name
					typeof(DateTime),                   // property type
					isReadOnly,                         // read-write (false) or read-only (true)
					Visibility.Public,                  // 'get' accessor visibility
					Visibility.Public,                  // 'set' accessor visibility
					DateTime.Now,                       // expected initial value
					DateTime.Now + TimeSpan.FromDays(1) // value to set
				];

				// reference type (specific initializer is available)
				yield return
				[
					null,                 // name
					typeof(string),       // property type
					isReadOnly,           // read-write (false) or read-only (true)
					Visibility.Public,    // 'get' accessor visibility
					Visibility.Public,    // 'set' accessor visibility
					"Just a string.",     // expected initial value
					"Yet another string." // value to set
				];

				// reference type (specific initializer is not available)
				// => initializer is realized with factory callback
				yield return
				[
					null,              // name
					typeof(int[]),     // property type
					false,             // read-write (false) or read-only (true)
					Visibility.Public, // 'get' accessor visibility
					Visibility.Public, // 'set' accessor visibility
					new[] { 100 },     // expected initial value
					new[] { 100, 200 } // value to set
				];
			}
		}
	}

	/// <summary>
	/// Test data for tests targeting
	/// <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,T)"/> and
	/// <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,object)"/>.
	/// The methods should throw an <see cref="ArgumentException"/> if the specified initial value is not
	/// assignable to a property of the specified type.
	/// </summary>
	public static IEnumerable<object[]> AddDependencyPropertyTestData_WithInitialValue_InvalidValue
	{
		get
		{
			// - property type: value type
			// - value type: value type, but not of the property type
			yield return [typeof(int), 0L];

			// - property type: value type
			// - value type: reference type
			yield return [typeof(int), "A String."];

			// - property type: value type
			// - value type: null
			yield return [typeof(int), null];

			// - property type: reference type
			// - value type: value type
			yield return [typeof(string), 0];

			// - property type: reference type
			// - value type: reference type, but not assignable to the property type
			yield return [typeof(string), Array.Empty<int>()];
		}
	}

	/// <summary>
	/// Test data for tests targeting
	/// <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,DependencyPropertyInitializer)"/> and
	/// <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,DependencyPropertyInitializer)"/>.
	/// </summary>
	public static IEnumerable<object[]> AddDependencyPropertyTestData_WithCustomInitializer
	{
		get
		{
			// check whether a specified name is handled properly
			yield return
			[
				"MyDependencyProperty",                                                              // name
				typeof(int),                                                                         // property type
				false,                                                                               // property is read-write
				Visibility.Public,                                                                   // 'get' accessor visibility
				Visibility.Public,                                                                   // 'set' accessor visibility
				new DependencyPropertyInitializer((_, msil) => { msil.Emit(OpCodes.Ldc_I4, 100); }), // initializer
				100,                                                                                 // expected initial value
				200                                                                                  // value to set
			];

			// check whether accessor property visibilities are handled properly
			foreach (Visibility getAccessorVisibility in Visibilities)
			foreach (Visibility setAccessorVisibility in Visibilities.Reverse())
			{
				yield return
				[
					null,                                                                                // name
					typeof(int),                                                                         // property type
					false,                                                                               // property is read-write
					getAccessorVisibility,                                                               // 'get' accessor visibility
					setAccessorVisibility,                                                               // 'set' accessor visibility
					new DependencyPropertyInitializer((_, msil) => { msil.Emit(OpCodes.Ldc_I4, 100); }), // initializer
					100,                                                                                 // expected initial value
					200                                                                                  // value to set
				];
			}

			// check whether value types and reference types of read-only and read-write
			// dependency properties are handled properly
			foreach (bool isReadOnly in new[] { false, true })
			{
				// value type
				yield return
				[
					null,                                                                                // name
					typeof(int),                                                                         // property type
					isReadOnly,                                                                          // read-write (false) or read-only (true)
					Visibility.Public,                                                                   // 'get' accessor visibility
					Visibility.Public,                                                                   // 'set' accessor visibility
					new DependencyPropertyInitializer((_, msil) => { msil.Emit(OpCodes.Ldc_I4, 100); }), // initializer
					100,                                                                                 // expected initial value
					200                                                                                  // value to set
				];

				// reference type
				yield return
				[
					null,                                                                                            // name
					typeof(string),                                                                                  // property type
					isReadOnly,                                                                                      // read-write (false) or read-only (true)
					Visibility.Public,                                                                               // 'get' accessor visibility
					Visibility.Public,                                                                               // 'set' accessor visibility
					new DependencyPropertyInitializer((_, msil) => { msil.Emit(OpCodes.Ldstr, "Just a string."); }), // initializer
					"Just a string.",                                                                                // expected initial value
					"Yet another string."                                                                            // value to set
				];
			}
		}
	}

	/// <summary>
	/// Test data for tests targeting
	/// <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,ProvideValueCallback{T})"/>.
	/// </summary>
	public static IEnumerable<object[]> AddDependencyPropertyTestData_WithTypedFactoryCallbackInitializer
	{
		get
		{
			// check whether a specified name is handled properly
			yield return
			[
				"MyDependencyProperty",                   // name
				typeof(int),                              // property type
				false,                                    // property is read-write
				Visibility.Public,                        // 'get' accessor visibility
				Visibility.Public,                        // 'set' accessor visibility
				new ProvideValueCallback<int>(() => 100), // initializer
				100,                                      // expected initial value
				200                                       // value to set
			];

			// check whether accessor property visibilities are handled properly
			foreach (Visibility getAccessorVisibility in Visibilities)
			foreach (Visibility setAccessorVisibility in Visibilities.Reverse())
			{
				yield return
				[
					null,                                     // name
					typeof(int),                              // property type
					false,                                    // property is read-write
					getAccessorVisibility,                    // 'get' accessor visibility
					setAccessorVisibility,                    // 'set' accessor visibility
					new ProvideValueCallback<int>(() => 100), // initializer
					100,                                      // expected initial value
					200                                       // value to set
				];
			}

			// check whether value types and reference types of read-only and read-write
			// dependency properties are handled properly
			foreach (bool isReadOnly in new[] { false, true })
			{
				// value type
				yield return
				[
					null,                                     // name
					typeof(int),                              // property type
					isReadOnly,                               // read-write (false) or read-only (true)
					Visibility.Public,                        // 'get' accessor visibility
					Visibility.Public,                        // 'set' accessor visibility
					new ProvideValueCallback<int>(() => 100), // initializer
					100,                                      // expected initial value
					200                                       // value to set
				];

				// reference type
				yield return
				[
					null,                                                     // name
					typeof(string),                                           // property type
					isReadOnly,                                               // read-write (false) or read-only (true)
					Visibility.Public,                                        // 'get' accessor visibility
					Visibility.Public,                                        // 'set' accessor visibility
					new ProvideValueCallback<string>(() => "Just a string."), // initializer
					"Just a string.",                                         // expected initial value
					"Yet another string."                                     // value to set
				];
			}
		}
	}

	/// <summary>
	/// Test data for tests targeting
	/// <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,ProvideValueCallback)"/>.
	/// </summary>
	public static IEnumerable<object[]> AddDependencyPropertyTestData_WithUntypedFactoryCallbackInitializer
	{
		get
		{
			// check whether a specified name is handled properly
			yield return
			[
				"MyDependencyProperty",              // name
				typeof(int),                         // property type
				false,                               // property is read-write
				Visibility.Public,                   // 'get' accessor visibility
				Visibility.Public,                   // 'set' accessor visibility
				new ProvideValueCallback(() => 100), // initializer
				100,                                 // expected initial value
				200                                  // value to set
			];

			// check whether accessor property visibilities are handled properly
			foreach (Visibility getAccessorVisibility in Visibilities)
			foreach (Visibility setAccessorVisibility in Visibilities.Reverse())
			{
				yield return
				[
					null,                                // name
					typeof(int),                         // property type
					false,                               // property is read-write
					getAccessorVisibility,               // 'get' accessor visibility
					setAccessorVisibility,               // 'set' accessor visibility
					new ProvideValueCallback(() => 100), // initializer
					100,                                 // expected initial value
					200                                  // value to set
				];
			}

			// check whether value types and reference types of read-only and read-write
			// dependency properties are handled properly
			foreach (bool isReadOnly in new[] { false, true })
			{
				// value type
				yield return
				[
					null,                                // name
					typeof(int),                         // property type
					isReadOnly,                          // read-write (false) or read-only (true)
					Visibility.Public,                   // 'get' accessor visibility
					Visibility.Public,                   // 'set' accessor visibility
					new ProvideValueCallback(() => 100), // initializer
					100,                                 // expected initial value
					200                                  // value to set
				];

				// reference type
				yield return
				[
					null,                                             // name
					typeof(string),                                   // property type
					isReadOnly,                                       // read-write (false) or read-only (true)
					Visibility.Public,                                // 'get' accessor visibility
					Visibility.Public,                                // 'set' accessor visibility
					new ProvideValueCallback(() => "Just a string."), // initializer
					"Just a string.",                                 // expected initial value
					"Yet another string."                             // value to set
				];
			}
		}
	}

	#endregion

	#region AddDependencyProperty<T>(string name, bool isReadOnly)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool)"/> method to add the dependency property
	/// and the <see cref="IGeneratedDependencyProperty.AddAccessorProperty"/> to add an accessor property.
	/// </summary>
	/// <param name="name">Name of the dependency property to add.</param>
	/// <param name="propertyType">Type of the dependency property to add.</param>
	/// <param name="isReadOnly">
	/// <c>true</c> to create a read-only dependency property;<br/>
	/// <c>false</c> to create a read-write dependency property.
	/// </param>
	/// <param name="getAccessorVisibility">Visibility of the 'get' accessor method of the accessor property to add.</param>
	/// <param name="setAccessorVisibility">Visibility of the 'set' accessor method of the accessor property to add.</param>
	/// <param name="defaultValue">The expected default value of the dependency property.</param>
	/// <param name="valueToSet">Value to set to the property </param>
	[Theory]
	[MemberData(nameof(AddDependencyPropertyTestData_WithDefaultValue))]
	public void AddDependencyPropertyT_WithDefaultValue(
		string     name,
		Type       propertyType,
		bool       isReadOnly,
		Visibility getAccessorVisibility,
		Visibility setAccessorVisibility,
		object     defaultValue,
		object     valueToSet)
	{
		// create a new type definition and add the dependency property
		var definition = new ClassDefinition(typeof(DependencyObject), null);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool)]));
		var addedDependencyPropertyDefinition = (IGeneratedDependencyProperty)addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly]);
		Assert.NotNull(addedDependencyPropertyDefinition);

		// add an accessor property for the dependency property
		Assert.Null(addedDependencyPropertyDefinition.AccessorProperty);
		IGeneratedProperty addedAccessorPropertyDefinition = addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility);
		Assert.Same(addedDependencyPropertyDefinition.AccessorProperty, addedAccessorPropertyDefinition);

		// adding an accessor property once again should throw an exception
		Assert.Throws<InvalidOperationException>(() => addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility));

		// update the name of the property, if it was not specified explicitly
		name ??= addedDependencyPropertyDefinition.Name;

		// check the definition of the dependency property and its accessor method
		CheckDependencyPropertyDefinition(
			definition,
			name,
			propertyType,
			getAccessorVisibility,
			setAccessorVisibility,
			isReadOnly,
			false,
			null);

		// create the defined type, check the result against the definition
		Type createdType = definition.CreateType();
		CheckTypeAgainstDefinition(createdType, definition);

		// create an instance of the created type and test the dependency property, incl. its accessor property
		CheckCreatedDependencyProperty(
			createdType,
			addedDependencyPropertyDefinition,
			defaultValue,
			valueToSet);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool)"/> method.<br/>
	/// The method should throw a <see cref="CodeGenException"/> if the created type does not derive from <see cref="DependencyObject"/>.
	/// </summary>
	[Fact]
	public void AddDependencyPropertyT_WithDefaultValue_TypeDoesNotDeriveFromDependencyObject()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;

		var definition = new ClassDefinition(null, ClassAttributes.None);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool)]));

		var targetInvocationException = Assert.Throws<TargetInvocationException>(() => addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly]));
		var exception = Assert.IsType<CodeGenException>(targetInvocationException.InnerException);
		Assert.Equal($"The defined type ({definition.TypeName}) does not derive from '{typeof(DependencyObject).FullName}'.", exception.Message);
	}

	#endregion // AddDependencyProperty<T>(string name, bool isReadOnly)

	#region AddDependencyProperty(Type type, string name, bool isReadOnly)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool)"/> method to add the dependency property
	/// and the <see cref="IGeneratedDependencyProperty.AddAccessorProperty"/> to add an accessor property.
	/// </summary>
	/// <param name="name">Name of the dependency property to add.</param>
	/// <param name="propertyType">Type of the dependency property to add.</param>
	/// <param name="isReadOnly">
	/// <c>true</c> to create a read-only dependency property;<br/>
	/// <c>false</c> to create a read-write dependency property.
	/// </param>
	/// <param name="getAccessorVisibility">Visibility of the 'get' accessor method of the accessor property to add.</param>
	/// <param name="setAccessorVisibility">Visibility of the 'set' accessor method of the accessor property to add.</param>
	/// <param name="defaultValue">The expected default value of the property.</param>
	/// <param name="valueToSet">Value to set to the dependency property.</param>
	[Theory]
	[MemberData(nameof(AddDependencyPropertyTestData_WithDefaultValue))]
	public void AddDependencyProperty_WithDefaultValue(
		string     name,
		Type       propertyType,
		bool       isReadOnly,
		Visibility getAccessorVisibility,
		Visibility setAccessorVisibility,
		object     defaultValue,
		object     valueToSet)
	{
		// create a new type definition and add the dependency property
		var definition = new ClassDefinition(typeof(DependencyObject), null);
		IGeneratedDependencyProperty addedDependencyPropertyDefinition = definition.AddDependencyProperty(propertyType, name, isReadOnly);
		Assert.NotNull(addedDependencyPropertyDefinition);

		// add an accessor property for the dependency property
		Assert.Null(addedDependencyPropertyDefinition.AccessorProperty);
		IGeneratedProperty addedAccessorPropertyDefinition = addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility);
		Assert.Same(addedDependencyPropertyDefinition.AccessorProperty, addedAccessorPropertyDefinition);

		// adding an accessor property once again should throw an exception
		Assert.Throws<InvalidOperationException>(() => addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility));

		// update the name of the property, if it was not specified explicitly
		name ??= addedDependencyPropertyDefinition.Name;

		// check the definition of the dependency property and its accessor method
		CheckDependencyPropertyDefinition(
			definition,
			name,
			propertyType,
			getAccessorVisibility,
			setAccessorVisibility,
			isReadOnly,
			false,
			null);

		// create the defined type, check the result against the definition
		Type createdType = definition.CreateType();
		CheckTypeAgainstDefinition(createdType, definition);

		// create an instance of the created type and test the dependency property, incl. its accessor property
		CheckCreatedDependencyProperty(
			createdType,
			addedDependencyPropertyDefinition,
			defaultValue,
			valueToSet);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool)"/> method.<br/>
	/// The method should throw a <see cref="CodeGenException"/> if the created type does not derive from <see cref="DependencyObject"/>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithDefaultValue_TypeDoesNotDeriveFromDependencyObject()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;

		var definition = new ClassDefinition(null, ClassAttributes.None);
		var exception = Assert.Throws<CodeGenException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly));
		Assert.Equal($"The defined type ({definition.TypeName}) does not derive from '{typeof(DependencyObject).FullName}'.", exception.Message);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool)"/> method.<br/>
	/// The method should throw an <see cref="ArgumentNullException"/> if the specified type is <c>null</c>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithDefaultValue_TypeIsNull()
	{
		const string name = null;
		const Type propertyType = null;
		const bool isReadOnly = false;

		var definition = new ClassDefinition(typeof(DependencyObject), null);
		var exception = Assert.Throws<ArgumentNullException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly));
		Assert.Equal("type", exception.ParamName);
	}

	#endregion // AddDependencyProperty(Type type, string name, bool isReadOnly)

	#region AddDependencyProperty<T>(string name, bool isReadOnly, T initialValue)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,T)"/> method to add the dependency property
	/// and the <see cref="IGeneratedDependencyProperty.AddAccessorProperty"/> to add an accessor property.
	/// </summary>
	/// <param name="name">Name of the dependency property to add.</param>
	/// <param name="propertyType">Type of the dependency property to add.</param>
	/// <param name="isReadOnly">
	/// <c>true</c> to create a read-only dependency property;<br/>
	/// <c>false</c> to create a read-write dependency property.
	/// </param>
	/// <param name="getAccessorVisibility">Visibility of the 'get' accessor method of the accessor property to add.</param>
	/// <param name="setAccessorVisibility">Visibility of the 'set' accessor method of the accessor property to add.</param>
	/// <param name="initialValue">The expected initial value of the dependency property.</param>
	/// <param name="valueToSet">Value to set to the dependency property.</param>
	[Theory]
	[MemberData(nameof(AddDependencyPropertyTestData_WithInitialValue))]
	public void AddDependencyPropertyT_WithInitialValue(
		string     name,
		Type       propertyType,
		bool       isReadOnly,
		Visibility getAccessorVisibility,
		Visibility setAccessorVisibility,
		object     initialValue,
		object     valueToSet)
	{
		// create a new type definition and add the dependency property
		var definition = new ClassDefinition(typeof(DependencyObject), null);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool), propertyType]));
		var addedDependencyPropertyDefinition = (IGeneratedDependencyProperty)addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly, initialValue]);
		Assert.NotNull(addedDependencyPropertyDefinition);

		// add an accessor property for the dependency property
		Assert.Null(addedDependencyPropertyDefinition.AccessorProperty);
		IGeneratedProperty addedAccessorPropertyDefinition = addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility);
		Assert.Same(addedDependencyPropertyDefinition.AccessorProperty, addedAccessorPropertyDefinition);

		// adding an accessor property once again should throw an exception
		Assert.Throws<InvalidOperationException>(() => addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility));

		// update the name of the property, if it was not specified explicitly
		name ??= addedDependencyPropertyDefinition.Name;

		// check the definition of the dependency property and its accessor method
		CheckDependencyPropertyDefinition(
			definition,
			name,
			propertyType,
			getAccessorVisibility,
			setAccessorVisibility,
			isReadOnly,
			true,
			initialValue);

		// create the defined type, check the result against the definition
		Type createdType = definition.CreateType();
		CheckTypeAgainstDefinition(createdType, definition);

		// create an instance of the created type and test the dependency property, incl. its accessor property
		CheckCreatedDependencyProperty(
			createdType,
			addedDependencyPropertyDefinition,
			initialValue,
			valueToSet);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,T)"/> method.<br/>
	/// The method should throw a <see cref="CodeGenException"/> if the created type does not derive from <see cref="DependencyObject"/>.
	/// </summary>
	[Fact]
	public void AddDependencyPropertyT_WithInitialValue_TypeDoesNotDeriveFromDependencyObject()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;
		const int initialValue = 0;

		var definition = new ClassDefinition(null, ClassAttributes.None);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool), propertyType]));

		var targetInvocationException = Assert.Throws<TargetInvocationException>(() => addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly, initialValue]));
		var exception = Assert.IsType<CodeGenException>(targetInvocationException.InnerException);
		Assert.Equal($"The defined type ({definition.TypeName}) does not derive from '{typeof(DependencyObject).FullName}'.", exception.Message);
	}

	#endregion // AddDependencyProperty<T>(string name, bool isReadOnly, T initialValue)

	#region AddDependencyProperty(Type type, string name, bool isReadOnly, object initialValue)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,object)"/> method to add the dependency property
	/// and the <see cref="IGeneratedDependencyProperty.AddAccessorProperty"/> to add an accessor property.
	/// </summary>
	/// <param name="name">Name of the dependency property to add.</param>
	/// <param name="propertyType">Type of the dependency property to add.</param>
	/// <param name="isReadOnly">
	/// <c>true</c> to create a read-only dependency property;<br/>
	/// <c>false</c> to create a read-write dependency property.
	/// </param>
	/// <param name="getAccessorVisibility">Visibility of the 'get' accessor method of the accessor property to add.</param>
	/// <param name="setAccessorVisibility">Visibility of the 'set' accessor method of the accessor property to add.</param>
	/// <param name="initialValue">The expected initial value of the property.</param>
	/// <param name="valueToSet">Value to set to the dependency property.</param>
	[Theory]
	[MemberData(nameof(AddDependencyPropertyTestData_WithInitialValue))]
	public void AddDependencyProperty_WithInitialValue(
		string     name,
		Type       propertyType,
		bool       isReadOnly,
		Visibility getAccessorVisibility,
		Visibility setAccessorVisibility,
		object     initialValue,
		object     valueToSet)
	{
		// create a new type definition and add the dependency property
		var definition = new ClassDefinition(typeof(DependencyObject), null);
		IGeneratedDependencyProperty addedDependencyPropertyDefinition = definition.AddDependencyProperty(propertyType, name, isReadOnly, initialValue);
		Assert.NotNull(addedDependencyPropertyDefinition);

		// add an accessor property for the dependency property
		Assert.Null(addedDependencyPropertyDefinition.AccessorProperty);
		IGeneratedProperty addedAccessorPropertyDefinition = addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility);
		Assert.Same(addedDependencyPropertyDefinition.AccessorProperty, addedAccessorPropertyDefinition);

		// adding an accessor property once again should throw an exception
		Assert.Throws<InvalidOperationException>(() => addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility));

		// update the name of the property, if it was not specified explicitly
		name ??= addedDependencyPropertyDefinition.Name;

		// check the definition of the dependency property and its accessor method
		CheckDependencyPropertyDefinition(
			definition,
			name,
			propertyType,
			getAccessorVisibility,
			setAccessorVisibility,
			isReadOnly,
			true,
			initialValue);

		// create the defined type, check the result against the definition
		Type createdType = definition.CreateType();
		CheckTypeAgainstDefinition(createdType, definition);

		// create an instance of the created type and test the dependency property, incl. its accessor property
		CheckCreatedDependencyProperty(
			createdType,
			addedDependencyPropertyDefinition,
			initialValue,
			valueToSet);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,object)"/> method.<br/>
	/// The method should throw a <see cref="CodeGenException"/> if the created type does not derive from <see cref="DependencyObject"/>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithInitialValue_TypeDoesNotDeriveFromDependencyObject()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;
		const int initialValue = 0;

		var definition = new ClassDefinition(null, ClassAttributes.None);
		var exception = Assert.Throws<CodeGenException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly, initialValue));
		Assert.Equal($"The defined type ({definition.TypeName}) does not derive from '{typeof(DependencyObject).FullName}'.", exception.Message);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,object)"/> method.
	/// The method should throw an <see cref="ArgumentNullException"/> if the specified type is <c>null</c>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithInitialValue_TypeIsNull()
	{
		const string name = null;
		const Type propertyType = null;
		const bool isReadOnly = false;
		const int initialValue = 0;

		var definition = new ClassDefinition(typeof(DependencyObject), null);
		var exception = Assert.Throws<ArgumentNullException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly, initialValue));
		Assert.Equal("type", exception.ParamName);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,object)"/> method.<br/>
	/// The method should throw an <see cref="ArgumentException"/> if the specified initial value is not assignable to
	/// a property of the specified type.
	/// </summary>
	/// <param name="propertyType">Type of the dependency property to add.</param>
	/// <param name="initialValue">The expected initial value of the property.</param>
	[Theory]
	[MemberData(nameof(AddDependencyPropertyTestData_WithInitialValue_InvalidValue))]
	public void AddDependencyProperty_WithInitialValue_InitialValueIsInvalid(Type propertyType, object initialValue)
	{
		const string name = null;
		const bool isReadOnly = false;

		var definition = new ClassDefinition(typeof(DependencyObject), null);
		var exception = Assert.Throws<ArgumentException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly, initialValue));
		Assert.Equal("initialValue", exception.ParamName);
	}

	#endregion // AddDependencyProperty(Type type, string name, bool isReadOnly, object initialValue)

	#region AddDependencyProperty<T>(string name, bool isReadOnly, DependencyPropertyInitializer initializer)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,DependencyPropertyInitializer)"/>
	/// method to add the dependency property and the <see cref="IGeneratedDependencyProperty.AddAccessorProperty"/>
	/// to add an accessor property.
	/// </summary>
	/// <param name="name">Name of the dependency property to add.</param>
	/// <param name="propertyType">Type of the dependency property to add.</param>
	/// <param name="isReadOnly">
	/// <c>true</c> to create a read-only dependency property;<br/>
	/// <c>false</c> to create a read-write dependency property.
	/// </param>
	/// <param name="getAccessorVisibility">Visibility of the 'get' accessor method of the accessor property to add.</param>
	/// <param name="setAccessorVisibility">Visibility of the 'set' accessor method of the accessor property to add.</param>
	/// <param name="initializer">
	/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
	/// value for the generated dependency property.
	/// </param>
	/// <param name="initialValue">The expected initial value of the dependency property.</param>
	/// <param name="valueToSet">Value to set to the dependency property.</param>
	[Theory]
	[MemberData(nameof(AddDependencyPropertyTestData_WithCustomInitializer))]
	public void AddDependencyPropertyT_WithCustomInitializer(
		string                        name,
		Type                          propertyType,
		bool                          isReadOnly,
		Visibility                    getAccessorVisibility,
		Visibility                    setAccessorVisibility,
		DependencyPropertyInitializer initializer,
		object                        initialValue,
		object                        valueToSet)
	{
		// create a new type definition and add the dependency property
		var definition = new ClassDefinition(typeof(DependencyObject), null);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool), typeof(DependencyPropertyInitializer)]));
		var addedDependencyPropertyDefinition = (IGeneratedDependencyProperty)addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly, initializer]);
		Assert.NotNull(addedDependencyPropertyDefinition);

		// add an accessor property for the dependency property
		Assert.Null(addedDependencyPropertyDefinition.AccessorProperty);
		IGeneratedProperty addedAccessorPropertyDefinition = addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility);
		Assert.Same(addedDependencyPropertyDefinition.AccessorProperty, addedAccessorPropertyDefinition);

		// adding an accessor property once again should throw an exception
		Assert.Throws<InvalidOperationException>(() => addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility));

		// update the name of the property, if it was not specified explicitly
		name ??= addedDependencyPropertyDefinition.Name;

		// check the definition of the dependency property and its accessor method
		CheckDependencyPropertyDefinition(
			definition,
			name,
			propertyType,
			getAccessorVisibility,
			setAccessorVisibility,
			isReadOnly,
			false,
			initialValue);

		// create the defined type, check the result against the definition
		Type createdType = definition.CreateType();
		CheckTypeAgainstDefinition(createdType, definition);

		// create an instance of the created type and test the dependency property, incl. its accessor property
		CheckCreatedDependencyProperty(
			createdType,
			addedDependencyPropertyDefinition,
			initialValue,
			valueToSet);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,DependencyPropertyInitializer)"/> method.<br/>
	/// The method should throw a <see cref="CodeGenException"/> if the created type does not derive from <see cref="DependencyObject"/>.
	/// </summary>
	[Fact]
	public void AddDependencyPropertyT_WithCustomInitializer_TypeDoesNotDeriveFromDependencyObject()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;

		var definition = new ClassDefinition(null, ClassAttributes.None);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool), typeof(DependencyPropertyInitializer)]));

		var targetInvocationException = Assert.Throws<TargetInvocationException>(() => addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly, (DependencyPropertyInitializer)Initializer]));
		var exception = Assert.IsType<CodeGenException>(targetInvocationException.InnerException);
		Assert.Equal($"The defined type ({definition.TypeName}) does not derive from '{typeof(DependencyObject).FullName}'.", exception.Message);
		return;

		static void Initializer(IGeneratedDependencyProperty dp, ILGenerator msil) => msil.Emit(OpCodes.Ldc_I4_0);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,DependencyPropertyInitializer)"/> method.<br/>
	/// The method should throw a <see cref="ArgumentNullException"/> if the specified initializer is <c>null</c>.
	/// </summary>
	[Fact]
	public void AddDependencyPropertyT_WithCustomInitializer_InitializerIsNull()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;
		const DependencyPropertyInitializer initializer = null;

		// create a new type definition and add the dependency property
		var definition = new ClassDefinition(typeof(DependencyObject), null);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool), typeof(DependencyPropertyInitializer)]));

		var targetInvocationException = Assert.Throws<TargetInvocationException>(() => addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly, initializer]));
		var exception = Assert.IsType<ArgumentNullException>(targetInvocationException.InnerException);
		Assert.Equal("initializer", exception.ParamName);
	}

	#endregion // AddDependencyProperty<T>(string name, bool isReadOnly, DependencyPropertyInitializer initializer)

	#region AddDependencyProperty(Type type, string name, bool isReadOnly, DependencyPropertyInitializer initializer)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,DependencyPropertyInitializer)"/>
	/// method to add the dependency property and the <see cref="IGeneratedDependencyProperty.AddAccessorProperty"/>
	/// to add an accessor property.
	/// </summary>
	/// <param name="name">Name of the dependency property to add.</param>
	/// <param name="propertyType">Type of the dependency property to add.</param>
	/// <param name="isReadOnly">
	/// <c>true</c> to create a read-only dependency property;<br/>
	/// <c>false</c> to create a read-write dependency property.
	/// </param>
	/// <param name="getAccessorVisibility">Visibility of the 'get' accessor method of the accessor property to add.</param>
	/// <param name="setAccessorVisibility">Visibility of the 'set' accessor method of the accessor property to add.</param>
	/// <param name="initializer">
	/// A callback that provides an implementation pushing an object onto the evaluation stack to use as the initial
	/// value for the generated dependency property.
	/// </param>
	/// <param name="initialValue">The expected initial value of the property.</param>
	/// <param name="valueToSet">Value to set to the dependency property.</param>
	[Theory]
	[MemberData(nameof(AddDependencyPropertyTestData_WithCustomInitializer))]
	public void AddDependencyProperty_WithCustomInitializer(
		string                        name,
		Type                          propertyType,
		bool                          isReadOnly,
		Visibility                    getAccessorVisibility,
		Visibility                    setAccessorVisibility,
		DependencyPropertyInitializer initializer,
		object                        initialValue,
		object                        valueToSet)
	{
		// create a new type definition and add the dependency property
		var definition = new ClassDefinition(typeof(DependencyObject), null);
		IGeneratedDependencyProperty addedDependencyPropertyDefinition = definition.AddDependencyProperty(propertyType, name, isReadOnly, initializer);
		Assert.NotNull(addedDependencyPropertyDefinition);

		// add an accessor property for the dependency property
		Assert.Null(addedDependencyPropertyDefinition.AccessorProperty);
		IGeneratedProperty addedAccessorPropertyDefinition = addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility);
		Assert.Same(addedDependencyPropertyDefinition.AccessorProperty, addedAccessorPropertyDefinition);

		// adding an accessor property once again should throw an exception
		Assert.Throws<InvalidOperationException>(() => addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility));

		// update the name of the property, if it was not specified explicitly
		name ??= addedDependencyPropertyDefinition.Name;

		// check the definition of the dependency property and its accessor method
		CheckDependencyPropertyDefinition(
			definition,
			name,
			propertyType,
			getAccessorVisibility,
			setAccessorVisibility,
			isReadOnly,
			false,
			initialValue);

		// create the defined type, check the result against the definition
		Type createdType = definition.CreateType();
		CheckTypeAgainstDefinition(createdType, definition);

		// create an instance of the created type and test the dependency property, incl. its accessor property
		CheckCreatedDependencyProperty(
			createdType,
			addedDependencyPropertyDefinition,
			initialValue,
			valueToSet);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,DependencyPropertyInitializer)"/> method.<br/>
	/// The method should throw a <see cref="CodeGenException"/> if the created type does not derive from <see cref="DependencyObject"/>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithCustomInitializer_TypeDoesNotDeriveFromDependencyObject()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;

		var definition = new ClassDefinition(null, ClassAttributes.None);
		var exception = Assert.Throws<CodeGenException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly, Initializer));
		Assert.Equal($"The defined type ({definition.TypeName}) does not derive from '{typeof(DependencyObject).FullName}'.", exception.Message);
		return;

		static void Initializer(IGeneratedDependencyProperty dp, ILGenerator msil) => msil.Emit(OpCodes.Ldc_I4_0);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,DependencyPropertyInitializer)"/> method.<br/>
	/// The method should throw an <see cref="ArgumentNullException"/> if the specified type is <c>null</c>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithCustomInitializer_TypeIsNull()
	{
		const string name = null;
		const Type propertyType = null;
		const bool isReadOnly = false;

		var definition = new ClassDefinition(typeof(DependencyObject), null);
		var exception = Assert.Throws<ArgumentNullException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly, Initializer));
		Assert.Equal("type", exception.ParamName);
		return;

		static void Initializer(IGeneratedDependencyProperty dp, ILGenerator msil) => msil.Emit(OpCodes.Ldc_I4_0);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,DependencyPropertyInitializer)"/> method.
	/// The method should throw an <see cref="ArgumentNullException"/> if the specified initializer is <c>null</c>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithCustomInitializer_InitializerIsNull()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;
		const DependencyPropertyInitializer initializer = null;

		var definition = new ClassDefinition(typeof(DependencyObject), null);
		var exception = Assert.Throws<ArgumentNullException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly, initializer));
		Assert.Equal("initializer", exception.ParamName);
	}

	#endregion // AddDependencyProperty<T>(Type type, string name, bool isReadOnly, DependencyPropertyInitializer initializer)

	#region AddDependencyProperty<T>(string name, bool isReadOnly, ProvideValueCallback<T> provideInitialValueCallback)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,ProvideValueCallback{T})"/>
	/// method to add the dependency property and the <see cref="IGeneratedDependencyProperty.AddAccessorProperty"/>
	/// to add an accessor property.
	/// </summary>
	/// <param name="name">Name of the dependency property to add.</param>
	/// <param name="propertyType">Type of the dependency property to add.</param>
	/// <param name="isReadOnly">
	/// <c>true</c> to create a read-only dependency property;<br/>
	/// <c>false</c> to create a read-write dependency property.
	/// </param>
	/// <param name="getAccessorVisibility">Visibility of the 'get' accessor method of the accessor property to add.</param>
	/// <param name="setAccessorVisibility">Visibility of the 'set' accessor method of the accessor property to add.</param>
	/// <param name="provideInitialValueCallback">A factory callback that provides the initial value of the generated dependency property.</param>
	/// <param name="initialValue">The expected initial value of the dependency property.</param>
	/// <param name="valueToSet">Value to set to the dependency property.</param>
	[Theory]
	[MemberData(nameof(AddDependencyPropertyTestData_WithTypedFactoryCallbackInitializer))]
	public void AddDependencyPropertyT_WithFactoryCallbackInitializer(
		string     name,
		Type       propertyType,
		bool       isReadOnly,
		Visibility getAccessorVisibility,
		Visibility setAccessorVisibility,
		Delegate   provideInitialValueCallback,
		object     initialValue,
		object     valueToSet)
	{
		// create a new type definition and add the dependency property
		var definition = new ClassDefinition(typeof(DependencyObject), null);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool), typeof(ProvideValueCallback<>).MakeGenericType(propertyType)]));
		var addedDependencyPropertyDefinition = (IGeneratedDependencyProperty)addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly, provideInitialValueCallback]);
		Assert.NotNull(addedDependencyPropertyDefinition);

		// add an accessor property for the dependency property
		Assert.Null(addedDependencyPropertyDefinition.AccessorProperty);
		IGeneratedProperty addedAccessorPropertyDefinition = addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility);
		Assert.Same(addedDependencyPropertyDefinition.AccessorProperty, addedAccessorPropertyDefinition);

		// adding an accessor property once again should throw an exception
		Assert.Throws<InvalidOperationException>(() => addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility));

		// update the name of the property, if it was not specified explicitly
		name ??= addedDependencyPropertyDefinition.Name;

		// check the definition of the dependency property and its accessor method
		CheckDependencyPropertyDefinition(
			definition,
			name,
			propertyType,
			getAccessorVisibility,
			setAccessorVisibility,
			isReadOnly,
			false,
			initialValue);

		// create the defined type, check the result against the definition
		Type createdType = definition.CreateType();
		CheckTypeAgainstDefinition(createdType, definition);

		// create an instance of the created type and test the dependency property, incl. its accessor property
		CheckCreatedDependencyProperty(
			createdType,
			addedDependencyPropertyDefinition,
			initialValue,
			valueToSet);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,ProvideValueCallback{T})"/> method.<br/>
	/// The method should throw a <see cref="CodeGenException"/> if the created type does not derive from <see cref="DependencyObject"/>.
	/// </summary>
	[Fact]
	public void AddDependencyPropertyT_WithFactoryCallbackInitializer_TypeDoesNotDeriveFromDependencyObject()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;

		var definition = new ClassDefinition(null, ClassAttributes.None);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool), typeof(ProvideValueCallback<>).MakeGenericType(propertyType)]));

		var targetInvocationException = Assert.Throws<TargetInvocationException>(() => addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly, (ProvideValueCallback<int>)ProvideInitialValueCallback]));
		var exception = Assert.IsType<CodeGenException>(targetInvocationException.InnerException);
		Assert.Equal($"The defined type ({definition.TypeName}) does not derive from '{typeof(DependencyObject).FullName}'.", exception.Message);
		return;

		static int ProvideInitialValueCallback() => 0;
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty{T}(string,bool,ProvideValueCallback{T})"/> method.<br/>
	/// The method should throw an <see cref="ArgumentNullException"/> if the specified callback is <c>null</c>.
	/// </summary>
	[Fact]
	public void AddDependencyPropertyT_WithFactoryCallbackInitializer_InitializerIsNull()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;
		const ProvideValueCallback<int> provideInitialValueCallback = null;

		var definition = new ClassDefinition(typeof(DependencyObject), null);
		MethodInfo addDependencyPropertyMethod = typeof(ClassDefinition)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(method => method.Name == nameof(ClassDefinition.AddDependencyProperty))
			.Where(method => method.GetGenericArguments().Length == 1)
			.Select(method => method.MakeGenericMethod(propertyType))
			.Single(
				method => method
					.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.SequenceEqual([typeof(string), typeof(bool), typeof(ProvideValueCallback<>).MakeGenericType(propertyType)]));

		var targetInvocationException = Assert.Throws<TargetInvocationException>(() => addDependencyPropertyMethod.Invoke(definition, [name, isReadOnly, provideInitialValueCallback]));
		var exception = Assert.IsType<ArgumentNullException>(targetInvocationException.InnerException);
		Assert.Equal("provideInitialValueCallback", exception.ParamName);
	}

	#endregion // AddDependencyProperty<T>(string name, bool isReadOnly, ProvideValueCallback<T> provideInitialValueCallback)

	#region AddDependencyProperty(Type type, string name, bool isReadOnly, ProvideValueCallback provideInitialValueCallback)

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,ProvideValueCallback)"/>
	/// method to add the dependency property and the <see cref="IGeneratedDependencyProperty.AddAccessorProperty"/>
	/// to add an accessor property.
	/// </summary>
	/// <param name="name">Name of the dependency property to add.</param>
	/// <param name="propertyType">Type of the dependency property to add.</param>
	/// <param name="isReadOnly">
	/// <c>true</c> to create a read-only dependency property;<br/>
	/// <c>false</c> to create a read-write dependency property.
	/// </param>
	/// <param name="getAccessorVisibility">Visibility of the 'get' accessor method of the accessor property to add.</param>
	/// <param name="setAccessorVisibility">Visibility of the 'set' accessor method of the accessor property to add.</param>
	/// <param name="provideInitialValueCallback">A factory callback that provides the initial value of the generated dependency property.</param>
	/// <param name="initialValue">The expected initial value of the property.</param>
	/// <param name="valueToSet">Value to set to the dependency property.</param>
	[Theory]
	[MemberData(nameof(AddDependencyPropertyTestData_WithUntypedFactoryCallbackInitializer))]
	public void AddDependencyProperty_WithFactoryCallbackInitializer(
		string               name,
		Type                 propertyType,
		bool                 isReadOnly,
		Visibility           getAccessorVisibility,
		Visibility           setAccessorVisibility,
		ProvideValueCallback provideInitialValueCallback,
		object               initialValue,
		object               valueToSet)
	{
		// create a new type definition and add the dependency property
		var definition = new ClassDefinition(typeof(DependencyObject), null);
		IGeneratedDependencyProperty addedDependencyPropertyDefinition = definition.AddDependencyProperty(propertyType, name, isReadOnly, provideInitialValueCallback);
		Assert.NotNull(addedDependencyPropertyDefinition);

		// add an accessor property for the dependency property
		Assert.Null(addedDependencyPropertyDefinition.AccessorProperty);
		IGeneratedProperty addedAccessorPropertyDefinition = addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility);
		Assert.Same(addedDependencyPropertyDefinition.AccessorProperty, addedAccessorPropertyDefinition);

		// adding an accessor property once again should throw an exception
		Assert.Throws<InvalidOperationException>(() => addedDependencyPropertyDefinition.AddAccessorProperty(null, getAccessorVisibility, setAccessorVisibility));

		// update the name of the property, if it was not specified explicitly
		name ??= addedDependencyPropertyDefinition.Name;

		// check the definition of the dependency property and its accessor method
		CheckDependencyPropertyDefinition(
			definition,
			name,
			propertyType,
			getAccessorVisibility,
			setAccessorVisibility,
			isReadOnly,
			false,
			initialValue);

		// create the defined type, check the result against the definition
		Type createdType = definition.CreateType();
		CheckTypeAgainstDefinition(createdType, definition);

		// create an instance of the created type and test the dependency property, incl. its accessor property
		CheckCreatedDependencyProperty(
			createdType,
			addedDependencyPropertyDefinition,
			initialValue,
			valueToSet);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,ProvideValueCallback)"/> method.<br/>
	/// The method should throw a <see cref="CodeGenException"/> if the created type does not derive from <see cref="DependencyObject"/>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithFactoryCallbackInitializer_TypeDoesNotDeriveFromDependencyObject()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;

		var definition = new ClassDefinition(null, ClassAttributes.None);
		var exception = Assert.Throws<CodeGenException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly, ProvideInitialValueCallback));
		Assert.Equal($"The defined type ({definition.TypeName}) does not derive from '{typeof(DependencyObject).FullName}'.", exception.Message);
		return;

		static object ProvideInitialValueCallback() => 0;
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,ProvideValueCallback)"/> method.<br/>
	/// The method should throw an <see cref="ArgumentNullException"/> if the specified type is <c>null</c>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithFactoryCallbackInitializer_TypeIsNull()
	{
		const string name = null;
		const Type propertyType = null;
		const bool isReadOnly = false;
		const ProvideValueCallback provideInitialValueCallback = null;

		var definition = new ClassDefinition(typeof(DependencyObject), null);
		var exception = Assert.Throws<ArgumentNullException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly, provideInitialValueCallback));
		Assert.Equal("type", exception.ParamName);
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.AddDependencyProperty(Type,string,bool,ProvideValueCallback)"/> method.<br/>
	/// The method should throw an <see cref="ArgumentNullException"/> if the specified callback is <c>null</c>.
	/// </summary>
	[Fact]
	public void AddDependencyProperty_WithFactoryCallbackInitializer_InitializerIsNull()
	{
		const string name = null;
		Type propertyType = typeof(int);
		const bool isReadOnly = false;
		const ProvideValueCallback provideInitialValueCallback = null;

		var definition = new ClassDefinition(typeof(DependencyObject), null);
		var exception = Assert.Throws<ArgumentNullException>(() => definition.AddDependencyProperty(propertyType, name, isReadOnly, provideInitialValueCallback));
		Assert.Equal("provideInitialValueCallback", exception.ParamName);
	}

	#endregion // AddDependencyProperty<T>(Type type, string name, bool isReadOnly, ProvideValueCallback provideInitialValueCallback)

	#region Helpers

	/// <summary>
	/// Checks whether the specified type definition contains a dependency property definition with the specified
	/// name and whether the dependency property and its accessor property is defined as expected.
	/// </summary>
	/// <param name="definition">
	/// The class definition containing the generated dependency property to check.
	/// </param>
	/// <param name="dependencyPropertyName">
	/// The name of the generated dependency property to check.
	/// </param>
	/// <param name="propertyType">
	/// Type of the dependency property.
	/// </param>
	/// <param name="getAccessorVisibility">
	/// Expected visibility of the 'get' accessor method of the accessor property of the dependency property.
	/// </param>
	/// <param name="setAccessorVisibility">
	/// Expected visibility of the 'set' accessor method of the accessor property of the dependency property.
	/// </param>
	/// <param name="isReadOnly">
	/// <c>true</c> if the dependency property is expected to be read-only;<br/>
	/// <c>false</c> if the dependency property is expected to be read-write.
	/// </param>
	/// <param name="hasInitialValue">
	/// <c>true</c> if the dependency property is expected to have an initial value;<br/>
	/// <c>false</c> if the dependency property is not expected to have an initial value.
	/// </param>
	/// <param name="initialValue">
	/// The expected initial value of the dependency property, if <paramref name="hasInitialValue"/> is <c>true</c>.
	/// </param>
	private static void CheckDependencyPropertyDefinition(
		ClassDefinition definition,
		string          dependencyPropertyName,
		Type            propertyType,
		Visibility      getAccessorVisibility,
		Visibility      setAccessorVisibility,
		bool            isReadOnly,
		bool            hasInitialValue,
		object          initialValue)
	{
		// check the definition of the dependency property
		// -----------------------------------------------------------------------------------------------------------------
		IGeneratedDependencyProperty dependencyPropertyDefinition = definition
			.GeneratedDependencyProperties
			.SingleOrDefault(dependencyProperty => dependencyProperty.Name == dependencyPropertyName);
		Assert.NotNull(dependencyPropertyDefinition);
		Assert.Equal(propertyType, dependencyPropertyDefinition.Type);
		Assert.Equal(isReadOnly, dependencyPropertyDefinition.IsReadOnly);
		Assert.Equal(hasInitialValue, dependencyPropertyDefinition.HasInitialValue);
		Assert.Equal(
			hasInitialValue
				? initialValue
				: propertyType.IsValueType
					? Activator.CreateInstance(propertyType)
					: null,
			dependencyPropertyDefinition.InitialValue);

		// check the definition of the backing field
		// -----------------------------------------------------------------------------------------------------------------
		Type expectedBackingFieldType = isReadOnly ? typeof(DependencyPropertyKey) : typeof(DependencyProperty);
		IGeneratedField backingFieldDefinition = definition
			.GeneratedFields
			.SingleOrDefault(field => field.Name == dependencyPropertyName + "Property");
		Assert.NotNull(backingFieldDefinition);
		Assert.Equal(Visibility.Public, backingFieldDefinition.Visibility);
		Assert.True(backingFieldDefinition.IsStatic);
		Assert.Equal(expectedBackingFieldType, backingFieldDefinition.FieldType);
		Assert.False(backingFieldDefinition.HasInitialValue);

		// check the definition of the accessor property
		// -----------------------------------------------------------------------------------------------------------------
		Assert.NotNull(dependencyPropertyDefinition.AccessorProperty);
		IGeneratedProperty accessorPropertyDefinition = dependencyPropertyDefinition.AccessorProperty;
		Assert.Equal(propertyType, accessorPropertyDefinition.PropertyType);

		// get accessor
		Assert.NotNull(accessorPropertyDefinition.GetAccessor);
		Assert.Equal(MethodKind.Normal, accessorPropertyDefinition.GetAccessor.Kind);
		Assert.Equal("get_" + dependencyPropertyDefinition.Name, accessorPropertyDefinition.GetAccessor.Name);
		Assert.NotNull(accessorPropertyDefinition.GetAccessor.MethodInfo);
		Assert.Equal(propertyType, accessorPropertyDefinition.GetAccessor.ReturnType);
		Assert.Empty(accessorPropertyDefinition.GetAccessor.ParameterTypes);
		Assert.Equal(getAccessorVisibility, accessorPropertyDefinition.GetAccessor.Visibility);

		// set accessor
		Assert.NotNull(accessorPropertyDefinition.SetAccessor);
		Assert.Equal(MethodKind.Normal, accessorPropertyDefinition.SetAccessor.Kind);
		Assert.Equal("set_" + dependencyPropertyDefinition.Name, accessorPropertyDefinition.SetAccessor.Name);
		Assert.NotNull(accessorPropertyDefinition.SetAccessor.MethodInfo);
		Assert.Equal(typeof(void), accessorPropertyDefinition.SetAccessor.ReturnType);
		Assert.Single(accessorPropertyDefinition.SetAccessor.ParameterTypes);
		Assert.Equal(propertyType, accessorPropertyDefinition.SetAccessor.ParameterTypes.First());
		Assert.Equal(setAccessorVisibility, accessorPropertyDefinition.SetAccessor.Visibility);
	}

	/// <summary>
	/// Checks whether the specified dynamically created type can be instantiated and then checks whether the dependency property
	/// itself and its accessor property can be got and set.
	/// </summary>
	/// <param name="createdType">The dynamically created type defining the dependency property to test.</param>
	/// <param name="dependencyPropertyDefinition">The definition of the property to test.</param>
	/// <param name="expectedInitialValue">The value the dependency property that is expected at start.</param>
	/// <param name="valueToSet">Value to set the dependency property to.</param>
	private static void CheckCreatedDependencyProperty(
		Type                         createdType,
		IGeneratedDependencyProperty dependencyPropertyDefinition,
		object                       expectedInitialValue,
		object                       valueToSet)
	{
		// create an instance of the dynamically created type
		object instance = Activator.CreateInstance(createdType);
		Assert.NotNull(instance);

		// check whether the expected static backing field (property name + "Property") has been generated
		// dependency property is read-only => backing field should be of type Windows.DependencyPropertyKey
		// dependency property is read/write => backing field should be of type Windows.DependencyProperty
		FieldInfo backingField = createdType.GetField(dependencyPropertyDefinition.Name + "Property", ExactDeclaredOnlyBindingFlags);
		Assert.NotNull(backingField);
		object dependencyPropertyBackingFieldValue = backingField.GetValue(null);
		Assert.NotNull(dependencyPropertyBackingFieldValue);

		// get the accessor property of the dependency property
		PropertyInfo accessorProperty = createdType
			.GetProperties(ExactDeclaredOnlyBindingFlags)
			.SingleOrDefault(property => property.Name == dependencyPropertyDefinition.Name);
		Assert.NotNull(accessorProperty);
		Assert.Equal(dependencyPropertyDefinition.Type, accessorProperty.PropertyType);
		Assert.True(accessorProperty.CanRead);
		Assert.True(accessorProperty.CanWrite);
		Assert.NotNull(accessorProperty.GetMethod);
		Assert.NotNull(accessorProperty.SetMethod);

		// check whether the dependency property itself and the accessor property return the expected value
		// --------------------------------------------------------------------------------------------------------

		// check whether the dependency property itself returns the expected initial value
		object dependencyPropertyValue = dependencyPropertyDefinition.IsReadOnly
			                                 ? ((DependencyObject)instance).GetValue(((DependencyPropertyKey)dependencyPropertyBackingFieldValue).DependencyProperty)
			                                 : ((DependencyObject)instance).GetValue((DependencyProperty)dependencyPropertyBackingFieldValue);
		Assert.Equal(expectedInitialValue, dependencyPropertyValue);

		// check whether the 'get' accessor of the accessor property returns the expected initial value
		object value = accessorProperty.GetMethod.Invoke(instance, []);
		Assert.Equal(expectedInitialValue, value);

		// set the dependency property using the dependency property itself and check whether the dependency
		// property and its accessor property return the value
		// --------------------------------------------------------------------------------------------------------

		// set the value of the dependency property using the dependency property itself
		if (dependencyPropertyDefinition.IsReadOnly)
			((DependencyObject)instance).SetValue((DependencyPropertyKey)dependencyPropertyBackingFieldValue, valueToSet);
		else
			((DependencyObject)instance).SetValue((DependencyProperty)dependencyPropertyBackingFieldValue, valueToSet);

		// now the dependency property and its accessor property should return the new value

		// check whether the dependency property returns the expected value
		dependencyPropertyValue = dependencyPropertyDefinition.IsReadOnly
			                          ? ((DependencyObject)instance).GetValue(((DependencyPropertyKey)dependencyPropertyBackingFieldValue).DependencyProperty)
			                          : ((DependencyObject)instance).GetValue((DependencyProperty)dependencyPropertyBackingFieldValue);
		Assert.Equal(valueToSet, dependencyPropertyValue);

		// check whether the 'get' accessor of the accessor property returns the expected value
		value = accessorProperty.GetMethod.Invoke(instance, []);
		Assert.Equal(valueToSet, value);

		// set the dependency property back to the initial value using the 'set' accessor of its accessor property
		// and check whether the dependency property and its accessor return the value
		// --------------------------------------------------------------------------------------------------------

		// set the dependency property using its accessor property
		accessorProperty.SetMethod.Invoke(instance, [expectedInitialValue]);

		// now the dependency property and its accessor property should return the initial value once again

		// check whether the dependency property returns the expected value
		dependencyPropertyValue = dependencyPropertyDefinition.IsReadOnly
			                          ? ((DependencyObject)instance).GetValue(((DependencyPropertyKey)dependencyPropertyBackingFieldValue).DependencyProperty)
			                          : ((DependencyObject)instance).GetValue((DependencyProperty)dependencyPropertyBackingFieldValue);
		Assert.Equal(expectedInitialValue, dependencyPropertyValue);

		// check whether the 'get' accessor of the accessor property returns the expected value
		value = accessorProperty.GetMethod.Invoke(instance, []);
		Assert.Equal(expectedInitialValue, value);
	}

	#endregion // Helpers

	#endregion // Adding Dependency Properties
}

#elif NETCOREAPP2_2 || NETCOREAPP3_1 || NET5_0 || NET6_0 || NET7_0 || NET8_0
// Dependency properties are not supported on .NET Core 2.2/3.1 and .NET5/6/7/8 without Windows extensions...
#else
#error Unhandled Target Framework.
#endif
