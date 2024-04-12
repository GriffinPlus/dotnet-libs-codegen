///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Typed interface of a generated field.
/// </summary>
/// <typeparam name="T">Type of the field.</typeparam>
public interface IGeneratedField<out T> : IGeneratedField
{
	/// <summary>
	/// Gets the initial value of the field.
	/// The <see cref="IInitialValueProvider.HasInitialValue"/> property determines whether this property contains a valid initial value.
	/// </summary>
	new T InitialValue { get; }
}
