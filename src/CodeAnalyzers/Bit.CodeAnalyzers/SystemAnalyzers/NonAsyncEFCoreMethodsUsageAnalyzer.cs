using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Bit.CodeAnalyzers.SystemAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NonAsyncEFCoreMethodsUsageAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = nameof(NonAsyncEFCoreMethodsUsageAnalyzer);

    public const string Title = "Use EF Core async methods rather than sync methods.";
    public const string Message = Title;
    public const string Description = Title;
    private const string Category = "System";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        SyntaxNode root = context.Node;

        if (root is not InvocationExpressionSyntax)
            return;

        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)root;
        IMethodSymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

        if (symbol == null)
            return;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression != null)
        {
            INamedTypeSymbol? instanceType = (context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type as INamedTypeSymbol)?.ConstructedFrom;

            if (instanceType is null)
                return;

            bool isUsingNonAsyncMethod = false;

            if (instanceType.ToString().Contains("Microsoft.EntityFrameworkCore.DbSet") || instanceType.ToString().Contains("System.Linq.IQueryable"))
            {
                if (invocation.Expression.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Any(identifier => identifier.Identifier.ValueText is nameof(Enumerable.ToList) or nameof(Enumerable.ToArray) or nameof(Enumerable.ToDictionary)
                            or nameof(Enumerable.Any) or nameof(Enumerable.All) or nameof(Enumerable.Max) or nameof(Enumerable.Min) or nameof(Enumerable.Average) or nameof(Enumerable.Sum) or nameof(Enumerable.Contains) or nameof(Enumerable.LongCount) or nameof(Enumerable.Count)
                            or nameof(Enumerable.FirstOrDefault) or nameof(Enumerable.First) or nameof(Enumerable.Single) or nameof(Enumerable.SingleOrDefault) or nameof(Enumerable.Last) or nameof(Enumerable.LastOrDefault) or nameof(Enumerable.ElementAt) or nameof(Enumerable.ElementAtOrDefault)
                            or "Add" or "AddRange" or "Find" or "ExecuteDelete" or "ExecuteUpdate" or "Load"))
                {
                    isUsingNonAsyncMethod = true;
                }
            }

            if (instanceType.ToString().Contains("Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade"))
            {
                if (invocation.Expression.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Any(identifier => identifier.Identifier.ValueText is "EnsureDeleted" or "BeginTransaction" or "CanConnect" or "CommitTransaction" or "EnsureCreated" or "EnsureDeleted" or "RollbackTransaction"))
                {
                    isUsingNonAsyncMethod = true;
                }
            }

            if (instanceType.ToString().Contains("Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry"))
            {
                if (invocation.Expression.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Any(identifier => identifier.Identifier.ValueText is "GetDatabaseValues" or "Reload"))
                {
                    isUsingNonAsyncMethod = true;
                }
            }

            if (instanceType.ToString().Contains("Microsoft.EntityFrameworkCore.ChangeTracking.ReferenceEntry"))
            {
                if (invocation.Expression.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Any(identifier => identifier.Identifier.ValueText is "Load"))
                {
                    isUsingNonAsyncMethod = true;
                }
            }

            do
            {
                if (instanceType.ToString() is "Microsoft.EntityFrameworkCore.DbContext")
                {
                    if (invocation.Expression.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Any(identifier => identifier.Identifier.ValueText is "SaveChanges" or "Find" or "Add" or "AddRange"))
                    {
                        isUsingNonAsyncMethod = true;
                    }
                }

                instanceType = instanceType.BaseType;
            } while (instanceType != null);

            if (isUsingNonAsyncMethod)
            {
                Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);
                context.ReportDiagnostic(diagn);
            }
        }
    }
}

