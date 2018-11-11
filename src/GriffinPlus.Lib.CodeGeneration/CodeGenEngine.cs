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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Code Generation Engine that can dynamically create various types defined by a type definition and code generation modules.
	/// </summary>
	public class CodeGenEngine
	{
		#region Class Variables

		private static LogWriter sLog = Log.GetWriter(typeof(CodeGenEngine));

		#endregion

		#region Internal Types

		private enum Phase
		{
			Idle,
			Initializing,
			Declaring,
			Implementing,
			Finished
		}

		#endregion

		#region Member Variables

		private readonly ClassDefinition mClassDefinition;
		private Phase mPhase = Phase.Idle;
		private TypeBuilder mTypeBuilder;
		private readonly List<InheritedField> mInheritedFields = new List<InheritedField>();
		private readonly List<IGeneratedFieldInternal> mGeneratedFields = new List<IGeneratedFieldInternal>();
		private readonly List<InheritedEvent> mInheritedEvents = new List<InheritedEvent>();
		private readonly List<GeneratedEvent> mGeneratedEvents = new List<GeneratedEvent>();
		private readonly List<InheritedProperty> mInheritedProperties = new List<InheritedProperty>();
		private readonly List<GeneratedProperty> mGeneratedProperties = new List<GeneratedProperty>();
		private readonly List<GeneratedDependencyProperty> mGeneratedDependencyProperties = new List<GeneratedDependencyProperty>();
		private readonly List<InheritedMethod> mInheritedMethods = new List<InheritedMethod>();
		private readonly List<GeneratedMethod> mGeneratedMethods = new List<GeneratedMethod>();
		private readonly List<object> mExternalObjects = new List<object>();

		#endregion

		#region Construction

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeGenEngine"/> class.
		/// </summary>
		/// <param name="definition">Class definition describing the class to create.</param>
		private CodeGenEngine(ClassDefinition definition)
		{
			mClassDefinition = definition ?? throw new ArgumentNullException(nameof(definition));

			// initialize information about the base type
			Type baseClassType = definition.BaseClassType ?? typeof(object);
			InitializeInheritedFields(baseClassType);
			InitializeInheritedEvents(baseClassType);
			InitializeInheritedProperties(baseClassType);
			InitializeInheritedMethods(baseClassType);
		}

		/// <summary>
		/// Populates the list of inherited fields.
		/// </summary>
		/// <param name="type">Type of the base class of the type in creation.</param>
		private void InitializeInheritedFields(Type type)
		{
			if (type != null)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
				FieldInfo[] fieldInfos = type.GetFields(flags);
				foreach (FieldInfo fieldInfo in fieldInfos)
				{
					Visibility accessModifier = fieldInfo.ToVisibility();
					if (accessModifier == Visibility.Private || accessModifier != Visibility.Internal) continue;
					mInheritedFields.Add(new InheritedField(this, fieldInfo));
				}
			}
		}

		/// <summary>
		/// Populates the list of inherited events.
		/// </summary>
		/// <param name="type">Type of the base class of the type in creation.</param>
		private void InitializeInheritedEvents(Type type)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			EventInfo[] eventInfos = type.GetEvents(flags);
			foreach (EventInfo eventInfo in eventInfos)
			{
				MethodInfo addMethod = eventInfo.GetAddMethod(true);
				MethodInfo removeMethod = eventInfo.GetAddMethod(true);
				Visibility addAccessModifier = addMethod.ToVisibility();
				Visibility removeAccessModifier = removeMethod.ToVisibility();
				if (addAccessModifier == Visibility.Private || addAccessModifier == Visibility.Internal) continue;
				if (removeAccessModifier == Visibility.Private || removeAccessModifier == Visibility.Internal) continue;
				mInheritedEvents.Add(new InheritedEvent(this, eventInfo));
			}
		}

		/// <summary>
		/// Populates the list of inherited properties.
		/// </summary>
		/// <param name="type">Type of the base class of the type in creation.</param>
		private void InitializeInheritedProperties(Type type)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			PropertyInfo[] propertyInfos = type.GetProperties(flags);
			foreach (PropertyInfo propertyInfo in propertyInfos)
			{
				// skip accessors that are 'private' or 'internal'
				// (cannot be accessed by a derived type defined in another assembly)
				int callableAccessorCount = 0;

				// check visibility of the 'get' accessor
				MethodInfo getAccessor = propertyInfo.GetGetMethod(true);
				if (getAccessor != null)
				{
					Visibility getAccessorAccessModifier = getAccessor.ToVisibility();
					if (getAccessorAccessModifier != Visibility.Private && getAccessorAccessModifier != Visibility.Internal)
					{
						callableAccessorCount++;
					}
				}

				// check visibility of the 'set' accessor
				MethodInfo setAccessor = propertyInfo.GetSetMethod(true);
				if (setAccessor != null)
				{
					Visibility setAccessorAccessModifier = setAccessor.ToVisibility();
					if (setAccessorAccessModifier != Visibility.Private && setAccessorAccessModifier != Visibility.Internal)
					{
						callableAccessorCount++;
					}
				}

				// keep property, if the getter and/or the setter are accessable
				if (callableAccessorCount > 0)
				{
					mInheritedProperties.Add(new InheritedProperty(this, propertyInfo));
				}
			}
		}

		/// <summary>
		/// Populates the list of inherited methods.
		/// </summary>
		/// <param name="type">Type of the base class of the type in creation.</param>
		private void InitializeInheritedMethods(Type type)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			MethodInfo[] methodInfos = type.GetMethods(flags);
			foreach (MethodInfo methodInfo in methodInfos)
			{
				Visibility modifier = methodInfo.ToVisibility();
				if (modifier == Visibility.Private || modifier == Visibility.Internal) continue;
				InheritedMethod inheritedMethod = new InheritedMethod(this, methodInfo);
				mInheritedMethods.Add(inheritedMethod);
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the type builder of the type in creation.
		/// </summary>
		public TypeBuilder TypeBuilder
		{
			get { return mTypeBuilder; }
		}

		/// <summary>
		/// Gets the collection of inherited fields.
		/// </summary>
		public IEnumerable<InheritedField> InheritedFields
		{
			get { return mInheritedFields; }
		}

		/// <summary>
		/// Gets the collection of generated fields.
		/// </summary>
		public IEnumerable<IGeneratedField> GeneratedFields
		{
			get { return mGeneratedFields; }
		}

		/// <summary>
		/// Gets the collection of inherited events.
		/// </summary>
		public IEnumerable<InheritedEvent> InheritedEvents
		{
			get { return mInheritedEvents; }
		}

		/// <summary>
		/// Gets the collection of generated events.
		/// </summary>
		public IEnumerable<GeneratedEvent> GeneratedEvents
		{
			get { return mGeneratedEvents; }
		}

		/// <summary>
		/// Gets the collection of inherited properties.
		/// </summary>
		public IEnumerable<InheritedProperty> InheritedProperties
		{
			get { return mInheritedProperties; }
		}

		/// <summary>
		/// Gets the collection of generated properties.
		/// </summary>
		public IEnumerable<GeneratedProperty> GeneratedProperties
		{
			get { return mGeneratedProperties; }
		}

		/// <summary>
		/// Gets the collection of generated dependency properties.
		/// </summary>
		public IEnumerable<GeneratedDependencyProperty> GeneratedDependencyProperties
		{
			get { return mGeneratedDependencyProperties; }
		}

		/// <summary>
		/// Gets the collection of inherited methods.
		/// </summary>
		public IEnumerable<InheritedMethod> InheritedMethods
		{
			get { return mInheritedMethods; }
		}

		/// <summary>
		/// Gets the collection of generated methods.
		/// </summary>
		public IEnumerable<GeneratedMethod> GeneratedMethods
		{
			get { return mGeneratedMethods; }
		}

		/// <summary>
		/// Gets the collection of external objects that is added to the created type.
		/// </summary>
		internal List<object> ExternalObjects
		{
			get { return mExternalObjects; }
		}

		#endregion

		#region Implementating a Class

		/// <summary>
		/// Implements the type in creation invoking all modules defined in the specified class definition.
		/// </summary>
		/// <returns>The created class.</returns>
		private Type GenerateClass()
		{
			try
			{
				// initialize code generation modules
				mPhase = Phase.Initializing;
				foreach (ICodeGenModule module in mClassDefinition.Modules)
				{
					module.Initialize(this);
				}

				// let modules do their declarations
				mPhase = Phase.Declaring;
				foreach (ICodeGenModule module in mClassDefinition.Modules)
				{
					module.Declare();
				}

				// freeze the declared members
				foreach (IGeneratedFieldInternal field in mGeneratedFields) field.Freeze();
				foreach (GeneratedEvent evnt in mGeneratedEvents) evnt.Freeze();
				foreach (GeneratedProperty property in mGeneratedProperties) property.Freeze();
				foreach (GeneratedMethod method in mGeneratedMethods) method.Freeze();
				foreach (GeneratedDependencyProperty property in mGeneratedDependencyProperties) property.Freeze();

				// create module builder
				AssemblyName assemblyName = new AssemblyName(Guid.NewGuid().ToString("D"));
				AppDomain domain = AppDomain.CurrentDomain;
				AssemblyBuilder assemblyBuilder = domain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
				ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

				// determine the name of the type to create
				string typeName = mClassDefinition.TypeName;
				if (typeName == null)
				{
					if (mClassDefinition.BaseClassType != null) typeName = mClassDefinition.BaseClassType.FullName;
					else typeName = "DynamicType_" + Guid.NewGuid().ToString("N");
				}

				// create a type builder
				Stack<TypeBuilder> typeBuilders = new Stack<TypeBuilder>();
				string[] splitTypeNameTokens = typeName.Split('+');
				if (splitTypeNameTokens.Length > 1)
				{
					TypeBuilder parent = moduleBuilder.DefineType(splitTypeNameTokens[0], TypeAttributes.Public | TypeAttributes.Class, null);
					typeBuilders.Push(parent);
					for (int i = 1; i < splitTypeNameTokens.Length; i++)
					{
						if (i + 1 < splitTypeNameTokens.Length)
						{
							parent = parent.DefineNestedType(splitTypeNameTokens[i], TypeAttributes.NestedPublic | TypeAttributes.Class);
							typeBuilders.Push(parent);
						}
						else
						{
							parent = parent.DefineNestedType(splitTypeNameTokens[i], TypeAttributes.NestedPublic | TypeAttributes.Class, mClassDefinition.BaseClassType);
							typeBuilders.Push(parent);
						}
					}
				}
				else
				{
					TypeBuilder builder = moduleBuilder.DefineType(splitTypeNameTokens[0], TypeAttributes.Public | TypeAttributes.Class, mClassDefinition.BaseClassType);
					typeBuilders.Push(builder);
				}

				mTypeBuilder = typeBuilders.Peek();

				// add fields, events, properties and methods to the type builder
				foreach (IGeneratedFieldInternal field in mGeneratedFields) field.AddToTypeBuilder();
				foreach (GeneratedEvent evnt in mGeneratedEvents) evnt.AddToTypeBuilder();
				foreach (GeneratedProperty property in mGeneratedProperties) property.AddToTypeBuilder();
				foreach (GeneratedMethod method in mGeneratedMethods) method.AddToTypeBuilder();
				foreach (GeneratedDependencyProperty property in mGeneratedDependencyProperties) property.AddToTypeBuilder();

				// let modules do their implementation
				mPhase = Phase.Implementing;
				foreach (ICodeGenModule module in mClassDefinition.Modules)
				{
					module.Implement();
				}

				// implement properties and events using an implementation strategy
				foreach (GeneratedProperty property in mGeneratedProperties) property.Implement();
				foreach (GeneratedEvent evnt in mGeneratedEvents) evnt.Implement();

				// add class constructor and let modules contribute their initialization code
				AddClassConstructor();

				// add constructor and let modules contribute their initialization code
				AddConstructors();

				// create the type of the class
				mPhase = Phase.Finished;
				Type createdType = null;
				while (typeBuilders.Count > 0)
				{
					TypeBuilder builder = typeBuilders.Pop();
					if (createdType == null) createdType = builder.CreateType();
					else builder.CreateType();
				}

				return createdType;
			}
			finally
			{
				// cleanup modules
				foreach (ICodeGenModule module in mClassDefinition.Modules)
				{
					module.Cleanup();
				}
			}
		}

		/// <summary>
		/// Adds the class constructor to the specified type and calls the specified modules to add their code to it.
		/// </summary>
		private void AddClassConstructor()
		{
			ConstructorBuilder constructorBuilder = mTypeBuilder.DefineConstructor(MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, Type.EmptyTypes);
			ILGenerator msil = constructorBuilder.GetILGenerator();

			// add field initialization code
			foreach (IGeneratedFieldInternal field in mGeneratedFields.Where(x => x.IsStatic))
			{
				field.ImplementFieldInitialization(msil);
			}

			// let modules add their code to the class constructor
			foreach (ICodeGenModule module in mClassDefinition.Modules)
			{
				module.ImplementClassConstruction(msil);
			}

			msil.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Adds a parameterless constructor to the specified type and calls the specified modules to add their code to it.
		/// </summary>
		private void AddConstructors()
		{
			List<ConstructorDefinition> constructors = new List<ConstructorDefinition>();

			// add constructors explicitly defined in the class definition
			constructors.AddRange(mClassDefinition.Constructors);

			// add pass-through constructors, if requested
			if (mClassDefinition.GeneratePassThroughConstructors && mTypeBuilder.BaseType != null)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
				ConstructorInfo[] constructorInfos = mTypeBuilder.BaseType.GetConstructors(flags);
				foreach (ConstructorInfo ci in constructorInfos)
				{
					// abort, if the base class constructor is not accessable
					Visibility accessModifier = ci.ToVisibility();
					if (accessModifier == Visibility.Private || accessModifier == Visibility.Internal) continue;

					// ensure that the pass-through constructor to generate is not specified explicitly
					Type[] parameterTypes = ci.GetParameters().Select(x => x.ParameterType).ToArray();
					bool skipConstructor = false;
					foreach (ConstructorDefinition cdef in constructors)
					{
						if (cdef.ParameterTypes.SequenceEqual(parameterTypes))
						{
							skipConstructor = true;
							break;
						}
					}

					if (skipConstructor)
					{
						sLog.Write(
							LogLevel.Warning,
							"A constructor with parameters [{0}] was explicitly defined, skipping adding pass-through constructor.",
							string.Join(", ", parameterTypes.Select(x => x.FullName)));

						continue;
					}

					// add pass-through constructor 
					constructors.Add(new ConstructorDefinition(Visibility.Public, parameterTypes, ImplementPassThroughConstructorBaseCall));
				}
			}

			// add default constructor, if no constructor is defined until now...
			if (constructors.Count == 0)
			{
				constructors.Add(ConstructorDefinition.Default);
			}

			// create constructors
			foreach (ConstructorDefinition constructorDefinition in constructors)
			{
				MethodAttributes flags = constructorDefinition.AccessModifier.ToMethodAttributes() | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
				ConstructorBuilder constructorBuilder = mTypeBuilder.DefineConstructor(flags, CallingConventions.HasThis, constructorDefinition.ParameterTypes.ToArray());
				ILGenerator msil = constructorBuilder.GetILGenerator();

				// call parameterless constructor of the base class, if the created class has a base class
				if (mTypeBuilder.BaseType != null)
				{
					if (constructorDefinition.ImplementBaseClassConstructorCallCallback != null)
					{
						// the constructor will use user-supplied code to call a base class constructor
						constructorDefinition.ImplementBaseClassConstructorCallCallback(constructorDefinition, mTypeBuilder, msil);
					}
					else
					{
						// the constructor does not have any special handling of the base class constructor call
						// => call parameterless constructor
						BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
						ConstructorInfo constructor = mTypeBuilder.BaseType.GetConstructor(bindingFlags, null, Type.EmptyTypes, null);
						if (constructor == null)
						{
							string error = string.Format("The base class ({0}) does not have an accessable parameterless constructor.", mTypeBuilder.BaseType.FullName);
							throw new CodeGenException(error);
						}
						msil.Emit(OpCodes.Ldarg_0);
						msil.Emit(OpCodes.Call, constructor);
					}
				}

				// add field initialization code
				foreach (IGeneratedFieldInternal field in mGeneratedFields.Where(x => !x.IsStatic))
				{
					field.ImplementFieldInitialization(msil);
				}

				// let modules add their code to the constructor
				foreach (ICodeGenModule module in mClassDefinition.Modules)
				{
					module.ImplementConstruction(msil, constructorDefinition);
				}

				// emit 'ret' to return from the constructor
				msil.Emit(OpCodes.Ret);
			}
		}

		/// <summary>
		/// Adds code to call the base class constructor with the same parameter types.
		/// </summary>
		/// <param name="constructorDefinition">Definition of the constructor that needs to emit code for calling a constructor of its base class.</param>
		/// <param name="typeBuilder">Type builder creating the requested type.</param>
		/// <param name="msil">IL code generator to use.</param>
		private static void ImplementPassThroughConstructorBaseCall(ConstructorDefinition constructorDefinition, TypeBuilder typeBuilder, ILGenerator msil)
		{
			// find base class constructor
			Type[] parameterTypes = constructorDefinition.ParameterTypes.ToArray();
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			ConstructorInfo baseClassConstructor = typeBuilder.BaseType.GetConstructor(flags, Type.DefaultBinder, parameterTypes, null);

			// load arguments onto the evaluation stack
			msil.Emit(OpCodes.Ldarg_0);
			for (int i = 0; i < parameterTypes.Length; i++)
			{
				CodeGenHelpers.EmitLoadArgument(msil, i + 1);
			}

			// call base class constructor
			msil.Emit(OpCodes.Call, baseClassConstructor);
		}

		#endregion

		#region Creating a Class

		/// <summary>
		/// Creates a new class defined by the specified class definition.
		/// </summary>
		/// <param name="definition">Class definition describing the class to create.</param>
		/// <returns>The created class.</returns>
		public static Type CreateClass(ClassDefinition definition)
		{
			CodeGenEngine engine = new CodeGenEngine(definition);
			Type generatedType = engine.GenerateClass();
			CodeGenExternalStorage.Add(generatedType, engine.ExternalObjects.ToArray());
			return generatedType;
		}

		#endregion

		#region Handling Interfaces

		/// <summary>
		/// Adds the specified interface to the type in creation.
		/// </summary>
		/// <typeparam name="T">Interface the type in creation implements.</typeparam>
		public void AddInterface<T>()
		{
			// ensure that the specified type is an interface
			if (!typeof(T).IsInterface)
			{
				string error = string.Format("The specified type ({0}) is not an interface.", typeof(T).FullName);
				throw new ArgumentException(error);
			}

			// ensure that the specified interface is not already implemented by some other module
			if (mTypeBuilder.GetInterfaces().Contains(typeof(T)))
			{
				string error = string.Format("The specified interface ({0}) is already implemented by another module.", typeof(T).FullName);
				sLog.Write(LogLevel.Error, error);
				throw new CodeGenException(error);
			}

			// add interface to the type
			mTypeBuilder.AddInterfaceImplementation(typeof(T));
		}

		#endregion

		#region Handling Fields

		/// <summary>
		/// Adds a new member field to the type in creation (without default value).
		/// </summary>
		/// <typeparam name="T">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <returns>The added field.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public IGeneratedField AddField<T>(string name = null, Visibility visibility = Visibility.Private)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			Type generatedFieldType = typeof(GeneratedField<T>);
			IGeneratedFieldInternal field = (IGeneratedFieldInternal)FastActivator.CreateInstance<CodeGenEngine, bool, string, Visibility>(
				generatedFieldType,
				this,
				false,
				name,
				visibility);

			mGeneratedFields.Add(field);
			return field;
		}

		/// <summary>
		/// Adds a new member field to the type in creation (with primitive default value).
		/// </summary>
		/// <typeparam name="T">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="defaultValue">Default value the field.</param>
		/// <returns>The added field.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public IGeneratedField AddField<T>(string name = null, Visibility visibility = Visibility.Private, T defaultValue = default(T))
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			Type generatedFieldType = typeof(GeneratedField<T>);
			IGeneratedFieldInternal field = (IGeneratedFieldInternal)FastActivator.CreateInstance<CodeGenEngine, bool, string, Visibility, T>(
				generatedFieldType,
				this,
				false,
				name,
				visibility,
				defaultValue);

			mGeneratedFields.Add(field);
			return field;
		}

		/// <summary>
		/// Adds a new member field to the type in creation (with factory callback).
		/// </summary>
		/// <typeparam name="T">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="factory">Factory method that creates the object to assign to the field when the type is instanciated.</param>
		/// <returns>The added field.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public IGeneratedField AddField<T>(string name, Visibility visibility, Func<T> factory)
		{
			if (factory == null) throw new ArgumentNullException(nameof(factory));
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			Type generatedFieldType = typeof(GeneratedField<T>);
			Type factoryType = typeof(Func<>).MakeGenericType(typeof(T));
			IGeneratedFieldInternal field = (IGeneratedFieldInternal)FastActivator.CreateInstanceDynamically(
				generatedFieldType,
				new[] { typeof(CodeGenEngine), typeof(bool), typeof(string), typeof(Visibility), factoryType },
				this,
				false,
				name,
				visibility,
				factory);

			mGeneratedFields.Add(field);
			return field;
		}

		/// <summary>
		/// Adds a new member field to the type in creation (with initializer).
		/// </summary>
		/// <typeparam name="T">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="initializer">Initializer method providing the value to store in the field.</param>
		/// <returns>The added field.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// The specified initializer method is executed in the 'implementation' phase, where all modules have declared their data.
		/// </remarks>
		public IGeneratedField AddField<T>(string name, Visibility visibility, FieldInitializer initializer)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			Type generatedFieldType = typeof(GeneratedField<T>);
			IGeneratedFieldInternal field = (IGeneratedFieldInternal)FastActivator.CreateInstance<CodeGenEngine, bool, string, Visibility, FieldInitializer>(
				generatedFieldType,
				this,
				false,
				name,
				visibility,
				initializer);

			mGeneratedFields.Add(field);
			return field;
		}

		/// <summary>
		/// Adds a new static field to the type in creation (without default value).
		/// </summary>
		/// <typeparam name="T">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Visbility of the field.</param>
		/// <returns>The added field.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public IGeneratedField AddStaticField<T>(string name = null, Visibility visibility = Visibility.Private)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			Type generatedFieldType = typeof(GeneratedField<T>);
			IGeneratedFieldInternal field = (IGeneratedFieldInternal)FastActivator.CreateInstance<CodeGenEngine, bool, string, Visibility>(
				generatedFieldType,
				this,
				true,
				name,
				visibility);

			mGeneratedFields.Add(field);
			return field;
		}

		/// <summary>
		/// Adds a new static field to the type in creation (with primitive default value).
		/// </summary>
		/// <typeparam name="T">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Access modifier to apply to the field.</param>
		/// <param name="defaultValue">Default value the field.</param>
		/// <returns>The added field.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public IGeneratedField AddStaticField<T>(string name = null, Visibility visibility = Visibility.Private, T defaultValue = default(T))
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			Type generatedFieldType = typeof(GeneratedField<T>);
			IGeneratedFieldInternal field = (IGeneratedFieldInternal)FastActivator.CreateInstance<CodeGenEngine, bool, string, Visibility, T>(
				generatedFieldType,
				this,
				true,
				name,
				visibility,
				defaultValue);

			mGeneratedFields.Add(field);
			return field;
		}

		/// <summary>
		/// Adds a new static field to the type in creation (with factory callback).
		/// </summary>
		/// <typeparam name="T">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="factory">Factory method that creates the object to assign to the field when the type is initialized.</param>
		/// <returns>The added field.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public IGeneratedField AddStaticField<T>(string name, Visibility visibility, Func<T> factory)
		{
			if (factory == null) throw new ArgumentNullException(nameof(factory));
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			Type generatedFieldType = typeof(GeneratedField<T>);
			Type factoryType = typeof(Func<>).MakeGenericType(typeof(T));
			IGeneratedFieldInternal field = (IGeneratedFieldInternal)FastActivator.CreateInstanceDynamically(
				generatedFieldType,
				new[] { typeof(CodeGenEngine), typeof(bool), typeof(string), typeof(Visibility), factoryType },
				this,
				true,
				name,
				visibility,
				factory);

			mGeneratedFields.Add(field);
			return field;
		}

		/// <summary>
		/// Adds a new static field to the type in creation (with initializer).
		/// </summary>
		/// <typeparam name="T">Type of the field to add.</typeparam>
		/// <param name="name">Name of the field to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Visibility of the field.</param>
		/// <param name="initializer">Initalizer method providing the value to store in the field.</param>
		/// <returns>The added field.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// The specified initializer method is executed in the 'implementation' phase, where all modules have declared their data.
		/// </remarks>
		public IGeneratedField AddStaticField<T>(string name, Visibility visibility, FieldInitializer initializer)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			Type generatedFieldType = typeof(GeneratedField<T>);
			IGeneratedFieldInternal field = (IGeneratedFieldInternal)FastActivator.CreateInstance<CodeGenEngine, bool, string, Visibility, FieldInitializer>(
				generatedFieldType,
				this,
				true,
				name,
				visibility,
				initializer);

			mGeneratedFields.Add(field);
			return field;
		}

		/// <summary>
		/// Adds a new field to the type in creation.
		/// </summary>
		/// <param name="type">Type of the field to add.</param>
		/// <param name="name">Name of the field to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="isStatic">true to create a static field; false to create a member field.</param>
		/// <param name="visibility">Access modifier to apply to the field.</param>
		/// <param name="initializer">Initalizer method providing the value to store in the field.</param>
		/// <returns>The added field.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// The specified initializer method is executed in the 'implementation' phase, where all modules have declared their data.
		/// </remarks>
		public IGeneratedField AddField(Type type, string name = null, bool isStatic = false, Visibility visibility = Visibility.Private, FieldInitializer initializer = null)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);
			if (type == null) throw new ArgumentNullException(nameof(type));

			Type generatedFieldType = typeof(GeneratedField<>).MakeGenericType(type);
			IGeneratedFieldInternal field = (IGeneratedFieldInternal)FastActivator.CreateInstance<CodeGenEngine, bool, string, Visibility, FieldInitializer>(
				generatedFieldType,
				this,
				isStatic,
				name,
				visibility,
				initializer);

			mGeneratedFields.Add(field);
			return field;
		}

		/// <summary>
		/// Removes the specified field from the type in creation.
		/// </summary>
		/// <param name="field">Field to remove.</param>
		/// <returns>
		/// true, if the field was successfully removed;
		/// false, if the specified field was not found.
		/// </returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public bool RemoveField(IGeneratedField field)
		{
			EnsurePhase(Phase.Declaring);
			int index = mGeneratedFields.FindIndex(x => object.ReferenceEquals(field, x));
			if (index < 0) return false;
			mGeneratedFields.RemoveAt(index);
			return true;
		}

		#endregion

		#region Handling Events

		/// <summary>
		/// Adds a new event to the type in creation (add/remove methods + event raiser method).
		/// </summary>
		/// <typeparam name="T">Type of the event to add.</typeparam>
		/// <param name="eventName">Name of the event to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Visibility of the event.</param>
		/// <param name="raiserMethodName">Name of the event raiser method (null to create the name dynamically to avoid name clashed with other modules).</param>
		/// <param name="implementation">Implementation strategy to use (may be null to provide an implementation in the 'implementation' phase).</param>
		/// <returns>The added event.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// 
		/// An event raiser method is implemented depending on the event type.
		/// If the event type is <see cref="System.EventHandler"/> or <see cref="System.EventHandler{TEventArgs}"/> with
		/// TEventArgs being <see cref="System.EventArgs"/> the event raiser method will have the signature <c>void OnEvent()</c>.
		/// If the event type is <see cref="System.EventHandler{TEventArgs}"/> with more specialized TEventArgs (e.g. SpecializedEventArgs)
		/// the event raiser method will have the signature <c>void OnEvent(SpecializedEventArgs e)</c>. Any other event type will
		/// produce an event raiser method that has the same parameters as the delegate.
		/// </remarks>
		public GeneratedEvent AddEvent<T>(
			string eventName,
			Visibility visibility,
			string raiserMethodName,
			IEventImplementation implementation)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(eventName);
			EnsureThatIdentifierHasNotBeenUsedYet(raiserMethodName);
			GeneratedEvent generatedEvent = new GeneratedEvent(this, EventKind.Normal, typeof(T), eventName, visibility, raiserMethodName, implementation);
			mGeneratedEvents.Add(generatedEvent);
			return generatedEvent;
		}

		/// <summary>
		/// Adds a new static event to the type in creation (add/remove methods + event raiser method).
		/// </summary>
		/// <typeparam name="T">Type of the event to add.</typeparam>
		/// <param name="eventName">Name of the event to add (null to create a name dynamically to avoid name clashes with other modules).</param>
		/// <param name="visibility">Visibility of the event.</param>
		/// <param name="raiserMethodName">Name of the event raiser method (null to create the name dynamically to avoid name clashed with other modules).</param>
		/// <param name="implementation">Implementation strategy to use (may be null to provide an implementation in the 'implementation' phase).</param>
		/// <returns>The added event.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// 
		/// An event raiser method is implemented depending on the event type.
		/// If the event type is <see cref="System.EventHandler"/> or <see cref="System.EventHandler{TEventArgs}"/> with
		/// TEventArgs being <see cref="System.EventArgs"/> the event raiser method will have the signature <c>void OnEvent()</c>.
		/// If the event type is <see cref="System.EventHandler{TEventArgs}"/> with more specialized TEventArgs (e.g. SpecializedEventArgs)
		/// the event raiser method will have the signature <c>void OnEvent(SpecializedEventArgs e)</c>. Any other event type will
		/// produce an event raiser method that has the same parameters as the delegate.
		/// </remarks>
		public GeneratedEvent AddStaticEvent<T>(
			string eventName,
			Visibility visibility,
			string raiserMethodName,
			IEventImplementation implementation)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(eventName);
			EnsureThatIdentifierHasNotBeenUsedYet(raiserMethodName);
			GeneratedEvent generatedEvent = new GeneratedEvent(this, EventKind.Static, typeof(T), eventName, visibility, raiserMethodName, implementation);
			mGeneratedEvents.Add(generatedEvent);
			return generatedEvent;
		}

		/// <summary>
		/// Removes the specified event from the type in creation.
		/// </summary>
		/// <param name="evnt">Event to remove.</param>
		/// <returns>
		/// true, if the event was successfully removed;
		/// false, if the specified event was not found.
		/// </returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public bool RemoveEvent(GeneratedEvent evnt)
		{
			EnsurePhase(Phase.Declaring);
			int index = mGeneratedEvents.FindIndex(x => object.ReferenceEquals(evnt, x));
			if (index < 0) return false;
			evnt.OnRemoving();
			mGeneratedEvents.RemoveAt(index);
			return true;
		}

		#endregion

		#region Handling Properties

		/// <summary>
		/// Gets inherited properties that have not been implemented/overridden, yet.
		/// </summary>
		/// <returns>Properties that still need an override.</returns>
		public InheritedProperty[] GetAbstractProperties()
		{
			List<InheritedProperty> abstractProperties = new List<InheritedProperty>();
			foreach (InheritedProperty property in mInheritedProperties.Where(x => x.Kind == PropertyKind.Abstract))
			{
				GeneratedProperty overrider = mGeneratedProperties.Find(x => x.Kind == PropertyKind.Override && x.Name == property.Name);
				if (overrider == null) abstractProperties.Add(property);
			}

			return abstractProperties.ToArray();
		}

		/// <summary>
		/// Adds a property.
		/// </summary>
		/// <param name="name">Name of the property.</param>
		/// <param name="type">Type of the property.</param>
		/// <param name="kind">
		/// Kind of the property to add. May be one of the following:
		/// - <see cref="PropertyKind.Static"/>
		/// - <see cref="PropertyKind.Normal"/>
		/// - <see cref="PropertyKind.Virtual"/>
		/// - <see cref="PropertyKind.Abstract"/>
		/// </param>
		/// <param name="implementation">Implementation strategy to use (may be null to provide an implementation in the 'implementation' phase).</param>
		/// <returns>The added property.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public GeneratedProperty AddProperty(string name, Type type, PropertyKind kind, IPropertyImplementation implementation)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			// ensure that a valid property kind was specified
			switch (kind)
			{
				case PropertyKind.Static:
				case PropertyKind.Normal:
				case PropertyKind.Virtual:
				case PropertyKind.Abstract:
					break;

				case PropertyKind.Override:
					throw new InvalidOperationException("This method should be used to create new properties only, overrides should be done using the CodeGenEngine.AddOverride() method.");

				default:
					throw new ArgumentException("Invalid property kind.", "kind");
			}

			// create property
			GeneratedProperty property = new GeneratedProperty(
				this,
				kind,
				type,
				name,
				implementation);

			mGeneratedProperties.Add(property);
			return property;
		}

		/// <summary>
		/// Adds an override for the specified inherited property.
		/// </summary>
		/// <param name="property">Property to add an override for.</param>
		/// <param name="implementation">Implementation strategy to use (may be null to provide an implementation in the 'implementation' phase).</param>
		/// <returns>The created property override.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public GeneratedProperty AddOverride(InheritedProperty property, IPropertyImplementation implementation)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(property.Name);

			// ensure that the property is abstract, virtual or an overrider
			switch (property.Kind)
			{
				case PropertyKind.Abstract:
				case PropertyKind.Virtual:
				case PropertyKind.Override:
					break;
				default:
					string error = string.Format("The specified property ({0}) is neither abstract, nor virtual nor an overrider.", property.Name);
					sLog.Write(LogLevel.Error, error);
					throw new CodeGenException(error);
			}

			// create overrider
			GeneratedProperty overrider = new GeneratedProperty(this, property, implementation);
			mGeneratedProperties.Add(overrider);
			return overrider;
		}

		/// <summary>
		/// Removes the specified property from the type in creation.
		/// </summary>
		/// <param name="property">Property to remove.</param>
		/// <returns>
		/// true, if the property was successfully removed;
		/// false, if the specified property was not found.
		/// </returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public bool RemoveProperty(GeneratedProperty property)
		{
			EnsurePhase(Phase.Declaring);
			int index = mGeneratedProperties.FindIndex(x => object.ReferenceEquals(property, x));
			if (index < 0) return false;
			property.OnRemoving();
			mGeneratedProperties.RemoveAt(index);
			return true;
		}

		#endregion

		#region Handling Dependency Properties

		/// <summary>
		/// Adds a dependency property.
		/// </summary>
		/// <typeparam name="T">Type of the dependency property.</typeparam>
		/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
		/// <param name="isReadOnly">
		/// true, if the dependency property should be read-only;
		/// false, if it should be read-write.
		/// </param>
		/// <returns>The added dependency property.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public GeneratedDependencyProperty AddDependencyProperty<T>(string name, bool isReadOnly)
		{
			return AddDependencyProperty(name, typeof(T), isReadOnly);
		}

		/// <summary>
		/// Adds a dependency property.
		/// </summary>
		/// <param name="name">Name of the dependency property (just the name, not with the 'Property' suffix).</param>
		/// <param name="type">Type of the dependency property.</param>
		/// <param name="isReadOnly">
		/// true, if the dependency property should be read-only;
		/// false, if it should be read-write.
		/// </param>
		/// <returns>The added dependency property.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public GeneratedDependencyProperty AddDependencyProperty(string name, Type type, bool isReadOnly)
		{
			EnsurePhase(Phase.Declaring);

			// ensure that the accessor property has not been implemented, yet
			foreach (GeneratedDependencyProperty generatedDependencyProperty in mGeneratedDependencyProperties)
			{
				if (generatedDependencyProperty.Name == name)
				{
					string error = string.Format("A dependency property with the specified name ({0}) is already implemented by the class in creation.", name);
					sLog.Write(LogLevel.Error, error);
					throw new CodeGenException(error);
				}
			}

			// implement the dependency property and its accessor
			GeneratedDependencyProperty dependencyProperty = new GeneratedDependencyProperty(
				this,
				name,
				type,
				isReadOnly);

			mGeneratedDependencyProperties.Add(dependencyProperty);

			return dependencyProperty;
		}

		#endregion

		#region Handling Methods

		/// <summary>
		/// Gets the method with the specified name and the specified parameters
		/// (works with inherited and generated methods).
		/// </summary>
		/// <param name="name">Name of the method to get.</param>
		/// <param name="parameterTypes">Types of the method's parameters.</param>
		/// <returns>The requested method; null, if the method was not found.</returns>
		public IMethod GetMethod(string name, Type[] parameterTypes)
		{
			// check whether the requested method is a generated method
			foreach (GeneratedMethod method in mGeneratedMethods)
			{
				if (method.Name == name && method.ParameterTypes.SequenceEqual(parameterTypes))
				{
					return method;
				}
			}

			// check inherited methods
			foreach (InheritedMethod method in mInheritedMethods)
			{
				if (method.Name == name && method.ParameterTypes.SequenceEqual(parameterTypes))
				{
					return method;
				}
			}

			// method was not found
			return null;
		}

		/// <summary>
		/// Adds a method to the type in creation.
		/// </summary>
		/// <param name="kind">
		/// Kind of the property to add. May be one of the following:
		/// - <see cref="MethodKind.Static"/>
		/// - <see cref="MethodKind.Normal"/>
		/// - <see cref="MethodKind.Virtual"/>
		/// - <see cref="MethodKind.Abstract"/>
		/// </param>
		/// <param name="name">Name of the method (null to generate an anonymous method).</param>
		/// <param name="returnType">Return type of the method.</param>
		/// <param name="parameterTypes">Types of the method's parameters.</param>
		/// <param name="visibility">Visibility of the method.</param>
		/// <returns>The added method.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public GeneratedMethod AddMethod(MethodKind kind, string name, Type returnType, Type[] parameterTypes, Visibility visibility)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(name);

			// ensure that a valid property kind was specified
			switch (kind)
			{
				case MethodKind.Static:
				case MethodKind.Normal:
				case MethodKind.Virtual:
				case MethodKind.Abstract:
					break;

				case MethodKind.Override:
					throw new InvalidOperationException("This method should be used to create new methods only, overrides should be done using the CodeGenEngine.AddOverride() method.");

				default:
					throw new ArgumentException("Invalid method kind.", "kind");
			}

			// create method
			GeneratedMethod method = new GeneratedMethod(
				this,
				kind,
				name,
				returnType,
				parameterTypes,
				visibility);

			mGeneratedMethods.Add(method);
			return method;
		}

		/// <summary>
		/// Adds an override for an inherited method.
		/// </summary>
		/// <param name="method">Method to override.</param>
		/// <returns>The added method override.</returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public GeneratedMethod AddOverride(InheritedMethod method)
		{
			EnsurePhase(Phase.Declaring);
			EnsureThatIdentifierHasNotBeenUsedYet(method.Name);

			// ensure that the property is abstract, virtual or an overrider
			switch (method.Kind)
			{
				case MethodKind.Abstract:
				case MethodKind.Virtual:
				case MethodKind.Override:
					break;
				default:
					string error = string.Format("The specified method ({0}) is neither abstract, nor virtual nor an overrider.", method.Name);
					sLog.Write(LogLevel.Error, error);
					throw new CodeGenException(error);
			}

			// create method
			GeneratedMethod overriddenMethod = new GeneratedMethod(this, method);
			mGeneratedMethods.Add(overriddenMethod);
			return overriddenMethod;
		}

		/// <summary>
		/// Removes the specified method from the type in creation.
		/// </summary>
		/// <param name="method">Method to remove.</param>
		/// <returns>
		/// true, if the method was successfully removed;
		/// false, if the specified method was not found.
		/// </returns>
		/// <remarks>
		/// This method must be called in the 'declaration' phase only.
		/// </remarks>
		public bool RemoveMethod(GeneratedMethod method)
		{
			EnsurePhase(Phase.Declaring);
			int index = mGeneratedMethods.FindIndex(x => object.ReferenceEquals(method, x));
			if (index < 0) return false;
			mGeneratedMethods.RemoveAt(index);
			return true;
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Ensures that the engine is in the specified phase.
		/// </summary>
		/// <param name="phase">Phase to check for.</param>
		/// <exception cref="CodeGenException">The engine is not in the specified phase.</exception>
		private void EnsurePhase(Phase phase)
		{
			if (mPhase != phase)
			{
				string error = string.Format("The code generation engine is not in the '{0}' state.", phase);
				throw new CodeGenException(error);
			}
		}

		/// <summary>
		/// Ensures that the specified identifier (field, property, method) has not been used, yet.
		/// </summary>
		/// <param name="identifier">Name of the identifier to check.</param>
		/// <exception cref="CodeGenException">The identifier with the specified name has already been declared.</exception>
		private void EnsureThatIdentifierHasNotBeenUsedYet(string identifier)
		{
			if (identifier == null) return; // null means that a unique name is choosen => no conflict...

			foreach (IGeneratedFieldInternal field in mGeneratedFields)
			{
				if (field.Name == identifier)
				{
					string error = string.Format("The specified identifier ({0}) has already been used to declare a field.", identifier);
					sLog.Write(LogLevel.Error, error);
					throw new CodeGenException(error);
				}
			}

			foreach (GeneratedEvent evnt in mGeneratedEvents)
			{
				if (evnt.Name == identifier)
				{
					string error = string.Format("The specified identifier ({0}) has already been used to declare an event.", identifier);
					sLog.Write(LogLevel.Error, error);
					throw new CodeGenException(error);
				}
			}

			foreach (GeneratedProperty property in mGeneratedProperties)
			{
				if (property.Name == identifier)
				{
					string error = string.Format("The specified identifier ({0}) has already been used to declare a property.", identifier);
					sLog.Write(LogLevel.Error, error);
					throw new CodeGenException(error);
				}
			}

			foreach (GeneratedMethod method in mGeneratedMethods)
			{
				if (method.Name == identifier)
				{
					string error = string.Format("The specified identifier ({0}) has already been used to declare a method.", identifier);
					sLog.Write(LogLevel.Error, error);
					throw new CodeGenException(error);
				}
			}
		}

		#endregion
	}
}
