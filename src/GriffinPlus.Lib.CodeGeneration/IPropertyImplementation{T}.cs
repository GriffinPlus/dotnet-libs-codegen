///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Typed interface of property implementation strategies that implement the get/set accessor methods.
	/// </summary>
	/// <typeparam name="T">Type of the property to implement.</typeparam>
	public interface IPropertyImplementation<T> : IPropertyImplementation
	{
		/// <summary>
		/// Adds other fields, events, properties and methods to the definition of the type in creation.
		/// </summary>
		/// <param name="typeDefinition">The type definition.</param>
		/// <param name="property">The property to implement.</param>
		void Declare(TypeDefinition typeDefinition, IGeneratedProperty<T> property);

		/// <summary>
		/// Implements the get accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the get accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the get accessor method to implement.</param>
		void ImplementGetAccessorMethod(
			TypeDefinition        typeDefinition,
			IGeneratedProperty<T> property,
			ILGenerator           msilGenerator);

		/// <summary>
		/// Implements the set accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the set accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the set accessor method to implement.</param>
		void ImplementSetAccessorMethod(
			TypeDefinition        typeDefinition,
			IGeneratedProperty<T> property,
			ILGenerator           msilGenerator);
	}

}
