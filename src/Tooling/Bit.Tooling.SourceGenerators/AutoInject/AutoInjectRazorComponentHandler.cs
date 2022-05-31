using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Bit.Tooling.SourceGenerators;

public class AutoInjectRazorComponentHandler : AutoInjectBaseHandler
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
        
        if (IsContainingSymbolEqualToContainingNamespace(classSymbol) is false)
        {
            return null;
        }

        string classNamespace = classSymbol.ContainingNamespace.ToDisplayString();

        IReadOnlyCollection<ISymbol> baseEligibleMembers = GetBaseClassEligibleMembers(classSymbol, attributeSymbol);
        IReadOnlyCollection<ISymbol> sortedMembers = eligibleMembers.OrderBy(o => o.Name).ToList();

        string source = $@"
using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace {classNamespace}
{{
    [global::System.CodeDom.Compiler.GeneratedCode(""MiladSourceGenerator"",""1.0"")]
    [global::System.Diagnostics.DebuggerNonUserCode]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class {classSymbol.Name}
    {{
        {GenerateInjectableProperties(sortedMembers, baseEligibleMembers)}
    }}
}}";
        return source;
    }

    private static string GenerateInjectableProperties(
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
                stringBuilder.Append(GenerateProperty(fieldSymbol.Type, fieldSymbol.Name));

            if (member is IPropertySymbol propertySymbol)
                stringBuilder.Append(GenerateProperty(propertySymbol.Type, propertySymbol.Name));
        }

        stringBuilder.Length--;

        return stringBuilder.ToString();
    }

    private static string GenerateProperty(ITypeSymbol @type, string name)
    {
        return $@"
{"\t\t"}[Inject]
{"\t\t"}[EditorBrowsable(EditorBrowsableState.Never)]
{"\t\t"}private {@type} ____{FormatMemberName(name)} {{ get => {name}; set => {name} = value; }};";
    }
}
