///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GriffinPlus.Lib.CodeGeneration.Demo.ViewModelWizard;

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
