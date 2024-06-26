﻿///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

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
/// The class does not contain abstract members, therefore it is instantiable.
/// </summary>
public class TestBaseClass_BaseClassMembersHidden : TestBaseClass
{
	public new             int Field_Public;
	protected internal new int Field_ProtectedInternal;
	protected new          int Field_Protected;
	internal new           int Field_Internal;
	private                int Field_Private; // new not needed, private members are not hidden as not visible to derived classes

	public new event                     EventHandler<EventArgs> Event_Normal_Public;
	protected internal new event         EventHandler<EventArgs> Event_Normal_ProtectedInternal;
	protected new event                  EventHandler<EventArgs> Event_Normal_Protected;
	internal new event                   EventHandler<EventArgs> Event_Normal_Internal;
	private event                        EventHandler<EventArgs> Event_Normal_Private; // new not needed, private events are not hidden as not visible to derived classes
	public new virtual event             EventHandler<EventArgs> Event_Virtual_Public;
	protected internal new virtual event EventHandler<EventArgs> Event_Virtual_ProtectedInternal;
	protected new virtual event          EventHandler<EventArgs> Event_Virtual_Protected;
	internal new virtual event           EventHandler<EventArgs> Event_Virtual_Internal;
	public new static event              EventHandler<EventArgs> Event_Static_Public;
	protected internal new static event  EventHandler<EventArgs> Event_Static_ProtectedInternal;
	protected new static event           EventHandler<EventArgs> Event_Static_Protected;
	internal new static event            EventHandler<EventArgs> Event_Static_Internal;
	private static event                 EventHandler<EventArgs> Event_Static_Private; // new not needed, private events are not hidden as not visible to derived classes

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

	public new                     void Method_Normal_Public()             { }
	protected internal new         void Method_Normal_ProtectedInternal()  { }
	protected new                  void Method_Normal_Protected()          { }
	internal new                   void Method_Normal_Internal()           { }
	private                        void Method_Normal_Private()            { } // new not needed, private methods are not hidden as not visible to derived classes
	public new virtual             void Method_Virtual_Public()            { }
	protected internal new virtual void Method_Virtual_ProtectedInternal() { }
	protected new virtual          void Method_Virtual_Protected()         { }
	internal new virtual           void Method_Virtual_Internal()          { }
	public new static              void Method_Static_Public()             { }
	protected internal new static  void Method_Static_ProtectedInternal()  { }
	protected new static           void Method_Static_Protected()          { }
	internal new static            void Method_Static_Internal()           { }
	private static                 void Method_Static_Private()            { } // new not needed, private methods are not hidden as not visible to derived classes

	public TestBaseClass_BaseClassMembersHidden() : base() { }
	public TestBaseClass_BaseClassMembersHidden(ParameterType_Public                        x) : base(x) { }
	protected internal TestBaseClass_BaseClassMembersHidden(ParameterType_ProtectedInternal x) : base(x) { }
	protected TestBaseClass_BaseClassMembersHidden(ParameterType_Protected                  x) : base(x) { }
	internal TestBaseClass_BaseClassMembersHidden(ParameterType_Internal                    x) : base(x) { }
	private TestBaseClass_BaseClassMembersHidden(ParameterType_Private                      x) : base() { }
}

#pragma warning restore CS0649
#pragma warning restore CS0169
#pragma warning restore CS0067  // Event is never used
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0051 // Remove unused private members
