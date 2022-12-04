﻿#pragma warning disable IDE0240
#nullable enable
#pragma warning restore IDE0240
using System;
using System.Diagnostics;
namespace EasyCSharp.GeneratorTools;

enum PropertyVisibility : byte { Default = default, Public, Protected, Private, DoNotGenerate }

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
class AddAttributeConverterAttribute : Attribute
{
    public AddAttributeConverterAttribute(Type AttributeType) { }
    public string? MethodName { get; set; }
    public string? ParametersAsString { get; set; }
}