using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Bit.Tooling.CodeAnalyzer.BitAnalyzers.Data.EntityFramework
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsNoTrackingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(AsNoTrackingAnalyzer);

        public const string Title = "AsNoTracking() must be called first";
        public const string Message = Title;
        public const string Description = Title;
        private const string Category = "Bit";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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

            if (symbol == null)
                return;

            string symbolName = symbol.ContainingType.ToDisplayString();
            if (symbolName == "System.Linq.Queryable" || symbolName == "System.Linq.Enumerable" ||
                symbolName == "System.Data.Entity.QueryableExtensions")
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression != null)
                {
                    INamedTypeSymbol? instanceType = (context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type as INamedTypeSymbol)?.ConstructedFrom;

                    if (instanceType != null &&
                        (instanceType.ToString() == "System.Data.Entity.Infrastructure.DbQuery<TEntity>" ||
                         instanceType.BaseType?.ToString() == "System.Data.Entity.Infrastructure.DbQuery<TEntity>"))
                    {
                        if (!invocation.Expression.DescendantNodes()
                            .OfType<InvocationExpressionSyntax>()
                            .Any(innerInvocation => innerInvocation.DescendantNodes()
                                .OfType<IdentifierNameSyntax>()
                                .Any(identifier => identifier.Identifier.ValueText == "AsNoTracking")))
                        {
                            Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);

                            context.ReportDiagnostic(diagn);
                        }
                    }
                }
            }

            // when symbol.ContainingType is either "System.Linq.Queryable" or "System.Linq.Enumerable" or "System.Data.Entity.QueryableExtensions"
            // and its called on instanceof System.Data.Entity.Infrastructure.DbQuery
            // and there is no AsNoTracking called before.
        }
    }
}
