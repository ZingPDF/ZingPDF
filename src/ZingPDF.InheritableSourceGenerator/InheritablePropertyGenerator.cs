using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZingPDF.InheritableSourceGenerator;

[Generator]
public class InheritablePropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register a syntax provider that selects all property declarations with [Inheritable]
        var propertiesWithAttributes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is PropertyDeclarationSyntax,
                transform: static (ctx, _) => GetInheritableProperty(ctx))
            .Where(static m => m is not null)!;

        var allInheritableProperties = propertiesWithAttributes.Collect();

        context.RegisterSourceOutput(allInheritableProperties, (spc, items) =>
        {
            GenerateInheritableKeys(spc, items!);
        });
    }

    private static (string className, string propertyName, string ns)? GetInheritableProperty(GeneratorSyntaxContext context)
    {
        var propertySyntax = (PropertyDeclarationSyntax)context.Node;

        // Check for [Inheritable] attribute
        if (propertySyntax.AttributeLists.Count == 0)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(propertySyntax) is not IPropertySymbol symbol)
            return null;

        if (!symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "InheritableAttribute"))
            return null;

        var className = symbol.ContainingType.Name;
        var propertyName = symbol.Name;
        var ns = symbol.ContainingNamespace.ToDisplayString();

        return (className, propertyName, ns);
    }

    private static void GenerateInheritableKeys(SourceProductionContext context, IEnumerable<(string className, string propertyName, string ns)?> properties)
    {
        var grouped = properties
            .GroupBy(p => (p.Value.ns, p.Value.className))
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("namespace GeneratedInheritableKeys");
        sb.AppendLine("{");
        sb.AppendLine("    public static class InheritableKeys");
        sb.AppendLine("    {");
        sb.AppendLine("        public static readonly Dictionary<Type, HashSet<string>> Map = new Dictionary<Type, HashSet<string>>");
        sb.AppendLine("        {");

        foreach (var group in grouped)
        {
            var fullType = $"{group.Key.ns}.{group.Key.className}";
            sb.AppendLine($"            [typeof({fullType})] = new HashSet<string> {{ {string.Join(", ", group.Select(p => $"\"{p.Value.propertyName}\""))} }},");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("InheritableKeys.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}