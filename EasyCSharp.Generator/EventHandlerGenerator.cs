using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using EasyCSharp.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EasyCSharp;
[Generator]
public class EventHandlerGenerator : GeneratorBase<MethodAttributeSyntaxReceiver>
{
    readonly static EventAttribute DefaultEventAttribute = new(typeof(EventHandler));
    protected override MethodAttributeSyntaxReceiver ConstructSyntaxReceiver()
        => new(typeof(EventAttribute).FullName);
    protected override void OnExecute(GeneratorExecutionContext context, MethodAttributeSyntaxReceiver SyntaxReceiver)
    {
        var CastFromAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(CastFromAttribute).FullName);
        var EventAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(EventAttribute).FullName);
        var TypeSymbol = context.Compilation.GetTypeByMetadataName(typeof(Type).FullName);
        foreach
            (
                var MethodSymbols in
                SyntaxReceiver.Methods
                .GroupBy<IMethodSymbol, INamedTypeSymbol>
                (f => f.ContainingType, SymbolEqualityComparer.Default)
            )
        {
            //Debugger.Launch();
            var ClassSymbol = MethodSymbols.Key;
            context.AddSource($"{ClassSymbol.Name}.GeneratedEvents.g.cs", $@"
using System.Runtime.CompilerServices;
namespace {ClassSymbol.ContainingNamespace}
{{
    partial class {ClassSymbol.Name}
    {{
        {string.Join("\n\n",
            from method in MethodSymbols
            let attribute = method.GetAttributes()
                .Single(ad => ad.AttributeClass?.Equals(EventAttributeSymbol, SymbolEqualityComparer.Default) ?? false)
            let args = attribute.ConstructorArguments
            where args.Length == 1
            /// Constructor #1
            let arg = args[0]
            where arg.Type?.Equals(TypeSymbol, SymbolEqualityComparer.Default) ?? false
            let delegateInvoke = (arg.Value as INamedTypeSymbol)?.DelegateInvokeMethod
            where delegateInvoke is not null
            /// <see cref="EventAttribute.Name"/>
            let EventName = attribute.NamedArguments.SingleOrDefault(x => x.Key == nameof(EventAttribute.Name)).Value.Value.CastOrDefault(DefaultEventAttribute.Name)
            /// <see cref="EventAttribute.AgressiveInline"/>
            let Inline = attribute.NamedArguments.SingleOrDefault(x => x.Key == nameof(EventAttribute.AgressiveInline)).Value.Value.CastOrDefault(DefaultEventAttribute.AgressiveInline)
            /// <see cref="EventAttribute.Visibility"/>
            let VisibilityPrefix = GetVisibilityPrefix(
                method.DeclaredAccessibility.ToString().ToLower(), 
                attribute.NamedArguments.SingleOrDefault(x => x.Key == nameof(EventAttribute.Visibility)).Value,
                DefaultEventAttribute.Visibility
            )
            // Visibility is not DoNotGenerate
            where VisibilityPrefix is not null

            let paramsWithCast =
            (
                from methodParam in method.Parameters
                let attr = methodParam.GetAttributes().SingleOrDefault(x => x.AttributeClass?.Equals(CastFromAttributeSymbol, SymbolEqualityComparer.Default) ?? false)
                let castFromType = attr != null && attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as INamedTypeSymbol : null
                select (methodParam, castFromType)
            ).ToArray() // Evaluate because we use it multiple times

            let annotatedParams =
            (
                from Param in delegateInvoke.Parameters
                let MatchedParam =
                    paramsWithCast.FirstOrDefault(
                        x =>
                        (x.castFromType ?? x.methodParam.Type).Equals(Param.Type, SymbolEqualityComparer.Default)
                    )
                select (Param, MatchedParam)
            ).ToArray()
            select
$@"/// <summary>
/// <inheritdoc cref=""{method.ToDisplayString()}""/>
/// </summary>"
                + (Inline ?"\n[MethodImpl(MethodImplOptions.AggressiveInlining)]" : "")
                + $"\n{VisibilityPrefix}{(method.IsStatic ? " static" : "")} {method.ReturnType} {EventName ?? method.Name}({string.Join(", ",
                    from x in annotatedParams.Enumerate()
                    select $"{(x.Item.MatchedParam.castFromType ?? x.Item.Param.Type).ToDisplayString()} {(x.Item.MatchedParam.methodParam is null ? $"__{x.Index + 1}" : x.Item.MatchedParam.methodParam.Name)}"
                )}) {{ {method.Name}({
                        string.Join(", ",
                            from x in paramsWithCast
                            select (
                                x.castFromType is null ? "" : $"({
                                    x.methodParam.Type
                                })"
                            ) + x.methodParam.Name
                        )
                    }); }}"
        ).IndentWOF(2)}
    }}
}}
");
        }
    }
    static string? GetVisiblityPrefix(string DefaultPrefix, PropertyVisibility propertyVisibility)
        => propertyVisibility switch
        {
            PropertyVisibility.Default => DefaultPrefix,
            PropertyVisibility.DoNotGenerate => null,
            PropertyVisibility.Public => $"public",
            PropertyVisibility.Private => $"private",
            PropertyVisibility.Protected => $"protected",
            _ => throw new ArgumentOutOfRangeException()
        };
    static string? GetVisibilityPrefix(string DefaultPrefix, TypedConstant Value, PropertyVisibility defaultVisibility)
    {
        try
        {
            PropertyVisibility propertyVisibility = (PropertyVisibility)(byte)Value.Value!;
            return GetVisiblityPrefix(DefaultPrefix, propertyVisibility);
        }
        catch
        {
            return GetVisiblityPrefix(DefaultPrefix, defaultVisibility);
        }
    }
}