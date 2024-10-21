///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

// ReSharper disable ArrangeMethodOrOperatorBody
// ReSharper disable EventNeverSubscribedTo.Local
// ReSharper disable InconsistentNaming
// ReSharper disable PublicConstructorInAbstractClass
// ReSharper disable RedundantBaseConstructorCall
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CS0067  // Event is never used
#pragma warning disable CS0169  // Field is never used
#pragma warning disable CS0649  // Field is never assigned to, and will always have its default value

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Base class for dynamically created classes.
/// This class hides fields, events, properties and methods with own members.
/// The class contains abstract members, therefore it is not instantiable.
/// </summary>
public abstract class TestBaseClass_Abstract_BaseClassMembersHidden_Abstract : TestBaseClass_Abstract
{
	public new             int Field_Public;
	protected internal new int Field_ProtectedInternal;
	protected new          int Field_Protected;
	internal new           int Field_Internal;
	private                int Field_Private; // new not needed, private members are not hidden as not visible to derived classes

	// public new abstract event             EventHandler Event_Abstract_Public;            // not allowed, would hide abstract event of base class
	// protected internal new abstract event EventHandler Event_Abstract_ProtectedInternal; // not allowed, would hide abstract event of base class
	// protected new abstract event          EventHandler Event_Abstract_Protected;         // not allowed, would hide abstract event of base class
	// internal new abstract event           EventHandler Event_Abstract_Internal;          // cannot be overridden in a derived class due to accessibility issues
	public new event                     EventHandler Event_Normal_Public;
	protected internal new event         EventHandler Event_Normal_ProtectedInternal;
	protected new event                  EventHandler Event_Normal_Protected;
	internal new event                   EventHandler Event_Normal_Internal;
	private event                        EventHandler Event_Normal_Private; // new not needed, private events are not hidden as not visible to derived classes
	public new virtual event             EventHandler Event_Virtual_Public;
	protected internal new virtual event EventHandler Event_Virtual_ProtectedInternal;
	protected new virtual event          EventHandler Event_Virtual_Protected;
	internal new virtual event           EventHandler Event_Virtual_Internal;
	public new static event              EventHandler Event_Static_Public;
	protected internal new static event  EventHandler Event_Static_ProtectedInternal;
	protected new static event           EventHandler Event_Static_Protected;
	internal new static event            EventHandler Event_Static_Internal;
	private static event                 EventHandler Event_Static_Private; // new not needed, private events are not hidden as not visible to derived classes

	// public new abstract             int Property_Abstract_Public            { get; set; } // not allowed, would hide abstract property of base class
	// protected internal new abstract int Property_Abstract_ProtectedInternal { get; set; } // not allowed, would hide abstract property of base class
	// protected new abstract          int Property_Abstract_Protected         { get; set; } // not allowed, would hide abstract property of base class
	// internal new abstract           int Property_Abstract_Internal          { get; set; } // cannot be overridden in a derived class due to accessibility issues
	public new                     int Property_Normal_Public             { get; set; }
	protected internal new         int Property_Normal_ProtectedInternal  { get; set; }
	protected new                  int Property_Normal_Protected          { get; set; }
	internal new                   int Property_Normal_Internal           { get; set; }
	private                        int Property_Normal_Private            { get; set; } // new not needed, private properties are not hidden as not visible to derived classes
	public new virtual             int Property_Virtual_Public            { get; set; }
	protected internal new virtual int Property_Virtual_ProtectedInternal { get; set; }
	protected new virtual          int Property_Virtual_Protected         { get; set; }
	internal new virtual           int Property_Virtual_Internal          { get; set; }
	public new static              int Property_Static_Public             { get; set; }
	protected internal new static  int Property_Static_ProtectedInternal  { get; set; }
	protected new static           int Property_Static_Protected          { get; set; }
	internal new static            int Property_Static_Internal           { get; set; }
	private static                 int Property_Static_Private            { get; set; } // new not needed, private properties are not hidden as not visible to derived classes

	// public new abstract             int Method_Abstract_Public(int x);               // not allowed, would hide abstract method of base class
	// protected internal new abstract int Method_Abstract_ProtectedInternal(int x);    // not allowed, would hide abstract method of base class
	// protected new abstract          int Method_Abstract_Protected(int x);            // not allowed, would hide abstract method of base class
	// internal new abstract           int Method_Abstract_Internal(int x);             // cannot be overridden in a derived class due to accessibility issues
	public new                     int Method_Normal_Public(int             x) => x;
	protected internal new         int Method_Normal_ProtectedInternal(int  x) => x;
	protected new                  int Method_Normal_Protected(int          x) => x;
	internal new                   int Method_Normal_Internal(int           x) => x;
	private                        int Method_Normal_Private(int            x) => x; // new not needed, private methods are not hidden as not visible to derived classes
	public new virtual             int Method_Virtual_Public(int            x) => x;
	protected internal new virtual int Method_Virtual_ProtectedInternal(int x) => x;
	protected new virtual          int Method_Virtual_Protected(int         x) => x;
	internal new virtual           int Method_Virtual_Internal(int          x) => x;
	public new static              int Method_Static_Public(int             x) => x;
	protected internal new static  int Method_Static_ProtectedInternal(int  x) => x;
	protected new static           int Method_Static_Protected(int          x) => x;
	internal new static            int Method_Static_Internal(int           x) => x;
	private static                 int Method_Static_Private(int            x) => x; // new not needed, private methods are not hidden as not visible to derived classes

	// -----------------------------------------------------------------------------------------------
	// constructors with different visibilities
	// -----------------------------------------------------------------------------------------------

	public TestBaseClass_Abstract_BaseClassMembersHidden_Abstract() : base() { }
	public TestBaseClass_Abstract_BaseClassMembersHidden_Abstract(ParameterType_Public                        x) : base(x) { }
	protected internal TestBaseClass_Abstract_BaseClassMembersHidden_Abstract(ParameterType_ProtectedInternal x) : base(x) { }
	protected TestBaseClass_Abstract_BaseClassMembersHidden_Abstract(ParameterType_Protected                  x) : base(x) { }
	internal TestBaseClass_Abstract_BaseClassMembersHidden_Abstract(ParameterType_Internal                    x) : base(x) { }
	private TestBaseClass_Abstract_BaseClassMembersHidden_Abstract(ParameterType_Private                      x) : base() { }

	// -----------------------------------------------------------------------------------------------
	// constructors for testing passing arguments
	// -----------------------------------------------------------------------------------------------

	public TestBaseClass_Abstract_BaseClassMembersHidden_Abstract(int    value) : base(value) { } // value type argument
	public TestBaseClass_Abstract_BaseClassMembersHidden_Abstract(string value) : base(value) { } // reference type argument
}

#pragma warning restore CS0649  // Field is never assigned to, and will always have its default value
#pragma warning restore CS0169  // Field is never used
#pragma warning restore CS0067  // Event is never used
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0051 // Remove unused private members
