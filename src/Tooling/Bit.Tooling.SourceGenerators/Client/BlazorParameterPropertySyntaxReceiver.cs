using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.Tooling.SourceGenerators
{
    public class BlazorParameterPropertySyntaxReceiver : ISyntaxContextReceiver
    {
        public List<IPropertySymbol> Properties { get; } = new List<IPropertySymbol>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax
                    && propertyDeclarationSyntax.AttributeLists.Any())
            {

                ClassDeclarationSyntax classDeclarationSyntax = (ClassDeclarationSyntax)propertyDeclarationSyntax.Parent;

                if (!classDeclarationSyntax.Modifiers.Any(k => k.IsKind(SyntaxKind.PartialKeyword)))
                    return;

                IPropertySymbol propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);

                INamedTypeSymbol @class = propertySymbol.ContainingType;

                if (@class.GetMembers().Any(m => m.Name == "SetParametersAsync"))
                    return;

                if (propertySymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "Microsoft.AspNetCore.Components.ParameterAttribute" || ad.AttributeClass.ToDisplayString() == "Microsoft.AspNetCore.Components.CascadingParameterAttribute"))
                {
                    Properties.Add(propertySymbol);
                }
            }
        }
    }
}
