# Griffin+ Dynamic Code Generation

[![Azure DevOps builds (branch)](https://img.shields.io/azure-devops/build/griffinplus/2f589a5e-e2ab-4c08-bee5-5356db2b2aeb/36/master?label=Build)](https://dev.azure.com/griffinplus/DotNET%20Libraries/_build/latest?definitionId=36&branchName=master)
[![Tests (master)](https://img.shields.io/azure-devops/tests/griffinplus/DotNET%20Libraries/36/master?label=Tests)](https://dev.azure.com/griffinplus/DotNET%20Libraries/_build/latest?definitionId=36&branchName=master)
[![NuGet Version](https://img.shields.io/nuget/v/GriffinPlus.Lib.CodeGeneration.svg)](https://www.nuget.org/packages/GriffinPlus.Lib.CodeGeneration)
[![NuGet Downloads](https://img.shields.io/nuget/dt/GriffinPlus.Lib.CodeGeneration.svg)](https://www.nuget.org/packages/GriffinPlus.Lib.CodeGeneration)

---
# >>> Attention: Work in Progress <<<

This project is work in progress and may change significantly in the next time. We do not recommend using it in productive applications, yet. Nevertheless you are invited to try it out and give us feedback.

---

## Overview

The Griffin+ *Dynamic Code Generation* library is set of classes that assist with creating and implementing types at runtime. It is the base to build powerful features that require code referring to types that are not known at compile time. This library takes over the implementation of repetitive tasks that differ only marginally from each other and thus reduces the error probability compared to error-prone manual implementations.

## Supported Platforms

The library is entirely written in C# using .NET Standard 2.0.

Therefore it should work on the following platforms (or higher):
- .NET Framework 4.6.1
- .NET Core 2.0
- .NET 5.0
- Mono 5.4
- Xamarin iOS 10.14
- Xamarin Mac 3.8
- Xamarin Android 8.0
- Universal Windows Platform (UWP) 10.0.16299

The library is tested automatically on the following frameworks and operating systems:
- .NET Framework 4.6.1 (Windows Server 2019)
- .NET Core 3.1 (Windows Server 2019 and Ubuntu 20.04)
- .NET 5.0 (Windows Server 2019 and Ubuntu 20.04)

## Usage

*TODO*

## Examples

The following examples show how the Griffin+ *Dynamic Code Generation* library can be used. The source code is available in the demo project contained in the repository as well.

### View Model Wizard

When working with the *Windows Presentation Foundation (WPF)* or similar UI frameworks it is often desirable to separate UI logic from data. Using the *Model-View-ViewModel (MVVM)* pattern is a common technique to achieve this. UI controls are then bound to a view model that contains data to present. UI interactions can change the state of the view model and changes to the view model should update the UI, so the view model has to provide a mechanism to notify the UI of changes. A common way to achieve this is to let the view model implement the `System.ComponentModel.INotifyPropertyChanged` interface and raise its `PropertyChanged` event when a property changes. The UI can then update itself to reflect the changed view model state.

Implementing view model properties with the required behavior usually follows a common pattern:

- Add a private member field storing the property value.
- Implement a *get* accessor method returning the value of the backing field
- Implement a *set* accessor method checking whether the value to set is different from the value in the backing field and - if so - update the value in the backing field and raise the `PropertyChanged` event

For larger view models implementing this manually can become boring and error-prone. The Griffin+ *Dynamic Code Generation* library can be used to automate the implementation process. To do so, you only need to create a view model class and declare properties to implement as *abstract*.

Lets assume the view model class (`CustomViewModel`) derives from a common view model class (`ViewModelBase`) providing very basic view model stuff, e.g. the `void OnPropertyChanged(string name)` method that raises the `PropertyChanged` event. The following implementation of the view model wizard takes the abstract view model, dynamically derives a class from it and implements all abstract properties using a property implementation strategy that adds everything to make the property behave as described above.

```csharp
/// <summary>
/// The View Model Wizard.
/// </summary>
public static class ViewModelWizard
{
    /// <summary>
    /// Takes the specified view model class with abstract properties, derives a class from it and implements these properties.
    /// The properties are backed by a member field. The set accessor calls the <see cref="ViewModelBase.OnPropertyChanged"/>
    /// method to notify clients that the property has changed, if the value of the property has changed. The implementation
    /// uses <see cref="object.Equals(object,object)"/> to determine whether the the value has changed.
    /// </summary>
    /// <typeparam name="TViewModel">The view model class to create.</typeparam>
    /// <returns></returns>
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
        foreach (var property in definition.GetAbstractPropertiesWithoutOverride())
        {
            property.Override(PropertyImplementations.SetterWithPropertyChanged);
        }

        // create the derived view model type and an instance of it using the parameterless constructor
        // (to use constructors with parameters, simply pass them to the activator)
        Type viewModelType = definition.CreateType();
        return (TViewModel)Activator.CreateInstance(viewModelType);
    }
}

/// <summary>
/// Base class for view models.
/// </summary>
public class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when one of the properties of the view model has changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">Name of the property that has changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// A custom view model with abstract properties the view model wizard should implement.
/// </summary>
public abstract class CustomViewModel : ViewModelBase
{
    private string mManuallyImplementedProperty;

    /// <summary>
    /// A manually implemented property that behaves as the view model wizard would implement it.
    /// </summary>
    public string ManuallyImplementedProperty
    {
        get => mManuallyImplementedProperty;
        set
        {
            if (!Equals(mManuallyImplementedProperty, value))
            {
                mManuallyImplementedProperty = value;
                OnPropertyChanged(nameof(ManuallyImplementedProperty));
            }
        }
    }

    /// <summary>
    /// Abstract property the view model wizard should implement.
    /// </summary>
    public abstract string AutomaticallyImplementedProperty { get; set; } // may also be only 'get' or 'set'
}
```

Theoretically it is possible to generate view models out of interfaces, but usually always all view models need some custom code that is implemented in C#. That is the point where interfaces are out.