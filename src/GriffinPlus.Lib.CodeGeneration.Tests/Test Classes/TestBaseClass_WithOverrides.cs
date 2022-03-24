///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;


// ReSharper disable RedundantBaseConstructorCall
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

#pragma warning disable CS0067  // Event is never used
#pragma warning disable IDE0060 // Remove unused parameter

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	/// <summary>
	/// Base class for dynamically created classes.
	/// This class overrides abstract and virtual events, properties and methods.
	/// </summary>
	public class TestBaseClass_WithOverrides : TestBaseClass_Abstract
	{
		public override event             EventHandler<EventArgs> Event_Abstract_Public;
		protected internal override event EventHandler<EventArgs> Event_Abstract_ProtectedInternal;
		protected override event          EventHandler<EventArgs> Event_Abstract_Protected;
		internal override event           EventHandler<EventArgs> Event_Abstract_Internal;
		public override event             EventHandler<EventArgs> Event_Virtual_Public;
		protected internal override event EventHandler<EventArgs> Event_Virtual_ProtectedInternal;
		protected override event          EventHandler<EventArgs> Event_Virtual_Protected;
		internal override event           EventHandler<EventArgs> Event_Virtual_Internal;

		public override             int Property_Abstract_Public            { get; set; }
		protected internal override int Property_Abstract_ProtectedInternal { get; set; }
		protected override          int Property_Abstract_Protected         { get; set; }
		internal override           int Property_Abstract_Internal          { get; set; }
		public override             int Property_Virtual_Public             { get; set; }
		protected internal override int Property_Virtual_ProtectedInternal  { get; set; }
		protected override          int Property_Virtual_Protected          { get; set; }
		internal override           int Property_Virtual_Internal           { get; set; }

		public override             void Method_Abstract_Public()            { }
		protected internal override void Method_Abstract_ProtectedInternal() { }
		protected override          void Method_Abstract_Protected()         { }
		internal override           void Method_Abstract_Internal()          { }
		public override             void Method_Virtual_Public()             { }
		protected internal override void Method_Virtual_ProtectedInternal()  { }
		protected override          void Method_Virtual_Protected()          { }
		internal override           void Method_Virtual_Internal()           { }

		public TestBaseClass_WithOverrides() : base() { }
		public TestBaseClass_WithOverrides(ParameterType_Public                        x) : base(x) { }
		protected internal TestBaseClass_WithOverrides(ParameterType_ProtectedInternal x) : base(x) { }
		protected TestBaseClass_WithOverrides(ParameterType_Protected                  x) : base(x) { }
		internal TestBaseClass_WithOverrides(ParameterType_Internal                    x) : base(x) { }
		private TestBaseClass_WithOverrides(ParameterType_Private                      x) : base() { }
	}

}

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS0067  // Event is never used
