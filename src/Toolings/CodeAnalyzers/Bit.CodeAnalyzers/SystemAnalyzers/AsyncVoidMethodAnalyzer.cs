using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
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
        context.RegisterSyntaxNodeAction(AnalyzeMethodSyntax, SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement);
    }

    private void AnalyzeMethodSyntax(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is MethodDeclarationSyntax method)
        {
            AnalyzeMethod(context, method.Modifiers, method.ReturnType, method.Body, method.Identifier);
        }
        else if (context.Node is LocalFunctionStatementSyntax localFunction)
        {
            AnalyzeMethod(context, localFunction.Modifiers, localFunction.ReturnType, localFunction.Body, localFunction.Identifier);
        }
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, SyntaxTokenList modifiers, TypeSyntax returnType, BlockSyntax? body, SyntaxToken identifier)
    {
        if (modifiers.Any(SyntaxKind.AsyncKeyword) is false)
            return;

        if ((returnType as PredefinedTypeSyntax)?.Keyword.IsKind(SyntaxKind.VoidKeyword) is not true)
            return;

        var statements = body?.Statements.Where(statement => statement is not LocalFunctionStatementSyntax).ToArray() ?? [];

        if (statements.FirstOrDefault() is not TryStatementSyntax { Catches.Count: > 0 })
        {
            context.ReportDiagnostic(Diagnostic.Create(DefaultRule, identifier.GetLocation()));
            return;
        }

        if (statements.Length > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(CorrenctUsageOfTryStatementRule, identifier.GetLocation()));
        }
    }
}
