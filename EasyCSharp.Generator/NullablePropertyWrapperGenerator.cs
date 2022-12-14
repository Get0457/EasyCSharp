using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EasyCSharp;

[Generator]
public class NullablePropertyWrapperGenerator : PropertyGenerator
{
    protected override string AttributeTypeName => typeof(NullablePropertyWrapperAttribute).FullName;
    protected override string FileName(string ClaseName) => $"{ClaseName}.GeneratedNullableProperty.g.cs";
    protected override string GetLogic(IFieldSymbol fieldSymbol, string PropertyName, string Indent)
        => $"return {fieldSymbol.Name};";
    protected override string SetLogic(IFieldSymbol fieldSymbol, string PropertyName, string Indent)
        => $"{fieldSymbol.Name} = value ?? default({fieldSymbol.Type});";
    protected override string HeadLogic(IFieldSymbol fieldSymbol, string propertyName)
        => $"{(fieldSymbol.Type.IsUnmanagedType ? $"System.Nullable<{fieldSymbol.Type.ToDisplayString()}>"
            : fieldSymbol.Type.WithNullableAnnotation(NullableAnnotation.Annotated))} {propertyName}";
}