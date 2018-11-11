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
using Xunit;

namespace UnitTests
{
	public class TestDataRecord
	{
		public TestDataRecord(Type fieldType, object value)
		{
			Type = fieldType;
			Value = value;
		}

		public Type Type { get; set; }
		public object Value { get; set; }
	}

	/// <summary>
	/// Tests around the <see cref="CodeGenEngine"/> class.
	/// </summary>
	public class TestData
	{
		public static IEnumerable<Type> PrimitiveTypes
		{
			get
			{
				yield return typeof(SByte);
				yield return typeof(Byte);
				yield return typeof(Int16);
				yield return typeof(UInt16);
				yield return typeof(Int32);
				yield return typeof(UInt32);
				yield return typeof(Int64);
				yield return typeof(UInt64);
				yield return typeof(Single);
				yield return typeof(Double);
			}
		}

		public static IEnumerable<Type> BasicTypes
		{
			get
			{
				foreach (var type in PrimitiveTypes) yield return type;
				yield return typeof(Decimal);
				yield return typeof(DateTime);
				yield return typeof(TimeSpan);
				yield return typeof(String);
				yield return typeof(Object);
			}
		}

		public static IEnumerable<TestDataRecord> MixedTestData
		{
			get
			{
				yield return new TestDataRecord(typeof(SByte), SByte.MinValue);
				yield return new TestDataRecord(typeof(SByte), SByte.MaxValue);
				yield return new TestDataRecord(typeof(Byte), Byte.MinValue);
				yield return new TestDataRecord(typeof(Byte), Byte.MaxValue);
				yield return new TestDataRecord(typeof(Int16), Int16.MinValue);
				yield return new TestDataRecord(typeof(Int16), Int16.MaxValue);
				yield return new TestDataRecord(typeof(UInt16), UInt16.MinValue);
				yield return new TestDataRecord(typeof(UInt16), UInt16.MaxValue);
				yield return new TestDataRecord(typeof(Int32), Int32.MinValue);
				yield return new TestDataRecord(typeof(Int32), Int32.MaxValue);
				yield return new TestDataRecord(typeof(UInt32), UInt32.MinValue);
				yield return new TestDataRecord(typeof(UInt32), UInt32.MaxValue);
				yield return new TestDataRecord(typeof(Int64), Int64.MinValue);
				yield return new TestDataRecord(typeof(Int64), Int64.MaxValue);
				yield return new TestDataRecord(typeof(UInt64), UInt64.MinValue);
				yield return new TestDataRecord(typeof(UInt64), UInt64.MaxValue);
				yield return new TestDataRecord(typeof(Single), Single.NegativeInfinity);
				yield return new TestDataRecord(typeof(Single), Single.MinValue);
				yield return new TestDataRecord(typeof(Single), Single.MaxValue);
				yield return new TestDataRecord(typeof(Single), Single.PositiveInfinity);
				yield return new TestDataRecord(typeof(Double), Double.NegativeInfinity);
				yield return new TestDataRecord(typeof(Double), Double.MinValue);
				yield return new TestDataRecord(typeof(Double), Double.MaxValue);
				yield return new TestDataRecord(typeof(Double), Double.PositiveInfinity);
				yield return new TestDataRecord(typeof(Decimal), Decimal.MinValue);
				yield return new TestDataRecord(typeof(Decimal), Decimal.Zero);
				yield return new TestDataRecord(typeof(Decimal), Decimal.MaxValue);
				yield return new TestDataRecord(typeof(DateTime), DateTime.MinValue);
				yield return new TestDataRecord(typeof(DateTime), DateTime.Now);
				yield return new TestDataRecord(typeof(DateTime), DateTime.MaxValue);
				yield return new TestDataRecord(typeof(TimeSpan), TimeSpan.MinValue);
				yield return new TestDataRecord(typeof(TimeSpan), TimeSpan.Zero);
				yield return new TestDataRecord(typeof(TimeSpan), TimeSpan.MaxValue);
				yield return new TestDataRecord(typeof(String), null);
				yield return new TestDataRecord(typeof(String), "");
				yield return new TestDataRecord(typeof(String), "Lorem Ipsum");
				yield return new TestDataRecord(typeof(Object), null);
				yield return new TestDataRecord(typeof(Object), new Object());
			}
		}

		public static IEnumerable<Visibility> Visibilities
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
	}
}
