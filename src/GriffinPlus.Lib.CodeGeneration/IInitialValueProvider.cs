///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Interface of members that can provide initial values, e.g. field and dependency properties.
	/// </summary>
	public interface IInitialValueProvider
	{
		/// <summary>
		/// Gets a value indicating whether the <see cref="InitialValue"/> property contains a valid initial value.
		/// </summary>
		bool HasInitialValue { get; }

		/// <summary>
		/// Gets the initial value of the member.
		/// The <see cref="HasInitialValue"/> property determines whether this property contains a valid initial value.
		/// </summary>
		object InitialValue { get; }
	}

}
