///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

// ReSharper disable ArrangeMethodOrOperatorBody
// ReSharper disable EventNeverSubscribedTo.Local
// ReSharper disable InconsistentNaming
// ReSharper disable PublicConstructorInAbstractClass
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0067  // Event is never used
#pragma warning disable CS0169  // Field is never used
#pragma warning disable CS0649  // Field is never assigned to, and will always have its default value

namespace GriffinPlus.Lib.CodeGeneration.Tests;

/// <summary>
/// Base class for dynamically created classes.
/// The class contains abstract members, therefore it is not instantiable.
/// </summary>
public abstract class TestBaseClass_Abstract : ITestBaseClass
{
	/// <summary>
	/// The argument passed to <see cref="TestBaseClass_Abstract(int)"/> or <see cref="TestBaseClass_Abstract(string)"/>.
	/// </summary>
	// ReSharper disable once MemberInitializerValueIgnored
	public object ConstructorArgument { get; set; } = "Unset";

	public             int Field_Public;
	protected internal int Field_ProtectedInternal;
	protected          int Field_Protected;
	internal           int Field_Internal;
	private            int Field_Private;

	// internal abstract event        EventHandler<EventArgs> Event_Abstract_Internal;          // cannot be overridden in a derived class due to accessibility issues
	public abstract event             EventHandler<EventArgs> Event_Abstract_Public;
	protected internal abstract event EventHandler<EventArgs> Event_Abstract_ProtectedInternal;
	protected abstract event          EventHandler<EventArgs> Event_Abstract_Protected;
	public event                      EventHandler<EventArgs> Event_Normal_Public;
	protected internal event          EventHandler<EventArgs> Event_Normal_ProtectedInternal;
	protected event                   EventHandler<EventArgs> Event_Normal_Protected;
	internal event                    EventHandler<EventArgs> Event_Normal_Internal;
	private event                     EventHandler<EventArgs> Event_Normal_Private;
	public virtual event              EventHandler<EventArgs> Event_Virtual_Public;
	protected internal virtual event  EventHandler<EventArgs> Event_Virtual_ProtectedInternal;
	protected virtual event           EventHandler<EventArgs> Event_Virtual_Protected;
	internal virtual event            EventHandler<EventArgs> Event_Virtual_Internal;
	public static event               EventHandler<EventArgs> Event_Static_Public;
	protected internal static event   EventHandler<EventArgs> Event_Static_ProtectedInternal;
	protected static event            EventHandler<EventArgs> Event_Static_Protected;
	internal static event             EventHandler<EventArgs> Event_Static_Internal;
	private static event              EventHandler<EventArgs> Event_Static_Private;

	// internal abstract        int Property_Abstract_Internal          { get; set; } // cannot be overridden in a derived class due to accessibility issues
	public abstract             int Property_Abstract_Public            { get; set; }
	protected internal abstract int Property_Abstract_ProtectedInternal { get; set; }
	protected abstract          int Property_Abstract_Protected         { get; set; }
	public                      int Property_Normal_Public              { get; set; }
	protected internal          int Property_Normal_ProtectedInternal   { get; set; }
	protected                   int Property_Normal_Protected           { get; set; }
	internal                    int Property_Normal_Internal            { get; set; }
	private                     int Property_Normal_Private             { get; set; }
	public virtual              int Property_Virtual_Public             { get; set; }
	protected internal virtual  int Property_Virtual_ProtectedInternal  { get; set; }
	protected virtual           int Property_Virtual_Protected          { get; set; }
	internal virtual            int Property_Virtual_Internal           { get; set; }
	public static               int Property_Static_Public              { get; set; }
	protected internal static   int Property_Static_ProtectedInternal   { get; set; }
	protected static            int Property_Static_Protected           { get; set; }
	internal static             int Property_Static_Internal            { get; set; }
	private static              int Property_Static_Private             { get; set; }

	// internal abstract        int Method_Abstract_Internal(int x);             // cannot be overridden in a derived class due to accessibility issues
	public abstract             int Method_Abstract_Public(int            x);
	protected internal abstract int Method_Abstract_ProtectedInternal(int x);
	protected abstract          int Method_Abstract_Protected(int         x);
	public                      int Method_Normal_Public(int              x) => x;
	protected internal          int Method_Normal_ProtectedInternal(int   x) => x;
	protected                   int Method_Normal_Protected(int           x) => x;
	internal                    int Method_Normal_Internal(int            x) => x;
	private                     int Method_Normal_Private(int             x) => x;
	public virtual              int Method_Virtual_Public(int             x) => x;
	protected internal virtual  int Method_Virtual_ProtectedInternal(int  x) => x;
	protected virtual           int Method_Virtual_Protected(int          x) => x;
	internal virtual            int Method_Virtual_Internal(int           x) => x;
	public static               int Method_Static_Public(int              x) => x;
	protected internal static   int Method_Static_ProtectedInternal(int   x) => x;
	protected static            int Method_Static_Protected(int           x) => x;
	internal static             int Method_Static_Internal(int            x) => x;
	private static              int Method_Static_Private(int             x) => x;

	// -----------------------------------------------------------------------------------------------
	// constructors with different visibilities
	// -----------------------------------------------------------------------------------------------

	public TestBaseClass_Abstract()
	{
		ConstructorArgument = null;
	}

	public TestBaseClass_Abstract(ParameterType_Public x)
	{
		ConstructorArgument = x;
	}

	protected internal TestBaseClass_Abstract(ParameterType_ProtectedInternal x)
	{
		ConstructorArgument = x;
	}

	protected TestBaseClass_Abstract(ParameterType_Protected x)
	{
		ConstructorArgument = x;
	}

	internal TestBaseClass_Abstract(ParameterType_Internal x)
	{
		ConstructorArgument = x;
	}

	private TestBaseClass_Abstract(ParameterType_Private x)
	{
		ConstructorArgument = x;
	}

	// -----------------------------------------------------------------------------------------------
	// constructors for testing passing arguments
	// -----------------------------------------------------------------------------------------------

	public TestBaseClass_Abstract(int value) // value type argument
	{
		ConstructorArgument = value;
	}

	public TestBaseClass_Abstract(string value) // reference type argument
	{
		ConstructorArgument = value;
	}
}

#pragma warning restore CS0649  // Field is never assigned to, and will always have its default value
#pragma warning restore CS0169  // Field is never used
#pragma warning restore CS0067  // Event is never used
#pragma warning restore IDE0051 // Remove unused private members
