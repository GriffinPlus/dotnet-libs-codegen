///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	/// <summary>
	/// Helper class that stores objects to play with in dynamically generated types.
	/// </summary>
	public class TestDataStorage : IDisposable
	{
		private static readonly Dictionary<int, TestDataStorage> sStorageByHandle = new Dictionary<int, TestDataStorage>();
		private static          int                              sNextHandleIndex = 0;
		private readonly        Dictionary<int, object>          mObjectByHandle  = new Dictionary<int, object>();

		/// <summary>
		/// Disposes the storage removing any data associated with the instance.
		/// </summary>
		public void Dispose()
		{
			lock (sStorageByHandle)
			{
				foreach (int handle in mObjectByHandle.Select(x => x.Key))
				{
					sStorageByHandle.Remove(handle);
				}
			}
		}

		/// <summary>
		/// Adds a new piece of data to the storage.
		/// </summary>
		/// <param name="value">Object to add.</param>
		/// <returns>Handle of the added object (needed to retrieve it).</returns>
		public int Add(object value)
		{
			lock (sStorageByHandle)
			{
				int handle = sNextHandleIndex++;
				mObjectByHandle.Add(handle, value);
				sStorageByHandle.Add(handle, this);
				return handle;
			}
		}

		/// <summary>
		/// Gets the object associated with the specified object handle.
		/// </summary>
		/// <param name="handle">Handle of the object to get.</param>
		/// <returns>The object associated with the specified object handle.</returns>
		public static object Get(int handle)
		{
			lock (sStorageByHandle)
			{
				if (!sStorageByHandle.TryGetValue(handle, out var storage))
					throw new ArgumentException($"No object with handle {handle} was found.");

				return storage.mObjectByHandle[handle];
			}
		}

		/// <summary>
		/// Sets the object with the specified object handle.
		/// </summary>
		/// <param name="handle">Handle of the object to set.</param>
		/// <param name="value">Value to set.</param>
		public static void Set(int handle, object value)
		{
			lock (sStorageByHandle)
			{
				if (!sStorageByHandle.TryGetValue(handle, out var storage))
					throw new ArgumentException($"No object with handle {handle} was found.");

				storage.mObjectByHandle[handle] = value;
			}
		}
	}

}
