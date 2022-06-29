using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bit.CodeAnalyzers.BitAnalyzers.Dto;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DtoAndComplexTypeClassesPublicDefaultCtorAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = nameof(DtoAndComplexTypeClassesPublicDefaultCtorAnalyzer);

    public const string Title = "Complex Type | Dtos must have a default public constructor.";
    public const string Message = Title;
    public const string Description = "Complex Type | Dtos must have a default public constructor. See https://github.com/bitfoundation/bitplatform/issues/17";
    private const string Category = "Bit";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        SyntaxNode root = context.Node;

        if (!(root is ClassDeclarationSyntax))
            return;

        ClassDeclarationSyntax classDec = (ClassDeclarationSyntax)context.Node;

        List<ConstructorDeclarationSyntax> constructors = classDec
            .Members.OfType<ConstructorDeclarationSyntax>()
            .ToList();

        if (constructors.Count == 0)
            return;

        if (!constructors.Any(ctor => ctor.Modifiers.Any(modifier => modifier.ValueText == "public") && ctor.ParameterList.Parameters.Count == 0))
        {
            ITypeSymbol classSymbol = context.SemanticModel.GetDeclaredSymbol(classDec);

            if (classSymbol.IsDto() || classSymbol.IsComplexType())
            {
                Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);
                context.ReportDiagnostic(diagn);
            }
        }
    }
}
