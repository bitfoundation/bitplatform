using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.SourceGenerators;

public static class BitSourceGeneratorUtil
{
    internal static readonly SymbolDisplayFormat FullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat NamespaceFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    public static string GetPackageVersion()
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        return version.ToString();
    }

    internal static string EscapeIdentifier(string name)
    {
        return SyntaxFacts.GetKeywordKind(name) is SyntaxKind.None ? name : $"@{name}";
    }

    internal static string GetNamespace(INamedTypeSymbol type)
    {
        return type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString(NamespaceFormat);
    }

    internal static bool HasNamedContainingTypes(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (string.IsNullOrEmpty(current.Name))
                return false;
        }

        return true;
    }

    internal static INamedTypeSymbol? FindNonPartialType(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (IsPartial(current) is false)
                return current;
        }

        return null;
    }

    internal static TypeDeclarationSyntax? GetTypeDeclaration(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<TypeDeclarationSyntax>().FirstOrDefault();
    }

    internal static string GetPartialDeclarations(INamedTypeSymbol type, Func<INamedTypeSymbol, string> getName)
    {
        var declarations = new List<string>();

        for (var current = type; current is not null; current = current.ContainingType)
        {
            declarations.Insert(0, $"partial {GetTypeKeyword(current)} {getName(current)}");
        }

        return string.Join("\n", declarations);
    }

    internal static string GenerateTypeSource(string @namespace, string partialDeclarations, string members, string? preamble = null)
    {
        var source = new StringBuilder();
        source.AppendLine("#nullable enable");

        if (string.IsNullOrEmpty(preamble) is false)
        {
            source.AppendLine(preamble);
        }

        var indent = string.Empty;

        if (@namespace.Length > 0)
        {
            source.AppendLine().AppendLine($"namespace {@namespace}").AppendLine("{");
            indent = "    ";
        }

        var declarations = partialDeclarations.Split('\n');

        foreach (var declaration in declarations)
        {
            source.AppendLine($"{indent}{declaration}").AppendLine($"{indent}{{");
            indent += "    ";
        }

        foreach (var line in members.Replace("\r\n", "\n").Trim('\n').Split('\n'))
        {
            source.AppendLine(line.Length == 0 ? line : $"{indent}{line}");
        }

        foreach (var _ in declarations)
        {
            indent = indent.Substring(4);
            source.AppendLine($"{indent}}}");
        }

        if (@namespace.Length > 0)
        {
            source.AppendLine("}");
        }

        return source.ToString();
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is TypeDeclarationSyntax declaration &&
                                                               declaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
    }

    private static string GetTypeKeyword(INamedTypeSymbol type)
    {
        return type switch
        {
            { IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
            { IsRecord: true } => "record",
            { TypeKind: TypeKind.Struct } => "struct",
            { TypeKind: TypeKind.Interface } => "interface",
            _ => "class"
        };
    }
}
