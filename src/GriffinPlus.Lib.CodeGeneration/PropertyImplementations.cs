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

namespace GriffinPlus.Lib.CodeGeneration
{
	/// <summary>
	/// Collection of predefined property implementation strategies.
	/// </summary>
	public static class PropertyImplementations
	{
		/// <summary>
		/// Property implementation that simply backs the property with a field.
		/// </summary>
		public static PropertyImplementation_Simple Simple
		{
			get { return new PropertyImplementation_Simple(); }
		}

		/// <summary>
		/// Property implementation that backs the property with a field and calls the 'OnPropertyChanged'
		/// method of the base class after changing the property value.
		/// </summary>
		public static PropertyImplementation_SetterWithPropertyChanged SetterWithPropertyChanged
		{
			get { return new PropertyImplementation_SetterWithPropertyChanged(); }
		}

		/// <summary>
		/// Property implementation that provides access to the specified dependency property.
		/// </summary>
		/// <param name="dp">Dependency property to access via the property.</param>
		public static PropertyImplementation_DependencyProperty DependencyPropertyAccessor(GeneratedDependencyProperty dp)
		{
			return new PropertyImplementation_DependencyProperty(dp);
		}
	}
}
