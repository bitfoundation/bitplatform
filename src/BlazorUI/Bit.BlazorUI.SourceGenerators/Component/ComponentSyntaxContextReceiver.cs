using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.BlazorUI.SourceGenerators.Component;

public class ComponentSyntaxContextReceiver : ISyntaxContextReceiver
{
    public IList<BlazorParameter> Parameters { get; } = [];

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not PropertyDeclarationSyntax propertyDeclarationSyntax || !propertyDeclarationSyntax.AttributeLists.Any()) return;

        var parent = propertyDeclarationSyntax.Parent;

        if (parent is null || parent.IsKind(SyntaxKind.ClassDeclaration) is false) return;

        var classDeclarationSyntax = (ClassDeclarationSyntax?)parent;

        if (classDeclarationSyntax?.Modifiers.Any(k => k.IsKind(SyntaxKind.PartialKeyword)) is false) return;

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);

        if (propertySymbol is null) return;

        var type = propertySymbol.ContainingType;

        if (type == null) return;

        if (type.GetMembers().Any(m => m.Name == "SetParametersAsync")) return;

        var attributes = propertySymbol.GetAttributes();

        if (attributes.Any(ad => ad.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.ParameterAttribute" ||
                                 ad.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.CascadingParameterAttribute"))
        {
            var resetClassBuilder = attributes.Any(a => a.AttributeClass?.ToDisplayString() == "Bit.BlazorUI.ResetClassBuilderAttribute");
            var resetStyleBuilder = attributes.Any(a => a.AttributeClass?.ToDisplayString() == "Bit.BlazorUI.ResetStyleBuilderAttribute");
            var isTwoWayBound = attributes.Any(a => a.AttributeClass?.ToDisplayString() == "Bit.BlazorUI.TwoWayBoundAttribute");

            var parameter = new BlazorParameter(propertySymbol, resetClassBuilder, resetStyleBuilder, isTwoWayBound);

            var callOnSetAttribute = attributes.SingleOrDefault(a => a.AttributeClass?.ToDisplayString() == "Bit.BlazorUI.CallOnSetAttribute");
            var name = callOnSetAttribute?.ConstructorArguments.FirstOrDefault().Value as string;

            parameter.CallOnSetMethodName = name;

            Parameters.Add(parameter);
        }
    }
}
