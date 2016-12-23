using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Foundation.CSharpAnalyzers.Foundation.Server.Api.ApiControllers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CamelCaseParameterNameAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(CamelCaseParameterNameAnalyzer);

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(FoundationAnalyzersResources.CamelCaseParameterNameAnalyzerTitle), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Message = new LocalizableResourceString(nameof(FoundationAnalyzersResources.CamelCaseParameterNameAnalyzerMessage), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(FoundationAnalyzersResources.CamelCaseParameterNameAnalyzerDescription), FoundationAnalyzersResources.ResourceManager, typeof(FoundationAnalyzersResources));
        private const string Category = "Foundation";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.Attribute);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is AttributeSyntax))
                return;

            AttributeSyntax attributeSyntax = (AttributeSyntax)context.Node;

            ISymbol attributeUsage = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;

            if (attributeUsage?.ContainingType?.ToString() == "Foundation.Api.ApiControllers.ParameterAttribute")
            {
                LiteralExpressionSyntax expression = (LiteralExpressionSyntax)(attributeSyntax.DescendantNodes().OfType<AttributeArgumentSyntax>().First()).Expression;

                string parameterNameValue = expression.Token.ValueText;

                if (!char.IsLower(parameterNameValue[0]))
                {
                    Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);

                    context.ReportDiagnostic(diagn);
                }
            }
        }
    }
}
