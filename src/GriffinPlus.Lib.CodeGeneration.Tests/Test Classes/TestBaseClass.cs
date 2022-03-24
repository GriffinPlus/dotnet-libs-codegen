///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-codegen)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

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

namespace GriffinPlus.Lib.CodeGeneration.Tests
{

	/// <summary>
	/// Base class for dynamically created classes.
	/// The class does not contain abstract members, therefore it is instantiable.
	/// </summary>
	public class TestBaseClass
	{
		/// <summary>
		/// Dummy type for parameter for constructors that are public
		/// (allows to see the type of the constructor in the debugger at the first glance).
		/// </summary>
		public enum ParameterType_Public { }

		/// <summary>
		/// Dummy type for parameter for constructors that are protected internal
		/// (allows to see the type of the constructor in the debugger at the first glance).
		/// </summary>
		public enum ParameterType_ProtectedInternal { }

		/// <summary>
		/// Dummy type for parameter for constructors that are protected
		/// (allows to see the type of the constructor in the debugger at the first glance).
		/// </summary>
		public enum ParameterType_Protected { }

		/// <summary>
		/// Dummy type for parameter for constructors that are internal
		/// (allows to see the type of the constructor in the debugger at the first glance).
		/// </summary>
		public enum ParameterType_Internal { }

		/// <summary>
		/// Dummy type for parameter for constructors that are private
		/// (allows to see the type of the constructor in the debugger at the first glance).
		/// </summary>
		public enum ParameterType_Private { }

		public             int Field_Public;
		protected internal int Field_ProtectedInternal;
		protected          int Field_Protected;
		internal           int Field_Internal;
		private            int Field_Private;

		// public abstract event             EventHandler<EventArgs> Event_Abstract_Public;
		// protected internal abstract event EventHandler<EventArgs> Event_Abstract_ProtectedInternal;
		// protected abstract event          EventHandler<EventArgs> Event_Abstract_Protected;
		// internal abstract event           EventHandler<EventArgs> Event_Abstract_Internal;
		public event                     EventHandler<EventArgs> Event_Normal_Public;
		protected internal event         EventHandler<EventArgs> Event_Normal_ProtectedInternal;
		protected event                  EventHandler<EventArgs> Event_Normal_Protected;
		internal event                   EventHandler<EventArgs> Event_Normal_Internal;
		private event                    EventHandler<EventArgs> Event_Normal_Private;
		public virtual event             EventHandler<EventArgs> Event_Virtual_Public;
		protected internal virtual event EventHandler<EventArgs> Event_Virtual_ProtectedInternal;
		protected virtual event          EventHandler<EventArgs> Event_Virtual_Protected;
		internal virtual event           EventHandler<EventArgs> Event_Virtual_Internal;
		public static event              EventHandler<EventArgs> Event_Static_Public;
		protected internal static event  EventHandler<EventArgs> Event_Static_ProtectedInternal;
		protected static event           EventHandler<EventArgs> Event_Static_Protected;
		internal static event            EventHandler<EventArgs> Event_Static_Internal;
		private static event             EventHandler<EventArgs> Event_Static_Private;

		// public abstract             int Property_Abstract_Public            { get; set; }
		// protected internal abstract int Property_Abstract_ProtectedInternal { get; set; }
		// protected abstract          int Property_Abstract_Protected         { get; set; }
		// internal abstract           int Property_Abstract_Internal          { get; set; }
		public                     int Property_Normal_Public             { get; set; }
		protected internal         int Property_Normal_ProtectedInternal  { get; set; }
		protected                  int Property_Normal_Protected          { get; set; }
		internal                   int Property_Normal_Internal           { get; set; }
		private                    int Property_Normal_Private            { get; set; }
		public virtual             int Property_Virtual_Public            { get; set; }
		protected internal virtual int Property_Virtual_ProtectedInternal { get; set; }
		protected virtual          int Property_Virtual_Protected         { get; set; }
		internal virtual           int Property_Virtual_Internal          { get; set; }
		public static              int Property_Static_Public             { get; set; }
		protected internal static  int Property_Static_ProtectedInternal  { get; set; }
		protected static           int Property_Static_Protected          { get; set; }
		internal static            int Property_Static_Internal           { get; set; }
		private static             int Property_Static_Private            { get; set; }

		// public abstract             void Method_Abstract_Public();
		// protected internal abstract void Method_Abstract_ProtectedInternal();
		// protected abstract          void Method_Abstract_Protected();
		// internal abstract           void Method_Abstract_Internal();
		public                     void Method_Normal_Public()             { }
		protected internal         void Method_Normal_ProtectedInternal()  { }
		protected                  void Method_Normal_Protected()          { }
		internal                   void Method_Normal_Internal()           { }
		private                    void Method_Normal_Private()            { }
		public virtual             void Method_Virtual_Public()            { }
		protected internal virtual void Method_Virtual_ProtectedInternal() { }
		protected virtual          void Method_Virtual_Protected()         { }
		internal virtual           void Method_Virtual_Internal()          { }
		public static              void Method_Static_Public()             { }
		protected internal static  void Method_Static_ProtectedInternal()  { }
		protected static           void Method_Static_Protected()          { }
		internal static            void Method_Static_Internal()           { }
		private static             void Method_Static_Private()            { }

		public TestBaseClass() { }
		public TestBaseClass(ParameterType_Public                        x) { }
		protected internal TestBaseClass(ParameterType_ProtectedInternal x) { }
		protected TestBaseClass(ParameterType_Protected                  x) { }
		internal TestBaseClass(ParameterType_Internal                    x) { }
		private TestBaseClass(ParameterType_Private                      x) { }
	}

}

#pragma warning restore CS0649
#pragma warning restore CS0169
#pragma warning restore CS0067  // Event is never used
#pragma warning restore IDE0051 // Remove unused private members
