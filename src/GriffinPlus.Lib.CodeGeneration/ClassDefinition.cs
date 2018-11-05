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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Definition of a class to create dynamically using the <see cref="CodeGenEngine"/>.
	/// </summary>
	public class ClassDefinition
	{
		private readonly string mTypeName;
		private readonly Type mBaseClassType;
		private readonly bool mGeneratePassThroughConstructors;
		private readonly List<ConstructorDefinition> mConstructors = new List<ConstructorDefinition>();
		private List<ICodeGenModule> mSortedModules = new List<ICodeGenModule>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ClassDefinition"/> class.
		/// </summary>
		/// <param name="name">Name of the class to create (null to create a name dynamically).</param>
		public ClassDefinition(string name = null)
		{
			mTypeName = name;
			mBaseClassType = null;
			mGeneratePassThroughConstructors = false;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ClassDefinition"/> class.
		/// </summary>
		/// <param name="baseClass">Base class to derive the created class from.</param>
		/// <param name="generatePassThroughConstructors">
		/// true to generate pass-through constructors;
		/// false to generate a parameterless constructor only.
		/// </param>
		/// <param name="name">Name of the class to create (null to keep the name of the base class).</param>
		public ClassDefinition(Type baseClass, bool generatePassThroughConstructors, string name = null)
		{
			if (baseClass == null) throw new ArgumentNullException(nameof(baseClass));

			// ensure that the base class is really a class
			if (!baseClass.IsClass)
			{
				string error = string.Format("The specified type ({0}) for the base class is not a class.", baseClass.FullName);
				throw new ArgumentException(error);
			}

			CodeGenHelpers.CheckTypeIsTotallyPublic(baseClass);

			mTypeName = name;
			mBaseClassType = baseClass;
			mGeneratePassThroughConstructors = generatePassThroughConstructors;
		}

		/// <summary>
		/// Gets the name of the type to create
		/// (null to keep the name of the base type, if any, or generate a random name, if the base type is not specified).
		/// </summary>
		public string TypeName
		{
			get { return mTypeName; }
		}

		/// <summary>
		/// Gets the type of the base class
		/// (null, if the type to create does not derive from another class).
		/// </summary>
		public Type BaseClassType
		{
			get { return mBaseClassType; }
		}

		/// <summary>
		/// Gets all code generation modules contributing to the type to create.
		/// </summary>
		public IEnumerable<ICodeGenModule> Modules
		{
			get { return mSortedModules; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether to generate pass-through constructors that
		/// call the base class constructors with the same parameter types.
		/// </summary>
		public bool GeneratePassThroughConstructors
		{
			get { return mGeneratePassThroughConstructors; }
		}

		/// <summary>
		/// Gets constructor definitions determining what constructors need to be created.
		/// </summary>
		public IEnumerable<ConstructorDefinition> Constructors
		{
			get { return mConstructors; }
		}

		/// <summary>
		/// Adds a code generation module contributing to the type to create.
		/// </summary>
		/// <param name="module">Module to add.</param>
		public void AddModule(ICodeGenModule module)
		{
			if (module == null) throw new ArgumentNullException(nameof(module));
			if (mSortedModules.Find(other => object.ReferenceEquals(other, module)) != null)
			{
				throw new CodeGenException("The specified module is already part of the type definition.");
			}

			// update list of modules
			// (ResolveModuleDependencies() can throw CodeGenException, if it detects a circular module dependency)
			List<ICodeGenModule> modules = new List<ICodeGenModule>(mSortedModules);
			modules.Add(module);
			mSortedModules = new List<ICodeGenModule>(ResolveModuleDependencies(modules));
		}

		/// <summary>
		/// Adds the definition of a constructor the type should implement.
		/// </summary>
		/// <param name="definition">Definition to add.</param>
		public void AddConstructorDefinition(ConstructorDefinition definition)
		{
			if (definition == null) throw new ArgumentNullException(nameof(definition));
			if (mConstructors.Contains(definition))
			{
				throw new CodeGenException("A constructor with the same signature is already part of the definition.");
			}
			mConstructors.Add(definition);
		}

		/// <summary>
		/// Adds code that calls the base class constructor when the constructor with the specified signature is called
		/// (calls the default constructor of the base class by default, must be overridden to call some other constructor instead).
		/// </summary>
		/// <param name="typeBuilder">Type builder creating the requested type.</param>
		/// <param name="msil">IL code generator to use.</param>
		/// <param name="constructorParameterTypes">Parameter types of the constructor currently being implemented.</param>
		protected internal virtual void ImplementBaseClassConstructorCall(TypeBuilder typeBuilder, ILGenerator msil, Type[] constructorParameterTypes)
		{
			if (typeBuilder.BaseType != null)
			{
				// call parameterless base class constructor
				BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
				ConstructorInfo constructor = typeBuilder.BaseType.GetConstructor(flags, null, Type.EmptyTypes, null);
				if (constructor == null)
				{
					string error = string.Format("The base class ({0}) does not have an accessable parameterless constructor.", typeBuilder.BaseType.FullName);
					throw new CodeGenException(error);
				}
				msil.Emit(OpCodes.Ldarg_0);
				msil.Emit(OpCodes.Call, constructor);
			}
		}

		#region Resolving Module Dependencies

		/// <summary>
		/// Resolves the dependencies of the specified modules recursively and returns all modules in the order in which
		/// the modules can do their code generation work without violating dependencies (helper method).
		/// </summary>
		/// <param name="modules">Code generation modules that contribute to the created type.</param>
		/// <returns>Code generation modules in the order that considers their dependencies.</returns>
		public static ICodeGenModule[] ResolveModuleDependencies(IEnumerable<ICodeGenModule> modules)
		{
			HashSet<ICodeGenModule> complete = new HashSet<ICodeGenModule>(IdentityComparer<ICodeGenModule>.Default);

			foreach (ICodeGenModule module in modules)
			{
				// add the current module to the 'processed' set to indicate that it is already part of the dependency chain
				// (depending module may not reference a module already listed there => circular dependency check)
				HashSet<ICodeGenModule> processed = new HashSet<ICodeGenModule>(IdentityComparer<ICodeGenModule>.Default)
				{
					module
				};

				// get depending modules and merge depending modules into the complete set of modules taking part
				HashSet<ICodeGenModule> dependencies = GetDependingModules(module, processed);
				complete.UnionWith(dependencies);
				complete.Add(module);
			}

			// resolve module dependencies
			// (the list will start with modules that do not have any dependencies and end with the root module(s))
			List<ICodeGenModule> sorted = new List<ICodeGenModule>();
			while (complete.Count > 0)
			{
				foreach (ICodeGenModule module in complete)
				{
					if (module.Dependencies == null || module.Dependencies.All(x => sorted.Contains(x, IdentityComparer<ICodeGenModule>.Default)))
					{
						complete.Remove(module);
						sorted.Add(module);
						break;
					}
				}
			}
			return sorted.ToArray();
		}

		/// <summary>
		/// Retrieves all depending modules of the specified module recursively and checks for circular dependencies.
		/// </summary>
		/// <param name="module">Code generation module to retrieve depending modules for.</param>
		/// <param name="alreadyProcessedModules">Modules that have already been processed.</param>
		/// <returns>Code generation modules that depend on the specified module.</returns>
		private static HashSet<ICodeGenModule> GetDependingModules(ICodeGenModule module, HashSet<ICodeGenModule> alreadyProcessedModules)
		{
			HashSet<ICodeGenModule> complete = new HashSet<ICodeGenModule>(IdentityComparer<ICodeGenModule>.Default);

			// add all modules the current module depends on and check for circular dependencies
			foreach (ICodeGenModule dependency in module.Dependencies)
			{
				// check whether the depending module has already been processed, which would indicate a circular dependency
				if (alreadyProcessedModules.Contains(dependency, IdentityComparer<ICodeGenModule>.Default))
				{
					string error = string.Format("Module ({0}) is referenced circularly.", dependency.ToString());
					throw new CodeGenException(error);
				}

				// add the current module to the 'processed' set to indicate that it is already part of the dependency chain
				// (depending module may not reference a module already listed there => circular dependency check)
				HashSet<ICodeGenModule> processed = new HashSet<ICodeGenModule>(alreadyProcessedModules, IdentityComparer<ICodeGenModule>.Default)
				{
					dependency
				};

				// resolve dependencies and merge depending modules into the complete set of modules taking part
				HashSet<ICodeGenModule> dependencies = GetDependingModules(dependency, processed);
				complete.UnionWith(dependencies);
				complete.Add(dependency);
			}

			return complete;
		}

		#endregion
	}
}
