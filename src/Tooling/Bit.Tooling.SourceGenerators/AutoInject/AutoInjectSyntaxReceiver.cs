using System.Linq;
using System.Reflection;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Tooling.SourceGenerators;

public class AutoInjectSyntaxReceiver : ISyntaxContextReceiver
{
    private static readonly string AutoInjectAttributeName = typeof(AutoInjectAttribute).FullName;

    public Collection<ISymbol> EligibleMembers { get; } = new();
    public Collection<INamedTypeSymbol> EligibleClassesWithBaseClassUsedAutoInject { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        MarkEligibleFields(context);
        MarkEligibleProperties(context);
        MarkEligibleClasses(context);
    }

    private void MarkEligibleClasses(GeneratorSyntaxContext context)
    {
        if ((context.Node is ClassDeclarationSyntax classDeclarationSyntax) is false)
            return;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (classSymbol == null)
            return;

        if (classDeclarationSyntax.Modifiers.Any(k => k.IsKind(SyntaxKind.PartialKeyword)) is false)
            return;

        if (classSymbol.BaseType == null)
            return;

        if (classSymbol.BaseType.ToDisplayString() == "System.Object")
            return;

        var isBaseTypeUseAutoInject = classSymbol.BaseType
                        .GetMembers()
                        .Any(m =>
                            (m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property) &&
                            m.GetAttributes()
                                .Any(a => a.AttributeClass != null &&
                                          a.AttributeClass.ToDisplayString() == AutoInjectAttributeName));

        var isCurrentClassUseAutoInject = classSymbol
                        .GetMembers()
                        .Any(m =>
                            (m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property) &&
                            m.GetAttributes()
                                .Any(a => a.AttributeClass != null &&
                                          a.AttributeClass.ToDisplayString() == AutoInjectAttributeName));

        if (isBaseTypeUseAutoInject && (isCurrentClassUseAutoInject is false))
            EligibleClassesWithBaseClassUsedAutoInject.Add(classSymbol);
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
