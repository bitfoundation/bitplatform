using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.CodeAnalyzers.SystemAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodAsyncVoidAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = nameof(MethodAsyncVoidAnalyzer);

    public const string Title = "Change return type of async method to Task";
    public const string Message = "Return type of async method must be Task or contain try/catch";
    public const string Description = Message;
    private const string Category = "System";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodSyntax, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethodSyntax(SyntaxNodeAnalysisContext context)
    {
        SyntaxNode root = context.Node;

        if ((root is MethodDeclarationSyntax) is false)
            return;

        MethodDeclarationSyntax methodDec = (MethodDeclarationSyntax)context.Node;

        if (methodDec.Modifiers.Any(SyntaxKind.AsyncKeyword) is false)
            return;

        var returnType = methodDec.ReturnType as PredefinedTypeSyntax;

        if (returnType is null)
            return;

        if (returnType.Keyword.IsKind(SyntaxKind.VoidKeyword) is false)
            return;

        if (methodDec.Body is null)
            return;

        if (methodDec.Body.Statements.Any(SyntaxKind.TryStatement))
            return;

        Diagnostic diagnostic = Diagnostic.Create(Rule, root.GetLocation(), Message);

        context.ReportDiagnostic(diagnostic);
    }
}
