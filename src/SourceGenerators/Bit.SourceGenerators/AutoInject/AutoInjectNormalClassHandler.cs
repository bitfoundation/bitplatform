﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Bit.SourceGenerators;

public static class AutoInjectNormalClassHandler
{
    public static string? Generate(INamedTypeSymbol? attributeSymbol, INamedTypeSymbol? classSymbol, IReadOnlyCollection<ISymbol> eligibleMembers)
    {
        if (classSymbol is null)
        {
            return null;
        }

        if (AutoInjectHelper.IsContainingSymbolEqualToContainingNamespace(classSymbol) is false)
        {
            return null;
        }

        string classNamespace = classSymbol.ContainingNamespace.ToDisplayString();

        IReadOnlyCollection<ISymbol> baseEligibleMembers = AutoInjectHelper.GetBaseClassEligibleMembers(classSymbol, attributeSymbol);
        IReadOnlyCollection<ISymbol> sortedMembers = eligibleMembers.OrderBy(o => o.Name).ToList();

        string source = $@"
namespace {classNamespace}
{{
    public partial class {AutoInjectHelper.GenerateClassName(classSymbol)}
    {{
        {GenerateConstructor(classSymbol, sortedMembers, baseEligibleMembers)}
    }}
}}";
        return source;
    }

    private static string GenerateConstructor(INamedTypeSymbol classSymbol, IReadOnlyCollection<ISymbol> eligibleMembers, IReadOnlyCollection<ISymbol> baseEligibleMembers)
    {
        string generateConstructor = $@"
        [global::System.CodeDom.Compiler.GeneratedCode(""Bit.SourceGenerators"",""{BitSourceGeneratorUtil.GetPackageVersion()}"")]
        [global::System.Diagnostics.DebuggerNonUserCode]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
{"\t\t"}public {classSymbol.Name}({GenerateConstructorParameters(eligibleMembers, baseEligibleMembers)}){PassParametersToBaseClass(baseEligibleMembers)}
{"\t\t"}{{
{AssignedInjectedParametersToMembers(eligibleMembers)}
{"\t\t"}}}
";
        return generateConstructor;
    }

    private static string PassParametersToBaseClass(IReadOnlyCollection<ISymbol> baseEligibleMembers)
    {
        if (baseEligibleMembers.Any() is false)
            return string.Empty;

        StringBuilder baseConstructor = new();

        baseConstructor.Append(": base(");

        foreach (ISymbol symbol in baseEligibleMembers)
        {
            baseConstructor.Append($@"{'\n'}{"\t\t\t\t\t\t"}autoInjected{AutoInjectHelper.FormatMemberName(symbol.Name)},");
        }

        baseConstructor.Length--;

        baseConstructor.Append(')');

        return baseConstructor.ToString();
    }

    private static string AssignedInjectedParametersToMembers(IReadOnlyCollection<ISymbol> eligibleMembers)
    {
        StringBuilder stringBuilder = new();
        foreach (ISymbol symbol in eligibleMembers)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append('\n');
            }
            stringBuilder.Append("\t\t\t")
                .Append($@"{symbol.Name} = autoInjected{AutoInjectHelper.FormatMemberName(symbol.Name)};");
        }

        return stringBuilder.ToString();
    }

    private static string GenerateConstructorParameters(IReadOnlyCollection<ISymbol> eligibleMembers, IReadOnlyCollection<ISymbol> baseEligibleMembers)
    {
        StringBuilder stringBuilder = new();
        List<ISymbol> members = new(eligibleMembers.Count + baseEligibleMembers.Count);

        members.AddRange(eligibleMembers);
        members.AddRange(baseEligibleMembers);
        members = members.OrderBy(o => o.Name).ToList();

        foreach (ISymbol member in members)
        {
            if (member is IFieldSymbol fieldSymbol)
            {
                var isNullable = fieldSymbol.NullableAnnotation is NullableAnnotation.Annotated;
                var nullValue = isNullable ? " = null" : string.Empty;

                stringBuilder.Append(
                    $@"{'\n'}{"\t\t\t"}{fieldSymbol.Type} autoInjected{AutoInjectHelper.FormatMemberName(fieldSymbol.Name)} {nullValue},");
            }

            if (member is IPropertySymbol propertySymbol)
            {
                var isNullable = propertySymbol.NullableAnnotation is NullableAnnotation.Annotated;
                var nullValue = isNullable ? " = null" : string.Empty;

                stringBuilder.Append(
                    $@"{'\n'}{"\t\t\t"}{propertySymbol.Type} autoInjected{AutoInjectHelper.FormatMemberName(propertySymbol.Name)} {nullValue},");
            }
        }

        stringBuilder.Length--;

        return stringBuilder.ToString();
    }
}
