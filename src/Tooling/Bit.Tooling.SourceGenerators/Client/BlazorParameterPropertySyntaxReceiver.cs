using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.Tooling.SourceGenerators;

public class BlazorParameterPropertySyntaxReceiver : ISyntaxContextReceiver
{
    public ICollection<IPropertySymbol> Properties { get; } = new List<IPropertySymbol>();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not PropertyDeclarationSyntax propertyDeclarationSyntax || !propertyDeclarationSyntax.AttributeLists.Any()) return;

        var classDeclarationSyntax = (ClassDeclarationSyntax?)propertyDeclarationSyntax.Parent;

        if (classDeclarationSyntax is null || classDeclarationSyntax.Modifiers.Any(k => k.IsKind(SyntaxKind.PartialKeyword)) is false) return;

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);

        if (propertySymbol is null) return;

        var type = propertySymbol.ContainingType;

        if (type.GetMembers().Any(m => m.Name == "SetParametersAsync")) return;

        if (propertySymbol.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.ParameterAttribute" || ad.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.CascadingParameterAttribute"))
        {
            Properties.Add(propertySymbol);
        }
    }
}
