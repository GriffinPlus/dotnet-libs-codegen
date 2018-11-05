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

using System;
using System.Linq;
using Xunit;

namespace GriffinPlus.Lib.CodeGeneration.Tests
{
	/// <summary>
	/// Tests around the <see cref="ConstructorDefinition"/> class.
	/// </summary>
	public class ConstructorDefinitionTests
	{
		#region Construction

		[Theory]
		[InlineData(Visibility.Public,            new Type[] { typeof(string) })]
		[InlineData(Visibility.Protected,         new Type[] { typeof(string), typeof(sbyte) })]
		[InlineData(Visibility.ProtectedInternal, new Type[] { typeof(string), typeof(sbyte), typeof(short) })]
		[InlineData(Visibility.Internal,          new Type[] { typeof(string), typeof(sbyte), typeof(short), typeof(int) })]
		[InlineData(Visibility.Private,           new Type[] { typeof(string), typeof(sbyte), typeof(short), typeof(int), typeof(long) })]
		public void Create_CallingParameterlessBaseClassConstructor(Visibility visibility, Type[] parameterTypes)
		{
			ConstructorDefinition definition = new ConstructorDefinition(visibility, parameterTypes, null); // null = call parameterless base class constructor
			Assert.Equal(visibility, definition.AccessModifier);
			Assert.Equal(parameterTypes, definition.ParameterTypes);
			Assert.Null(definition.ImplementBaseClassConstructorCallCallback);
		}

		[Theory]
		[InlineData(Visibility.Public,            new Type[] { typeof(string) })]
		[InlineData(Visibility.Protected,         new Type[] { typeof(string), typeof(sbyte) })]
		[InlineData(Visibility.ProtectedInternal, new Type[] { typeof(string), typeof(sbyte), typeof(short) })]
		[InlineData(Visibility.Internal,          new Type[] { typeof(string), typeof(sbyte), typeof(short), typeof(int) })]
		[InlineData(Visibility.Private,           new Type[] { typeof(string), typeof(sbyte), typeof(short), typeof(int), typeof(long) })]
		public void Create_CallingParameterizedBaseClassConstructor(Visibility visibility, Type[] parameterTypes)
		{
			// pass a callback method to call when it comes to implementing the call of a base class constructor
			// (empty implementation, as it is not significant for this test...)
			ImplementBaseClassConstructorCallCallback callback = (constructorDefinition, typeBuilder, msil) => { };
			ConstructorDefinition definition = new ConstructorDefinition(visibility, parameterTypes, callback);
			Assert.Equal(visibility, definition.AccessModifier);
			Assert.Equal(parameterTypes, definition.ParameterTypes);
			Assert.Same(callback, definition.ImplementBaseClassConstructorCallCallback);
		}

		[Fact]
		public void Create_ParameterTypesIsNull()
		{
			var exception = Assert.Throws<ArgumentNullException>(() =>
			{
				new ConstructorDefinition(Visibility.Public, null, null);
			});

			Assert.Equal("parameterTypes", exception.ParamName);
			
		}

		#endregion

		#region Property: Default

		[Fact]
		public void Default()
		{
			ConstructorDefinition definition = ConstructorDefinition.Default;
			Assert.Equal(Visibility.Public, definition.AccessModifier);
			Assert.Equal(Type.EmptyTypes, definition.ParameterTypes);
			Assert.Null(definition.ImplementBaseClassConstructorCallCallback); // null => call parameterless constructor of base class, if any 
		}

		#endregion

		#region Equals / GetHashCode

		[Theory]
		// fully equivalent constructor definitions
		[InlineData(Visibility.Public,            new Type[] { typeof(object) },                 Visibility.Public,            new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.Protected,         new Type[] { typeof(object) },                 Visibility.Protected,         new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.ProtectedInternal, new Type[] { typeof(object) },                 Visibility.ProtectedInternal, new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.Internal,          new Type[] { typeof(object) },                 Visibility.Internal,          new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.Private,           new Type[] { typeof(object) },                 Visibility.Private,           new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.Public,            new Type[] { typeof(object), typeof(string) }, Visibility.Public,            new Type[] { typeof(object), typeof(string) }, true)]
		[InlineData(Visibility.Protected,         new Type[] { typeof(object), typeof(string) }, Visibility.Protected,         new Type[] { typeof(object), typeof(string) }, true)]
		[InlineData(Visibility.ProtectedInternal, new Type[] { typeof(object), typeof(string) }, Visibility.ProtectedInternal, new Type[] { typeof(object), typeof(string) }, true)]
		[InlineData(Visibility.Internal,          new Type[] { typeof(object), typeof(string) }, Visibility.Internal,          new Type[] { typeof(object), typeof(string) }, true)]
		[InlineData(Visibility.Private,           new Type[] { typeof(object), typeof(string) }, Visibility.Private,           new Type[] { typeof(object), typeof(string) }, true)]
		// different visibilities do NOT influence the equality check
		[InlineData(Visibility.Public,            new Type[] { typeof(object) },                 Visibility.Protected,         new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.Protected,         new Type[] { typeof(object) },                 Visibility.ProtectedInternal, new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.ProtectedInternal, new Type[] { typeof(object) },                 Visibility.Internal,          new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.Internal,          new Type[] { typeof(object) },                 Visibility.Private,           new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.Private,           new Type[] { typeof(object) },                 Visibility.Public,            new Type[] { typeof(object) },                 true)]
		[InlineData(Visibility.Public,            new Type[] { typeof(object), typeof(string) }, Visibility.Protected,         new Type[] { typeof(object), typeof(string) }, true)]
		[InlineData(Visibility.Protected,         new Type[] { typeof(object), typeof(string) }, Visibility.ProtectedInternal, new Type[] { typeof(object), typeof(string) }, true)]
		[InlineData(Visibility.ProtectedInternal, new Type[] { typeof(object), typeof(string) }, Visibility.Internal,          new Type[] { typeof(object), typeof(string) }, true)]
		[InlineData(Visibility.Internal,          new Type[] { typeof(object), typeof(string) }, Visibility.Private,           new Type[] { typeof(object), typeof(string) }, true)]
		[InlineData(Visibility.Private,           new Type[] { typeof(object), typeof(string) }, Visibility.Public,            new Type[] { typeof(object), typeof(string) }, true)]
		// different parameters
		[InlineData(Visibility.Public,            new Type[] { typeof(object) },                 Visibility.Public,            new Type[] { typeof(int) },                    false)]
		[InlineData(Visibility.Protected,         new Type[] { typeof(object) },                 Visibility.Protected,         new Type[] { typeof(int) },                    false)]
		[InlineData(Visibility.ProtectedInternal, new Type[] { typeof(object) },                 Visibility.ProtectedInternal, new Type[] { typeof(int) },                    false)]
		[InlineData(Visibility.Internal,          new Type[] { typeof(object) },                 Visibility.Internal,          new Type[] { typeof(int) },                    false)]
		[InlineData(Visibility.Private,           new Type[] { typeof(object) },                 Visibility.Private,           new Type[] { typeof(int) },                    false)]
		[InlineData(Visibility.Public,            new Type[] { typeof(object), typeof(string) }, Visibility.Public,            new Type[] { typeof(int), typeof(string) },    false)]
		[InlineData(Visibility.Protected,         new Type[] { typeof(object), typeof(string) }, Visibility.Protected,         new Type[] { typeof(int), typeof(string) },    false)]
		[InlineData(Visibility.ProtectedInternal, new Type[] { typeof(object), typeof(string) }, Visibility.ProtectedInternal, new Type[] { typeof(int), typeof(string) },    false)]
		[InlineData(Visibility.Internal,          new Type[] { typeof(object), typeof(string) }, Visibility.Internal,          new Type[] { typeof(int), typeof(string) },    false)]
		[InlineData(Visibility.Private,           new Type[] { typeof(object), typeof(string) }, Visibility.Private,           new Type[] { typeof(int), typeof(string) },    false)]

		public void EqualityAndHashCode(Visibility visibility1, Type[] parameters1, Visibility visibility2, Type[] parameters2, bool equal)
		{
			ConstructorDefinition definition1 = new ConstructorDefinition(visibility1, parameters1, null);
			ConstructorDefinition definition2 = new ConstructorDefinition(visibility2, parameters2, null);
			Assert.Equal(equal, definition1.Equals(definition2)); // IEquatable<T>.Equals()
			Assert.Equal(equal, definition2.Equals(definition1)); // IEquatable<T>.Equals()
			Assert.Equal(equal, (definition1 as object).Equals(definition2)); // object.Equals()
			Assert.Equal(equal, (definition2 as object).Equals(definition1)); // object.Equals()
			Assert.Equal(equal, definition1.GetHashCode() == definition2.GetHashCode()); // different definitions CAN have the same hash code, but it is unlikely...
		}

		#endregion
	}
}
