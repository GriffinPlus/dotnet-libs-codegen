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

using GriffinPlus.Lib.Logging;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Base class for code generation modules.
	/// </summary>
	public partial class CodeGenModule : ICodeGenModule
	{
		#region Class Variables

		private static LogWriter sLog = Log.GetWriter(typeof(CodeGenModule));

		#endregion

		#region Member Variables

		private CodeGenEngine mEngine;
		private ICodeGenModule[] mDependencies;

		#endregion

		#region Construction

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeGenModule"/> class.
		/// </summary>
		/// <param name="dependencies">Modules the current module depends on.</param>
		public CodeGenModule(params ICodeGenModule[] dependencies)
		{
			mDependencies = dependencies;
		}

		#endregion

		#region Common Properties

		/// <summary>
		/// Gets the code generation engine that creates the desired type.
		/// </summary>
		public CodeGenEngine Engine
		{
			get { return mEngine; }
		}

		/// <summary>
		/// Gets the type builder that is used to create the desired type.
		/// </summary>
		public TypeBuilder TypeBuilder
		{
			get { return mEngine.TypeBuilder; }
		}

		/// <summary>
		/// Gets the modules the current module depends on.
		/// </summary>
		public ICodeGenModule[] Dependencies
		{
			get { return mDependencies; }
			protected set { mDependencies = value; }
		}

		#endregion

		#region Initializing

		/// <summary>
		/// Is called by <see cref="CodeGenEngine"/> when it starts processing.
		/// </summary>
		void ICodeGenModule.Initialize(CodeGenEngine engine)
		{
			mEngine = engine;
			OnInitialize();
		}

		/// <summary>
		/// When overridden in a derived class, performs additional module initialization.
		/// </summary>
		protected virtual void OnInitialize()
		{
			
		}

		#endregion

		#region Cleaning Up

		/// <summary>
		/// Is called by <see cref="CodeGenEngine"/> when processing has finished.
		/// </summary>
		void ICodeGenModule.Cleanup()
		{
			mEngine = null;
			OnCleanup();
		}

		/// <summary>
		/// When overridden in a derived class, performs additional module cleanup.
		/// </summary>
		protected virtual void OnCleanup()
		{

		}

		#endregion

		#region Declaring

		/// <summary>
		/// Is called by <see cref="CodeGenEngine"/> to declare fields, events, properties and methods.
		/// </summary>
		void ICodeGenModule.Declare()
		{
			OnDeclare();
		}

		/// <summary>
		/// When overridden in a derived class, declares fields, events, properties and methods.
		/// </summary>
		protected virtual void OnDeclare()
		{

		}

		#endregion

		#region Implementing

		/// <summary>
		/// Is called by <see cref="CodeGenEngine"/> to add the implementation of declared event raisers, property accessors and methods.
		/// </summary>
		void ICodeGenModule.Implement()
		{
			OnImplement();
		}

		/// <summary>
		/// When overridden in a derived class, implements declared event raisers, property accessors and methods.
		/// </summary>
		protected virtual void OnImplement()
		{

		}

		#endregion

		#region Contributing Code to the Class Constructor

		/// <summary>
		/// Is called by <see cref="CodeGenEngine"/> to add code to the class constructor.
		/// </summary>
		/// <param name="msil">IL Generator attached to the class constructor.</param>
		void ICodeGenModule.ImplementClassConstruction(ILGenerator msil)
		{
			OnImplementClassConstructor(msil);
		}

		/// <summary>
		/// When overridden in a derived class, adds code to the class constructor of the type in creation.
		/// </summary>
		/// <param name="msil">IL Generator attached to the class constructor.</param>
		protected virtual void OnImplementClassConstructor(ILGenerator msil)
		{
			
		}

		#endregion

		#region Contributing Construction Code

		/// <summary>
		/// Is called by <see cref="CodeGenEngine"/> to add code to a constructor.
		/// </summary>
		/// <param name="msil">IL Generator attached to the appropriate constructor.</param>
		/// <param name="definition">Definition of the constructor being implemented.</param>
		void ICodeGenModule.ImplementConstruction(ILGenerator msil, ConstructorDefinition definition)
		{
			OnImplementConstructor(msil, definition);
		}

		/// <summary>
		/// When overridden in a derived class, adds code to a constructor of the type in creation.
		/// </summary>
		/// <param name="msil">IL Generator attached to the appropriate constructor.</param>
		/// <param name="definition">Definition of the constructor being implemented.</param>
		protected virtual void OnImplementConstructor(ILGenerator msil, ConstructorDefinition definition)
		{

		}

		#endregion

	}
}

