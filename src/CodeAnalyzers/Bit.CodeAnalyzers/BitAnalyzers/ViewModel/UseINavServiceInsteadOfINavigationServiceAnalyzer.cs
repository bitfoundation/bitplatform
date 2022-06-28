using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Bit.Tooling.CodeAnalyzer.BitAnalyzers.ViewModel
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseINavServiceInsteadOfINavigationServiceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(UseINavServiceInsteadOfINavigationServiceAnalyzer);

        public const string Title = "Use INavService instead of INavigationService";
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
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.IdentifierName);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is IdentifierNameSyntax))
                return;

            root = (IdentifierNameSyntax)context.Node;

            ISymbol navSymbol = context.SemanticModel.GetSymbolInfo(root).Symbol;

            if (navSymbol == null)
                return;

            if (navSymbol.ToString() != "Prism.Navigation.INavigationService")
                return;

            Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);

            context.ReportDiagnostic(diagn);
        }
    }
}
