///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	public enum TestEnumS8 : sbyte
	{
		Min = sbyte.MinValue,
		Max = sbyte.MaxValue
	}

	public enum TestEnumU8 : byte
	{
		Min = byte.MinValue,
		Max = byte.MaxValue
	}

	public enum TestEnumS16 : short
	{
		Min = short.MinValue,
		Max = short.MaxValue
	}

	public enum TestEnumU16 : ushort
	{
		Min = ushort.MinValue,
		Max = ushort.MaxValue
	}

	public enum TestEnumS32
	{
		Min = int.MinValue,
		Max = int.MaxValue
	}

	public enum TestEnumU32 : uint
	{
		Min = uint.MinValue,
		Max = uint.MaxValue
	}

	public enum TestEnumS64 : long
	{
		Min = long.MinValue,
		Max = long.MaxValue
	}

	public enum TestEnumU64 : ulong
	{
		Min = ulong.MinValue,
		Max = ulong.MaxValue
	}

}
