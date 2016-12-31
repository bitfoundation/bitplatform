using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Foundation.CSharpAnalyzers.SystemAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DateTimeOffsetInsteadOfDateTimeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(DateTimeOffsetInsteadOfDateTimeAnalyzer);

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(FoundationAnalyzersResources.DateTimeOffsetInsteadOfDateTimeAnalyzerTitle), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Message = new LocalizableResourceString(nameof(FoundationAnalyzersResources.DateTimeOffsetInsteadOfDateTimeAnalyzerMessage), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(FoundationAnalyzersResources.DateTimeOffsetInsteadOfDateTimeAnalyzerDescription), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private const string Category = "Foundation";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.IdentifierName);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is IdentifierNameSyntax))
                return;

            root = (IdentifierNameSyntax)context.Node;

            ISymbol dateSymbol = context.SemanticModel.GetSymbolInfo(root).Symbol;

            if (dateSymbol == null)
                return;

            if (dateSymbol.ToString() != "System.DateTime")
                return;

            Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);

            context.ReportDiagnostic(diagn);
        }
    }
}
