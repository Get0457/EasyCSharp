﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using EasyCSharp.GeneratorTools;
namespace CopySourceGenerator;

[Generator]
public class CopySourceCode : GeneratorBase<ClassAttributeSyntaxReceiver>
{
    protected override ClassAttributeSyntaxReceiver ConstructSyntaxReceiver()
        => new(typeof(CopySourceAttribute).FullName);
    protected override void OnInitialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(a => a.AddSource("CopySourceGenerator.g.cs", """
            using System;

            namespace CopySourceGenerator
            {
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public class CopySourceAttribute : Attribute
                {
                    public CopySourceAttribute(string MemberName, Type Type)
                    {

                    }
                }
            }
            """));
    }
    protected override void OnExecute(GeneratorExecutionContext context, ClassAttributeSyntaxReceiver SyntaxReceiver)
    {
        try
        {
            var CopySourceFromAtributeType = context.Compilation.GetTypeByMetadataName(typeof(CopySourceAttribute).FullName);
            
            foreach (var ClassSymbol in SyntaxReceiver.Classes)
            {
                if (!ClassSymbol.IsType) return;
                var attributes = (
                    from attr in ClassSymbol.GetAttributes()
                    where attr.AttributeClass?.Equals(CopySourceFromAtributeType, SymbolEqualityComparer.Default) ?? false
                    where attr.ConstructorArguments.Length == 2
                    let parsed = (
                        MemberName: attr.ConstructorArguments[0].Value?.ToString(),
                        Type: attr.ConstructorArguments[1].Value as INamedTypeSymbol
                    )
                    where parsed.MemberName is not null && parsed.Type is not null
                    select parsed
                ).ToArray();
                if (attributes.Length > 0)
                {
                    context.AddSource($"{ClassSymbol}.CopySourceGenerated.g.cs", $$"""
                        namespace {{ClassSymbol.ContainingNamespace}} {
                            partial {{ClassSymbol.TypeKind.ToString().ToLower()}} {{ClassSymbol.Name}} {
                                {{string.Join("\n\n",
                                        from attribute in attributes
                                        select $"const string {attribute.MemberName} = \"\"\"\n" +
                                        $"{attribute.Type.DeclaringSyntaxReferences[0].SyntaxTree}\n\"\"\";".Indent(3)
                                    )}}
                            }
                        }
                        """);
                }
            }
        }
        catch (Exception e)
        {
            context.AddSource("Exception.cs", $"/* {e.GetType()} {e.Message} {e.StackTrace} */");
        }
    }
}
static partial class Extension
{
    public static string Indent(this string Original, int IndentTimes = 1, int IndentSpace = 4)
    {
        var Indent = new string(' ', IndentSpace * IndentTimes);
        var slashNindent = $"\n{Indent}";
        return Indent + Original.Replace("\n", slashNindent);
    }
}