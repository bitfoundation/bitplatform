﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.SourceGenerators;

public class BlazorParameterPropertySyntaxReceiver : ISyntaxContextReceiver
{
    public IList<IPropertySymbol> Properties { get; } = [];

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax
                && propertyDeclarationSyntax.AttributeLists.Any())
        {
            try
            {
                var classDeclarationSyntax = propertyDeclarationSyntax.Parent as ClassDeclarationSyntax;

                if (classDeclarationSyntax?.Modifiers.Any(k => k.IsKind(SyntaxKind.PartialKeyword)) is false)
                    return;

                var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);

                if (propertySymbol is null) return;

                var type = propertySymbol.ContainingType;

                if (type == null) return;

                if (type.GetMembers().Any(m => m.Name == "SetParametersAsync")) return;


                if (propertySymbol.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.ParameterAttribute"
                                                          || ad.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.CascadingParameterAttribute"))
                {
                    Properties.Add(propertySymbol);
                }
            }
            catch (Exception exp)
            {
                throw new InvalidOperationException($"Error processing property {propertyDeclarationSyntax.Identifier.Text} in {propertyDeclarationSyntax.SyntaxTree.FilePath}", exp);
            }
        }
    }
}
