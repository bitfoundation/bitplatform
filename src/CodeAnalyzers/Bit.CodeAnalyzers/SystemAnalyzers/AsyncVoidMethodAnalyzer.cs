using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.CodeAnalyzers.SystemAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncVoidMethodAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = nameof(AsyncVoidMethodAnalyzer);
    private const string Title = "Avoid using 'async void'";
    private const string Message = "Either change the return type to 'Task' or use try/catch to handle the exceptions.";
    private const string Description = Message;
    private const string Category = "System";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodSyntax, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethodSyntax(SyntaxNodeAnalysisContext context)
    {
        SyntaxNode root = context.Node;

        var methodDec = context.Node as MethodDeclarationSyntax;

        if (methodDec is null)
            return;

        if (methodDec.Modifiers.Any(SyntaxKind.AsyncKeyword) is false)
            return;

        var returnType = methodDec.ReturnType as PredefinedTypeSyntax;

        if (returnType is null)
            return;

        if (returnType.Keyword.IsKind(SyntaxKind.VoidKeyword) is false)
            return;

        if (methodDec.Body?.Statements.FirstOrDefault()?.IsKind(SyntaxKind.TryStatement) is true)
            return;

        Diagnostic diagnostic = Diagnostic.Create(Rule, root.GetLocation(), Message);

        context.ReportDiagnostic(diagnostic);
    }
}
