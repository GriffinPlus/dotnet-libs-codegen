///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Reflection;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// An inherited property.
	/// </summary>
	/// <typeparam name="T">Type of the field.</typeparam>
	[DebuggerDisplay("Declaring Type = {PropertyInfo.DeclaringType.FullName}, Property Info = {PropertyInfo}")]
	class InheritedProperty<T> : Member, IInheritedProperty<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InheritedProperty{T}"/> class.
		/// </summary>
		/// <param name="typeDefinition">The type definition the member belongs to.</param>
		/// <param name="property">Property the type in creation has inherited.</param>
		internal InheritedProperty(TypeDefinition typeDefinition, PropertyInfo property) :
			base(typeDefinition)
		{
			PropertyInfo = property;

			// init get accessor method
			MethodInfo getAccessor = PropertyInfo.GetGetMethod(true);
			if (getAccessor != null) GetAccessor = new InheritedMethod(typeDefinition, getAccessor);

			// init set accessor method
			MethodInfo setAccessor = PropertyInfo.GetSetMethod(true);
			if (setAccessor != null) SetAccessor = new InheritedMethod(typeDefinition, setAccessor);
		}

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string Name => PropertyInfo.Name;

		/// <summary>
		/// Gets the type of the property.
		/// </summary>
		public Type PropertyType => PropertyInfo.PropertyType;

		/// <summary>
		/// Gets the property kind indicating whether the property is static, virtual, abstract or an override of an abstract/virtual property.
		/// </summary>
		public PropertyKind Kind => PropertyInfo.ToPropertyKind();

		/// <summary>
		/// Gets the 'get' accessor method
		/// (<c>null</c> if the property does not have a 'get' accessor).
		/// </summary>
		public IInheritedMethod GetAccessor { get; }

		/// <summary>
		/// Gets the 'get' accessor method
		/// (<c>null</c> if the property does not have a 'get' accessor).
		/// </summary>
		IMethod IProperty.GetAccessor => GetAccessor;

		/// <summary>
		/// Gets the 'set' accessor method
		/// (<c>null</c> if the property does not have a 'set' accessor).
		/// </summary>
		public IInheritedMethod SetAccessor { get; }

		/// <summary>
		/// Gets the 'set' accessor method
		/// (<c>null</c> if the property does not have a 'set' accessor).
		/// </summary>
		IMethod IProperty.SetAccessor => SetAccessor;

		/// <summary>
		/// Gets the <see cref="System.Reflection.PropertyInfo"/> associated with the property.
		/// </summary>
		public PropertyInfo PropertyInfo { get; }

		/// <summary>
		/// Adds an override for the current property.
		/// </summary>
		/// <param name="implementation">Implementation strategy that implements the get/set accessor methods of the property.</param>
		/// <returns>The generated property.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
		public IGeneratedProperty<T> Override(IPropertyImplementation implementation)
		{
			return TypeDefinition.AddPropertyOverride(this, implementation);
		}

		/// <summary>
		/// Adds an override for the current property.
		/// </summary>
		/// <param name="implementation">Implementation strategy that implements the get/set accessor methods of the property.</param>
		/// <returns>The generated property.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <c>null</c>.</exception>
		IGeneratedProperty IInheritedProperty.Override(IPropertyImplementation implementation)
		{
			return TypeDefinition.AddPropertyOverride(this, implementation);
		}

		/// <summary>
		/// Adds an override for the current property.
		/// </summary>
		/// <param name="getAccessorImplementationCallback">A callback that implements the get accessor method of the property.</param>
		/// <param name="setAccessorImplementationCallback">A callback that implements the set accessor method of the property.</param>
		/// <returns>The generated property.</returns>
		/// <exception cref="ArgumentNullException">
		/// The property has a get accessor, but <paramref name="getAccessorImplementationCallback"/> is <c>null</c>
		/// -or-
		/// The property has a set accessor, but <paramref name="setAccessorImplementationCallback"/> is <c>null</c>
		/// </exception>
		public IGeneratedProperty<T> Override(
			PropertyAccessorImplementationCallback getAccessorImplementationCallback,
			PropertyAccessorImplementationCallback setAccessorImplementationCallback)
		{
			return TypeDefinition.AddPropertyOverride(
				this,
				getAccessorImplementationCallback,
				setAccessorImplementationCallback);
		}

		/// <summary>
		/// Adds an override for the current property.
		/// </summary>
		/// <param name="getAccessorImplementationCallback">A callback that implements the get accessor method of the property.</param>
		/// <param name="setAccessorImplementationCallback">A callback that implements the set accessor method of the property.</param>
		/// <returns>The generated property.</returns>
		/// <exception cref="ArgumentNullException">
		/// The property has a get accessor, but <paramref name="getAccessorImplementationCallback"/> is <c>null</c>
		/// -or-
		/// The property has a set accessor, but <paramref name="setAccessorImplementationCallback"/> is <c>null</c>
		/// </exception>
		IGeneratedProperty IInheritedProperty.Override(
			PropertyAccessorImplementationCallback getAccessorImplementationCallback,
			PropertyAccessorImplementationCallback setAccessorImplementationCallback)
		{
			return TypeDefinition.AddPropertyOverride(
				this,
				getAccessorImplementationCallback,
				setAccessorImplementationCallback);
		}
	}

}
