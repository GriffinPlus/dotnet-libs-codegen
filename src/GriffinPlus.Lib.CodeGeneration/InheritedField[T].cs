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
	/// An inherited field.
	/// </summary>
	/// <typeparam name="T">Type of the field.</typeparam>
	[DebuggerDisplay("Declaring Type = {FieldInfo.DeclaringType.FullName}, Field Info = {FieldInfo}")]
	class InheritedField<T> : Member, IInheritedField<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InheritedField{T}"/> class.
		/// </summary>
		/// <param name="typeDefinition">The type definition the member belongs to.</param>
		/// <param name="field">Field the type in creation has inherited.</param>
		internal InheritedField(TypeDefinition typeDefinition, FieldInfo field) :
			base(typeDefinition)
		{
			FieldInfo = field;
		}

		/// <summary>
		/// Gets the name of the field.
		/// </summary>
		public string Name => FieldInfo.Name;

		/// <summary>
		/// Gets the type of the field.
		/// </summary>
		public Type FieldType => FieldInfo.FieldType;

		/// <summary>
		/// Gets a value indicating whether the field is class variable (<c>true</c>) or a member variable (<c>false</c>).
		/// </summary>
		public bool IsStatic => FieldInfo.IsStatic;

		/// <summary>
		/// Gets the access modifier of the field.
		/// </summary>
		public Visibility Visibility => FieldInfo.ToVisibility();

		/// <summary>
		/// Gets the <see cref="System.Reflection.FieldInfo"/> associated with the field.
		/// </summary>
		public FieldInfo FieldInfo { get; }
	}

}
