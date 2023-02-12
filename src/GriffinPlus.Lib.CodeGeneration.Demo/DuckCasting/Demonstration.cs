///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;
using System.Reflection.Emit;

namespace GriffinPlus.Lib.CodeGeneration.Demo.DuckCasting
{

	/// <summary>
	/// The View Model Wizard.
	/// </summary>
	public static class Demonstration
	{
		/// <summary>
		/// Demonstrates the view model wizard.
		/// </summary>
		public static void Demonstrate()
		{
			Console.WriteLine("--- Duck Casting Demonstration ---");

			var duck = DuckCast<IDuck>(typeof(Chimera));
			Console.WriteLine($"This is a duck, it is called {duck.Name}.");
			Console.Write("When it makes sounds, it sounds like... ");
			duck.Quack();
			duck.Walk();

			var cat = DuckCast<ICat>(typeof(Chimera));
			Console.WriteLine($"This is a cat, it is called {cat.Name}.");
			Console.Write("When it makes sounds, it sounds like... ");
			cat.Meow();
			cat.Walk();

			var dog = DuckCast<IDog>(typeof(Chimera));
			Console.WriteLine($"This is a duck, it is called {duck.Name}.");
			Console.Write("When it makes sounds, it sounds like... ");
			dog.Bark();
			dog.Walk();
		}

		/// <summary>
		/// Takes the specified base class, dynamically derives a class from it and lets the class implement the
		/// specified interface, so base class members can be accessed via the interface.
		/// </summary>
		/// <param name="baseClass">
		/// The base class the dynamically created class should inherit from.
		/// This class must implement all members defined in <typeparamref name="TInterface"/>.
		/// </param>
		/// <typeparam name="TInterface">
		/// The interface defining members to access on <paramref name="baseClass"/>.
		/// </typeparam>
		/// <returns>
		/// An instance of the dynamically created class that inherits from <paramref name="baseClass"/> and implements
		/// the interface <typeparamref name="TInterface"/>.
		/// </returns>
		public static TInterface DuckCast<TInterface>(Type baseClass)
		{
			if (baseClass == null) throw new ArgumentNullException(nameof(baseClass));
			if (!typeof(TInterface).IsInterface) throw new ArgumentException($"{typeof(TInterface).FullName} is not an interface.");

			// create a new class definition deriving from the specified base class providing members that should be made
			// accessible via the specified interface
			var definition = new ClassDefinition(baseClass);

			// let the derived class pass through all constructors defined in the base class, if any
			// (this ensures that the derived class can be created just as the base class)
			definition.AddPassThroughConstructors();

			// add the specified interface to the type definition and wire through events, properties and methods
			definition.AddImplementedInterface<TInterface>();

			foreach (var property in definition.GetPropertiesOfImplementedInterfaces())
			{
				definition.AddProperty(property.PropertyType, property.Name, PropertyImplementations.Simple);
			}

			foreach (var method in definition.GetMethodsOfImplementedInterfaces())
			{
				definition.AddMethod(
					MethodKind.Virtual,
					method.Name,
					method.ReturnType,
					method.GetParameters().Select(x => x.ParameterType).ToArray(),
					Visibility.Public,
					(generatedMethod, msil) =>
					{
						msil.Emit(OpCodes.Ret);
					});
			}

			// create an instance of the derived class using the parameterless constructor
			// (to use constructors with parameters, simply pass them to the activator)
			Type createdType = definition.CreateType();
			return (TInterface)Activator.CreateInstance(createdType);
		}
	}

}
