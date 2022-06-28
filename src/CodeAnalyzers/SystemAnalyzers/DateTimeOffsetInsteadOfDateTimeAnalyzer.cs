using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Bit.Tooling.CodeAnalyzer.SystemAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DateTimeOffsetInsteadOfDateTimeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(DateTimeOffsetInsteadOfDateTimeAnalyzer);

        public const string Title = "Replace DateTime usage with DateTimeOffset";
        public const string Message = Title;
        public const string Description = Title;
        private const string Category = "System";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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
