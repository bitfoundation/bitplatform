using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Bit.SourceGenerators;

public static class AutoInjectNormalClassHandler
{
    public static string? Generate(
        INamedTypeSymbol? attributeSymbol,
        INamedTypeSymbol? classSymbol,
        IReadOnlyCollection<ISymbol> eligibleMembers)
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
    [global::System.CodeDom.Compiler.GeneratedCode(""Bit.SourceGenerators"",""{AutoInjectHelper.GetPackageVersion()}"")]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class {classSymbol.Name}
    {{
        {GenerateConstructor(classSymbol, sortedMembers, baseEligibleMembers)}
    }}
}}";
        return source;
    }

    private static string GenerateConstructor(
        INamedTypeSymbol classSymbol,
        IReadOnlyCollection<ISymbol> eligibleMembers,
        IReadOnlyCollection<ISymbol> baseEligibleMembers)
    {
        string generateConstructor = $@"
{"\t\t"}public {classSymbol.Name}({GenerateConstructorParameters(eligibleMembers, baseEligibleMembers)}){PassParametersToBaseClass(baseEligibleMembers)}
{"\t\t"}{{
    {"\t\t"}{AssignedInjectedParametersToMembers(eligibleMembers)}
{"\t\t"}}}
";
        return generateConstructor;
    }

    private static string PassParametersToBaseClass(IReadOnlyCollection<ISymbol> baseEligibleMembers)
    {
        if (baseEligibleMembers.Any() is false)
            return "";

        StringBuilder baseConstructor = new StringBuilder();

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
        StringBuilder stringBuilder = new StringBuilder();
        foreach (ISymbol symbol in eligibleMembers)
        {
            stringBuilder.Append($@"{symbol.Name} = autoInjected{AutoInjectHelper.FormatMemberName(symbol.Name)};");
        }

        return stringBuilder.ToString();
    }

    private static string GenerateConstructorParameters(
        IReadOnlyCollection<ISymbol> eligibleMembers,
        IReadOnlyCollection<ISymbol> baseEligibleMembers)
    {
        StringBuilder stringBuilder = new StringBuilder();
        List<ISymbol> members = new List<ISymbol>(eligibleMembers.Count + baseEligibleMembers.Count);

        members.AddRange(eligibleMembers);
        members.AddRange(baseEligibleMembers);
        members = members.OrderBy(o => o.Name).ToList();

        foreach (ISymbol member in members)
        {
            if (member is IFieldSymbol fieldSymbol)
                stringBuilder.Append(
                    $@"{'\n'}{"\t\t\t"}{fieldSymbol.Type} autoInjected{AutoInjectHelper.FormatMemberName(fieldSymbol.Name)},");

            if (member is IPropertySymbol propertySymbol)
                stringBuilder.Append(
                    $@"{'\n'}{"\t\t\t"}{propertySymbol.Type} autoInjected{AutoInjectHelper.FormatMemberName(propertySymbol.Name)},");
        }

        stringBuilder.Length--;

        return stringBuilder.ToString();
    }
}
