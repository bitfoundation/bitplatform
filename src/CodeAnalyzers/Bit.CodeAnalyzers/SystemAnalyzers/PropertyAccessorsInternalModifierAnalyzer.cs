using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bit.CodeAnalyzers.SystemAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PropertyAccessorsInternalModifierAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = nameof(PropertyAccessorsInternalModifierAnalyzer);

    public const string Title = "Remove internal from modifiers of your property's accessors";
    public const string Message = "Property's accessors may not be internal";
    public const string Description = "Analyzer which deny you from using internal on your properties accessors";
    private const string Category = "System";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.PropertyDeclaration);
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        SyntaxNode root = context.Node;

        if (!(root is PropertyDeclarationSyntax))
            return;

        PropertyDeclarationSyntax propDec = (PropertyDeclarationSyntax)context.Node;

        if (propDec.AccessorList?.Accessors.Any(acc => acc.Modifiers.Any(m => m.ValueText == "internal")) == true)
        {
            Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);
            context.ReportDiagnostic(diagn);
        }
    }
}
