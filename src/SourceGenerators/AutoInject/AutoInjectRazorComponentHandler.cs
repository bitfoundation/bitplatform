using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Bit.Tooling.SourceGenerators;

public static class AutoInjectRazorComponentHandler
{
    public static string? Generate(
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

        IReadOnlyCollection<ISymbol> sortedMembers = eligibleMembers.OrderBy(o => o.Name).ToList();

        string source = $@"
using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace {classNamespace}
{{
    [global::System.CodeDom.Compiler.GeneratedCode(""Bit.Tooling.SourceGenerators"",""{PackageHelper.GetPackageVersion()}"")]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class {classSymbol.Name}
    {{
        {GenerateInjectableProperties(sortedMembers)}
    }}
}}";
        return source;
    }

    private static string GenerateInjectableProperties(IReadOnlyCollection<ISymbol> eligibleMembers)
    {
        StringBuilder stringBuilder = new StringBuilder();

        foreach (ISymbol member in eligibleMembers)
        {
            if (member is IFieldSymbol fieldSymbol)
                stringBuilder.Append(GenerateProperty(fieldSymbol.Type, fieldSymbol.Name));

            if (member is IPropertySymbol propertySymbol)
                stringBuilder.Append(GenerateProperty(propertySymbol.Type, propertySymbol.Name));
        }

        return stringBuilder.ToString();
    }

    private static string GenerateProperty(ITypeSymbol @type, string name)
    {
        return $@"
{"\t\t"}[Inject]
{"\t\t"}[EditorBrowsable(EditorBrowsableState.Never)]
{"\t\t"}private {@type} ____{AutoInjectHelper.FormatMemberName(name)} {{ get => {name}; set => {name} = value; }}";
    }
}
