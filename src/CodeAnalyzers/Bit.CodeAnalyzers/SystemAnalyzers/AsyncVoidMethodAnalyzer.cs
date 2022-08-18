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
    private const string Category = "System";
    private const string Title = "Avoid using 'async void'";

    private const string DefaultRuleDiagnosticId = nameof(AsyncVoidMethodAnalyzer) + "1";
    private const string DefaultMessage = "Either change the return type to 'Task' or use try/catch to handle the exceptions.";
    private const string DefaultDescription = DefaultMessage;
    private static readonly DiagnosticDescriptor DefaultRule = new DiagnosticDescriptor(DefaultRuleDiagnosticId, Title, DefaultMessage, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DefaultDescription);

    private const string CorrenctUsageOfTryStatementRuleDiagnosticId = nameof(AsyncVoidMethodAnalyzer) + "2";
    private const string CorrenctUsageOfTryStatementMessage = "The try/catch block must contain whole body to capable of handling error";
    private const string CorrenctUsageOfTryStatementDescription = CorrenctUsageOfTryStatementMessage;
    private static readonly DiagnosticDescriptor CorrenctUsageOfTryStatementRule = new DiagnosticDescriptor(CorrenctUsageOfTryStatementRuleDiagnosticId, Title, CorrenctUsageOfTryStatementMessage, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: CorrenctUsageOfTryStatementDescription);


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DefaultRule, CorrenctUsageOfTryStatementRule);


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

        if (methodDec.Body?.Statements.FirstOrDefault()?.IsKind(SyntaxKind.TryStatement) is false)
        {
            RepoertError(context, root, DefaultRule);
            return;
        }

        if (IsTryStatementUsedCorrectly(methodDec))
            return;

        RepoertError(context, root, CorrenctUsageOfTryStatementRule);
    }

    private bool IsTryStatementUsedCorrectly(MethodDeclarationSyntax methodDeclaration)
    {
        return
            methodDeclaration.Body?.Statements.FirstOrDefault()?.IsKind(SyntaxKind.TryStatement) is true &&
            methodDeclaration.Body?.Statements.Count == 1;
    }

    private void RepoertError(SyntaxNodeAnalysisContext context, SyntaxNode node, DiagnosticDescriptor rule)
    {
        Diagnostic diagnostic = Diagnostic.Create(rule, node.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
