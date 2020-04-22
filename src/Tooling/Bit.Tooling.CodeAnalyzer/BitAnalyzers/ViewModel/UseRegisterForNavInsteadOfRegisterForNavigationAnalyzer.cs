using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Bit.Tooling.CodeAnalyzer.BitAnalyzers.ViewModel
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseRegisterForNavInsteadOfRegisterForNavigationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(UseRegisterForNavInsteadOfRegisterForNavigationAnalyzer);

        public const string Title = "Use RegisterForNav instead of RegisterForNavigation";
        public const string Message = Title;
        public const string Description = Title;
        private const string Category = "Bit";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is InvocationExpressionSyntax))
                return;

            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)root;

            IMethodSymbol symbol = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(invocation).Symbol;

            if (symbol == null || symbol.Name != "RegisterForNavigation")
                return;

            string symbolName = symbol.ContainingType.ToDisplayString();

            if (symbolName.StartsWith("Prism.Ioc.IContainerRegistryExtensions", StringComparison.InvariantCultureIgnoreCase))
            {
                Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);

                context.ReportDiagnostic(diagn);
            }
        }
    }
}
