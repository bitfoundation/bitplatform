using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace Foundation.CSharpAnalyzers.SystemAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PropertyAccessorsInternalModifierAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(PropertyAccessorsInternalModifierAnalyzer);

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(FoundationAnalyzersResources.PropertyAccessorsInternalModifierAnalyzerTitle), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Message = new LocalizableResourceString(nameof(FoundationAnalyzersResources.PropertyAccessorsInternalModifierAnalyzerMessage), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(FoundationAnalyzersResources.PropertyAccessorsInternalModifierAnalyzerDescription), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private const string Category = "Foundation";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.PropertyDeclaration);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is PropertyDeclarationSyntax))
                return;

            PropertyDeclarationSyntax propDec = (PropertyDeclarationSyntax)context.Node;

            if (propDec.AccessorList.Accessors.Any(acc => acc.Modifiers.Any(m => m.ValueText == "internal")))
            {
                Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);
                context.ReportDiagnostic(diagn);
            }
        }
    }
}
