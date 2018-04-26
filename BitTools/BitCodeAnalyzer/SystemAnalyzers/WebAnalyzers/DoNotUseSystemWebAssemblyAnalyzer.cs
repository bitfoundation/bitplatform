using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BitCodeAnalyzer.SystemAnalyzers.WebAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseSystemWebAssemblyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(DoNotUseSystemWebAssemblyAnalyzer);

        public const string Title = "Don not use System.Web assembly types in your codes";
        public const string Message = "Most System.Web Types won't work in ASP.NET Core.";
        public const string Description = Message;
        private const string Category = "System";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.IdentifierName);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is IdentifierNameSyntax))
                return;

            root = (IdentifierNameSyntax)context.Node;

            ISymbol usageSymbol = context.SemanticModel.GetSymbolInfo(root).Symbol;

            if (usageSymbol?.Kind == SymbolKind.NamedType && usageSymbol.ContainingAssembly?.Name == "System.Web")
            {
                Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);

                context.ReportDiagnostic(diagn);
            }
        }
    }
}
