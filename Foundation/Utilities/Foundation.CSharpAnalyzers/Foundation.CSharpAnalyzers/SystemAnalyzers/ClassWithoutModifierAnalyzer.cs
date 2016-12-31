using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Foundation.CSharpAnalyzers.SystemAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassWithoutModifierAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(ClassWithoutModifierAnalyzer);

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(FoundationAnalyzersResources.ClassWithoutModifierAnalyzerTitle), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Message = new LocalizableResourceString(nameof(FoundationAnalyzersResources.ClassWithoutModifierAnalyzerMessage), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(FoundationAnalyzersResources.ClassWithoutModifierAnalyzerDescription), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private const string Category = "Foundation";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is ClassDeclarationSyntax))
                return;

            ClassDeclarationSyntax classDec = (ClassDeclarationSyntax)context.Node;

            if (!classDec.Modifiers.Any())
            {
                Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);
                context.ReportDiagnostic(diagn);
            }
        }
    }
}
