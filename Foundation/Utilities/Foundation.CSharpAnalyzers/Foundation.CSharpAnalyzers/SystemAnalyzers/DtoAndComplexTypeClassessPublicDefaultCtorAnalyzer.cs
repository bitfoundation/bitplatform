using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Foundation.CSharpAnalyzers.SystemAnalyzers
{

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer);

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(FoundationAnalyzersResources.DtoAndComplexTypeClassessPublicDefaultCtorAnalyzerTitle), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Message = new LocalizableResourceString(nameof(FoundationAnalyzersResources.DtoAndComplexTypeClassessPublicDefaultCtorAnalyzerMessage), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(FoundationAnalyzersResources.DtoAndComplexTypeClassessPublicDefaultCtorAnalyzerDescription), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
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

            List<ConstructorDeclarationSyntax> constructors = classDec
                .Members.OfType<ConstructorDeclarationSyntax>()
                .ToList();

            if (constructors.Count == 0)
                return;

            if (!constructors.Any(ctor => ctor.Modifiers.Any(modifier => modifier.ValueText == "public") && ctor.ParameterList.Parameters.Count == 0))
            {
                Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);
                context.ReportDiagnostic(diagn);
            };
        }
    }
}
