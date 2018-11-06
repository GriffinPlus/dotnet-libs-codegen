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
using System.Linq;
using Xunit;

namespace UnitTests
{
	/// <summary>
	/// Tests around the <see cref="ClassDefinition"/> class.
	/// </summary>
	public class ClassDefinitionTests
	{
		#region Create

		public class Create_BaseClass_MixedConstructors
		{
			public Create_BaseClass_MixedConstructors()
			{
			}

			public Create_BaseClass_MixedConstructors(int x)
			{
			}
		}

		[Fact]
		public void Create_DynamicallyGeneratedName_WithoutBaseClass()
		{
			ClassDefinition definition = new ClassDefinition(null); // null = create class name dynamically
			Assert.Null(definition.TypeName); // dynamically created name
			Assert.Null(definition.BaseClassType); // no base class
			Assert.False(definition.GeneratePassThroughConstructors); // do not generate pass-through-contructors
		}

		[Theory]
		[InlineData(false)] // no pass-through constructors, dynamically created type name
		[InlineData(true)]  // pass-through constructors, dynamically created type name
		public void Create_DynamicallyGeneratedName_WithBaseClass(bool generatePassThroughConstructors)
		{
			Type baseClass = typeof(Create_BaseClass_MixedConstructors);
			ClassDefinition definition = new ClassDefinition(baseClass, generatePassThroughConstructors, null);
			Assert.Null(definition.TypeName);
			Assert.Equal(baseClass, definition.BaseClassType);
			Assert.Equal(generatePassThroughConstructors, definition.GeneratePassThroughConstructors);
		}

		[Theory]
		[InlineData(false, "MyType")] // no pass-through constructors, dynamically created type name
		[InlineData(true, "MyType")]  // pass-through constructors, dynamically created type name
		public void Create_CustomTypeName_WithBaseClass(bool generatePassThroughConstructors, string typeName)
		{
			Type baseClass = typeof(Create_BaseClass_MixedConstructors);
			ClassDefinition definition = new ClassDefinition(baseClass, generatePassThroughConstructors, typeName);
			Assert.Equal(typeName, definition.TypeName);
			Assert.Equal(baseClass, definition.BaseClassType);
			Assert.Equal(generatePassThroughConstructors, definition.GeneratePassThroughConstructors);
		}

		[Theory]
		[InlineData(false, null)]        // no pass-through constructors, dynamically created type name
		[InlineData(true, null)]         // pass-through constructors, dynamically created type name
		[InlineData(false, "MyType")]    // no pass-through constructors, custom type name
		[InlineData(true, "MyType")]     // pass-through constructors, custom type name
		public void Create_BaseClassIsNull(bool generatePassThroughConstructors, string typeName)
		{
			var exception = Assert.Throws<ArgumentNullException>(() =>
			{
				Type baseClass = null;
				ClassDefinition definition = new ClassDefinition(baseClass, generatePassThroughConstructors, typeName);
			});

			Assert.Equal("baseClass", exception.ParamName);
		}

		#endregion

		#region AddConstructorDefinition

		[Theory]
		[InlineData(Visibility.Public,            new Type[] { typeof(string) })]
		[InlineData(Visibility.Protected,         new Type[] { typeof(string), typeof(sbyte) })]
		[InlineData(Visibility.ProtectedInternal, new Type[] { typeof(string), typeof(sbyte), typeof(short) })]
		[InlineData(Visibility.Internal,          new Type[] { typeof(string), typeof(sbyte), typeof(short), typeof(int) })]
		[InlineData(Visibility.Private,           new Type[] { typeof(string), typeof(sbyte), typeof(short), typeof(int), typeof(long) })]
		public void AddConstructorDefinition_CallingParameterlessBaseClassConstructor(Visibility visibility, Type[] parameterTypes)
		{
			ClassDefinition definition = new ClassDefinition(); // no base class, dynamic type name
			ConstructorDefinition constructor = new ConstructorDefinition(visibility, parameterTypes, null); // null => call parameterless base class constructor
			definition.AddConstructorDefinition(constructor);
			Assert.Single(definition.Constructors);
			Assert.Same(constructor, definition.Constructors.First());
		}

		[Fact]
		public void AddConstructorDefinition_ConstructorDefinitionIsNull()
		{
			ClassDefinition definition = new ClassDefinition();

			var exception = Assert.Throws<ArgumentNullException>(() =>
			{
				definition.AddConstructorDefinition(null);
			});

			Assert.Equal("definition", exception.ParamName);
		}

		#endregion

		#region ResolveModuleDependencies

		class TestModule : CodeGenModule
		{
			public TestModule(string id, params ICodeGenModule[] dependencies)
			{
				Id = id;
				Dependencies = dependencies;
			}

			public string Id {
				get;
				set;
			}

			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				TestModule other = obj as TestModule;
				if (other == null) return false;
				return this.Id == other.Id;
			}

			public override string ToString()
			{
				return Id;
			}
		}

		/// <summary>
		/// Tests whether sorting modules by their dependency works properly (sequential dependencies only).
		/// </summary>
		[Fact]
		public void ResolveModuleDependencies_Sequential()
		{
			// Root <- A0 <- B0 <- C0 <- D0

			TestModule d0 = new TestModule("D0");
			TestModule c0 = new TestModule("C0", d0);
			TestModule b0 = new TestModule("B0", c0);
			TestModule a0 = new TestModule("A0", b0);
			TestModule root = new TestModule("Root", a0);

			ICodeGenModule[] sorted = ClassDefinition.ResolveModuleDependencies(new TestModule[] { root });

			Assert.Equal(5, sorted.Length);
			Assert.Equal("D0", ((TestModule)sorted[0]).Id);
			Assert.Equal("C0", ((TestModule)sorted[1]).Id);
			Assert.Equal("B0", ((TestModule)sorted[2]).Id);
			Assert.Equal("A0", ((TestModule)sorted[3]).Id);
			Assert.Equal("Root", ((TestModule)sorted[4]).Id);
		}

		/// <summary>
		/// Tests whether sorting modules by their dependency works properly (dependency tree).
		/// </summary>
		[Fact]
		public void ResolveModuleDependencies_Tree()
		{
			// Root <- A0 <- B0 <- C0
			//            <- B1 <- C1
			//            <- B2

			TestModule c0 = new TestModule("C0");
			TestModule c1 = new TestModule("C1");
			TestModule b0 = new TestModule("B0", c0);
			TestModule b1 = new TestModule("B1", c1);
			TestModule b2 = new TestModule("B2");
			TestModule a0 = new TestModule("A0", b0, b1, b2);
			TestModule root = new TestModule("Root", a0);

			ICodeGenModule[] sorted = ClassDefinition.ResolveModuleDependencies(new TestModule[] { root });

			Assert.Equal(7, sorted.Length);
			Assert.Equal("C0", ((TestModule)sorted[0]).Id);
			Assert.Equal("B0", ((TestModule)sorted[1]).Id);
			Assert.Equal("C1", ((TestModule)sorted[2]).Id);
			Assert.Equal("B1", ((TestModule)sorted[3]).Id);
			Assert.Equal("B2", ((TestModule)sorted[4]).Id);
			Assert.Equal("A0", ((TestModule)sorted[5]).Id);
			Assert.Equal("Root", ((TestModule)sorted[6]).Id);
		}

		/// <summary>
		/// Tests whether sorting modules by their dependency works properly
		/// (expecting exception, if a circular dependency is detected).
		/// </summary>
		[Fact]
		public void ResolveModuleDependencies_CircularDependency()
		{
			// Root <- A0 <-+
			//          ^   |
			//          +---+
			Assert.Throws<CodeGenException>(() =>
			{
				TestModule a0 = new TestModule("A0", new ICodeGenModule[1]);
				a0.Dependencies[0] = a0;
				TestModule root = new TestModule("root", a0);

				ClassDefinition.ResolveModuleDependencies(new TestModule[] { root });
			});
		}

		#endregion
	}
}
