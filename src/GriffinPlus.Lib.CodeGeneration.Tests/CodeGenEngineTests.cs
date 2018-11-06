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
using System.Reflection;
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

		[Fact]
		public void GenerateEmptyClass()
		{
			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);
		}

		[Fact]
		public void GenerateEmptyClass_DerivedFromClassWithParameterlessConstructor()
		{
			// create the type
			ClassDefinition classDefinition = new ClassDefinition(typeof(ClassWithParameterlessConstructor), false, "MyClass");
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(ClassWithParameterlessConstructor), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);
		}

		[Fact]
		public void GenerateEmptyClass_DerivedFromClassWithoutParameterlessConstructor()
		{
			Assert.Throws<CodeGenException>(() =>
			{
				// should throw an exception, because the base class does not have a parameterless constructor
				ClassDefinition classDefinition = new ClassDefinition(typeof(ClassWithoutParameterlessConstructor), false, "MyClass");
				CodeGenEngine.CreateClass(classDefinition);
			});
		}

		#endregion

		#region AddField: Primitive Fields

		public class TestModule_AddPrimitiveFields_WithoutDefaultValue : CodeGenModule
		{
			protected override void OnDeclare()
			{
				Engine.AddField<SByte>  ("mSByte",  Visibility.Public);
				Engine.AddField<Byte>   ("mByte",   Visibility.Public);
				Engine.AddField<Int16>  ("mInt16",  Visibility.Public);
				Engine.AddField<UInt16> ("mUInt16", Visibility.Public);
				Engine.AddField<Int32>  ("mInt32",  Visibility.Public);
				Engine.AddField<UInt32> ("mUInt32", Visibility.Public);
				Engine.AddField<Int64>  ("mInt64",  Visibility.Public);
				Engine.AddField<UInt64> ("mUInt64", Visibility.Public);
				Engine.AddField<Single> ("mSingle", Visibility.Public);
				Engine.AddField<Double> ("mDouble", Visibility.Public);
			}
		}

		[Fact]
		public void AddPrimitiveFields_WithoutDefaultValue()
		{
			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(new TestModule_AddPrimitiveFields_WithoutDefaultValue());
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether fields have a value of the expected type
			Assert.IsType<SByte>(obj.mSByte);
			Assert.IsType<Byte>(obj.mByte);
			Assert.IsType<Int16>(obj.mInt16);
			Assert.IsType<UInt16>(obj.mUInt16);
			Assert.IsType<Int32>(obj.mInt32);
			Assert.IsType<UInt32>(obj.mUInt32);
			Assert.IsType<Int64>(obj.mInt64);
			Assert.IsType<UInt64>(obj.mUInt64);
			Assert.IsType<Single>(obj.mSingle);
			Assert.IsType<Double>(obj.mDouble);

			// check whether fields have the expected default value
			Assert.Equal(default(SByte),  obj.mSByte);
			Assert.Equal(default(Byte),   obj.mByte);
			Assert.Equal(default(Int16),  obj.mInt16);
			Assert.Equal(default(UInt16), obj.mUInt16);
			Assert.Equal(default(Int32),  obj.mInt32);
			Assert.Equal(default(UInt32), obj.mUInt32);
			Assert.Equal(default(Int64),  obj.mInt64);
			Assert.Equal(default(UInt64), obj.mUInt64);
			Assert.Equal(default(Single), obj.mSingle);
			Assert.Equal(default(Double), obj.mDouble);
		}

		public class TestModule_AddPrimitiveFields_WithDefaultValue : CodeGenModule
		{
			protected override void OnDeclare()
			{
				Engine.AddField<SByte>  ("mMin_SByte",   Visibility.Public, SByte.MinValue);
				Engine.AddField<SByte>  ("mMax_SByte",   Visibility.Public, SByte.MaxValue);
				Engine.AddField<Byte>   ("mMin_Byte",    Visibility.Public, Byte.MinValue);
				Engine.AddField<Byte>   ("mMax_Byte",    Visibility.Public, Byte.MaxValue);
				Engine.AddField<Int16>  ("mMin_Int16",   Visibility.Public, Int16.MinValue);
				Engine.AddField<Int16>  ("mMax_Int16",   Visibility.Public, Int16.MaxValue);
				Engine.AddField<UInt16> ("mMin_UInt16",  Visibility.Public, UInt16.MinValue);
				Engine.AddField<UInt16> ("mMax_UInt16",  Visibility.Public, UInt16.MaxValue);
				Engine.AddField<Int32>  ("mMin_Int32",   Visibility.Public, Int32.MinValue);
				Engine.AddField<Int32>  ("mMax_Int32",   Visibility.Public, Int32.MaxValue);
				Engine.AddField<UInt32> ("mMin_UInt32",  Visibility.Public, UInt32.MinValue);
				Engine.AddField<UInt32> ("mMax_UInt32",  Visibility.Public, UInt32.MaxValue);
				Engine.AddField<Int64>  ("mMin_Int64",   Visibility.Public, Int64.MinValue);
				Engine.AddField<Int64>  ("mMax_Int64",   Visibility.Public, Int64.MaxValue);
				Engine.AddField<UInt64> ("mMin_UInt64",  Visibility.Public, UInt64.MinValue);
				Engine.AddField<UInt64> ("mMax_UInt64",  Visibility.Public, UInt64.MaxValue);
				Engine.AddField<Single> ("mMin_Single",  Visibility.Public, Single.MinValue);
				Engine.AddField<Single> ("mMax_Single",  Visibility.Public, Single.MaxValue);
				Engine.AddField<Double> ("mMin_Double",  Visibility.Public, Double.MinValue);
				Engine.AddField<Double> ("mMax_Double",  Visibility.Public, Double.MaxValue);
			}
		}

		[Fact]
		public void AddPrimitiveFields_WithDefaultValue()
		{
			// create the class
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(new TestModule_AddPrimitiveFields_WithDefaultValue());
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// instantiate the class
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether fields with default values contain a value of the expected type
			Assert.IsType<SByte>(obj.mMin_SByte);
			Assert.IsType<SByte>(obj.mMax_SByte);
			Assert.IsType<Byte>(obj.mMin_Byte);
			Assert.IsType<Byte>(obj.mMax_Byte);
			Assert.IsType<Int16>(obj.mMin_Int16);
			Assert.IsType<Int16>(obj.mMax_Int16);
			Assert.IsType<UInt16>(obj.mMin_UInt16);
			Assert.IsType<UInt16>(obj.mMax_UInt16);
			Assert.IsType<Int32>(obj.mMin_Int32);
			Assert.IsType<Int32>(obj.mMax_Int32);
			Assert.IsType<UInt32>(obj.mMin_UInt32);
			Assert.IsType<UInt32>(obj.mMax_UInt32);
			Assert.IsType<Int64>(obj.mMin_Int64);
			Assert.IsType<Int64>(obj.mMax_Int64);
			Assert.IsType<UInt64>(obj.mMin_UInt64);
			Assert.IsType<UInt64>(obj.mMax_UInt64);
			Assert.IsType<Single>(obj.mMin_Single);
			Assert.IsType<Single>(obj.mMax_Single);
			Assert.IsType<Double>(obj.mMin_Double);
			Assert.IsType<Double>(obj.mMax_Double);

			// check whether fields with default values contain the expected value
			Assert.Equal(SByte.MinValue,  obj.mMin_SByte);
			Assert.Equal(SByte.MaxValue,  obj.mMax_SByte);
			Assert.Equal(Byte.MinValue,   obj.mMin_Byte);
			Assert.Equal(Byte.MaxValue,   obj.mMax_Byte);
			Assert.Equal(Int16.MinValue,  obj.mMin_Int16);
			Assert.Equal(Int16.MaxValue,  obj.mMax_Int16);
			Assert.Equal(UInt16.MinValue, obj.mMin_UInt16);
			Assert.Equal(UInt16.MaxValue, obj.mMax_UInt16);
			Assert.Equal(Int32.MinValue,  obj.mMin_Int32);
			Assert.Equal(Int32.MaxValue,  obj.mMax_Int32);
			Assert.Equal(UInt32.MinValue, obj.mMin_UInt32);
			Assert.Equal(UInt32.MaxValue, obj.mMax_UInt32);
			Assert.Equal(Int64.MinValue,  obj.mMin_Int64);
			Assert.Equal(Int64.MaxValue,  obj.mMax_Int64);
			Assert.Equal(UInt64.MinValue, obj.mMin_UInt64);
			Assert.Equal(UInt64.MaxValue, obj.mMax_UInt64);
			Assert.Equal(Single.MinValue, obj.mMin_Single);
			Assert.Equal(Single.MaxValue, obj.mMax_Single);
			Assert.Equal(Double.MinValue, obj.mMin_Double);
			Assert.Equal(Double.MaxValue, obj.mMax_Double);
		}

		#endregion

		#region AddStaticField: Primitive Fields

		public class TestModule_AddStaticPrimitiveFields_WithoutDefaultValue : CodeGenModule
		{
			protected override void OnDeclare()
			{
				Engine.AddStaticField<SByte>  ("sSByte",  Visibility.Public);
				Engine.AddStaticField<Byte>   ("sByte",   Visibility.Public);
				Engine.AddStaticField<Int16>  ("sInt16",  Visibility.Public);
				Engine.AddStaticField<UInt16> ("sUInt16", Visibility.Public);
				Engine.AddStaticField<Int32>  ("sInt32",  Visibility.Public);
				Engine.AddStaticField<UInt32> ("sUInt32", Visibility.Public);
				Engine.AddStaticField<Int64>  ("sInt64",  Visibility.Public);
				Engine.AddStaticField<UInt64> ("sUInt64", Visibility.Public);
				Engine.AddStaticField<Single> ("sSingle", Visibility.Public);
				Engine.AddStaticField<Double> ("sDouble", Visibility.Public);
			}
		}

		private static object GetStaticFieldValue(Type type, string name)
		{
			FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
			object obj = field.GetValue(null);
			return obj;
		}

		[Fact]
		public void AddStaticPrimitiveFields_WithoutDefaultValue()
		{
			// create the type
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(new TestModule_AddStaticPrimitiveFields_WithoutDefaultValue());
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// instantiate the type
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether fields have a value of the expected type
			Assert.IsType<SByte>(GetStaticFieldValue(classType, "sSByte"));
			Assert.IsType<Byte>(GetStaticFieldValue(classType, "sByte"));
			Assert.IsType<Int16>(GetStaticFieldValue(classType, "sInt16"));
			Assert.IsType<UInt16>(GetStaticFieldValue(classType, "sUInt16"));
			Assert.IsType<Int32>(GetStaticFieldValue(classType, "sInt32"));
			Assert.IsType<UInt32>(GetStaticFieldValue(classType, "sUInt32"));
			Assert.IsType<Int64>(GetStaticFieldValue(classType, "sInt64"));
			Assert.IsType<UInt64>(GetStaticFieldValue(classType, "sUInt64"));
			Assert.IsType<Single>(GetStaticFieldValue(classType, "sSingle"));
			Assert.IsType<Double>(GetStaticFieldValue(classType, "sDouble"));

			// check whether fields have the expected default value
			Assert.Equal(default(SByte),  GetStaticFieldValue(classType, "sSByte"));
			Assert.Equal(default(Byte),   GetStaticFieldValue(classType, "sByte"));
			Assert.Equal(default(Int16),  GetStaticFieldValue(classType, "sInt16")); 
			Assert.Equal(default(UInt16), GetStaticFieldValue(classType, "sUInt16"));
			Assert.Equal(default(Int32),  GetStaticFieldValue(classType, "sInt32"));
			Assert.Equal(default(UInt32), GetStaticFieldValue(classType, "sUInt32"));
			Assert.Equal(default(Int64),  GetStaticFieldValue(classType, "sInt64"));
			Assert.Equal(default(UInt64), GetStaticFieldValue(classType, "sUInt64"));
			Assert.Equal(default(Single), GetStaticFieldValue(classType, "sSingle"));
			Assert.Equal(default(Double), GetStaticFieldValue(classType, "sDouble"));
		}

		public class TestModule_AddStaticPrimitiveFields_WithDefaultValue : CodeGenModule
		{
			protected override void OnDeclare()
			{
				Engine.AddStaticField<SByte>  ("sMin_SByte",   Visibility.Public, SByte.MinValue);
				Engine.AddStaticField<SByte>  ("sMax_SByte",   Visibility.Public, SByte.MaxValue);
				Engine.AddStaticField<Byte>   ("sMin_Byte",    Visibility.Public, Byte.MinValue);
				Engine.AddStaticField<Byte>   ("sMax_Byte",    Visibility.Public, Byte.MaxValue);
				Engine.AddStaticField<Int16>  ("sMin_Int16",   Visibility.Public, Int16.MinValue);
				Engine.AddStaticField<Int16>  ("sMax_Int16",   Visibility.Public, Int16.MaxValue);
				Engine.AddStaticField<UInt16> ("sMin_UInt16",  Visibility.Public, UInt16.MinValue);
				Engine.AddStaticField<UInt16> ("sMax_UInt16",  Visibility.Public, UInt16.MaxValue);
				Engine.AddStaticField<Int32>  ("sMin_Int32",   Visibility.Public, Int32.MinValue);
				Engine.AddStaticField<Int32>  ("sMax_Int32",   Visibility.Public, Int32.MaxValue);
				Engine.AddStaticField<UInt32> ("sMin_UInt32",  Visibility.Public, UInt32.MinValue);
				Engine.AddStaticField<UInt32> ("sMax_UInt32",  Visibility.Public, UInt32.MaxValue);
				Engine.AddStaticField<Int64>  ("sMin_Int64",   Visibility.Public, Int64.MinValue);
				Engine.AddStaticField<Int64>  ("sMax_Int64",   Visibility.Public, Int64.MaxValue);
				Engine.AddStaticField<UInt64> ("sMin_UInt64",  Visibility.Public, UInt64.MinValue);
				Engine.AddStaticField<UInt64> ("sMax_UInt64",  Visibility.Public, UInt64.MaxValue);
				Engine.AddStaticField<Single> ("sMin_Single",  Visibility.Public, Single.MinValue);
				Engine.AddStaticField<Single> ("sMax_Single",  Visibility.Public, Single.MaxValue);
				Engine.AddStaticField<Double> ("sMin_Double",  Visibility.Public, Double.MinValue);
				Engine.AddStaticField<Double> ("sMax_Double",  Visibility.Public, Double.MaxValue);
			}
		}

		[Fact]
		public void AddStaticPrimitiveFields_WithDefaultValue()
		{
			// create the class
			ClassDefinition classDefinition = new ClassDefinition("MyClass");
			classDefinition.AddModule(new TestModule_AddStaticPrimitiveFields_WithDefaultValue());
			Type classType = CodeGenEngine.CreateClass(classDefinition);
			Assert.NotNull(classType);
			Assert.Equal(typeof(object), classType.BaseType);
			Assert.Equal("MyClass", classType.Name);

			// instantiate the class
			dynamic obj = Activator.CreateInstance(classType);
			Assert.NotNull(obj);

			// check whether fields with default values contain a value of the expected type
			Assert.IsType<SByte>(GetStaticFieldValue(classType, "sMin_SByte"));
			Assert.IsType<SByte>(GetStaticFieldValue(classType, "sMax_SByte"));
			Assert.IsType<Byte>(GetStaticFieldValue(classType, "sMin_Byte"));
			Assert.IsType<Byte>(GetStaticFieldValue(classType, "sMax_Byte"));
			Assert.IsType<Int16>(GetStaticFieldValue(classType, "sMin_Int16"));
			Assert.IsType<Int16>(GetStaticFieldValue(classType, "sMax_Int16"));
			Assert.IsType<UInt16>(GetStaticFieldValue(classType, "sMin_UInt16"));
			Assert.IsType<UInt16>(GetStaticFieldValue(classType, "sMax_UInt16"));
			Assert.IsType<Int32>(GetStaticFieldValue(classType, "sMin_Int32"));
			Assert.IsType<Int32>(GetStaticFieldValue(classType, "sMax_Int32"));
			Assert.IsType<UInt32>(GetStaticFieldValue(classType, "sMin_UInt32"));
			Assert.IsType<UInt32>(GetStaticFieldValue(classType, "sMax_UInt32"));
			Assert.IsType<Int64>(GetStaticFieldValue(classType, "sMin_Int64"));
			Assert.IsType<Int64>(GetStaticFieldValue(classType, "sMax_Int64"));
			Assert.IsType<UInt64>(GetStaticFieldValue(classType, "sMin_UInt64"));
			Assert.IsType<UInt64>(GetStaticFieldValue(classType, "sMax_UInt64"));
			Assert.IsType<Single>(GetStaticFieldValue(classType, "sMin_Single"));
			Assert.IsType<Single>(GetStaticFieldValue(classType, "sMax_Single"));
			Assert.IsType<Double>(GetStaticFieldValue(classType, "sMin_Double"));
			Assert.IsType<Double>(GetStaticFieldValue(classType, "sMax_Double"));

			// check whether fields with default values contain the expected value
			Assert.Equal(SByte.MinValue,  GetStaticFieldValue(classType, "sMin_SByte"));
			Assert.Equal(SByte.MaxValue,  GetStaticFieldValue(classType, "sMax_SByte"));
			Assert.Equal(Byte.MinValue,   GetStaticFieldValue(classType, "sMin_Byte"));
			Assert.Equal(Byte.MaxValue,   GetStaticFieldValue(classType, "sMax_Byte"));
			Assert.Equal(Int16.MinValue,  GetStaticFieldValue(classType, "sMin_Int16"));
			Assert.Equal(Int16.MaxValue,  GetStaticFieldValue(classType, "sMax_Int16"));
			Assert.Equal(UInt16.MinValue, GetStaticFieldValue(classType, "sMin_UInt16"));
			Assert.Equal(UInt16.MaxValue, GetStaticFieldValue(classType, "sMax_UInt16"));
			Assert.Equal(Int32.MinValue,  GetStaticFieldValue(classType, "sMin_Int32"));
			Assert.Equal(Int32.MaxValue,  GetStaticFieldValue(classType, "sMax_Int32"));
			Assert.Equal(UInt32.MinValue, GetStaticFieldValue(classType, "sMin_UInt32"));
			Assert.Equal(UInt32.MaxValue, GetStaticFieldValue(classType, "sMax_UInt32"));
			Assert.Equal(Int64.MinValue,  GetStaticFieldValue(classType, "sMin_Int64"));
			Assert.Equal(Int64.MaxValue,  GetStaticFieldValue(classType, "sMax_Int64"));
			Assert.Equal(UInt64.MinValue, GetStaticFieldValue(classType, "sMin_UInt64"));
			Assert.Equal(UInt64.MaxValue, GetStaticFieldValue(classType, "sMax_UInt64"));
			Assert.Equal(Single.MinValue, GetStaticFieldValue(classType, "sMin_Single"));
			Assert.Equal(Single.MaxValue, GetStaticFieldValue(classType, "sMax_Single"));
			Assert.Equal(Double.MinValue, GetStaticFieldValue(classType, "sMin_Double"));
			Assert.Equal(Double.MaxValue, GetStaticFieldValue(classType, "sMax_Double"));
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

	}
}