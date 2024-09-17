///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;

// ReSharper disable SuggestBaseTypeForParameter

namespace GriffinPlus.Lib.CodeGeneration.Tests;

using static Helpers;

/// <summary>
/// Common tests around the <see cref="ClassDefinition"/> class.
/// </summary>
public class ClassDefinitionTests_AbstractBaseClass
{
	#region GetAbstractPropertiesWithoutOverride()

	/// <summary>
	/// Test data for
	/// </summary>
	public static IEnumerable<object[]> GetAbstractPropertiesWithoutOverrideTestData
	{
		get
		{
			yield return [typeof(TestBaseClass_Abstract)];
			yield return [typeof(TestBaseClass_BaseClassMembersHidden_Abstract)];
		}
	}

	/// <summary>
	/// Tests the <see cref="ClassDefinition.GetAbstractPropertiesWithoutOverride"/> method.
	/// </summary>
	[Theory]
	[MemberData(nameof(GetAbstractPropertiesWithoutOverrideTestData))]
	private void GetAbstractPropertiesWithoutOverride(Type baseClass)
	{
		var definition = new ClassDefinition(baseClass, null, ClassAttributes.None);

		// check whether all abstract properties are returned without implementing any overrider...
		IInheritedProperty[] actualProperties = definition.GetAbstractPropertiesWithoutOverride();
		PropertyInfo[] actualPropertyInfos = actualProperties.Select(x => x.PropertyInfo).ToArray();
		PropertyInfo[] expectedPropertyInfos = GetInheritedProperties(baseClass, includeHidden: false).Where(IsAbstract).ToArray();
		int expectedPropertyCount = expectedPropertyInfos.Length;
		Assert.True(expectedPropertyCount >= 2);
		Assert.Equal(actualPropertyInfos, expectedPropertyInfos);

		// now override the first and the last property with a simple implementation
		// (the kind of implementation is not important here, just some accepted strategy)
		actualProperties[0].Override(PropertyImplementations.Simple);
		actualProperties[^1].Override(PropertyImplementations.Simple);
		expectedPropertyCount -= 2;

		// now override the first abstract property and check again
		// (the first and the last property should not be returned anymore)
		actualProperties = definition.GetAbstractPropertiesWithoutOverride();
		actualPropertyInfos = actualProperties.Select(x => x.PropertyInfo).ToArray();
		expectedPropertyInfos = expectedPropertyInfos.Where(IsAbstract).Skip(1).Take(expectedPropertyCount).ToArray();
		Assert.Equal(expectedPropertyCount, expectedPropertyInfos.Length);
		Assert.Equal(actualPropertyInfos, expectedPropertyInfos);
	}

	#endregion
}
