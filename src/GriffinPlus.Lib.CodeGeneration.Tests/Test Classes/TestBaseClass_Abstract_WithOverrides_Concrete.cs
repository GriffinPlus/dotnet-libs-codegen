///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

// ReSharper disable ArrangeMethodOrOperatorBody
// ReSharper disable RedundantBaseConstructorCall
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

#pragma warning disable CS0067  // Event is never used
#pragma warning disable IDE0060 // Remove unused parameter

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Base class for dynamically created classes.
/// This class overrides abstract and virtual events, properties and methods.
/// </summary>
public class TestBaseClass_Abstract_WithOverrides_Concrete : TestBaseClass_Abstract
{
	// internal override event        EventHandler Event_Abstract_Internal;          // cannot be overridden in deriving class due to accessibility issues
	public override event             EventHandler Event_Abstract_Public;
	protected internal override event EventHandler Event_Abstract_ProtectedInternal;
	protected override event          EventHandler Event_Abstract_Protected;
	public override event             EventHandler Event_Virtual_Public;
	protected internal override event EventHandler Event_Virtual_ProtectedInternal;
	protected override event          EventHandler Event_Virtual_Protected;
	internal override event           EventHandler Event_Virtual_Internal;

	// internal override        int Property_Abstract_Internal          { get; set; } // cannot be overridden in deriving class due to accessibility issues
	public override             int Property_Abstract_Public            { get; set; }
	protected internal override int Property_Abstract_ProtectedInternal { get; set; }
	protected override          int Property_Abstract_Protected         { get; set; }
	public override             int Property_Virtual_Public             { get; set; }
	protected internal override int Property_Virtual_ProtectedInternal  { get; set; }
	protected override          int Property_Virtual_Protected          { get; set; }
	internal override           int Property_Virtual_Internal           { get; set; }

	// internal override        int Method_Abstract_Internal(int x)          => x // cannot be overridden in deriving class due to accessibility issues
	public override             int Method_Abstract_Public(int            x) => x;
	protected internal override int Method_Abstract_ProtectedInternal(int x) => x;
	protected override          int Method_Abstract_Protected(int         x) => x;
	public override             int Method_Virtual_Public(int             x) => x;
	protected internal override int Method_Virtual_ProtectedInternal(int  x) => x;
	protected override          int Method_Virtual_Protected(int          x) => x;
	internal override           int Method_Virtual_Internal(int           x) => x;

	// -----------------------------------------------------------------------------------------------
	// constructors with different visibilities
	// -----------------------------------------------------------------------------------------------

	public TestBaseClass_Abstract_WithOverrides_Concrete() : base() { }
	public TestBaseClass_Abstract_WithOverrides_Concrete(ParameterType_Public                        x) : base(x) { }
	protected internal TestBaseClass_Abstract_WithOverrides_Concrete(ParameterType_ProtectedInternal x) : base(x) { }
	protected TestBaseClass_Abstract_WithOverrides_Concrete(ParameterType_Protected                  x) : base(x) { }
	internal TestBaseClass_Abstract_WithOverrides_Concrete(ParameterType_Internal                    x) : base(x) { }
	private TestBaseClass_Abstract_WithOverrides_Concrete(ParameterType_Private                      x) : base() { }

	// -----------------------------------------------------------------------------------------------
	// constructors for testing passing arguments
	// -----------------------------------------------------------------------------------------------

	public TestBaseClass_Abstract_WithOverrides_Concrete(int    value) : base(value) { } // value type argument
	public TestBaseClass_Abstract_WithOverrides_Concrete(string value) : base(value) { } // reference type argument
}

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS0067  // Event is never used
