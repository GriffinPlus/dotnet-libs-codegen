///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.CodeGeneration.Demo.ViewModelWizard;

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
			if (Equals(mManuallyImplementedProperty, value)) return;
			mManuallyImplementedProperty = value;
			OnPropertyChanged(nameof(ManuallyImplementedProperty));
		}
	}

	/// <summary>
	/// Abstract property the view model wizard should implement.
	/// </summary>
	public abstract string AutomaticallyImplementedProperty { get; set; } // may also be only 'get' or 'set'
}
