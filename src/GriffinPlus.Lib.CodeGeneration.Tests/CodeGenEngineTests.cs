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

using GriffinPlus.Lib.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace UnitTests
{
	/// <summary>
	/// Tests around the <see cref="CodeGenEngine"/> class.
	/// </summary>
	public class CodeGenEngineTests
	{
		#region Generating Empty Class (Standalone and Derived From Base Class)

		public class ClassWithParameterlessConstructor
		{
			public ClassWithParameterlessConstructor()
			{
			}
		}

		public class ClassWithoutParameterlessConstructor
		{
			public ClassWithoutParameterlessConstructor(int x)
			{
			}
		}

		[Theory]
		[InlineData("MyClass")] // specific class name
		[InlineData(null)]      // random class name
		public void GenerateEmptyClass(string className)
		{
			// create the type
			ClassDefinition classDefinition = new ClassDefinition(className);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			if (className != null) Assert.Equal(className, classType.Name);

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);
		}

		[Theory]
		[InlineData("MyClass", false)] // specific class name, do not create pass-through constructors
		[InlineData(null,      false)] // random class name, do not create pass-through constructors
		[InlineData("MyClass", true)]  // specific class name, create pass-through constructors
		[InlineData(null,      true)]  // random class name, create pass-through constructors
		public void GenerateEmptyClass_DerivedFromClassWithParameterlessConstructor(string className, bool createPassThroughConstructors)
		{
			// create the type
			ClassDefinition classDefinition = new ClassDefinition(typeof(ClassWithParameterlessConstructor), false, "MyClass");
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(ClassWithParameterlessConstructor), classType.BaseType);
			if (className != null) Assert.Equal(className, classType.Name);

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);
		}

		[Theory]
		[InlineData("MyClass")] // specific class name
		[InlineData(null)] // random class name
		public void GenerateEmptyClass_DerivedFromClassWithoutParameterlessConstructor(string className)
		{
			Assert.Throws<CodeGenException>(() =>
			{
				// should throw an exception, because the base class does not have a parameterless constructor
				ClassDefinition classDefinition = new ClassDefinition(typeof(ClassWithoutParameterlessConstructor), false, className);
				CodeGenEngine.CreateClass(classDefinition);
			});
		}

		#endregion

		#region AddField

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField CodeGenEngine.AddField<T>(string name = null, Visibility visibility = Visibility.Private)
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="defaultValue">Expected value the field should have after creating the object dynamically.</param>
		[Theory]
		[MemberData(nameof(TestDataGenerator_AddField_WithoutDefaultValue))]
		public void AddField_WithoutDefaultValue(Visibility visibility, Type fieldType, object defaultValue)
		{
			// generate the field name
			string fieldName = "m" + fieldType.Name;

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				MethodInfo genericMethod = typeof(CodeGenEngine)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(x => x.Name == "AddField" && x.IsGenericMethodDefinition && x.GetGenericArguments().Length == 1)
					.Where(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string), typeof(Visibility) }))
					.Single();
				MethodInfo method = genericMethod.MakeGenericMethod(fieldType);
				method.Invoke(m.Engine, new object[] { fieldName, visibility });
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(field);
			Assert.Equal(fieldType, field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether fields have a value of the expected type and default type
			var fieldValue = GetFieldValue(obj, fieldName);
			Assert.Equal(defaultValue, fieldValue);
			if (defaultValue != null) 
			{
				// the type of the field must match the type of the default value
				Assert.IsType(defaultValue.GetType(), fieldValue);

				// if the field type is a reference type, the value must actually be the same...
				if (!fieldType.IsValueType) Assert.Same(defaultValue, fieldValue);
			}
		}

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField CodeGenEngine.AddField<T>(string name = null, Visibility visibility = Visibility.Private, T defaultValue = default(T))
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="defaultValue">Expected value the field should have after creating the object dynamically.</param>
		[Theory]
		[MemberData(nameof(TestDataGenerator_AddField_WithDefaultValue))]
		public void AddField_WithDefaultValue(Visibility visibility, Type fieldType, object defaultValue)
		{
			// generate the field name
			string fieldName = "m" + fieldType.Name;

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				MethodInfo genericMethod = typeof(CodeGenEngine)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(x => x.Name == "AddField" && x.IsGenericMethodDefinition && x.GetGenericArguments().Length == 1)
					.Where(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string), typeof(Visibility), x.GetGenericArguments()[0] }))
					.Single();
				MethodInfo method = genericMethod.MakeGenericMethod(fieldType);
				method.Invoke(m.Engine, new object[] { fieldName, visibility, defaultValue });
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(field);
			Assert.Equal(fieldType, field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether fields have a value of the expected type and default type
			var fieldValue = GetFieldValue(obj, fieldName);
			Assert.Equal(defaultValue, fieldValue);
			if (defaultValue != null) 
			{
				// the type of the field must match the type of the default value
				Assert.IsType(defaultValue.GetType(), fieldValue);

				// if the field type is a reference type, the value must actually be the same...
				if (!fieldType.IsValueType) Assert.Same(defaultValue, fieldValue);
			}
		}

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField CodeGenEngine.AddField&lt;T&gt;(string name = null, Visibility visibility = Visibility.Private, Func&lt;T&gt; factory)
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="defaultValue">Expected value the field should have after creating the object dynamically.</param>
		[Theory]
		[InlineData(Visibility.Public)]
		[InlineData(Visibility.Protected)]
		[InlineData(Visibility.ProtectedInternal)]
		[InlineData(Visibility.Internal)]
		[InlineData(Visibility.Private)]
		public void AddField_WithFactoryMethod_Struct(Visibility visibility)
		{
			// generate the field name
			string fieldName = "mField";

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				m.Engine.AddField<DemoStruct>(fieldName, visibility, () => new DemoStruct() { MyInt32 = 42, MyString = "Lorem Ipsum" });
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(field);
			Assert.Equal(typeof(DemoStruct), field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the type
			object obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether the field has the expected value
			var fieldValue = GetFieldValue(obj, fieldName);
			Assert.NotNull(fieldValue);
			Assert.IsType<DemoStruct>(fieldValue);
			Assert.Equal(42, ((DemoStruct)fieldValue).MyInt32);
			Assert.Equal("Lorem Ipsum", ((DemoStruct)fieldValue).MyString);
		}

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField CodeGenEngine.AddField&lt;T&gt;(string name = null, Visibility visibility = Visibility.Private, Func&lt;T&gt; factory)
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="defaultValue">Expected value the field should have after creating the object dynamically.</param>
		[Theory]
		[InlineData(Visibility.Public)]
		[InlineData(Visibility.Protected)]
		[InlineData(Visibility.ProtectedInternal)]
		[InlineData(Visibility.Internal)]
		[InlineData(Visibility.Private)]
		public void AddField_WithFactoryMethod_Class(Visibility visibility)
		{
			// generate the field name
			string fieldName = "mField";

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				m.Engine.AddField<DemoClass>(fieldName, visibility, () => new DemoClass() { MyInt32 = 42, MyString = "Lorem Ipsum" } );
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(field);
			Assert.Equal(typeof(DemoClass), field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the type
			object obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether the field has the expected value
			var fieldValue = GetFieldValue(obj, fieldName);
			Assert.NotNull(fieldValue);
			Assert.IsType<DemoClass>(fieldValue);
			Assert.Equal(42, ((DemoClass)fieldValue).MyInt32);
			Assert.Equal("Lorem Ipsum", ((DemoClass)fieldValue).MyString);
		}

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField CodeGenEngine.AddField<T>(string name, Visibility visibility, FieldInitializer initializer)
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="expectedValue">Expected value the field should have after creating the object dynamically.</param>
		/// <param name="initializer">Delegate that emits code to initialize the field.</param>
		[Theory]
		[MemberData(nameof(TestDataGenerator_AddField_WithInitializer))]
		public void AddField_WithInitializer(Visibility visibility, Type fieldType, object expectedValue, FieldInitializer initializer)
		{
			// generate the field name
			string fieldName = "m" + fieldType.Name;

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				MethodInfo genericMethod = typeof(CodeGenEngine)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(x => x.Name == "AddField" && x.IsGenericMethodDefinition && x.GetGenericArguments().Length == 1)
					.Where(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(FieldInitializer) }))
					.Single();
				MethodInfo method = genericMethod.MakeGenericMethod(fieldType);
				method.Invoke(m.Engine, new object[] { fieldName, visibility, initializer });
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(field);
			Assert.Equal(fieldType, field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether fields have a value of the expected type and default type
			var fieldValue = GetFieldValue(obj, fieldName);
			Assert.Equal(expectedValue, fieldValue);
		}

		private static object GetFieldValue(object obj, string name)
		{
			FieldInfo field = obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			object value = field.GetValue(obj);
			return value;
		}

		#endregion

		#region AddStaticField

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField AddStaticField<T>(string name = null, Visibility visibility = Visibility.Private)
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="defaultValue">Expected value the field should have after creating the object dynamically.</param>
		[Theory]
		[MemberData(nameof(TestDataGenerator_AddField_WithoutDefaultValue))]
		public void AddStaticField_WithoutDefaultValue(Visibility visibility, Type fieldType, object defaultValue)
		{
			// generate the field name
			string fieldName = "s" + fieldType.Name;

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				MethodInfo genericMethod = typeof(CodeGenEngine)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(x => x.Name == "AddStaticField" && x.IsGenericMethodDefinition && x.GetGenericArguments().Length == 1)
					.Where(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string), typeof(Visibility) }))
					.Single();
				MethodInfo method = genericMethod.MakeGenericMethod(fieldType);
				method.Invoke(m.Engine, new object[] { fieldName, visibility });
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(field);
			Assert.Equal(fieldType, field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the type
			// (not really needed to access the static field, but nice to know whether the type can be instantiated)
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether field has the expected value
			var fieldValue = GetStaticFieldValue(classType, fieldName);
			Assert.Equal(defaultValue, fieldValue);
			if (defaultValue != null) 
			{
				Assert.IsType(defaultValue.GetType(), fieldValue); // the type of the field must match the type of the default value
				if (!fieldType.IsValueType) Assert.Same(defaultValue, fieldValue); // if the field type is a reference type, the value must actually be the same...
			}
		}

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField AddStaticField<T>(string name = null, Visibility visibility = Visibility.Private, T defaultValue = default(T))
		/// (works for primitive types only, more complex types require an initializer)
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="defaultValue">Expected value the field should have after creating the object dynamically.</param>
		[Theory]
		[MemberData(nameof(TestDataGenerator_AddField_WithDefaultValue))]
		public void AddStaticField_WithDefaultValue(Visibility visibility, Type fieldType, object defaultValue)
		{
			// generate the field name
			string fieldName = "s" + fieldType.Name;

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				MethodInfo genericMethod = typeof(CodeGenEngine)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(x => x.Name == "AddStaticField" && x.IsGenericMethodDefinition && x.GetGenericArguments().Length == 1)
					.Where(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string), typeof(Visibility), x.GetGenericArguments()[0] }))
					.Single();
				MethodInfo method = genericMethod.MakeGenericMethod(fieldType);
				method.Invoke(m.Engine, new object[] { fieldName, visibility, defaultValue });
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(field);
			Assert.Equal(fieldType, field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the class
			// (not really needed to access the static field, but nice to know whether the type can be instantiated)
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether field has the expected value
			var fieldValue = GetStaticFieldValue(classType, fieldName);
			Assert.Equal(defaultValue, fieldValue);
			if (defaultValue != null) 
			{
				Assert.IsType(defaultValue.GetType(), fieldValue); // the type of the field must match the type of the default value
				if (!fieldType.IsValueType) Assert.Same(defaultValue, fieldValue); // if the field type is a reference type, the value must actually be the same...
			}
		}

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField CodeGenEngine.AddStaticField&lt;T&gt;(string name = null, Visibility visibility = Visibility.Private, Func&lt;T&gt; factory)
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="defaultValue">Expected value the field should have after creating the object dynamically.</param>
		[Theory]
		[InlineData(Visibility.Public)]
		[InlineData(Visibility.Protected)]
		[InlineData(Visibility.ProtectedInternal)]
		[InlineData(Visibility.Internal)]
		[InlineData(Visibility.Private)]
		public void AddStaticField_WithFactoryMethod_Struct(Visibility visibility)
		{
			// generate the field name
			string fieldName = "sField";

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				m.Engine.AddStaticField<DemoStruct>(fieldName, visibility, () => new DemoStruct() { MyInt32 = 42, MyString = "Lorem Ipsum" });
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(field);
			Assert.Equal(typeof(DemoStruct), field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the type
			object obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether the field has the expected value
			var fieldValue = GetStaticFieldValue(classType, fieldName);
			Assert.NotNull(fieldValue);
			Assert.IsType<DemoStruct>(fieldValue);
			Assert.Equal(42, ((DemoStruct)fieldValue).MyInt32);
			Assert.Equal("Lorem Ipsum", ((DemoStruct)fieldValue).MyString);
		}

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField CodeGenEngine.AddStaticField&lt;T&gt;(string name = null, Visibility visibility = Visibility.Private, Func&lt;T&gt; factory)
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="defaultValue">Expected value the field should have after creating the object dynamically.</param>
		[Theory]
		[InlineData(Visibility.Public)]
		[InlineData(Visibility.Protected)]
		[InlineData(Visibility.ProtectedInternal)]
		[InlineData(Visibility.Internal)]
		[InlineData(Visibility.Private)]
		public void AddStaticField_WithFactoryMethod_Class(Visibility visibility)
		{
			// generate the field name
			string fieldName = "sField";

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				m.Engine.AddStaticField<DemoClass>(fieldName, visibility, () => new DemoClass() { MyInt32 = 42, MyString = "Lorem Ipsum" } );
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(field);
			Assert.Equal(typeof(DemoClass), field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the type
			object obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether the field has the expected value
			var fieldValue = GetStaticFieldValue(classType, fieldName);
			Assert.NotNull(fieldValue);
			Assert.IsType<DemoClass>(fieldValue);
			Assert.Equal(42, ((DemoClass)fieldValue).MyInt32);
			Assert.Equal("Lorem Ipsum", ((DemoClass)fieldValue).MyString);
		}

		/// <summary>
		/// Tests the following method:
		/// public GeneratedField CodeGenEngine.AddStaticField<T>(string name, Visibility visibility, FieldInitializer initializer)
		/// </summary>
		/// <param name="visibility">Visibility of the field to add.</param>
		/// <param name="fieldType">Type of the field to add.</param>
		/// <param name="expectedValue">Expected value the field should have after creating the object dynamically.</param>
		/// <param name="initializer">Delegate that emits code to initialize the field.</param>
		[Theory]
		[MemberData(nameof(TestDataGenerator_AddField_WithInitializer))]
		public void AddStaticField_WithInitializer(Visibility visibility, Type fieldType, object expectedValue, FieldInitializer initializer)
		{
			// generate the field name
			string fieldName = "s" + fieldType.Name;

			// setup code generation module
			CallbackCodeGenModule module = new CallbackCodeGenModule();
			module.Declare = (m) =>
			{
				MethodInfo genericMethod = typeof(CodeGenEngine)
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(x => x.Name == "AddStaticField" && x.IsGenericMethodDefinition && x.GetGenericArguments().Length == 1)
					.Where(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string), typeof(Visibility), typeof(FieldInitializer) }))
					.Single();
				MethodInfo method = genericMethod.MakeGenericMethod(fieldType);
				method.Invoke(m.Engine, new object[] { fieldName, visibility, initializer });
			};

			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(module);
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// check whether the field was generated correctly
			var field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(field);
			Assert.Equal(fieldType, field.FieldType);
			Assert.Equal(visibility, field.ToVisibility());

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether fields have a value of the expected type and default type
			var fieldValue = GetStaticFieldValue(classType, fieldName);
			Assert.Equal(expectedValue, fieldValue);
		}

		private static object GetStaticFieldValue(Type type, string name)
		{
			FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			object value = field.GetValue(null);
			return value;
		}

		#endregion

		#region AddEvent: Built-in Implementation (Standard)

		public delegate void SpecialDelegateEventHandler(string s, int x);

		public class SpecializedEventArgs : EventArgs
		{

		}

		public class TestModule_AddEvent_UsingBuiltinImplementation_Standard : CodeGenModule
		{
			protected override void OnDeclare()
			{
				GeneratedEvent event1 = Engine.AddEvent<EventHandler>                      ("NonGeneric",         Visibility.Public, null, EventImplementations.Standard);
				GeneratedEvent event2 = Engine.AddEvent<EventHandler<EventArgs>>           ("GenericBasic",       Visibility.Public, null, EventImplementations.Standard);
				GeneratedEvent event3 = Engine.AddEvent<EventHandler<SpecializedEventArgs>>("GenericSpecialized", Visibility.Public, null, EventImplementations.Standard);
				GeneratedEvent event4 = Engine.AddEvent<SpecialDelegateEventHandler>       ("Special",            Visibility.Public, null, EventImplementations.Standard);

				event1.Raiser.Visibility = Visibility.Public;
				event2.Raiser.Visibility = Visibility.Public;
				event3.Raiser.Visibility = Visibility.Public;
				event4.Raiser.Visibility = Visibility.Public;
			}
		}

		[Fact]
		public void AddEvents_UsingBuiltinImplementation_Standard()
		{
			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(new TestModule_AddEvent_UsingBuiltinImplementation_Standard());
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// event type: System.EventHandler
			EventHandler handler1  = (sender,e) => {
				Assert.Same(obj, sender);
				Assert.Same(EventArgs.Empty, e);
			};
			obj.NonGeneric += handler1;
			obj.OnNonGeneric();
			obj.NonGeneric -= handler1;

			// event type: System.EventHandler<T> with T == System.EventArgs
			EventHandler<EventArgs> handler2  = (sender,e) => {
				Assert.Same(obj, sender);
				Assert.Same(EventArgs.Empty, e);
			};
			obj.GenericBasic += handler2;
			obj.OnGenericBasic();
			obj.GenericBasic -= handler2;

			// event type: System.EventHandler<T> with T derived from System.EventArgs
			SpecializedEventArgs e3 = new SpecializedEventArgs();
			EventHandler<SpecializedEventArgs> handler3  = (sender,e) => {
				Assert.Same(obj, sender);
				Assert.Same(e3, e);
			};
			obj.GenericSpecialized += handler3;
			obj.OnGenericSpecialized(e3);
			obj.GenericSpecialized -= handler3;

			// event type: custom delegate type
			SpecialDelegateEventHandler handler4 = (s,x) => {
				Assert.Equal("test", s);
				Assert.Equal(42, x);
			};
			obj.Special += handler4;
			obj.OnSpecial("test", 42);
			obj.Special -= handler4;
		}

		#endregion

		#region AddStaticEvent: Built-in Implementation (Standard)

		public class TestModule_AddStaticEvent_UsingBuiltinImplementation_Standard : CodeGenModule
		{
			protected override void OnDeclare()
			{
				GeneratedEvent event1 = Engine.AddStaticEvent<EventHandler>                      ("NonGeneric",         Visibility.Public, null, EventImplementations.Standard);
				GeneratedEvent event2 = Engine.AddStaticEvent<EventHandler<EventArgs>>           ("GenericBasic",       Visibility.Public, null, EventImplementations.Standard);
				GeneratedEvent event3 = Engine.AddStaticEvent<EventHandler<SpecializedEventArgs>>("GenericSpecialized", Visibility.Public, null, EventImplementations.Standard);
				GeneratedEvent event4 = Engine.AddStaticEvent<SpecialDelegateEventHandler>       ("Special",            Visibility.Public, null, EventImplementations.Standard);

				event1.Raiser.Visibility = Visibility.Public;
				event2.Raiser.Visibility = Visibility.Public;
				event3.Raiser.Visibility = Visibility.Public;
				event4.Raiser.Visibility = Visibility.Public;
			}
		}

		[Fact]
		public void AddStaticEvent_UsingBuiltinImplementation_Standard()
		{
			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(new TestModule_AddStaticEvent_UsingBuiltinImplementation_Standard());
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// event type: System.EventHandler
			// ----------------------------------------------------------------------------------
			EventHandler handler1  = (sender,e) => {
				Assert.Null(sender);
				Assert.Same(EventArgs.Empty, e);
			};
			EventInfo eventInfo1 = classType.GetEvent("NonGeneric");
			eventInfo1.AddEventHandler(null, handler1);
			MethodInfo eventRaiser1 = classType.GetMethod("OnNonGeneric");
			eventRaiser1.Invoke(null, Type.EmptyTypes);
			eventInfo1.RemoveEventHandler(null, handler1);

			// event type: System.EventHandler<T> with T == System.EventArgs
			// ----------------------------------------------------------------------------------
			EventHandler<EventArgs> handler2  = (sender,e) => {
				Assert.Null(sender);
				Assert.Same(EventArgs.Empty, e);
			};
			EventInfo eventInfo2 = classType.GetEvent("GenericBasic");
			eventInfo2.AddEventHandler(null, handler2);
			MethodInfo eventRaiser2 = classType.GetMethod("OnGenericBasic");
			eventRaiser2.Invoke(null, Type.EmptyTypes);
			eventInfo2.RemoveEventHandler(null, handler2);

			// event type: System.EventHandler<T> with T derived from System.EventArgs
			// ----------------------------------------------------------------------------------
			SpecializedEventArgs e3 = new SpecializedEventArgs();
			EventHandler<SpecializedEventArgs> handler3  = (sender,e) => {
				Assert.Null(sender);
				Assert.Same(e3, e);
			};
			EventInfo eventInfo3 = classType.GetEvent("GenericSpecialized");
			eventInfo3.AddEventHandler(null, handler3);
			MethodInfo eventRaiser3 = classType.GetMethod("OnGenericSpecialized");
			eventRaiser3.Invoke(null, new object[] { e3 });
			eventInfo3.RemoveEventHandler(null, handler3);

			// event type: custom delegate type
			// ----------------------------------------------------------------------------------
			SpecialDelegateEventHandler handler4 = (s,x) => {
				Assert.Equal("test", s);
				Assert.Equal(42, x);
			};
			EventInfo eventInfo4 = classType.GetEvent("Special");
			eventInfo4.AddEventHandler(null, handler4);
			MethodInfo eventRaiser4 = classType.GetMethod("OnSpecial");
			eventRaiser4.Invoke(null, new object[] { "test", 42 });
			eventInfo4.RemoveEventHandler(null, handler4);
		}

		#endregion

		#region GetAbstractProperties

		public abstract class GetAbstractPropertiesClassA
		{
			public abstract int Property1 { get; set; }
			public abstract int Property2 { get; set; }
			public abstract int Property3 { get; protected set; }
		}

		public abstract class GetAbstractPropertiesClassB : GetAbstractPropertiesClassA
		{
			public override int Property1 {
				get { return 0; }
				set { }
			}
		}

		public abstract class GetAbstractPropertiesClassC : GetAbstractPropertiesClassB
		{
			public override int Property2
			{
				get { return 0; }
				set { }
			}
		}

		public class TestModule_GetAbstractProperties : CodeGenModule
		{
			protected override void OnDeclare()
			{
				// expecting "Property3" to be the only abstract property
				InheritedProperty[] properties = Engine.GetAbstractProperties();
				Assert.Single(properties);
				Assert.Equal("Property3", properties[0].Name);
				Assert.Equal(typeof(int), properties[0].Type);
				Assert.Equal(PropertyKind.Abstract, properties[0].Kind);
				Assert.Equal(Visibility.Public, properties[0].GetAccessor.Visibility);
				Assert.Equal(Visibility.Protected, properties[0].SetAccessor.Visibility);

				// add basic implementation to satisfy the type builder
				properties[0].Override(PropertyImplementations.Simple);
			}
		}

		[Fact]
		public void GetAbstractProperties()
		{
			// create the type
			ClassDefinition classDefinition = new ClassDefinition(typeof(GetAbstractPropertiesClassC), false, "MyClass");
			classDefinition.AddModule(new TestModule_GetAbstractProperties());
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(GetAbstractPropertiesClassC), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);
		}

		#endregion

		#region Test Data

		// The method works for all types.
		// The default value of a field of type T should be default(T)
		public static IEnumerable<object[]> TestDataGenerator_AddField_WithoutDefaultValue
		{
			get
			{
				foreach (Visibility visibility in TestData.Visibilities)
				{
					foreach (Type fieldType in TestData.BasicTypes)
					{
						var defaultValue = fieldType.IsValueType ? Activator.CreateInstance(fieldType) : null;
						yield return new [] { visibility, fieldType, defaultValue };
					}
				}
			}
		}

		// The method works for primitive types only.
		// Other types require an initializer.
		public static IEnumerable<object[]> TestDataGenerator_AddField_WithDefaultValue
		{
			get
			{
				foreach (Visibility visibility in TestData.Visibilities)
				{
					foreach (TestDataRecord data in TestData.MixedTestData.Where(x => x.Type.IsPrimitive))
					{
						yield return new [] { visibility, data.Type, data.Value };
					}
				}
			}
		}

		public static IEnumerable<object[]> TestDataGenerator_AddField_WithInitializer
		{
			get
			{
				foreach (Visibility visibility in TestData.Visibilities)
				{
					// a string
					var demoString = "Lorem Ipsum";
					yield return new object[] {
						visibility,
						typeof(string),
						demoString,
						new FieldInitializer((msil, field) =>
						{
							msil.Emit(OpCodes.Ldstr, demoString);
						})
					};

					// a custom struct
					var demoStruct = new DemoStruct() { MyInt32 = 42, MyString = "Lorem Ipsum" };
					yield return new object[] {
						visibility,
						typeof(DemoStruct),
						demoStruct,
						new FieldInitializer((msil, field) =>
						{
							// create new object
							var local = msil.DeclareLocal(typeof(DemoStruct));
							msil.Emit(OpCodes.Ldloca, local);
							msil.Emit(OpCodes.Initobj, typeof(DemoStruct));

							// set MyInt32
							msil.Emit(OpCodes.Ldloca, local);
							msil.Emit(OpCodes.Ldc_I4, demoStruct.MyInt32);
							msil.Emit(OpCodes.Stfld, typeof(DemoStruct).GetField("MyInt32"));

							// set MyString
							msil.Emit(OpCodes.Ldloca, local);
							msil.Emit(OpCodes.Ldstr, demoStruct.MyString);
							msil.Emit(OpCodes.Stfld, typeof(DemoStruct).GetField("MyString"));

							// push object onto the evaluation stack to let the code generation engine push it into the field
							msil.Emit(OpCodes.Ldloc, local);
						})
					};

					// a custom class
					var demoClass = new DemoClass() { MyInt32 = 42, MyString = "Lorem Ipsum" };
					yield return new object[] {
						visibility,
						typeof(DemoClass),
						demoClass,
						new FieldInitializer((msil, field) =>
						{
							// create new object
							var local = msil.DeclareLocal(typeof(DemoClass));
							msil.Emit(OpCodes.Newobj, typeof(DemoClass).GetConstructor(Type.EmptyTypes));
							msil.Emit(OpCodes.Stloc, local);

							// set MyInt32
							msil.Emit(OpCodes.Ldloc, local);
							msil.Emit(OpCodes.Ldc_I4, demoClass.MyInt32);
							msil.Emit(OpCodes.Stfld, typeof(DemoClass).GetField("MyInt32"));

							// set MyString
							msil.Emit(OpCodes.Ldloc, local);
							msil.Emit(OpCodes.Ldstr, demoClass.MyString);
							msil.Emit(OpCodes.Stfld, typeof(DemoClass).GetField("MyString"));

							// push object onto the evaluation stack to let the code generation engine push it into the field
							msil.Emit(OpCodes.Ldloc, local);
						})
					};
				}
			}
		}

		#endregion

	}
}