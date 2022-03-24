///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.CodeGeneration
{

	/// <summary>
	/// Exception that is thrown when something goes wrong within the code generation engine.
	/// </summary>
	[Serializable]
	public class CodeGenException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CodeGenException"/> class.
		/// </summary>
		public CodeGenException() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeGenException"/> class.
		/// </summary>
		/// <param name="message">Message describing the reason why the exception is thrown.</param>
		public CodeGenException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeGenException"/> class.
		/// </summary>
		/// <param name="format">String that is used to format the final message describing the reason why the exception is thrown.</param>
		/// <param name="args">Arguments used to format the final exception message.</param>
		public CodeGenException(string format, params object[] args) :
			base(string.Format(format, args)) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeGenException"/> class.
		/// </summary>
		/// <param name="message">Message describing the reason why the exception is thrown.</param>
		/// <param name="ex">Some other exception that caused the exception to be thrown.</param>
		public CodeGenException(string message, Exception ex) : base(message, ex) { }
	}

}
