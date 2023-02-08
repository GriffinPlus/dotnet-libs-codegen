///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Base class for property implementation strategies that implement the 'get'/'set' accessor methods.
	/// </summary>
	public abstract class PropertyImplementation : IPropertyImplementation
	{
		/// <summary>
		/// Adds additional fields, events, properties and methods to the type definition.
		/// </summary>
		/// <param name="typeDefinition">The type definition.</param>
		/// <param name="property">The property to implement.</param>
		public virtual void Declare(TypeDefinition typeDefinition, IGeneratedProperty property) { }

		/// <summary>
		/// Implements the 'get' accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the 'get' accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the 'get' accessor method to implement.</param>
		public abstract void ImplementGetAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedProperty property,
			ILGenerator        msilGenerator);

		/// <summary>
		/// Implements the 'set' accessor method of the property.
		/// </summary>
		/// <param name="typeDefinition">Definition of the type in creation.</param>
		/// <param name="property">The property the 'set' accessor method to implement belongs to.</param>
		/// <param name="msilGenerator">MSIL generator attached to the 'set' accessor method to implement.</param>
		public abstract void ImplementSetAccessorMethod(
			TypeDefinition     typeDefinition,
			IGeneratedProperty property,
			ILGenerator        msilGenerator);
	}

}
