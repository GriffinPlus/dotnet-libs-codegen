///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration;

/// <summary>
/// Definition of a type to create dynamically.
/// </summary>
public sealed class ClassDefinition : TypeDefinition
{
	/// <summary>
	/// Initializes a new definition of a class not deriving from a base type.
	/// </summary>
	/// <param name="name">Name of the class to create (<c>null</c> to create a name dynamically).</param>
	public ClassDefinition(string name = null) : base(null, false, name) { }

	/// <summary>
	/// Initializes a new definition of a class deriving from the specified base class.
	/// </summary>
	/// <param name="baseClass">Base class to derive the created class from.</param>
	/// <param name="name">Name of the class to create (<c>null</c> to keep the name of the base class).</param>
	/// <exception cref="ArgumentNullException"><paramref name="baseClass"/> is <c>null</c>.</exception>
	public ClassDefinition(Type baseClass, string name = null) : base(null, baseClass, name) { }

	/// <summary>
	/// Initializes a new definition of a class not deriving from a base type
	/// (associates the type definition with the specified module definition, for internal use only).
	/// </summary>
	/// <param name="module">Module definition to associate the class definition with.</param>
	/// <param name="name">Name of the class to create (<c>null</c> to create a name dynamically).</param>
	internal ClassDefinition(ModuleDefinition module, string name = null) : base(module, false, name) { }

	/// <summary>
	/// Initializes a new definition of a class deriving from the specified base class
	/// (associates the type definition with the specified module definition, for internal use only).
	/// </summary>
	/// <param name="module">Module definition to associate the class definition with.</param>
	/// <param name="baseClass">Base class to derive the created class from.</param>
	/// <param name="name">Name of the class to create (<c>null</c> to keep the name of the base class).</param>
	/// <exception cref="ArgumentNullException"><paramref name="baseClass"/> is <c>null</c>.</exception>
	internal ClassDefinition(ModuleDefinition module, Type baseClass, string name = null) : base(module, baseClass, name) { }

	/// <summary>
	/// Adds pass-through constructors for public constructors exposed by the base class, if any.
	/// This only adds constructors that have not been added explicitly before.
	/// </summary>
	/// <returns>The added constructors.</returns>
	public IGeneratedConstructor[] AddPassThroughConstructors()
	{
		var constructors = new List<IGeneratedConstructor>();

		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		Debug.Assert(TypeBuilder.BaseType != null, "TypeBuilder.BaseType != null");
		ConstructorInfo[] constructorInfos = TypeBuilder.BaseType.GetConstructors(flags);
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (ConstructorInfo constructorInfo in constructorInfos)
		{
			// skip constructor if it is private or internal
			// (cannot be accessed by a derived type defined in another assembly)
			if (constructorInfo.IsPrivate || constructorInfo.IsAssembly) continue;

			// ensure that the pass-through constructor to generate is not specified explicitly
			Type[] parameterTypes = constructorInfo.GetParameters().Select(x => x.ParameterType).ToArray();
			bool skipConstructor = Constructors.Any(definition => definition.ParameterTypes.SequenceEqual(parameterTypes));

			if (skipConstructor)
			{
				// a constructor with the parameters was explicitly defined
				// => skip adding pass-through constructor
				continue;
			}

			// add pass-through constructor
			IGeneratedConstructor constructor = AddConstructor(Visibility.Public, parameterTypes, ImplementPassThroughConstructorBaseCall);
			constructors.Add(constructor);
		}

		return [.. constructors];
	}

	/// <summary>
	/// Emits code to call the base class constructor with the same parameter types.
	/// </summary>
	/// <param name="generatedConstructor">Constructor that needs to emit code for calling a constructor of its base class.</param>
	/// <param name="msilGenerator">MSIL code generator attached to the constructor.</param>
	private void ImplementPassThroughConstructorBaseCall(IGeneratedConstructor generatedConstructor, ILGenerator msilGenerator)
	{
		// find base class constructor
		Type[] parameterTypes = generatedConstructor.ParameterTypes.ToArray();
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		Debug.Assert(TypeBuilder.BaseType != null, "TypeBuilder.BaseType != null");
		ConstructorInfo baseClassConstructor = TypeBuilder.BaseType.GetConstructor(flags, Type.DefaultBinder, parameterTypes, null);
		Debug.Assert(baseClassConstructor != null, nameof(baseClassConstructor) + " != null");

		// load arguments onto the evaluation stack
		msilGenerator.Emit(OpCodes.Ldarg_0);
		for (int i = 0; i < parameterTypes.Length; i++)
		{
			CodeGenHelpers.EmitLoadArgument(msilGenerator, i + 1);
		}

		// call base class constructor
		msilGenerator.Emit(OpCodes.Call, baseClassConstructor);
	}
}
