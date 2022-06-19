using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.Tooling.SourceGenerators;

public class AutoInjectSyntaxReceiver : ISyntaxContextReceiver
{
    private static readonly string AutoInjectAttributeName = typeof(AutoInjectAttribute).FullName;

    public Collection<ISymbol> EligibleMembers { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        MarkEligibleFields(context);
        MarkEligibleProperties(context);
    }

    private void MarkEligibleFields(GeneratorSyntaxContext context)
    {
        if ((context.Node is FieldDeclarationSyntax fieldDeclarationSyntax) is false ||
            fieldDeclarationSyntax.AttributeLists.Any() is false) return;

        var classDeclarationSyntax = (ClassDeclarationSyntax?)fieldDeclarationSyntax.Parent;

        if (classDeclarationSyntax is null)
            return;

        if (!classDeclarationSyntax.Modifiers.Any(k => k.IsKind(SyntaxKind.PartialKeyword)))
            return;

        foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
        {
            var fieldSymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, variable) as IFieldSymbol;
            if (fieldSymbol != null &&
                fieldSymbol.GetAttributes()
                    .Any(ad => ad.AttributeClass != null &&
                               ad.AttributeClass.ToDisplayString() == AutoInjectAttributeName))
            {
                EligibleMembers.Add(fieldSymbol);
            }
        }
    }

    private void MarkEligibleProperties(GeneratorSyntaxContext context)
    {
        if (!(context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax) ||
            propertyDeclarationSyntax.AttributeLists.Count <= 0) return;

        var classDeclarationSyntax = (ClassDeclarationSyntax?)propertyDeclarationSyntax.Parent;

        if (classDeclarationSyntax is null) return;

        if (classDeclarationSyntax.Modifiers.Any(k => k.IsKind(SyntaxKind.PartialKeyword)) is false) return;

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);

        if (propertySymbol is null) return;

        if (propertySymbol.GetAttributes().Any(ad => ad.AttributeClass != null && ad.AttributeClass.ToDisplayString() == AutoInjectAttributeName))
        {
            EligibleMembers.Add(propertySymbol);
        }
    }
}
