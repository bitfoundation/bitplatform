using System.Collections.Immutable;
using Bit.CodeAnalyzers.SystemAnalyzers;
using Bit.CodeAnalyzers.SystemCodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bit.CodeAnalyzers.Tests;

[TestClass]
public class AnalyzerConfigurationTests
{
    private static DiagnosticAnalyzer[] Analyzers =>
    [
        .. typeof(AsyncVoidMethodAnalyzer).Assembly.GetTypes()
            .Where(type => type.IsAbstract is false && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
    ];

    [TestMethod]
    public void KeepsTheDiagnosticsConsumersSuppressById()
    {
        var descriptors = Analyzers
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => $"{descriptor.Id} | {descriptor.Title} | {descriptor.MessageFormat} | {descriptor.Category} | {descriptor.DefaultSeverity} | {descriptor.IsEnabledByDefault}");

        Assert.AreEqual("""
            AsyncVoidMethodAnalyzer1 | Avoid using 'async void' | Either change the return type to 'Task' or use try/catch to handle the exceptions. | System | Error | True
            AsyncVoidMethodAnalyzer2 | Avoid using 'async void' | The try/catch block must contain whole body to capable of handling error | System | Error | True
            ClassWithoutModifierAnalyzer | Add modifier to your class. | Your class must have at least one modifier. (Public, internal, etc) | System | Error | True
            NonAsyncEFCoreMethodsUsageAnalyzer | Use EF Core async methods rather than sync methods. | Use EF Core async methods rather than sync methods. | System | Warning | True
            PropertyAccessorsInternalModifierAnalyzer | Remove internal from modifiers of your property's accessors | Property's accessors may not be internal | System | Error | True
            """.ReplaceLineEndings(), string.Join(Environment.NewLine, descriptors));
    }

    [TestMethod]
    public void EveryAnalyzerRunsConcurrentlyAndReportsInGeneratedCode()
    {
        foreach (var analyzer in Analyzers)
        {
            var context = new RecordingAnalysisContext();

            analyzer.Initialize(context);

            Assert.IsTrue(context.IsConcurrentExecutionEnabled, analyzer.GetType().Name);
            Assert.AreEqual(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics, context.GeneratedCodeAnalysisFlags, analyzer.GetType().Name);
        }
    }

    [TestMethod]
    public void CodeFixProviderFixesTheClassWithoutModifierDiagnostic()
    {
        var codeFixProvider = new ClassWithoutModifierCodeFixProvider();

        Assert.AreEqual(ClassWithoutModifierAnalyzer.DiagnosticId, codeFixProvider.FixableDiagnosticIds.Single());
        Assert.IsNotNull(codeFixProvider.GetFixAllProvider());
    }

    private sealed class RecordingAnalysisContext : AnalysisContext
    {
        public bool IsConcurrentExecutionEnabled { get; private set; }

        public GeneratedCodeAnalysisFlags? GeneratedCodeAnalysisFlags { get; private set; }

        public override void EnableConcurrentExecution()
        {
            IsConcurrentExecutionEnabled = true;
        }

        public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags analysisMode)
        {
            GeneratedCodeAnalysisFlags = analysisMode;
        }

        public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
        {
        }

        public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action)
        {
        }

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
        }

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
        }

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
        }

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
        }

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
        }

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
        }

        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
        }
    }
}
