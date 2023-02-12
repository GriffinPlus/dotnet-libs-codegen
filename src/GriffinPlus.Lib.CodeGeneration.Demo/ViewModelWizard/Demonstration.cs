///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration.Demo.ViewModelWizard
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
			Console.WriteLine("--- View Model Wizard Demonstration ---");

			// let the view model wizard implement the abstract custom view model
			var viewModel = CreateViewModel<CustomViewModel>();

			// register 'PropertyChanged' event to get notified when a property changes
			viewModel.PropertyChanged += (sender, args) => Console.WriteLine($"Property '{args.PropertyName}' has changed.");

			// play with the manually implemented property
			Console.WriteLine();
			Console.WriteLine("Now playing with the manually implemented property.");
			Console.WriteLine($"Initial value of the '{nameof(CustomViewModel.ManuallyImplementedProperty)}' property: {viewModel.ManuallyImplementedProperty ?? "<null>"}");
			Console.WriteLine($"Setting the value of the '{nameof(CustomViewModel.ManuallyImplementedProperty)}' property (should raise the PropertyChanged event)...");
			viewModel.ManuallyImplementedProperty = "A String";
			Console.WriteLine($"Value of the '{nameof(CustomViewModel.ManuallyImplementedProperty)}' property: {viewModel.ManuallyImplementedProperty ?? "<null>"}");
			Console.WriteLine($"Setting the value of the '{nameof(CustomViewModel.ManuallyImplementedProperty)}' property to the same value once again (should not raise the PropertyChanged event)...");
			viewModel.ManuallyImplementedProperty = "A String";

			// play with the property implemented by the view model wizard
			Console.WriteLine();
			Console.WriteLine("Now playing with the property implemented by the view model wizard.");
			Console.WriteLine($"Initial value of the '{nameof(CustomViewModel.AutomaticallyImplementedProperty)}' property: {viewModel.AutomaticallyImplementedProperty ?? "<null>"}");
			Console.WriteLine($"Setting the value of the '{nameof(CustomViewModel.AutomaticallyImplementedProperty)}' property.");
			viewModel.AutomaticallyImplementedProperty = "A String";
			Console.WriteLine($"Value of the '{nameof(CustomViewModel.AutomaticallyImplementedProperty)}' property: {viewModel.AutomaticallyImplementedProperty ?? "<null>"}");
			Console.WriteLine($"Setting the value of the '{nameof(CustomViewModel.AutomaticallyImplementedProperty)}' property to the same value once again (should not raise the PropertyChanged event)...");
			viewModel.AutomaticallyImplementedProperty = "A String";
		}

		/// <summary>
		/// Takes the specified view model class with abstract properties, derives a class from it and implements these properties.
		/// The properties are backed by a member field. The 'set' accessor calls the <see cref="ViewModelBase.OnPropertyChanged"/>
		/// method to notify clients that the property has changed, if the value of the property has changed. The implementation
		/// uses <see cref="object.Equals(object,object)"/> to determine whether the the value has changed.
		/// </summary>
		/// <typeparam name="TViewModel">The view model class to create.</typeparam>
		/// <returns>The created view model.</returns>
		public static TViewModel CreateViewModel<TViewModel>() where TViewModel : ViewModelBase
		{
			// create a new class definition deriving from the ViewModelBase class providing common functionality
			var definition = new ClassDefinition(typeof(TViewModel));

			// let the derived class pass through all constructors defined in the base class, if any
			// (this ensures that the derived class can be created just as the base class)
			definition.AddPassThroughConstructors();

			// let the derived class implement all abstract properties using an implementation strategy that adds a backing
			// field for the property value and implements the get/set accessor methods as declared by the abstract property
			// in the abstract view model class
			foreach (IInheritedProperty property in definition.GetAbstractPropertiesWithoutOverride())
			{
				property.Override(PropertyImplementations.SetterWithPropertyChanged);
			}

			// create the derived view model type and an instance of it using the parameterless constructor
			// (to use constructors with parameters, simply pass them to the activator)
			Type viewModelType = definition.CreateType();
			return (TViewModel)Activator.CreateInstance(viewModelType);
		}
	}

}
