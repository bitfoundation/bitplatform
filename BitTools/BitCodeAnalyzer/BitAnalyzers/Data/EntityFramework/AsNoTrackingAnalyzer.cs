using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BitCodeAnalyzer.BitAnalyzers.Data.EntityFramework
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsNoTrackingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(AsNoTrackingAnalyzer);

        public const string Title = "AsNoTracking() must be called first";
        public const string Message = Title;
        public const string Description = Title;
        private const string Category = "Bit";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is InvocationExpressionSyntax))
                return;

            InvocationExpressionSyntax invoke = (InvocationExpressionSyntax)root;
            IMethodSymbol symbol = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(invoke).Symbol;

            // when symbol.ContainingType is either "System.Linq.Queryable" or "System.Linq.Enumerable" or "System.Data.Entity.QueryableExtensions"
            // and its called on instanceof System.Data.Entity.Infrastructure.DbSqlQuery
            // and there is no AsNoTracking called before.
        }
    }
}
