# Griffin+ Dynamic Code Generation

[![Build (master)](https://img.shields.io/appveyor/ci/ravenpride/dotnet-libs-codegen/master.svg?logo=appveyor)](https://ci.appveyor.com/project/ravenpride/dotnet-libs-codegen/branch/master)
[![Tests (master)](https://img.shields.io/appveyor/tests/ravenpride/dotnet-libs-codegen/master.svg?logo=appveyor)](https://ci.appveyor.com/project/ravenpride/dotnet-libs-codegen/branch/master/tests)
[![NuGet Version](https://img.shields.io/nuget/v/GriffinPlus.Lib.CodeGeneration.svg)](https://www.nuget.org/packages/GriffinPlus.Lib.CodeGeneration)
[![NuGet Downloads](https://img.shields.io/nuget/dt/GriffinPlus.Lib.CodeGeneration.svg)](https://www.nuget.org/packages/GriffinPlus.Lib.CodeGeneration)

## Overview

The *Dynamic Code Generation* library is set of classes that assist with creating and implementing types at runtime. It is the base to build powerful features that require code referring to types that are not known at compilation time. This library takes over the implementation of repetitive tasks that differ only marginally from each other and thus reduces the error probability compared to manual implementations, since failures are often caused by copy-and-paste errors.

For possible areas of application, please see below.

## Supported Platforms

The *Dynamic Code Generation* library is entirely written in C# and should work with .NET Framework 4.6.1 or higher. Due to the excessive use of the `System.Reflection.Emit.AssemblyBuilder` class, it is *not compatible* with .NET Standard.

## Usage

TODO

## Areas of Application

The following areas of application are suggestions what you can do with the *Dynamic Code Generation* library. The library does not contain out-of-the-box examples for these use cases.

#### Dynamic Proxies
Dynamic proxies are classes that are created at runtime and imitate an existing class, that is, the proxy class provides an interface similar to the original class and delegates core functionality to the original class. The proxy class usually provides additional functionality ranging from additional locking mechanisms to accessing a remote instance of the original class (ala .NET Remoting).

#### View Models
Anyone who has worked with WPF and the MVVM pattern will have noticed that view models consist largely of properties for binding data to corresponding views. These properties are almost always implemented in the same way: a public *getter* method and (often) a *setter* method that fires the *PropertyChanged* event of the implemented *INotifyPropertyChanged* interface, which updates the view when the value of the property changes. With dynamic code generation it is possible to build abstract view models whose properties only need to be declared by name (but not implemented). Dynamic code generation allows to dynamically create a class that inherits from the abstract view model class and implements the corresponding properties as desired.
