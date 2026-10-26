using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
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

    private static readonly (string ReceiverTypeMetadataName, ImmutableHashSet<string> MethodNames)[] NonAsyncMethods =
    [
        ("System.Linq.IQueryable", ImmutableHashSet.Create(nameof(Enumerable.ToList), nameof(Enumerable.ToArray), nameof(Enumerable.ToDictionary),
            nameof(Enumerable.Any), nameof(Enumerable.All), nameof(Enumerable.Max), nameof(Enumerable.Min), nameof(Enumerable.Average), nameof(Enumerable.Sum), nameof(Enumerable.Contains), nameof(Enumerable.LongCount), nameof(Enumerable.Count),
            nameof(Enumerable.FirstOrDefault), nameof(Enumerable.First), nameof(Enumerable.Single), nameof(Enumerable.SingleOrDefault), nameof(Enumerable.Last), nameof(Enumerable.LastOrDefault), nameof(Enumerable.ElementAt), nameof(Enumerable.ElementAtOrDefault),
            "Add", "AddRange", "Find", "ExecuteDelete", "ExecuteUpdate", "Load")),
        ("Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade", ImmutableHashSet.Create("EnsureDeleted", "BeginTransaction", "CanConnect", "CommitTransaction", "EnsureCreated", "RollbackTransaction")),
        ("Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry", ImmutableHashSet.Create("GetDatabaseValues", "Reload")),
        ("Microsoft.EntityFrameworkCore.ChangeTracking.NavigationEntry", ImmutableHashSet.Create("Load")),
        ("Microsoft.EntityFrameworkCore.IDbContextFactory`1", ImmutableHashSet.Create("CreateDbContext")),
        ("Microsoft.EntityFrameworkCore.DbContext", ImmutableHashSet.Create("SaveChanges", "Find", "Add", "AddRange"))
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;

        if (compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext") is null)
            return;

        var expressionType = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression");

        var nonAsyncMethods = new List<(INamedTypeSymbol ReceiverType, ImmutableHashSet<string> MethodNames)>();

        foreach (var (receiverTypeMetadataName, methodNames) in NonAsyncMethods)
        {
            var receiverType = compilation.GetTypeByMetadataName(receiverTypeMetadataName);

            if (receiverType is not null)
            {
                nonAsyncMethods.Add((receiverType, methodNames));
            }
        }

        context.RegisterOperationAction(operationContext => AnalyzeInvocation(operationContext, nonAsyncMethods, expressionType), OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, List<(INamedTypeSymbol ReceiverType, ImmutableHashSet<string> MethodNames)> nonAsyncMethods, INamedTypeSymbol? expressionType)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var methodName = invocation.TargetMethod.Name;

        if (GetReceiverType(invocation) is not { } receiverType)
            return;

        if (nonAsyncMethods.Any(nonAsyncMethod => nonAsyncMethod.MethodNames.Contains(methodName) && IsOrInheritsFrom(receiverType, nonAsyncMethod.ReceiverType)) is false)
            return;

        if (expressionType is not null && IsInsideExpressionTree(invocation, expressionType))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetMethodNameLocation(invocation.Syntax)));
    }

    private static ITypeSymbol? GetReceiverType(IInvocationOperation invocation)
    {
        var receiver = invocation.TargetMethod.IsExtensionMethod
            ? invocation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0)?.Value
            : invocation.Instance;

        while (receiver is IConversionOperation { IsImplicit: true } conversion)
        {
            receiver = conversion.Operand;
        }

        return receiver?.Type;
    }

    private static bool IsOrInheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        if (type is ITypeParameterSymbol typeParameter)
            return typeParameter.ConstraintTypes.Any(constraintType => IsOrInheritsFrom(constraintType, baseType));

        if (baseType.TypeKind is TypeKind.Interface)
            return SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, baseType)
                || type.AllInterfaces.Any(@interface => SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, baseType));

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType))
                return true;
        }

        return false;
    }

    private static bool IsInsideExpressionTree(IOperation operation, INamedTypeSymbol expressionType)
    {
        for (var current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is IAnonymousFunctionOperation && current.Parent?.Type is { } convertedType && IsOrInheritsFrom(convertedType, expressionType))
                return true;
        }

        return false;
    }

    private static Location GetMethodNameLocation(SyntaxNode syntax)
    {
        var expression = (syntax as InvocationExpressionSyntax)?.Expression;

        return expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name.GetLocation(),
            SimpleNameSyntax simpleName => simpleName.GetLocation(),
            _ => syntax.GetLocation()
        };
    }
}
