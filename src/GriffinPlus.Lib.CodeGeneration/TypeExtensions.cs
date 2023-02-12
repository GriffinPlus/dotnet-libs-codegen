﻿///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Extension methods for <see cref="System.Type"/>.
	/// </summary>
	public static class TypeExtensions
	{
		/// <summary>
		/// Gets all public properties of the specified type.
		/// </summary>
		/// <param name="type">Type to get all public properties from.</param>
		/// <returns>All properties of the specified type.</returns>
		public static PropertyInfo[] GetPublicProperties(this Type type)
		{
			if (type.IsInterface)
			{
				var propertyInfos = new List<PropertyInfo>();

				var considered = new HashSet<Type>();
				var queue = new Queue<Type>();
				considered.Add(type);
				queue.Enqueue(type);
				while (queue.Count > 0)
				{
					Type subType = queue.Dequeue();
					foreach (Type subInterface in subType.GetInterfaces())
					{
						if (considered.Contains(subInterface)) continue;
						considered.Add(subInterface);
						queue.Enqueue(subInterface);
					}

					PropertyInfo[] typeProperties = subType.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
					IEnumerable<PropertyInfo> newPropertyInfos = typeProperties.Where(x => !propertyInfos.Contains(x));
					propertyInfos.InsertRange(0, newPropertyInfos);
				}

				return propertyInfos.ToArray();
			}

			return type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
		}

		/// <summary>
		/// Gets all public methods of the specified type.
		/// </summary>
		/// <param name="type">Type to get all public methods from.</param>
		/// <returns>All methods of the specified type.</returns>
		public static MethodInfo[] GetPublicMethods(this Type type)
		{
			if (type.IsInterface)
			{
				var methodInfos = new List<MethodInfo>();

				var considered = new HashSet<Type>();
				var queue = new Queue<Type>();
				considered.Add(type);
				queue.Enqueue(type);
				while (queue.Count > 0)
				{
					Type subType = queue.Dequeue();
					foreach (Type subInterface in subType.GetInterfaces())
					{
						if (considered.Contains(subInterface)) continue;
						considered.Add(subInterface);
						queue.Enqueue(subInterface);
					}

					MethodInfo[] typeMethods = subType.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
					IEnumerable<MethodInfo> newMethodInfos = typeMethods.Where(x => !methodInfos.Contains(x) && !x.IsSpecialName);
					methodInfos.InsertRange(0, newMethodInfos);
				}

				return methodInfos.ToArray();
			}

			return type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
		}

		/// <summary>
		/// Determines whether the specified type is a subclass of the specified generic class.
		/// </summary>
		/// <param name="typeToCheck">Type to check.</param>
		/// <param name="genericType">Generic class to check for.</param>
		/// <returns>
		/// <c>true</c>, if the specified type derives from the specified generic type;
		/// otherwise <c>false</c>.
		/// </returns>
		public static bool IsSubclassOfRawGeneric(this Type typeToCheck, Type genericType)
		{
			while (typeToCheck != null && typeToCheck != typeof(object))
			{
				Type currentType = typeToCheck.IsGenericType ? typeToCheck.GetGenericTypeDefinition() : typeToCheck;
				if (genericType == currentType) return true;
				typeToCheck = typeToCheck.BaseType;
			}

			return false;
		}
	}

}
